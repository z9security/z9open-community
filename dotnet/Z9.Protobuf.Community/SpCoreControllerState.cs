using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    internal class SpCoreControllerState : ISpCoreControllerState
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string _callbackHostAddress;
        private readonly string _callbackHostAddress_Secondary;
        private readonly int _callbackHostPort;

        /// <summary>
        /// Automatically set last modified on DbChange
        /// </summary>
        private const bool AutoSetLastModifiedOnDbChange = true;

        private readonly ConcurrentDictionary<long, DbChange> _pendingDbChanges = new ConcurrentDictionary<long, DbChange>();
        private readonly ConcurrentDictionary<long, DevActionReq> _pendingDevActions = new ConcurrentDictionary<long, DevActionReq>();

        private static int _nextStateId;
        private ExecutorThread _executorThread;
        private bool _stopping;
        private Identification _identification;
        private bool _online;
        private long _nextDbChangeRequestId = 1;
        private long _nextDevActionRequestId = 1;
        private int _maxBodyLength = 0x0003ffff;

        public SpCoreControllerState(SpCoreControllerStateConnectionInfo connectionInfo, string callbackHostAddress, string callbackHostAddress_Secondary, int callbackHostPort, X509Certificate2 sslCertificate)
        {
            ConnectionInfo = connectionInfo;
            _callbackHostAddress = callbackHostAddress;
            _callbackHostAddress_Secondary = callbackHostAddress_Secondary;
            _callbackHostPort = callbackHostPort;
            SslCertificate = sslCertificate;

            int stateId = Interlocked.Increment(ref _nextStateId);

            var controller = ConnectionInfo.Controller;

            Outgoing = controller.ExtController.ControllerConfig.DevInitiatesConnectionCase != ControllerConfig.DevInitiatesConnectionOneofCase.DevInitiatesConnection || !controller.ExtController.ControllerConfig.DevInitiatesConnection;
            StrictFirmwareVersionSecurity = false;

            if (Outgoing || controller.MacAddressCase == Dev.MacAddressOneofCase.None)
            {
                LogPrefix =
                    $"[{stateId}] [{controller.Address + (!Outgoing || controller.PortCase != Dev.PortOneofCase.Port || controller.Port < 0 ? string.Empty : $":{controller.Port}")}] ";
                StateName =
                    $"[{stateId}] [{controller.Address + (!Outgoing || controller.PortCase != Dev.PortOneofCase.Port || controller.Port < 0 ? string.Empty : $":{controller.Port}")}] ";
            }
            else
            {
                LogPrefix =  $"[{stateId}] [{controller.MacAddress}] ";
                StateName =  $"[{stateId}] [{controller.MacAddress}] ";
            }


            Logger.Debug(LogPrefix + "Constructed, UseSslEncryption=" + ConnectionInfo.SslSettings.UseSslEncryption);
        }

        public SpCoreControllerStateConnectionInfo ConnectionInfo { get; }

        public string LogPrefix { get; }
        public string StateName { get; }
        public bool Outgoing { get; }
        public bool StrictFirmwareVersionSecurity { get; set; }
        public ISpCoreControllerStateObserver Observer { get; set; }
        public X509Certificate2 SslCertificate { get; }
        public Exception exception;
        public SpCoreMessageInputStream mis;
        public SpCoreMessageOutputStream mos;

        public void Start()
        {
            CheckNotStopped();

            Logger.Debug(LogPrefix + "Start");

            _executorThread = new ExecutorThread(this, ConnectionInfo.SslSettings, _callbackHostAddress, _callbackHostAddress_Secondary, _callbackHostPort, ConnectionInfo.TakeoverStorage);
            _executorThread.Start();
        }

        public void Stop()
        {
            Logger.Debug(LogPrefix + "Stop");
            _stopping = true;
            _executorThread?.Stop();
        }

        public string GetLogPrefix()
        {
            return LogPrefix;
        }


        public void OnConnected(TcpClient client, string host)
        {
            if (_stopping)
            {
                return;
            }
            Logger.Info($"{LogPrefix}OnConnected: {client.Client.RemoteEndPoint}, UseSslEncryption={ConnectionInfo.SslSettings.UseSslEncryption}");

            _executorThread.OnConnected(client, host);
        }

        /// <summary>
        /// Called from OutgoingConnectionThread when there is a connection timeout.  The OutgoingConnectionThread keeps trying to connect.
        /// </summary>
        /// <param name="e"></param>
        public void OnConnectTimeout(Exception e)
        {
            if (_stopping)
                return;
            Logger.Info(LogPrefix + "OnConnectTimeout: " + e.Message);

            if (!_online)
            {
                return;
            }

            _online = false;
            Observer?.OnOffline(this);
        }

        public void OnExecutorStopped()
        {
            Observer?.OnOffline(this);
        }

        /// <summary>
        /// Called when ReaderThread or WriterThread encounter an Exception.
        /// </summary>
        /// <param name="e"></param>
        public void OnReaderThreadException(Exception e)
        {
            if (_stopping)
            {
                return;
            }

            Logger.Debug(LogPrefix + "OnReaderThreadException: " + e.Message);

            _executorThread?.OnReaderThreadException(e);

            if (!_online)
            {
                return;
            }

            _online = false;
            Observer?.OnOffline(this);
        }

        public void OnWriterThreadException(Exception e)
        {
            if (_stopping)
            {
                return;
            }

            Logger.Debug(LogPrefix + "OnWriterThreadException: " + e.Message);

            _executorThread?.OnWriterThreadException(e);

            if (!_online)
            {
                return;
            }

            _online = false;
            Observer?.OnOffline(this);
        }

        public void OnSpCoreMessage(SpCoreMessage message)
        {
            if (_stopping)
            {
                return;
            }

            Logger.Debug(LogPrefix + "Read: " + message.Type);
            bool handled = true;
            switch (message.Type)
            {
                case SpCoreMessage.Types.Type.Ping:
                    OnPing();
                    break;
                case SpCoreMessage.Types.Type.Identification:
                    OnIdentification(message);
                    break;
                case SpCoreMessage.Types.Type.Terminate:
                    OnTerminate(message);
                    break;
                default:
                    handled = false;
                    break;

            }

            if (message.Type == SpCoreMessage.Types.Type.Evt || (message.Type == SpCoreMessage.Types.Type.Multi && message.Evt.Count > 0))
            {
                OnEvt(message);
                handled = true;
            }

            if (message.Type == SpCoreMessage.Types.Type.DevActionResp || (message.Type == SpCoreMessage.Types.Type.Multi && message.DevActionResp != null))
            {
                OnDevActionResp(message);
                handled = true;
            }

            if (message.Type == SpCoreMessage.Types.Type.DbChangeResp || (message.Type == SpCoreMessage.Types.Type.Multi && message.DbChangeResp != null))
            {
                OnDbChangeResp(message);
                handled = true;
            }

            if (!handled)
            {
                Logger.Info(LogPrefix + $"Read: Unhandled message type: {message.Type}");
            }
        }

        private void OnPing()
        {
            if (_stopping)
            {
                return;
            }

            Logger.Debug(LogPrefix + "OnPing");
        }

        private void OnIdentification(SpCoreMessage message)
        {
            if (_stopping)
            {
                return;
            }

            _identification = message.Identification;

            Logger.Info(LogPrefix + "OnIdentification id = " + (_identification.IdCase == Identification.IdOneofCase.Id ? _identification.Id : "")
                 + " softwareVersion=" + (_identification.SoftwareVersionCase == Identification.SoftwareVersionOneofCase.SoftwareVersion ? _identification.SoftwareVersion : "")
                 + " devMod=" + (_identification.SpCoreDevModCase == Identification.SpCoreDevModOneofCase.SpCoreDevMod ? _identification.SpCoreDevMod.ToString() : "")
                 + " serialNumber=" + (_identification.SerialNumberCase == Identification.SerialNumberOneofCase.SerialNumber ? _identification.SerialNumber : "")
                 + " password=" + ((_identification.PasswordCase == Identification.PasswordOneofCase.Password && _identification.Password.Length > 0) ? "********" : ""));

            _maxBodyLength = message.Identification.MaxBodyLength;

            _online = true;
            Observer?.OnOnline(this);
        }

        public bool CheckIdentification()
        {
            if (!Outgoing)
            {
                String receivedPassword = null;
                if (_identification.PasswordCase == Identification.PasswordOneofCase.Password)
                    receivedPassword = _identification.Password;
                String expectPassword = null;
                var controller = ConnectionInfo.Controller;

                if (controller.ExtController.ControllerConfig.PasswordCase == ControllerConfig.PasswordOneofCase.Password)
                    expectPassword = controller.ExtController.ControllerConfig.Password;

                if (expectPassword != null)
                {
                    if (String.IsNullOrEmpty(receivedPassword))
                    {
                        if (!StrictFirmwareVersionSecurity)
                        {
                            Logger.Warn(LogPrefix + "CheckIdentification: " + "Expected password=********, received none, allowing");
                        }
                        else
                        {
                            Logger.Warn(LogPrefix + "CheckIdentification: " + "Expected password=********, received none, allowing, rejecting");
                            throw new Exception("Expected password=********, received none");
                        }
                    }
                    else if (!receivedPassword.Equals(expectPassword))
                    {
                        Logger.Warn(LogPrefix + "CheckIdentification: " + "Expected password=********, received incorrect password");
                        throw new Exception("Expected password=********, received incorrect password");
                    }
                    else
                    {
                        Logger.Info(LogPrefix + "CheckIdentification: " + "Verified password=********");
                    }
                }
                else
                {
                    Logger.Warn(LogPrefix + "CheckIdentification: " + "No password configured");
                }
            }

            return true;
        }


        public Identification GetIdentification()
        {
            CheckNotStopped();

            return _identification;
        }

        private void OnTerminate(SpCoreMessage message)
        {
            if (_stopping)
            {
                return;
            }

            var reason = message.TerminationReasonCase == SpCoreMessage.TerminationReasonOneofCase.TerminationReason ? message.TerminationReason : TerminationReason.None;

            Logger.Info(LogPrefix + $"OnTerminate: terminationReason={reason}");

            throw new SpCoreReceivedTerminateMessageException(reason, $"TERMINATE message received, reason={reason}");
        }

        public bool IsOnline()
        {
            return _online;
        }

        public bool IsStopping()
        {
            return _stopping;
        }

        public void DbChange(DbChange dbChange)
        {
            CheckNotStopped();

            var message = new SpCoreMessage { Type = SpCoreMessage.Types.Type.DbChange };

            if (dbChange.RequestIdCase == Spcore.Proto.DbChange.RequestIdOneofCase.None || dbChange.RequestId == 0)
                dbChange.RequestId = Interlocked.Increment(ref _nextDbChangeRequestId);

            Logger.Debug(LogPrefix + "DbChange requestId=" + dbChange.RequestId);

            SetRequiredDefaults(dbChange);

            // Community edition: No LastModified support

            SetDevPlatforms(dbChange);

            _pendingDbChanges[dbChange.RequestId] = dbChange;
            message.DbChange = dbChange;

            VerifyDataSizeIsValid(dbChange.CalculateSize(), dbChange.RequestId);

            _executorThread.QueueMessage(message);
        }

        private void VerifyDataSizeIsValid(int dataSize, long requestId)
        {
            if (dataSize > _maxBodyLength)
            {
                string errorMessage = $"Message size {dataSize} exceeds maximum specified by identification {_maxBodyLength}";
                Logger.Error($"{LogPrefix}requestId={requestId}: {errorMessage}");
                throw new Exception(errorMessage);
            }
        }

        private void OnDbChangeResp(SpCoreMessage message)
        {
            if (_stopping)
                return;
            Logger.Debug(LogPrefix + "OnDbChangeResp requestId=" + message.DbChangeResp.RequestId);
            if (!_pendingDbChanges.TryRemove(message.DbChangeResp.RequestId, out var dbChange))
            {
                Logger.Warn(LogPrefix + "OnDbChangeResp requestId=" + message.DbChangeResp.RequestId + ": TryRemove returned false");
                return;
            }

            Observer?.OnDbChangeResp(this, dbChange, message.DbChangeResp);
        }

        public void DevActionReq(DevActionReq devActionReq)
        {
            CheckNotStopped();

            if (devActionReq.DevActionTypeCase == Spcore.Proto.DevActionReq.DevActionTypeOneofCase.None)
            {
                throw new ArgumentException("Missing DevActionType");
            }

            if (devActionReq.DevUnidCase == Spcore.Proto.DevActionReq.DevUnidOneofCase.None)
            {
                throw new ArgumentException("Missing DevUnid");
            }

            var message = new SpCoreMessage { Type = SpCoreMessage.Types.Type.DevActionReq };
            if (devActionReq.RequestIdCase == Spcore.Proto.DevActionReq.RequestIdOneofCase.None || devActionReq.RequestId == 0)
                devActionReq.RequestId = Interlocked.Increment(ref _nextDevActionRequestId);

            Logger.Debug(LogPrefix + "DevActionReq requestId=" + devActionReq.RequestId);


            _pendingDevActions[devActionReq.RequestId] = devActionReq;
            message.DevActionReq = devActionReq;

            VerifyDataSizeIsValid(devActionReq.CalculateSize(), devActionReq.RequestId);

            _executorThread.QueueMessage(message);
        }

        void OnDevActionResp(SpCoreMessage m)
        {
            if (_stopping)
                return;

            Logger.Debug(LogPrefix + "OnDevActionResp requestId=" + m.DevActionResp.RequestId);
            DevActionReq devActionReq;
            if (!_pendingDevActions.TryRemove(m.DevActionResp.RequestId, out devActionReq))
            {
                Logger.Warn(LogPrefix + "OnDevActionResp requestId=" + m.DevActionResp.RequestId + ": TryRemove returned false");
                return;
            }

            if (Observer != null)
                Observer.OnDevActionResp(this, devActionReq, m.DevActionResp);
        }

        public void StartEvts()
        {
            CheckNotStopped();

            SpCoreMessage m = new SpCoreMessage();
            m.Type = SpCoreMessage.Types.Type.EvtControl;
            m.EvtControl = new EvtControl();
            m.EvtControl.EvtFlowControl = EvtFlowControl.StartContinuous;

            _executorThread.QueueMessage(m);
        }

        public void ConsumeEvts(HashSet<Int64> unids)
        {
            CheckNotStopped();

            SpCoreMessage m = new SpCoreMessage();
            m.Type = SpCoreMessage.Types.Type.EvtControl;
            m.EvtControl = new EvtControl();
            foreach (Int64 unid in unids)
                m.EvtControl.ConsumeEvt.Add(unid);

            _executorThread.QueueMessage(m);
        }

        void OnEvt(SpCoreMessage m)
        {
            if (_stopping)
                return;

            Logger.Debug(LogPrefix + "OnEvt count=" + m.Evt.Count);

            if (m.Evt.Count == 0)
                return;

            if (Observer != null)
            {
                List<Evt> evts = new List<Evt>();
                foreach (Evt evt in m.Evt)
                    evts.Add(evt);

                Observer.OnEvts(this, evts);
            }
        }

        /// <summary>
        /// To avoid the panel choking on required fields being missing,
        /// for example: Can't parse message of type "z9.spcore.proto.SpCoreMessage" because it is missing required fields: (cannot determine missing fields for lite message)
        /// We set them to the true default (0, false), not the "useful" default (which for example the Java object model does).
        /// We log warnings for the case where these defaults don't match the "useful" default.
        /// </summary>
        /// <param name="c"></param>
        void SetRequiredDefaults(DbChange c)
        {
            // Community edition: limited types
            foreach (Sched o in c.Sched)
            {
                SpCoreProtoUtil.InitRequired(o, true);
            }
            foreach (Hol o in c.Hol)
            {
                SpCoreProtoUtil.InitRequired(o, true);
            }
            foreach (Priv o in c.Priv)
            {
                SpCoreProtoUtil.InitRequired(o, true);
            }
            foreach (Cred o in c.Cred)
            {
                SpCoreProtoUtil.InitRequired(o, true);
            }
            foreach (Dev o in c.Dev)
            {
                SpCoreProtoUtil.InitRequired(o, true);
            }
            foreach (DataLayout o in c.DataLayout)
            {
                if (o.EnabledCase == DataLayout.EnabledOneofCase.None)
                {
                    Logger.Warn("Defaulting DataLayout.Enabled to false");
                    o.Enabled = false;
                }
            }
        }

        // Community edition: No LastModified support

        DevPlatform DevPlatformToSend(DevPlatform value)
        {
            if (_identification != null && (_identification.ProtocolCapabilities == null || _identification.ProtocolCapabilities.MaxEnumDevPlatformCase == ProtocolCapabilities.MaxEnumDevPlatformOneofCase.None || ((int)value) > _identification.ProtocolCapabilities.MaxEnumDevPlatform))
            {
                // Log when changing platform to Z9Security
                if (_identification.ProtocolCapabilities == null)
                {
                    Logger.Warn($"{LogPrefix}DevPlatformToSend: Changing DevPlatform from {value} to Z9Security because ProtocolCapabilities is null");
                }
                else if (_identification.ProtocolCapabilities.MaxEnumDevPlatformCase == ProtocolCapabilities.MaxEnumDevPlatformOneofCase.None)
                {
                    Logger.Warn($"{LogPrefix}DevPlatformToSend: Changing DevPlatform from {value} to Z9Security because MaxEnumDevPlatformCase is None");
                }
                else if (((int)value) > _identification.ProtocolCapabilities.MaxEnumDevPlatform)
                {
                    Logger.Warn($"{LogPrefix}DevPlatformToSend: Changing DevPlatform from {value} (value={(int)value}) to Z9Security because it exceeds MaxEnumDevPlatform limit of {_identification.ProtocolCapabilities.MaxEnumDevPlatform}");
                }
                return DevPlatform.Z9Security;
            }
            else
            {
                Logger.Debug($"{LogPrefix}DevPlatformToSend: Sending (unchanged) DevPlatform {value}");
                return value;
            }
        }

        void SetDevPlatforms(DbChange c)
        {
            // Community edition: simplified without DevModAuto
            foreach (Dev o in c.Dev)
            {
                if (o.DevPlatformCase == Dev.DevPlatformOneofCase.DevPlatform)
                {
                    DevPlatform oldValue = o.DevPlatform;
                    o.DevPlatform = DevPlatformToSend(o.DevPlatform);
                    Logger.Debug($"{LogPrefix}SetDevPlatforms: Dev.Unid={o.Unid} DevPlatform changed from {oldValue} to DevPlatformToSend {o.DevPlatform}");
                }
            }
        }

        private void CheckNotStopped()
        {
            if (_stopping)
            {
                throw new Exception("stopped");
            }
        }

        public void SendIdentification()
        {
            // empty Identification will be filled in by ExecutorThread/WriterThread
            SpCoreMessage m = new SpCoreMessage
            {
                Type = SpCoreMessage.Types.Type.Identification
            };
            _executorThread.QueueMessage(m);
        }
    }
}
