using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    internal interface IConnectionThread
    {
        void Start();
        void Stop();
        void Join();
    }

    internal class OutgoingConnectionThread : IConnectionThread
    {
        private static readonly log4net.ILog Logger =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static readonly int DefaultPort = 9730;
        static readonly int ReceiveTimeout = 20000;

        private readonly SpCoreControllerState _controllerState;

        Thread _thread;
        bool _stopping;

        public OutgoingConnectionThread(SpCoreControllerState controllerState)
        {
            _controllerState = controllerState;
        }

        public void Start()
        {
            _stopping = false;
            _thread = new Thread(Run)
            {
                IsBackground = true
            };
            _thread.Start();
        }

        public void Stop()
        {
            _stopping = true;
        }

        public void Join()
        {
            _thread.Join();
        }

        public void Run()
        {
            DateTime offlineAfter = DateTime.Now.AddMilliseconds(5000);
            bool notifiedConnectTimeout = false;
            while (!_stopping)
            {
                try
                {
                    Connect();
                    break;
                }
                catch (Exception e)
                {
                    if (e is SocketException)
                    {
                        Logger.Info($"Connection failed due to {e.Message} (see debug)");
                        Logger.Debug(e, e);
                    }
                    else
                    {
                        Logger.Error(_controllerState.LogPrefix, e);
                    }

                    if (!notifiedConnectTimeout)
                    {
                        DateTime now = DateTime.Now;
                        if (now.CompareTo(offlineAfter) > 0)
                        {
                            _controllerState.OnConnectTimeout(e);
                            notifiedConnectTimeout = true;
                        }
                    }

                    Thread.Sleep(1000);
                }
            }
        }

        void Connect()
        {
            var controller = _controllerState.ConnectionInfo.Controller;

            string host = controller.Address;
            int port = controller.PortCase == Dev.PortOneofCase.Port
                ? controller.Port
                : DefaultPort;

            var ipAddress = ResolveIPAddress(host);
            var remoteEndPoint = new IPEndPoint(ipAddress, port);

            Logger.Info($"{_controllerState.LogPrefix}ipAddress={ipAddress} remoteEP={remoteEndPoint}");

            // Create a TCP/IP  socket.  
            var client = new TcpClient(ipAddress.AddressFamily) {ReceiveTimeout = ReceiveTimeout};

            client.Connect(remoteEndPoint);

            Logger.Info($"{_controllerState.LogPrefix}TCP client connected to {remoteEndPoint}");

            _controllerState.OnConnected(client, host);
        }

        // ReSharper disable once InconsistentNaming
        private IPAddress ResolveIPAddress(string address)
        {
            var addressList = Dns.GetHostAddresses(address);

            if (addressList.Length <= 0)
            {
                throw new Exception($"{_controllerState.LogPrefix}Unable to resolve host {address}");
            }

            return addressList[0];

        }
    }
}