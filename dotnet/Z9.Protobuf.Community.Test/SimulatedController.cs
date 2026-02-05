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
    /// A simulated SpCore controller for loopback testing.
    /// Connects to a host (panel-initiates-connection), handles identification, and responds to DbChange/DevActionReq.
    /// Tracks all received data for verification.
    /// </summary>
    public class SimulatedController : IDisposable
    {
        private Thread _thread;
        private volatile bool _stopping;
        private TcpClient _client;
        private SpCoreMessageInputStream _mis;
        private SpCoreMessageOutputStream _mos;
        private readonly object _writeLock = new object();

        private string _hostAddress;
        private int _hostPort;

        public bool ClientConnected { get; private set; }
        public bool IdentificationReceived { get; private set; }
        public Exception LastException { get; private set; }

        // Track all received messages
        public List<DbChange> ReceivedDbChanges { get; } = new List<DbChange>();
        public List<DevActionReq> ReceivedDevActions { get; } = new List<DevActionReq>();

        // Track individual data types from DbChange (community-supported types)
        public List<Cred> ReceivedCreds { get; } = new List<Cred>();
        public List<CredTemplate> ReceivedCredTemplates { get; } = new List<CredTemplate>();
        public List<DataLayout> ReceivedDataLayouts { get; } = new List<DataLayout>();
        public List<DataFormat> ReceivedDataFormats { get; } = new List<DataFormat>();
        public List<Dev> ReceivedDevs { get; } = new List<Dev>();
        public List<Priv> ReceivedPrivs { get; } = new List<Priv>();
        public List<HolCal> ReceivedHolCals { get; } = new List<HolCal>();
        public List<HolType> ReceivedHolTypes { get; } = new List<HolType>();
        public List<Sched> ReceivedScheds { get; } = new List<Sched>();

        // Track delete operations
        public List<int> DeletedCredUnids { get; } = new List<int>();
        public List<int> DeletedCredTemplateUnids { get; } = new List<int>();
        public List<int> DeletedDataLayoutUnids { get; } = new List<int>();
        public List<int> DeletedDataFormatUnids { get; } = new List<int>();
        public List<int> DeletedDevUnids { get; } = new List<int>();
        public List<int> DeletedPrivUnids { get; } = new List<int>();
        public List<int> DeletedHolCalUnids { get; } = new List<int>();
        public List<int> DeletedHolTypeUnids { get; } = new List<int>();
        public List<int> DeletedSchedUnids { get; } = new List<int>();

        // Track delete-all flags
        public bool CredDeleteAllReceived { get; private set; }
        public bool CredTemplateDeleteAllReceived { get; private set; }
        public bool DataLayoutDeleteAllReceived { get; private set; }
        public bool DataFormatDeleteAllReceived { get; private set; }
        public bool DevDeleteAllReceived { get; private set; }
        public bool PrivDeleteAllReceived { get; private set; }
        public bool HolCalDeleteAllReceived { get; private set; }
        public bool HolTypeDeleteAllReceived { get; private set; }
        public bool SchedDeleteAllReceived { get; private set; }

        // Track EvtControl messages
        public List<EvtControl> ReceivedEvtControls { get; } = new List<EvtControl>();

        // Configurable error responses
        public string NextDbChangeError { get; set; }
        public string NextDevActionError { get; set; }

        // Event counter for unique IDs
        private long _evtUnidCounter = 1;
        private readonly object _evtControlLock = new object();
        private bool _evtFlowContinuous;
        private int _evtOneBatchRemaining;

        public SimulatedController()
        {
        }

        public void Start(string hostAddress, int hostPort)
        {
            _hostAddress = hostAddress;
            _hostPort = hostPort;

            _thread = new Thread(Run) { IsBackground = true, Name = "SimulatedController" };
            _thread.Start();
        }

        public void Stop()
        {
            _stopping = true;
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
                _client = new TcpClient();
                _client.Connect(_hostAddress, _hostPort);
                ClientConnected = true;

                var stream = _client.GetStream();
                _mis = new SpCoreMessageInputStream(stream);
                _mos = new SpCoreMessageOutputStream(stream);

                // Panel initiates: send identification immediately after connecting
                SendIdentification();

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
                    ProcessDbChange(message.DbChange);
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
                    lock (ReceivedEvtControls)
                    {
                        ReceivedEvtControls.Add(message.EvtControl);
                    }
                    UpdateEvtFlowControl(message.EvtControl);
                    break;

                default:
                    // Ignore other message types
                    break;
            }
        }

        private void ProcessDbChange(DbChange dbChange)
        {
            lock (ReceivedDbChanges)
            {
                ReceivedDbChanges.Add(dbChange);

                // Track individual data types
                foreach (var item in dbChange.Cred)
                    ReceivedCreds.Add(item);
                foreach (var item in dbChange.CredTemplate)
                    ReceivedCredTemplates.Add(item);
                foreach (var item in dbChange.DataLayout)
                    ReceivedDataLayouts.Add(item);
                foreach (var item in dbChange.DataFormat)
                    ReceivedDataFormats.Add(item);
                foreach (var item in dbChange.Dev)
                    ReceivedDevs.Add(item);
                foreach (var item in dbChange.Priv)
                    ReceivedPrivs.Add(item);
                foreach (var item in dbChange.HolCal)
                    ReceivedHolCals.Add(item);
                foreach (var item in dbChange.HolType)
                    ReceivedHolTypes.Add(item);
                foreach (var item in dbChange.Sched)
                    ReceivedScheds.Add(item);

                // Track deletes
                foreach (var unid in dbChange.CredDelete)
                    DeletedCredUnids.Add(unid);
                foreach (var unid in dbChange.CredTemplateDelete)
                    DeletedCredTemplateUnids.Add(unid);
                foreach (var unid in dbChange.DataLayoutDelete)
                    DeletedDataLayoutUnids.Add(unid);
                foreach (var unid in dbChange.DataFormatDelete)
                    DeletedDataFormatUnids.Add(unid);
                foreach (var unid in dbChange.DevDelete)
                    DeletedDevUnids.Add(unid);
                foreach (var unid in dbChange.PrivDelete)
                    DeletedPrivUnids.Add(unid);
                foreach (var unid in dbChange.HolCalDelete)
                    DeletedHolCalUnids.Add(unid);
                foreach (var unid in dbChange.HolTypeDelete)
                    DeletedHolTypeUnids.Add(unid);
                foreach (var unid in dbChange.SchedDelete)
                    DeletedSchedUnids.Add(unid);

                // Track delete-all flags
                if (dbChange.CredDeleteAllCase == DbChange.CredDeleteAllOneofCase.CredDeleteAll && dbChange.CredDeleteAll)
                    CredDeleteAllReceived = true;
                if (dbChange.CredTemplateDeleteAllCase == DbChange.CredTemplateDeleteAllOneofCase.CredTemplateDeleteAll && dbChange.CredTemplateDeleteAll)
                    CredTemplateDeleteAllReceived = true;
                if (dbChange.DataLayoutDeleteAllCase == DbChange.DataLayoutDeleteAllOneofCase.DataLayoutDeleteAll && dbChange.DataLayoutDeleteAll)
                    DataLayoutDeleteAllReceived = true;
                if (dbChange.DataFormatDeleteAllCase == DbChange.DataFormatDeleteAllOneofCase.DataFormatDeleteAll && dbChange.DataFormatDeleteAll)
                    DataFormatDeleteAllReceived = true;
                if (dbChange.DevDeleteAllCase == DbChange.DevDeleteAllOneofCase.DevDeleteAll && dbChange.DevDeleteAll)
                    DevDeleteAllReceived = true;
                if (dbChange.PrivDeleteAllCase == DbChange.PrivDeleteAllOneofCase.PrivDeleteAll && dbChange.PrivDeleteAll)
                    PrivDeleteAllReceived = true;
                if (dbChange.HolCalDeleteAllCase == DbChange.HolCalDeleteAllOneofCase.HolCalDeleteAll && dbChange.HolCalDeleteAll)
                    HolCalDeleteAllReceived = true;
                if (dbChange.HolTypeDeleteAllCase == DbChange.HolTypeDeleteAllOneofCase.HolTypeDeleteAll && dbChange.HolTypeDeleteAll)
                    HolTypeDeleteAllReceived = true;
                if (dbChange.SchedDeleteAllCase == DbChange.SchedDeleteAllOneofCase.SchedDeleteAll && dbChange.SchedDeleteAll)
                    SchedDeleteAllReceived = true;
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
                        SupportsIdentificationPassword = false,
                        SupportsIdentificationPasswordUpstream = false
                    }
                }
            };

            WriteMessage(message);
        }

        private void SendDbChangeResp(long requestId)
        {
            var resp = new DbChangeResp { RequestId = requestId };

            // If an error is configured, set it
            if (!string.IsNullOrEmpty(NextDbChangeError))
            {
                resp.Exception = NextDbChangeError;
                NextDbChangeError = null;
            }

            var message = new SpCoreMessage
            {
                Type = SpCoreMessage.Types.Type.DbChangeResp,
                DbChangeResp = resp
            };

            WriteMessage(message);
        }

        private void SendDevActionResp(long requestId)
        {
            var resp = new DevActionResp { RequestId = requestId };

            // If an error is configured, set it
            if (!string.IsNullOrEmpty(NextDevActionError))
            {
                resp.Exception = NextDevActionError;
                NextDevActionError = null;
            }

            var message = new SpCoreMessage
            {
                Type = SpCoreMessage.Types.Type.DevActionResp,
                DevActionResp = resp
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

        /// <summary>
        /// Sends an event from the controller to the host.
        /// </summary>
        public void SendEvent(Evt evt)
        {
            if (!TryBeginEvtBatch())
                return;

            // Assign a unique ID if not set
            if (evt.UnidCase == Evt.UnidOneofCase.None)
            {
                evt.Unid = Interlocked.Increment(ref _evtUnidCounter);
            }

            var message = new SpCoreMessage
            {
                Type = SpCoreMessage.Types.Type.Evt
            };
            message.Evt.Add(evt);

            WriteMessage(message);
        }

        /// <summary>
        /// Sends multiple events from the controller to the host.
        /// </summary>
        public void SendEvents(IEnumerable<Evt> evts)
        {
            if (!TryBeginEvtBatch())
                return;

            var message = new SpCoreMessage
            {
                Type = SpCoreMessage.Types.Type.Evt
            };

            foreach (var evt in evts)
            {
                // Assign a unique ID if not set
                if (evt.UnidCase == Evt.UnidOneofCase.None)
                {
                    evt.Unid = Interlocked.Increment(ref _evtUnidCounter);
                }
                message.Evt.Add(evt);
            }

            WriteMessage(message);
        }

        private void UpdateEvtFlowControl(EvtControl control)
        {
            if (control == null || control.EvtFlowControlCase != EvtControl.EvtFlowControlOneofCase.EvtFlowControl)
                return;

            lock (_evtControlLock)
            {
                switch (control.EvtFlowControl)
                {
                    case EvtFlowControl.StartContinuous:
                        _evtFlowContinuous = true;
                        break;
                    case EvtFlowControl.StopContinuous:
                        _evtFlowContinuous = false;
                        break;
                    case EvtFlowControl.SendOneBatch:
                        _evtOneBatchRemaining++;
                        break;
                }
            }
        }

        private bool TryBeginEvtBatch()
        {
            lock (_evtControlLock)
            {
                if (_evtFlowContinuous)
                    return true;

                if (_evtOneBatchRemaining > 0)
                {
                    _evtOneBatchRemaining--;
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Creates a simple event with the given code.
        /// </summary>
        public static Evt CreateEvent(EvtCode evtCode, int priority = 5)
        {
            var nowMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return new Evt
            {
                EvtCode = evtCode,
                Priority = priority,
                HwTime = new DateTimeData { Millis = nowMillis },
                DbTime = new DateTimeData { Millis = nowMillis }
            };
        }
    }
}
