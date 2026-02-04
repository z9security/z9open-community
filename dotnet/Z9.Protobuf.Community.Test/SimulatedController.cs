using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Z9.Protobuf;
using Z9.Spcore.Proto;

namespace Z9.Protobuf.Community.Test
{
    /// <summary>
    /// A simulated SpCore controller for loopback testing (Community edition).
    /// Listens on a TCP port, handles identification, and responds to DbChange/DevActionReq.
    /// </summary>
    public class SimulatedController : IDisposable
    {
        private TcpListener _listener;
        private Thread _thread;
        private volatile bool _stopping;
        private TcpClient _client;
        private SpCoreMessageInputStream _mis;
        private SpCoreMessageOutputStream _mos;
        private readonly object _writeLock = new object();

        public int Port { get; private set; }
        public bool ClientConnected { get; private set; }
        public bool IdentificationReceived { get; private set; }
        public List<DbChange> ReceivedDbChanges { get; } = new List<DbChange>();
        public List<DevActionReq> ReceivedDevActions { get; } = new List<DevActionReq>();
        public Exception LastException { get; private set; }

        public SimulatedController()
        {
        }

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;

            _thread = new Thread(Run) { IsBackground = true, Name = "SimulatedController" };
            _thread.Start();
        }

        public void Stop()
        {
            _stopping = true;
            _listener?.Stop();
            _client?.Close();
            _thread?.Join(TimeSpan.FromSeconds(5));
        }

        public void Dispose()
        {
            Stop();
        }

        private void Run()
        {
            try
            {
                _client = _listener.AcceptTcpClient();
                ClientConnected = true;

                var stream = _client.GetStream();
                _mis = new SpCoreMessageInputStream(stream);
                _mos = new SpCoreMessageOutputStream(stream);

                while (!_stopping)
                {
                    SpCoreMessage message;
                    try
                    {
                        message = _mis.Read();
                    }
                    catch (IOException)
                    {
                        if (_stopping) break;
                        throw;
                    }

                    if (message != null)
                    {
                        HandleMessage(message);
                    }
                }
            }
            catch (SocketException) when (_stopping)
            {
                // Expected when stopping
            }
            catch (ObjectDisposedException) when (_stopping)
            {
                // Expected when stopping
            }
            catch (EndOfStreamException)
            {
                // Expected when client disconnects
            }
            catch (IOException ex) when (ex.InnerException is SocketException)
            {
                // Expected when client disconnects
            }
            catch (Exception ex)
            {
                LastException = ex;
            }
        }

        private void HandleMessage(SpCoreMessage message)
        {
            switch (message.Type)
            {
                case SpCoreMessage.Types.Type.Ping:
                    // Absorb pings, no response needed
                    break;

                case SpCoreMessage.Types.Type.Identification:
                    IdentificationReceived = true;
                    SendIdentification();
                    break;

                case SpCoreMessage.Types.Type.DbChange:
                    lock (ReceivedDbChanges)
                    {
                        ReceivedDbChanges.Add(message.DbChange);
                    }
                    SendDbChangeResp(message.DbChange.RequestId);
                    break;

                case SpCoreMessage.Types.Type.DevActionReq:
                    lock (ReceivedDevActions)
                    {
                        ReceivedDevActions.Add(message.DevActionReq);
                    }
                    SendDevActionResp(message.DevActionReq.RequestId);
                    break;

                case SpCoreMessage.Types.Type.EvtControl:
                    // Absorb event control messages
                    break;

                default:
                    // Ignore other message types
                    break;
            }
        }

        private void SendIdentification()
        {
            var message = new SpCoreMessage
            {
                Type = SpCoreMessage.Types.Type.Identification,
                Identification = new Identification
                {
                    Id = "00:11:22:33:44:55",
                    SoftwareVersion = "1.0.0-test",
                    ProtocolVersion = "0.1",
                    MaxBodyLength = SpCoreMessageHeader.MAX_LENGTH,
                    SpCoreDevMod = DevMod.IoControllerZ9Spcore,
                    ProtocolCapabilities = new ProtocolCapabilities
                    {
                        // Community edition: limited ProtocolCapabilities
                        SupportsIdentificationPassword = false,
                        SupportsIdentificationPasswordUpstream = false
                    }
                }
            };

            WriteMessage(message);
        }

        private void SendDbChangeResp(long requestId)
        {
            var message = new SpCoreMessage
            {
                Type = SpCoreMessage.Types.Type.DbChangeResp,
                DbChangeResp = new DbChangeResp
                {
                    RequestId = requestId
                    // No Exception set = success
                }
            };

            WriteMessage(message);
        }

        private void SendDevActionResp(long requestId)
        {
            var message = new SpCoreMessage
            {
                Type = SpCoreMessage.Types.Type.DevActionResp,
                DevActionResp = new DevActionResp
                {
                    RequestId = requestId
                    // No Exception set = success
                }
            };

            WriteMessage(message);
        }

        private void WriteMessage(SpCoreMessage message)
        {
            lock (_writeLock)
            {
                _mos?.Write(message);
            }
        }
    }
}
