using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    internal class ExecutorThread
    {
        private static readonly log4net.ILog Logger =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly SpCoreControllerState _controllerState;
        private readonly ISslSettings _sslSettings;
        private readonly string _callbackHostAddress;
        private readonly string _callbackHostAddress_Secondary;
        private readonly int _callbackHostPort;
        private readonly ISpCoreTakeoverStorage _takeoverStorage;
        private readonly object _lock = new object();
        private readonly AutoResetEvent _connectedEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _disconnectedEvent = new AutoResetEvent(false);
        private readonly int ConnectionInitTimeout = 5000;

        private TcpClient _client;
        private Thread _thread;
        private bool _stopping;
        private IConnectionThread _connectionThread;
        private ReaderThread _readerThread;
        private WriterThread _writerThread;
        readonly ManualResetEvent _stopped = new ManualResetEvent(true);
        private string _host;

        public ExecutorThread(SpCoreControllerState controllerState, ISslSettings sslSettings, string callbackHostAddress, string callbackHostAddress_Secondary, int callbackHostPort, ISpCoreTakeoverStorage takeoverStorage)
        {
            _controllerState = controllerState;
            _sslSettings = sslSettings;
            _callbackHostAddress = callbackHostAddress;
            _callbackHostAddress_Secondary = callbackHostAddress_Secondary;
            _callbackHostPort = callbackHostPort;
            _takeoverStorage = takeoverStorage;
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

            _connectedEvent.Set();
            _disconnectedEvent.Set();
        }

        public void Run()
        {
            try
            {

            bool isReconnectAttempt = false;
            Exception exception = null;
            TcpClient client = null;

            while (!_stopping)
            {
                if (isReconnectAttempt)
                {
                    Thread.Sleep(ConnectionInitTimeout);
                    if (_stopping)
                        break;
                }

                lock (_lock)
                {
                    if (_controllerState.Outgoing)
                    {
                        _connectionThread = new OutgoingConnectionThread(_controllerState);
                    }
                    else
                    {
                        // Community edition: no takeover support
                        _connectionThread = null;
                    }
                }

                _connectionThread?.Start();

                // wait for connection
                while (!_stopping && client == null)
                {
                    _connectedEvent.WaitOne(ConnectionInitTimeout);
                    if (_stopping)
                    {
                        break;
                    }

                    client = _client;

                    if (client == null || !client.Connected)
                    {
                        continue;
                    }

                    Logger.Info($"{_controllerState.LogPrefix}Run: Connected");

                    isReconnectAttempt = true;

                    exception = _controllerState.exception = null;

                    Stream stream = client.GetStream();

                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (_sslSettings.UseSslEncryption)
                    {
                        var sslStream = new SslStream(
                            stream,
                            false,
                            ValidateServerCertificate,
                            null
                        );

                        try
                        {
                            if (_controllerState.Outgoing)
                            {
                                var certificateMappings =
                                    _sslSettings.CertificateMappings; // do only one lookup in the registry
                                sslStream.AuthenticateAsClient(
                                    certificateMappings.ContainsKey(_host) ? certificateMappings[_host] : _host,
                                    null,
                                    _sslSettings.EnabledSslProtocols,
                                    true);
                            }
                            else
                            {
                                sslStream.AuthenticateAsServer(_controllerState.SslCertificate, false, _sslSettings.EnabledSslProtocols, false);
                            }
                        }
                        catch (AuthenticationException authenticationException)
                        {
                            Logger.Error($"{_controllerState.LogPrefix}Exception: {authenticationException.Message}", authenticationException);
                            if (authenticationException.InnerException != null)
                            {
                                Logger.Error(
                                    $"{_controllerState.LogPrefix}Inner exception: {authenticationException.InnerException.Message}", authenticationException.InnerException);
                            }

                            Logger.Info($"{_controllerState.LogPrefix}Authentication failed: closing the connection.");
                            lock (_lock)
                            {
                                if (_controllerState.exception == null)
                                {
                                    _controllerState.exception = authenticationException;
                                    _disconnectedEvent.Set();
                                }
                            }

                            break;
                        }
                        catch (IOException ioException)
                        {
                            Logger.Warn($"{_controllerState.LogPrefix}", ioException);
                            lock (_lock)
                            {
                                if (_controllerState.exception == null)
                                {
                                    _controllerState.exception = ioException;
                                    _disconnectedEvent.Set();
                                }
                            }

                            break;
                        }
                        catch (CryptographicException cryptoException)
                        {
                            Logger.Warn($"{_controllerState.LogPrefix}", cryptoException);
                            lock (_lock)
                            {
                                if (_controllerState.exception == null)
                                {
                                    _controllerState.exception = cryptoException;
                                    _disconnectedEvent.Set();
                                }
                            }

                            break;
                        }

                        _controllerState.mos = new SpCoreMessageOutputStream(sslStream);
                        _controllerState.mis = new SpCoreMessageInputStream(sslStream);
                    }
                    else
#pragma warning disable 162
                        // ReSharper disable HeuristicUnreachableCode
                    {
                        _controllerState.mos = new SpCoreMessageOutputStream(stream);
                        _controllerState.mis = new SpCoreMessageInputStream(stream);
                    }
                    // ReSharper restore HeuristicUnreachableCode
#pragma warning restore 162


                    _writerThread = new WriterThread(_controllerState);
                    _readerThread = new ReaderThread(_controllerState);

                    Logger.Debug($"{_controllerState.LogPrefix}Run: Starting reader and writer threads");

                    _writerThread.Start();
                    _readerThread.Start();
                }

                // wait for disconnection
                while (!_stopping && exception == null)
                {
                    _disconnectedEvent.WaitOne(ConnectionInitTimeout);
                    if (_stopping)
                    {
                        break;
                    }

                    if (_controllerState.exception == null)
                    {
                        continue;
                    }

                    exception = _controllerState.exception;
                    client = null;
                    CloseThreadsAndSocket();
                }
            }

            CloseThreadsAndSocket();

            _stopped.Set();
            _controllerState.OnExecutorStopped();
            Logger.Debug(_controllerState.LogPrefix + "Run: exiting");
            }
            catch (Exception e)
            {
                Logger.Error(_controllerState.LogPrefix + "Run: " + e, e);
                Logger.Error(_controllerState.LogPrefix + "Run: exiting");
                throw e;
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

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (_sslSettings.IgnoreSslErrors)
            {
                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) ==
                    SslPolicyErrors.RemoteCertificateNameMismatch)
                {
                    Logger.Warn(
                        $"{_controllerState.LogPrefix}Certificate name mismatch: {Environment.NewLine}{certificate}");

                    return true;
                }

                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) ==
                    SslPolicyErrors.RemoteCertificateChainErrors)
                {
                    Logger.Warn(
                        $"{_controllerState.LogPrefix}Certificate chain error: {Environment.NewLine}{string.Join(", ", chain.ChainStatus.Select(chainStatus => chainStatus.StatusInformation))}" +
                        $"{Environment.NewLine}{string.Join(", ", chain.ChainElements.Cast<X509ChainElement>().Select(chainElement => chainElement.Certificate))}");

                    return true;
                }
            }

            Logger.Error($"{_controllerState.LogPrefix}Certificate error: {sslPolicyErrors}");

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        public void QueueMessage(SpCoreMessage message)
        {
            _writerThread?.QueueMessage(message);
        }

        private void CloseThreadsAndSocket()
        {
            Logger.Debug(_controllerState.LogPrefix + "CloseThreadsAndSocket");

            _connectionThread?.Stop();
            _readerThread?.Stop();
            _writerThread?.Stop();

            try
            {
                var client = _client;
                if (client != null && client.Connected)
                {
                    Logger.Info(_controllerState.LogPrefix + "Closing socket");
                    client.Close();
                }
            }
            catch (Exception e)
            {
                Logger.Error(_controllerState.LogPrefix, e);
            }

            try
            {
                _connectionThread?.Join();
            }
            catch (Exception e)
            {
                Logger.Error(_controllerState.LogPrefix, e);
            }

            try
            {
                _readerThread?.Join();
            }
            catch (Exception e)
            {
                Logger.Error(_controllerState.LogPrefix, e);
            }

            try
            {
                _writerThread?.Join();
            }
            catch (Exception e)
            {
                Logger.Debug(_controllerState.LogPrefix, e);
            }


            lock (_lock)
            {
                _client = null;
                _controllerState.mos = null;
                _controllerState.mis = null;
            }

            Logger.Debug(_controllerState.LogPrefix + "CloseThreadsAndSocket complete");
        }

        public void OnReaderThreadException(Exception e)
        {
            Logger.Info(_controllerState.LogPrefix + "OnReaderThreadException: " + e.Message);
            lock (_lock)
            {
                if (_controllerState.exception == null)
                {
                    _controllerState.exception = e;
                    _disconnectedEvent.Set();
                }
            }
        }

        public void OnWriterThreadException(Exception e)
        {
            Logger.Info(_controllerState.LogPrefix + "OnWriterThreadException: " + e.Message);
            lock (_lock)
            {
                if (_controllerState.exception == null)
                {
                    _controllerState.exception = e;
                    _disconnectedEvent.Set();
                }
            }
        }

        public void OnConnected(TcpClient client, string host)
        {
            Logger.Info($"{_controllerState.LogPrefix}OnConnected: {client.Client.RemoteEndPoint}");

            lock (_lock)
            {
                _client = client;
                _host = host;
                _connectedEvent.Set();
            }
        }

        public static FileInfo ExportToCrtFile_Temp(X509Certificate2 x509Certificate)
        {
            String crtPath = Path.GetTempFileName();
            return ExportToCrtFile(x509Certificate, crtPath);
        }

        static FileInfo ExportToCrtFile(X509Certificate2 x509Certificate, String crtPath)
        {
            String crtString = ExportToCrtString(x509Certificate);
            Logger.Debug($"ExportToCrt: {crtString}");
            System.IO.File.WriteAllBytes(crtPath, Encoding.ASCII.GetBytes(crtString));
            Logger.Info($"ExportToCrt: wrote CRT to: {crtPath}");
            return new FileInfo(crtPath);
        }

        static byte[] ExportToCrtBytes(X509Certificate2 cert)
        {
            String crtString = ExportToCrtString(cert);
            Logger.Debug($"ExportToCrtBytes: {crtString}");
            return Encoding.ASCII.GetBytes(crtString);
        }

        static String ExportToCrtString(X509Certificate2 cert)
        {
            return ExportToCrtString(cert.RawData);
        }

        const int BASE_64_LINE_BREAK_INTERVAL = 64;

        static String ExportToCrtString(byte[] rawCertData)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("-----BEGIN CERTIFICATE-----");
            string base64 = Convert.ToBase64String(rawCertData, Base64FormattingOptions.None);
            int thisLineLength = 0;
            for (int i = 0; i < base64.Length; ++i)
            {
                if (thisLineLength >= BASE_64_LINE_BREAK_INTERVAL)
                {
                    builder.Append(Environment.NewLine);
                    thisLineLength = 0;
                }

                char c = base64[i];
                builder.Append(c);
                ++thisLineLength;
            }
            builder.Append(Environment.NewLine);
            builder.AppendLine("-----END CERTIFICATE-----");

            return builder.ToString();
        }

    }
}
