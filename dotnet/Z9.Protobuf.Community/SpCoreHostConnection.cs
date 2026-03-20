using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    /// <summary>
    /// Controller-side connection to a Z9 Open host. Handles TCP connection, optional TLS,
    /// identification, ping keepalive, message reading, and automatic reconnection.
    /// </summary>
    public class SpCoreHostConnection
    {
        private static readonly log4net.ILog Logger =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ISpCoreHostConnectionObserver _observer;
        private readonly string _hostAddress;
        private readonly int _hostPort;
        private readonly string _controllerId;
        private readonly string _softwareVersion;
        private readonly ISslSettings _sslSettings;
        private readonly string _softwareVersionProduct;
        private readonly string _password;
        private readonly int _reconnectDelayMs;

        private readonly object _writeLock = new object();
        private readonly ManualResetEvent _stopped = new ManualResetEvent(true);

        private SpCoreMessageOutputStream _mos;
        private volatile bool _running;
        private volatile bool _online;

        private string LogPrefix => $"[{_controllerId}] ";

        public bool IsOnline => _online;

        public SpCoreHostConnection(
            ISpCoreHostConnectionObserver observer,
            string hostAddress, int hostPort,
            string controllerId, string softwareVersion,
            ISslSettings sslSettings = null,
            string softwareVersionProduct = null,
            string password = null,
            int reconnectDelayMs = 5000)
        {
            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
            _hostAddress = hostAddress;
            _hostPort = hostPort;
            _controllerId = controllerId;
            _softwareVersion = softwareVersion;
            _sslSettings = sslSettings;
            _softwareVersionProduct = softwareVersionProduct;
            _password = password;
            _reconnectDelayMs = reconnectDelayMs;
        }

        public void Start()
        {
            if (_running) return;
            _running = true;
            _stopped.Reset();

            var thread = new Thread(ConnectionLoop) { IsBackground = true, Name = "SpCoreHostConnection" };
            thread.Start();
            Logger.Info(LogPrefix + $"Start: connecting to {_hostAddress}:{_hostPort}");
        }

        public void Stop()
        {
            Logger.Info(LogPrefix + "Stop");
            _running = false;
            _stopped.WaitOne(TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Thread-safe method to send a message to the host (e.g. Evt, DevStateRecord).
        /// </summary>
        public void WriteMessage(SpCoreMessage message)
        {
            lock (_writeLock)
            {
                _mos?.Write(message);
            }
        }

        private void ConnectionLoop()
        {
            try
            {
                bool isReconnectAttempt = false;

                while (_running)
                {
                    if (isReconnectAttempt)
                    {
                        Logger.Info(LogPrefix + $"ConnectionLoop: reconnecting in {_reconnectDelayMs}ms");
                        Thread.Sleep(_reconnectDelayMs);
                        if (!_running) break;
                    }

                    isReconnectAttempt = true;

                    try
                    {
                        ConnectAndRun();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(LogPrefix + $"ConnectionLoop: {ex.Message}");
                        SetOffline(ex);
                    }
                }
            }
            finally
            {
                _stopped.Set();
                Logger.Debug(LogPrefix + "ConnectionLoop: exiting");
            }
        }

        private void ConnectAndRun()
        {
            using (var client = new TcpClient())
            {
                client.Connect(_hostAddress, _hostPort);
                Logger.Info(LogPrefix + $"ConnectAndRun: TCP connected to {_hostAddress}:{_hostPort}");

                Stream stream = client.GetStream();

                if (_sslSettings != null && _sslSettings.UseSslEncryption)
                {
                    var sslStream = new SslStream(stream, false, ValidateServerCertificate, null);

                    try
                    {
                        var certificateMappings = _sslSettings.CertificateMappings;
                        string targetHost = certificateMappings.ContainsKey(_hostAddress)
                            ? certificateMappings[_hostAddress]
                            : _hostAddress;

                        sslStream.AuthenticateAsClient(
                            targetHost,
                            null,
                            _sslSettings.EnabledSslProtocols,
                            true);
                        Logger.Info(LogPrefix + "ConnectAndRun: TLS handshake complete");
                    }
                    catch (AuthenticationException ex)
                    {
                        Logger.Error(LogPrefix + $"ConnectAndRun: TLS authentication failed: {ex.Message}", ex);
                        if (ex.InnerException != null)
                            Logger.Error(LogPrefix + $"ConnectAndRun: TLS inner: {ex.InnerException.Message}", ex.InnerException);
                        throw;
                    }

                    stream = sslStream;
                }

                var mis = new SpCoreMessageInputStream(stream);
                lock (_writeLock)
                {
                    _mos = new SpCoreMessageOutputStream(stream);
                }

                SendIdentification();

                using (var pingTimer = new Timer(PingTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)))
                {
                    try
                    {
                        ReadLoop(mis);
                    }
                    finally
                    {
                        lock (_writeLock)
                        {
                            _mos = null;
                        }
                    }
                }
            }
        }

        private void SendIdentification()
        {
            var message = new SpCoreMessage
            {
                Type = SpCoreMessage.Types.Type.Identification,
                Identification = new Identification
                {
                    Id = _controllerId,
                    SoftwareVersion = _softwareVersion,
                    ProtocolVersion = "0.1",
                    MaxBodyLength = SpCoreMessageHeader.MAX_LENGTH,
                    SpCoreDevMod = DevMod.IoControllerCommunity,
                    ProtocolCapabilities = new ProtocolCapabilities()
                }
            };

            if (!string.IsNullOrEmpty(_softwareVersionProduct))
                message.Identification.SoftwareVersionProduct = _softwareVersionProduct;

            if (!string.IsNullOrEmpty(_password))
                message.Identification.Password = _password;

            SetProtocolCapabilities(message.Identification.ProtocolCapabilities, _password);

            Logger.Debug(LogPrefix + "SendIdentification");
            WriteMessage(message);
        }

        private static void SetProtocolCapabilities(ProtocolCapabilities c, string password)
        {
            c.MaxEnumDevMod = Enum.GetNames(typeof(DevMod)).Length - 1;
            c.MaxEnumDevUse = Enum.GetNames(typeof(DevUse)).Length - 1;
            c.MaxEnumDevActionParamsType = Enum.GetNames(typeof(DevActionParamsType)).Length - 1;
            c.MaxEnumDevActionType = Enum.GetNames(typeof(DevActionType)).Length - 1;
            c.MaxEnumEvtCode = Enum.GetNames(typeof(EvtCode)).Length - 1;
            c.MaxEnumEvtSubCode = Enum.GetNames(typeof(EvtSubCode)).Length - 1;
            c.MaxEnumDevPlatform = Enum.GetNames(typeof(DevPlatform)).Length - 1;
            c.MaxEnumTerminationReason = Enum.GetNames(typeof(TerminationReason)).Length - 1;

            if (!string.IsNullOrEmpty(password))
            {
                c.SupportsIdentificationPassword = true;
                c.SupportsIdentificationPasswordUpstream = true;
            }
        }

        private void ReadLoop(SpCoreMessageInputStream mis)
        {
            try
            {
                while (_running)
                {
                    var message = mis.Read();
                    if (message != null)
                    {
                        HandleMessage(message);
                    }
                }
            }
            catch (SpCoreReceivedTerminateMessageException ex)
            {
                Logger.Info(LogPrefix + $"ReadLoop: Terminate received: {ex.Message}");
                SetOffline(ex);
            }
            catch (EndOfStreamException)
            {
                Logger.Info(LogPrefix + "ReadLoop: host disconnected (end of stream)");
                SetOffline(new EndOfStreamException("Host disconnected"));
            }
            catch (IOException ex)
            {
                if (_running)
                {
                    Logger.Warn(LogPrefix + $"ReadLoop: {ex.Message}");
                    SetOffline(ex);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogPrefix + $"ReadLoop: {ex}", ex);
                SetOffline(ex);
            }
        }

        private void HandleMessage(SpCoreMessage message)
        {
            Logger.Debug(LogPrefix + "HandleMessage: " + message.Type);

            switch (message.Type)
            {
                case SpCoreMessage.Types.Type.Ping:
                    break;

                case SpCoreMessage.Types.Type.Identification:
                    OnIdentification(message.Identification);
                    break;

                case SpCoreMessage.Types.Type.Terminate:
                    OnTerminate(message);
                    break;

                case SpCoreMessage.Types.Type.DbChange:
                    OnDbChange(message.DbChange);
                    break;

                case SpCoreMessage.Types.Type.DevActionReq:
                    OnDevActionReq(message.DevActionReq);
                    break;

                case SpCoreMessage.Types.Type.EvtControl:
                    OnEvtControl(message.EvtControl);
                    break;

                case SpCoreMessage.Types.Type.DevStateRecordControl:
                    OnDevStateRecordControl(message.DevStateRecordControl);
                    break;

                default:
                    Logger.Info(LogPrefix + $"HandleMessage: unhandled type: {message.Type}");
                    break;
            }
        }

        private void OnIdentification(Identification identification)
        {
            Logger.Info(LogPrefix + "OnIdentification: id="
                + (identification.IdCase == Identification.IdOneofCase.Id ? identification.Id : "")
                + " softwareVersion=" + (identification.SoftwareVersionCase == Identification.SoftwareVersionOneofCase.SoftwareVersion ? identification.SoftwareVersion : ""));
            _online = true;

            try
            {
                _observer.OnOnline(this, identification);
            }
            catch (Exception ex)
            {
                Logger.Error(LogPrefix + $"OnIdentification: observer error: {ex}", ex);
            }
        }

        private void OnTerminate(SpCoreMessage message)
        {
            var reason = message.TerminationReasonCase == SpCoreMessage.TerminationReasonOneofCase.TerminationReason
                ? message.TerminationReason
                : TerminationReason.None;

            Logger.Info(LogPrefix + $"OnTerminate: reason={reason}");
            throw new SpCoreReceivedTerminateMessageException(reason, $"TERMINATE message received, reason={reason}");
        }

        private void OnDbChange(DbChange dbChange)
        {
            long requestId = dbChange.RequestId;
            string error = null;

            try
            {
                error = _observer.OnDbChange(this, dbChange);
            }
            catch (Exception ex)
            {
                Logger.Error(LogPrefix + $"OnDbChange: observer error: {ex}", ex);
                error = ex.Message;
            }

            var resp = new DbChangeResp { RequestId = requestId };
            if (!string.IsNullOrEmpty(error))
                resp.Exception = error;

            WriteMessage(new SpCoreMessage
            {
                Type = SpCoreMessage.Types.Type.DbChangeResp,
                DbChangeResp = resp
            });
        }

        private void OnDevActionReq(DevActionReq req)
        {
            long requestId = req.RequestId;
            string error = null;

            try
            {
                error = _observer.OnDevActionReq(this, req);
            }
            catch (Exception ex)
            {
                Logger.Error(LogPrefix + $"OnDevActionReq: observer error: {ex}", ex);
                error = ex.Message;
            }

            var resp = new DevActionResp { RequestId = requestId };
            if (!string.IsNullOrEmpty(error))
                resp.Exception = error;

            WriteMessage(new SpCoreMessage
            {
                Type = SpCoreMessage.Types.Type.DevActionResp,
                DevActionResp = resp
            });
        }

        private void OnEvtControl(EvtControl evtControl)
        {
            try
            {
                _observer.OnEvtControl(this, evtControl);
            }
            catch (Exception ex)
            {
                Logger.Error(LogPrefix + $"OnEvtControl: observer error: {ex}", ex);
            }
        }

        private void OnDevStateRecordControl(DevStateRecordControl control)
        {
            try
            {
                _observer.OnDevStateRecordControl(this, control);
            }
            catch (Exception ex)
            {
                Logger.Error(LogPrefix + $"OnDevStateRecordControl: observer error: {ex}", ex);
            }
        }

        private void SetOffline(Exception exception)
        {
            if (!_online) return;
            _online = false;

            try
            {
                _observer.OnOffline(this, exception);
            }
            catch (Exception ex)
            {
                Logger.Error(LogPrefix + $"SetOffline: observer error: {ex}", ex);
            }
        }

        private void PingTick(object state)
        {
            if (!_running) return;
            try
            {
                WriteMessage(new SpCoreMessage { Type = SpCoreMessage.Types.Type.Ping });
            }
            catch (Exception ex)
            {
                Logger.Debug(LogPrefix + $"PingTick: {ex.Message}");
            }
        }

        private bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateNotAvailable)
            {
                return true;
            }

            if (_sslSettings.IgnoreSslErrors)
            {
                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) ==
                    SslPolicyErrors.RemoteCertificateNameMismatch)
                {
                    Logger.Warn(
                        LogPrefix + $"Certificate name mismatch: {Environment.NewLine}{certificate}");

                    return true;
                }

                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) ==
                    SslPolicyErrors.RemoteCertificateChainErrors)
                {
                    Logger.Warn(
                        LogPrefix + $"Certificate chain error: {Environment.NewLine}{string.Join(", ", chain.ChainStatus.Select(chainStatus => chainStatus.StatusInformation))}" +
                        $"{Environment.NewLine}{string.Join(", ", chain.ChainElements.Cast<X509ChainElement>().Select(chainElement => chainElement.Certificate))}");

                    return true;
                }
            }

            Logger.Error(LogPrefix + $"Certificate error: {sslPolicyErrors}");

            return false;
        }
    }
}
