using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Threading;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    internal class WriterThread
    {
        private static readonly log4net.ILog Logger =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly SpCoreControllerState _controllerState;
        private readonly BlockingCollection<SpCoreMessage> _outgoingMessages = new BlockingCollection<SpCoreMessage>();

        Thread _thread;
        bool _stopping;
        readonly ManualResetEvent _stopped = new ManualResetEvent(true);


        public WriterThread(SpCoreControllerState controllerState)
        {
            _controllerState = controllerState;
        }

        public void Start()
        {
            _stopping = false;
            _stopped.Reset();

            _thread = new Thread(Run) {IsBackground = true};
            _thread.Start();
        }

        public void Stop()
        {
            _stopping = true;
            _stopped.WaitOne(TimeSpan.FromSeconds(30));
        }

        public void Join()
        {
            _thread.Join();
        }

        public void Run()
        {
            try
            {
                if (_controllerState.Outgoing)
                    WriteIdentification();
                WritePing();
                DateTime nextPing = DateTime.Now.AddMilliseconds(1000);

                while (!_stopping)
                {
                    DateTime now = DateTime.Now;
                    if (now.CompareTo(nextPing) > 0)
                    {
                        WritePing();
                        nextPing = now.AddMilliseconds(1000);
                    }

                    if (_outgoingMessages.TryTake(out var message, 1000))
                    {
                        if (_stopping)
                            break;
                        Logger.Debug(_controllerState.LogPrefix + "Writing: " + message.Type);
                        if (message.Type == SpCoreMessage.Types.Type.Identification)
                            WriteIdentification(); // empty Identification message used as indicator we should send full Identification.  Used for !Outgoing only.
                        else
                            _controllerState.mos.Write(message);
                    }
                }
            }
            catch (System.IO.IOException e)
            {
                Logger.Warn($"{_controllerState.LogPrefix} Communication interrupted: {e.Message} (see debug)");
                Logger.Debug(e, e);
                if (!_stopping)
                    _controllerState.OnWriterThreadException(e);
            }
            catch (Exception e)
            {
                Logger.Error(_controllerState.LogPrefix, e);
                if (!_stopping)
                    _controllerState.OnWriterThreadException(e);
            }
            finally
            {
                if (_stopping)
                {
                    _stopped.Set();
                }
            }
        }

        public void QueueMessage(SpCoreMessage message)
        {
            if (!_outgoingMessages.TryAdd(message))
            {
                throw new Exception("outgoingMessages queue full");
            }
        }

        // to be called only from this thread, in Run
        void WriteIdentification()
        {
            var message = new SpCoreMessage {Type = SpCoreMessage.Types.Type.Identification};

            message.Identification = new Identification();
            message.Identification.Id = ""; // Don't change this ever, because the controllers won't check the licensing for SDK access.
            // Also, to ensure that the controller enforces the licensing for SDK access, don't set;
            // - bootId
            // - spCoreDevMod
            message.Identification.MaxBodyLength = SpCoreMessageHeader.MAX_LENGTH;
            message.Identification.ProtocolVersion = "0.1";
            message.Identification.ProtocolCapabilities = new ProtocolCapabilities();
            SetProtocolCapabilities(message.Identification.ProtocolCapabilities);

            var controller = _controllerState.ConnectionInfo.Controller;

            if (controller != null &&
                controller.ExtController != null &&
                controller.ExtController.ControllerConfig != null &&
                controller.ExtController.ControllerConfig.PasswordCase == ControllerConfig.PasswordOneofCase.Password && !String.IsNullOrWhiteSpace(controller.ExtController.ControllerConfig.Password))
                message.Identification.Password = controller.ExtController.ControllerConfig.Password;

            Logger.Debug(_controllerState.LogPrefix + "Writing: " + message.Type);
            _controllerState.mos.Write(message);
        }

        private static void SetProtocolCapabilities(ProtocolCapabilities c)
        {
            // Community edition: only set the capabilities that exist in community proto
            c.MaxEnumDevMod = Enum.GetNames(typeof(DevMod)).Length - 1;
            c.MaxEnumDevUse = Enum.GetNames(typeof(DevUse)).Length - 1;
            c.MaxEnumDevActionParamsType = Enum.GetNames(typeof(DevActionParamsType)).Length - 1;
            c.MaxEnumDevActionType = Enum.GetNames(typeof(DevActionType)).Length - 1;
            c.MaxEnumEvtCode = Enum.GetNames(typeof(EvtCode)).Length - 1;
            c.MaxEnumEvtSubCode = Enum.GetNames(typeof(EvtSubCode)).Length - 1;
            c.MaxEnumDevPlatform = Enum.GetNames(typeof(DevPlatform)).Length - 1;
            c.MaxEnumTerminationReason = Enum.GetNames(typeof(TerminationReason)).Length - 1;

            c.SupportsIdentificationPassword = true;
            c.SupportsIdentificationPasswordUpstream = true;
        }

        // to be called only from this thread, in Run
        void WritePing()
        {
            var message = new SpCoreMessage {Type = SpCoreMessage.Types.Type.Ping};

            Logger.Debug(_controllerState.LogPrefix + "Writing: " + message.Type);
            _controllerState.mos.Write(message);
        }
    }
}
