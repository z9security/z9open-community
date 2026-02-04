using System;
using System.Threading;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    class ReaderThread
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly SpCoreControllerState controllerState;

        Thread thread;
        bool stopping;
        readonly ManualResetEvent _stopped = new ManualResetEvent(true);

        public ReaderThread(SpCoreControllerState controllerState)
        {
            this.controllerState = controllerState;
        }

        public void Start()
        {
            stopping = false;
            _stopped.Reset();

            thread = new Thread(Run) {IsBackground = true};
            thread.Start();
        }

        public void Stop()
        {
            stopping = true;
            _stopped.WaitOne(TimeSpan.FromSeconds(30));
        }

        public void Join()
        {
            thread.Join();
        }

        public void Run()
        {
            try
            {
                while (!stopping)
                {
                    SpCoreMessage m = controllerState.mis.Read();
                    controllerState.OnSpCoreMessage(m);
                }
            }
            catch (System.IO.IOException e)
            {
                Logger.Warn($"{controllerState.LogPrefix} Communication interrupted: {e.Message} (see debug)");
                Logger.Debug(e, e);
                if (!stopping)
                    controllerState.OnReaderThreadException(e);
            }
            catch (SpCoreReceivedTerminateMessageException e)
            {
                Logger.Info(controllerState.LogPrefix + $"SpCoreReceivedTerminateMessageException: Reason={e.Reason}");
                if (!stopping)
                    controllerState.OnReaderThreadException(e);

            }
            catch (Exception e)
            {
                Logger.Error(controllerState.LogPrefix + e.Message, e);
                if (!stopping)
                    controllerState.OnReaderThreadException(e);
            }
            finally
            {
                if (stopping)
                {
                    _stopped.Set();
                }
            }
        }

    }
}
