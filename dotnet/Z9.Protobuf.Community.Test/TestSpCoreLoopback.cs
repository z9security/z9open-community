using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Z9.Protobuf;
using Z9.Spcore.Proto;

namespace Z9.Protobuf.Community.Test
{
    /// <summary>
    /// Simple SSL settings that disable SSL for testing.
    /// </summary>
    public class NoSslSettings : ISslSettings
    {
        public bool UseSslEncryption => false;
        public bool IgnoreSslErrors => true;
        public Dictionary<string, string> CertificateMappings => new Dictionary<string, string>();
        public SslProtocols EnabledSslProtocols => SslProtocols.None;
    }

    /// <summary>
    /// Test observer that captures callbacks for verification.
    /// </summary>
    public class LoopbackTestObserver : SpCoreControllerStateObserverBase
    {
        public ManualResetEvent OnlineEvent { get; } = new ManualResetEvent(false);
        public ManualResetEvent OfflineEvent { get; } = new ManualResetEvent(false);
        public ManualResetEvent DbChangeRespEvent { get; } = new ManualResetEvent(false);
        public ManualResetEvent DevActionRespEvent { get; } = new ManualResetEvent(false);

        public List<DbChangeResp> DbChangeResponses { get; } = new List<DbChangeResp>();
        public List<DevActionResp> DevActionResponses { get; } = new List<DevActionResp>();
        public List<Evt> ReceivedEvents { get; } = new List<Evt>();

        private int _dbChangeRespCount = 0;

        public override void OnOnline(ISpCoreControllerState state)
        {
            OnlineEvent.Set();
        }

        public override void OnOffline(ISpCoreControllerState state)
        {
            OfflineEvent.Set();
        }

        public override void OnDbChangeResp(ISpCoreControllerState state, DbChange dbChange, DbChangeResp dbChangeResp)
        {
            lock (DbChangeResponses)
            {
                DbChangeResponses.Add(dbChangeResp);
                _dbChangeRespCount++;
            }
            DbChangeRespEvent.Set();
        }

        public override void OnDevActionResp(ISpCoreControllerState state, DevActionReq devActionReq, DevActionResp devActionResp)
        {
            lock (DevActionResponses)
            {
                DevActionResponses.Add(devActionResp);
            }
            DevActionRespEvent.Set();
        }

        public override void OnEvts(ISpCoreControllerState state, List<Evt> evts)
        {
            lock (ReceivedEvents)
            {
                ReceivedEvents.AddRange(evts);
            }
        }

        public void ResetDbChangeRespEvent()
        {
            DbChangeRespEvent.Reset();
        }

        public int GetDbChangeRespCount()
        {
            lock (DbChangeResponses)
            {
                return _dbChangeRespCount;
            }
        }
    }

    [TestClass]
    public class TestSpCoreLoopback
    {
        private SimulatedController _controller;
        private ISpCoreControllerState _state;
        private LoopbackTestObserver _observer;
        private SpCoreControllerStateMgr _mgr;

        private static int FindFreePort()
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private void SetupConnection()
        {
            int listenPort = FindFreePort();

            _observer = new LoopbackTestObserver();
            var mgrConfig = new SpCoreControllerStateMgrConfig
            {
                CallbackHostAddress = "127.0.0.1",
                ListenPort = listenPort,
                NoTakeover = true
            };
            _mgr = new SpCoreControllerStateMgr(mgrConfig, new NoSslSettings(), null);

            var dev = new Dev
            {
                Address = "127.0.0.1",
                MacAddress = "00:11:22:33:44:55"
            };
            dev.ExtController = new Controller
            {
                ControllerConfig = new ControllerConfig
                {
                    DevInitiatesConnection = true
                }
            };

            var connectionInfo = new SpCoreControllerStateConnectionInfo
            {
                Controller = dev,
                SslSettings = new NoSslSettings()
            };

            _state = _mgr.Add(connectionInfo, _observer);

            // Give the listener a moment to start, then have the panel connect in
            Thread.Sleep(100);
            _controller = new SimulatedController();
            _controller.Start("127.0.0.1", listenPort);

            Assert.IsTrue(_observer.OnlineEvent.WaitOne(10000), "Should come online within 10 seconds");
        }

        private void TeardownConnection()
        {
            _state?.Stop();
            _controller?.Dispose();
            _mgr?.Dispose();

            if (_controller?.LastException != null)
            {
                Assert.Fail($"Controller had exception: {_controller.LastException}");
            }
        }

        private void SendDbChangeAndWait(DbChange dbChange)
        {
            int countBefore = _observer.GetDbChangeRespCount();
            _observer.ResetDbChangeRespEvent();
            _state.DbChange(dbChange);
            Assert.IsTrue(_observer.DbChangeRespEvent.WaitOne(5000), "Should get DbChangeResp within 5 seconds");
        }

        [TestMethod]
        public void TestIdentificationAndDataExchange()
        {
            int listenPort = FindFreePort();

            using (var controller = new SimulatedController())
            {
                var observer = new LoopbackTestObserver();
                var mgrConfig = new SpCoreControllerStateMgrConfig
                {
                    CallbackHostAddress = "127.0.0.1",
                    ListenPort = listenPort,
                    NoTakeover = true
                };
                var mgr = new SpCoreControllerStateMgr(mgrConfig, new NoSslSettings(), null);

                var dev = new Dev
                {
                    Address = "127.0.0.1",
                    MacAddress = "00:11:22:33:44:55"
                };
                dev.ExtController = new Controller
                {
                    ControllerConfig = new ControllerConfig
                    {
                        DevInitiatesConnection = true
                    }
                };

                var connectionInfo = new SpCoreControllerStateConnectionInfo
                {
                    Controller = dev,
                    SslSettings = new NoSslSettings()
                };

                var state = mgr.Add(connectionInfo, observer);

                Thread.Sleep(100);
                controller.Start("127.0.0.1", listenPort);

                try
                {
                    // Wait for online
                    Assert.IsTrue(observer.OnlineEvent.WaitOne(10000), "Should come online within 10 seconds");
                    Assert.IsTrue(state.IsOnline(), "State should report online");

                    // In panel-initiates flow, host sends identification after matching, so wait briefly
                    SpinWait.SpinUntil(() => controller.IdentificationReceived, 5000);
                    Assert.IsTrue(controller.IdentificationReceived, "Controller should have received identification");

                    // Act - Send DbChange
                    var dbChange = new DbChange();
                    dbChange.Sched.Add(new Sched { Unid = 1, Name = "Test Schedule" });
                    state.DbChange(dbChange);

                    Assert.IsTrue(observer.DbChangeRespEvent.WaitOne(5000), "Should get DbChangeResp within 5 seconds");
                    Assert.AreEqual(1, observer.DbChangeResponses.Count, "Should have one DbChangeResp");
                    Assert.AreEqual(DbChangeResp.ExceptionOneofCase.None, observer.DbChangeResponses[0].ExceptionCase, "DbChangeResp should indicate success (no exception)");

                    // Act - Send DevActionReq
                    var devAction = new DevActionReq
                    {
                        DevUnid = 1,
                        DevActionType = DevActionType.DoorMomentaryUnlock
                    };
                    state.DevActionReq(devAction);

                    Assert.IsTrue(observer.DevActionRespEvent.WaitOne(5000), "Should get DevActionResp within 5 seconds");
                    Assert.AreEqual(1, observer.DevActionResponses.Count, "Should have one DevActionResp");
                    Assert.AreEqual(DevActionResp.ExceptionOneofCase.None, observer.DevActionResponses[0].ExceptionCase, "DevActionResp should indicate success (no exception)");

                    // Verify controller received the messages
                    Assert.AreEqual(1, controller.ReceivedDbChanges.Count, "Controller should have received one DbChange");
                    Assert.AreEqual(1, controller.ReceivedDevActions.Count, "Controller should have received one DevActionReq");
                }
                finally
                {
                    state.Stop();
                    mgr.Dispose();
                }

                // Check for any exceptions in the controller
                if (controller.LastException != null)
                {
                    Assert.Fail($"Controller had exception: {controller.LastException}");
                }
            }
        }

        [TestMethod]
        public void TestConnectionAndDisconnect()
        {
            int listenPort = FindFreePort();

            using (var controller = new SimulatedController())
            {
                var observer = new LoopbackTestObserver();
                var mgrConfig = new SpCoreControllerStateMgrConfig
                {
                    CallbackHostAddress = "127.0.0.1",
                    ListenPort = listenPort,
                    NoTakeover = true
                };
                var mgr = new SpCoreControllerStateMgr(mgrConfig, new NoSslSettings(), null);

                var dev = new Dev
                {
                    Address = "127.0.0.1",
                    MacAddress = "00:11:22:33:44:55"
                };
                dev.ExtController = new Controller
                {
                    ControllerConfig = new ControllerConfig
                    {
                        DevInitiatesConnection = true
                    }
                };

                var connectionInfo = new SpCoreControllerStateConnectionInfo
                {
                    Controller = dev,
                    SslSettings = new NoSslSettings()
                };

                var state = mgr.Add(connectionInfo, observer);

                Thread.Sleep(100);
                controller.Start("127.0.0.1", listenPort);

                try
                {
                    Assert.IsTrue(observer.OnlineEvent.WaitOne(10000), "Should come online");
                    Assert.IsTrue(controller.ClientConnected, "Controller should show client connected");
                }
                finally
                {
                    state.Stop();
                    mgr.Dispose();
                }

                // After stopping, should eventually go offline
                // Note: The offline event may or may not fire depending on timing
            }
        }

        [TestMethod]
        public void TestCredInsertAndDelete()
        {
            try
            {
                SetupConnection();

                // Insert credentials
                var dbChange = new DbChange();
                dbChange.Cred.Add(new Cred { Unid = 100, Name = "Employee 1", Enabled = true });
                dbChange.Cred.Add(new Cred { Unid = 101, Name = "Employee 2", Enabled = true });
                SendDbChangeAndWait(dbChange);

                Assert.AreEqual(2, _controller.ReceivedCreds.Count, "Should have received 2 credentials");
                Assert.AreEqual(100, _controller.ReceivedCreds[0].Unid);
                Assert.AreEqual("Employee 1", _controller.ReceivedCreds[0].Name);
                Assert.AreEqual(101, _controller.ReceivedCreds[1].Unid);
                Assert.AreEqual("Employee 2", _controller.ReceivedCreds[1].Name);

                // Delete one credential
                var deleteChange = new DbChange();
                deleteChange.CredDelete.Add(100);
                SendDbChangeAndWait(deleteChange);

                Assert.AreEqual(1, _controller.DeletedCredUnids.Count, "Should have 1 deleted cred unid");
                Assert.AreEqual(100, _controller.DeletedCredUnids[0]);

                // Delete all credentials
                var deleteAllChange = new DbChange();
                deleteAllChange.CredDeleteAll = true;
                SendDbChangeAndWait(deleteAllChange);

                Assert.IsTrue(_controller.CredDeleteAllReceived, "Should have received CredDeleteAll");
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestCredTemplateInsertAndDelete()
        {
            try
            {
                SetupConnection();

                // Insert credential templates
                var dbChange = new DbChange();
                dbChange.CredTemplate.Add(new CredTemplate { Unid = 200, Name = "Employee Card" });
                dbChange.CredTemplate.Add(new CredTemplate { Unid = 201, Name = "Visitor Card" });
                SendDbChangeAndWait(dbChange);

                Assert.AreEqual(2, _controller.ReceivedCredTemplates.Count, "Should have received 2 credential templates");
                Assert.AreEqual(200, _controller.ReceivedCredTemplates[0].Unid);
                Assert.AreEqual("Employee Card", _controller.ReceivedCredTemplates[0].Name);

                // Delete one
                var deleteChange = new DbChange();
                deleteChange.CredTemplateDelete.Add(200);
                SendDbChangeAndWait(deleteChange);

                Assert.AreEqual(1, _controller.DeletedCredTemplateUnids.Count);
                Assert.AreEqual(200, _controller.DeletedCredTemplateUnids[0]);

                // Delete all
                var deleteAllChange = new DbChange();
                deleteAllChange.CredTemplateDeleteAll = true;
                SendDbChangeAndWait(deleteAllChange);

                Assert.IsTrue(_controller.CredTemplateDeleteAllReceived);
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestDataLayoutInsertAndDelete()
        {
            try
            {
                SetupConnection();

                var dbChange = new DbChange();
                dbChange.DataLayout.Add(new DataLayout { Unid = 300, Name = "Standard Layout" });
                SendDbChangeAndWait(dbChange);

                Assert.AreEqual(1, _controller.ReceivedDataLayouts.Count);
                Assert.AreEqual(300, _controller.ReceivedDataLayouts[0].Unid);
                Assert.AreEqual("Standard Layout", _controller.ReceivedDataLayouts[0].Name);

                // Delete
                var deleteChange = new DbChange();
                deleteChange.DataLayoutDelete.Add(300);
                SendDbChangeAndWait(deleteChange);

                Assert.AreEqual(1, _controller.DeletedDataLayoutUnids.Count);

                // Delete all
                var deleteAllChange = new DbChange();
                deleteAllChange.DataLayoutDeleteAll = true;
                SendDbChangeAndWait(deleteAllChange);

                Assert.IsTrue(_controller.DataLayoutDeleteAllReceived);
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestDataFormatInsertAndDelete()
        {
            try
            {
                SetupConnection();

                var dbChange = new DbChange();
                dbChange.DataFormat.Add(new DataFormat { Unid = 400, Name = "26-bit Wiegand" });
                SendDbChangeAndWait(dbChange);

                Assert.AreEqual(1, _controller.ReceivedDataFormats.Count);
                Assert.AreEqual(400, _controller.ReceivedDataFormats[0].Unid);
                Assert.AreEqual("26-bit Wiegand", _controller.ReceivedDataFormats[0].Name);

                // Delete
                var deleteChange = new DbChange();
                deleteChange.DataFormatDelete.Add(400);
                SendDbChangeAndWait(deleteChange);

                Assert.AreEqual(1, _controller.DeletedDataFormatUnids.Count);

                // Delete all
                var deleteAllChange = new DbChange();
                deleteAllChange.DataFormatDeleteAll = true;
                SendDbChangeAndWait(deleteAllChange);

                Assert.IsTrue(_controller.DataFormatDeleteAllReceived);
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestDevInsertAndDelete()
        {
            try
            {
                SetupConnection();

                var dbChange = new DbChange();
                var testDev = new Dev { Unid = 500, Name = "Front Door Reader" };
                dbChange.Dev.Add(testDev);
                SendDbChangeAndWait(dbChange);

                Assert.AreEqual(1, _controller.ReceivedDevs.Count);
                Assert.AreEqual(500, _controller.ReceivedDevs[0].Unid);
                Assert.AreEqual("Front Door Reader", _controller.ReceivedDevs[0].Name);

                // Delete
                var deleteChange = new DbChange();
                deleteChange.DevDelete.Add(500);
                SendDbChangeAndWait(deleteChange);

                Assert.AreEqual(1, _controller.DeletedDevUnids.Count);

                // Delete all
                var deleteAllChange = new DbChange();
                deleteAllChange.DevDeleteAll = true;
                SendDbChangeAndWait(deleteAllChange);

                Assert.IsTrue(_controller.DevDeleteAllReceived);
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestPrivInsertAndDelete()
        {
            try
            {
                SetupConnection();

                var dbChange = new DbChange();
                dbChange.Priv.Add(new Priv { Unid = 600, Name = "Door Access Level A", Enabled = true, PrivType = PrivType.Door });
                dbChange.Priv.Add(new Priv { Unid = 601, Name = "Door Access Level B", Enabled = true, PrivType = PrivType.Door });
                SendDbChangeAndWait(dbChange);

                Assert.AreEqual(2, _controller.ReceivedPrivs.Count);
                Assert.AreEqual(600, _controller.ReceivedPrivs[0].Unid);
                Assert.AreEqual("Door Access Level A", _controller.ReceivedPrivs[0].Name);
                Assert.AreEqual(PrivType.Door, _controller.ReceivedPrivs[0].PrivType);

                // Delete
                var deleteChange = new DbChange();
                deleteChange.PrivDelete.Add(600);
                SendDbChangeAndWait(deleteChange);

                Assert.AreEqual(1, _controller.DeletedPrivUnids.Count);

                // Delete all
                var deleteAllChange = new DbChange();
                deleteAllChange.PrivDeleteAll = true;
                SendDbChangeAndWait(deleteAllChange);

                Assert.IsTrue(_controller.PrivDeleteAllReceived);
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestHolCalInsertAndDelete()
        {
            try
            {
                SetupConnection();

                var dbChange = new DbChange();
                dbChange.HolCal.Add(new HolCal { Unid = 700, Name = "US Holidays 2024" });
                SendDbChangeAndWait(dbChange);

                Assert.AreEqual(1, _controller.ReceivedHolCals.Count);
                Assert.AreEqual(700, _controller.ReceivedHolCals[0].Unid);
                Assert.AreEqual("US Holidays 2024", _controller.ReceivedHolCals[0].Name);

                // Delete
                var deleteChange = new DbChange();
                deleteChange.HolCalDelete.Add(700);
                SendDbChangeAndWait(deleteChange);

                Assert.AreEqual(1, _controller.DeletedHolCalUnids.Count);

                // Delete all
                var deleteAllChange = new DbChange();
                deleteAllChange.HolCalDeleteAll = true;
                SendDbChangeAndWait(deleteAllChange);

                Assert.IsTrue(_controller.HolCalDeleteAllReceived);
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestHolTypeInsertAndDelete()
        {
            try
            {
                SetupConnection();

                var dbChange = new DbChange();
                dbChange.HolType.Add(new HolType { Unid = 800, Name = "Federal Holiday" });
                SendDbChangeAndWait(dbChange);

                Assert.AreEqual(1, _controller.ReceivedHolTypes.Count);
                Assert.AreEqual(800, _controller.ReceivedHolTypes[0].Unid);
                Assert.AreEqual("Federal Holiday", _controller.ReceivedHolTypes[0].Name);

                // Delete
                var deleteChange = new DbChange();
                deleteChange.HolTypeDelete.Add(800);
                SendDbChangeAndWait(deleteChange);

                Assert.AreEqual(1, _controller.DeletedHolTypeUnids.Count);

                // Delete all
                var deleteAllChange = new DbChange();
                deleteAllChange.HolTypeDeleteAll = true;
                SendDbChangeAndWait(deleteAllChange);

                Assert.IsTrue(_controller.HolTypeDeleteAllReceived);
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestHolInsertAndDelete()
        {
            try
            {
                SetupConnection();

                var dbChange = new DbChange();
                dbChange.Hol.Add(new Hol { Unid = 850, NumDays = 1, Repeat = true, AllHolTypes = true, PreserveSchedDay = false });
                SendDbChangeAndWait(dbChange);

                Assert.AreEqual(1, _controller.ReceivedHols.Count);
                Assert.AreEqual(850, _controller.ReceivedHols[0].Unid);
                Assert.AreEqual(1, _controller.ReceivedHols[0].NumDays);
                Assert.IsTrue(_controller.ReceivedHols[0].Repeat);

                // Delete
                var deleteChange = new DbChange();
                deleteChange.HolDelete.Add(850);
                SendDbChangeAndWait(deleteChange);

                Assert.AreEqual(1, _controller.DeletedHolUnids.Count);

                // Delete all
                var deleteAllChange = new DbChange();
                deleteAllChange.HolDeleteAll = true;
                SendDbChangeAndWait(deleteAllChange);

                Assert.IsTrue(_controller.HolDeleteAllReceived);
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestSchedInsertAndDelete()
        {
            try
            {
                SetupConnection();

                var dbChange = new DbChange();
                dbChange.Sched.Add(new Sched { Unid = 900, Name = "Business Hours" });
                dbChange.Sched.Add(new Sched { Unid = 901, Name = "After Hours" });
                SendDbChangeAndWait(dbChange);

                Assert.AreEqual(2, _controller.ReceivedScheds.Count);
                Assert.AreEqual(900, _controller.ReceivedScheds[0].Unid);
                Assert.AreEqual("Business Hours", _controller.ReceivedScheds[0].Name);
                Assert.AreEqual(901, _controller.ReceivedScheds[1].Unid);
                Assert.AreEqual("After Hours", _controller.ReceivedScheds[1].Name);

                // Delete
                var deleteChange = new DbChange();
                deleteChange.SchedDelete.Add(900);
                SendDbChangeAndWait(deleteChange);

                Assert.AreEqual(1, _controller.DeletedSchedUnids.Count);

                // Delete all
                var deleteAllChange = new DbChange();
                deleteAllChange.SchedDeleteAll = true;
                SendDbChangeAndWait(deleteAllChange);

                Assert.IsTrue(_controller.SchedDeleteAllReceived);
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestBulkDataExchange()
        {
            try
            {
                SetupConnection();

                // Send a DbChange with multiple data types at once
                var dbChange = new DbChange();

                // Add credentials
                dbChange.Cred.Add(new Cred { Unid = 1, Name = "Employee 1", Enabled = true });
                dbChange.Cred.Add(new Cred { Unid = 2, Name = "Employee 2", Enabled = true });

                // Add credential template
                dbChange.CredTemplate.Add(new CredTemplate { Unid = 10, Name = "Standard Card" });

                // Add data layout
                dbChange.DataLayout.Add(new DataLayout { Unid = 20, Name = "Default Layout" });

                // Add data format
                dbChange.DataFormat.Add(new DataFormat { Unid = 30, Name = "26-bit Wiegand" });

                // Add device
                var testDev = new Dev { Unid = 40, Name = "Main Entrance" };
                dbChange.Dev.Add(testDev);

                // Add privileges
                dbChange.Priv.Add(new Priv { Unid = 50, Name = "Access Level 1", Enabled = true, PrivType = PrivType.Door });
                dbChange.Priv.Add(new Priv { Unid = 51, Name = "Access Level 2", Enabled = true, PrivType = PrivType.Door });

                // Add holiday calendar
                dbChange.HolCal.Add(new HolCal { Unid = 60, Name = "2024 Holidays" });

                // Add holiday type
                dbChange.HolType.Add(new HolType { Unid = 70, Name = "National Holiday" });

                // Add holiday
                dbChange.Hol.Add(new Hol { Unid = 75, NumDays = 1, Repeat = true, AllHolTypes = true, PreserveSchedDay = false, HolCalUnid = 60 });

                // Add schedules
                dbChange.Sched.Add(new Sched { Unid = 80, Name = "Always" });
                dbChange.Sched.Add(new Sched { Unid = 81, Name = "Business Hours" });

                SendDbChangeAndWait(dbChange);

                // Verify all data was received
                Assert.AreEqual(2, _controller.ReceivedCreds.Count, "Should have 2 credentials");
                Assert.AreEqual(1, _controller.ReceivedCredTemplates.Count, "Should have 1 credential template");
                Assert.AreEqual(1, _controller.ReceivedDataLayouts.Count, "Should have 1 data layout");
                Assert.AreEqual(1, _controller.ReceivedDataFormats.Count, "Should have 1 data format");
                Assert.AreEqual(1, _controller.ReceivedDevs.Count, "Should have 1 device");
                Assert.AreEqual(2, _controller.ReceivedPrivs.Count, "Should have 2 privileges");
                Assert.AreEqual(1, _controller.ReceivedHolCals.Count, "Should have 1 holiday calendar");
                Assert.AreEqual(1, _controller.ReceivedHolTypes.Count, "Should have 1 holiday type");
                Assert.AreEqual(1, _controller.ReceivedHols.Count, "Should have 1 holiday");
                Assert.AreEqual(2, _controller.ReceivedScheds.Count, "Should have 2 schedules");
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestBulkDelete()
        {
            try
            {
                SetupConnection();

                // Send a DbChange with multiple delete operations
                var dbChange = new DbChange();

                dbChange.CredDelete.Add(1);
                dbChange.CredDelete.Add(2);
                dbChange.CredTemplateDelete.Add(10);
                dbChange.DataLayoutDelete.Add(20);
                dbChange.DataFormatDelete.Add(30);
                dbChange.DevDelete.Add(40);
                dbChange.PrivDelete.Add(50);
                dbChange.PrivDelete.Add(51);
                dbChange.HolCalDelete.Add(60);
                dbChange.HolTypeDelete.Add(70);
                dbChange.SchedDelete.Add(80);
                dbChange.SchedDelete.Add(81);

                SendDbChangeAndWait(dbChange);

                // Verify all deletes were received
                Assert.AreEqual(2, _controller.DeletedCredUnids.Count);
                Assert.AreEqual(1, _controller.DeletedCredTemplateUnids.Count);
                Assert.AreEqual(1, _controller.DeletedDataLayoutUnids.Count);
                Assert.AreEqual(1, _controller.DeletedDataFormatUnids.Count);
                Assert.AreEqual(1, _controller.DeletedDevUnids.Count);
                Assert.AreEqual(2, _controller.DeletedPrivUnids.Count);
                Assert.AreEqual(1, _controller.DeletedHolCalUnids.Count);
                Assert.AreEqual(1, _controller.DeletedHolTypeUnids.Count);
                Assert.AreEqual(2, _controller.DeletedSchedUnids.Count);
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestDeleteAllFlags()
        {
            try
            {
                SetupConnection();

                // Send DbChange with all delete-all flags
                var dbChange = new DbChange();
                dbChange.CredDeleteAll = true;
                dbChange.CredTemplateDeleteAll = true;
                dbChange.DataLayoutDeleteAll = true;
                dbChange.DataFormatDeleteAll = true;
                dbChange.DevDeleteAll = true;
                dbChange.PrivDeleteAll = true;
                dbChange.HolCalDeleteAll = true;
                dbChange.HolTypeDeleteAll = true;
                dbChange.SchedDeleteAll = true;

                SendDbChangeAndWait(dbChange);

                // Verify all delete-all flags were received
                Assert.IsTrue(_controller.CredDeleteAllReceived);
                Assert.IsTrue(_controller.CredTemplateDeleteAllReceived);
                Assert.IsTrue(_controller.DataLayoutDeleteAllReceived);
                Assert.IsTrue(_controller.DataFormatDeleteAllReceived);
                Assert.IsTrue(_controller.DevDeleteAllReceived);
                Assert.IsTrue(_controller.PrivDeleteAllReceived);
                Assert.IsTrue(_controller.HolCalDeleteAllReceived);
                Assert.IsTrue(_controller.HolTypeDeleteAllReceived);
                Assert.IsTrue(_controller.SchedDeleteAllReceived);
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestEventReporting()
        {
            try
            {
                SetupConnection();

                _state.StartEvts();
                Assert.IsTrue(WaitForEvtFlowControl(EvtFlowControl.StartContinuous, 5000), "Expected StartContinuous EvtControl");

                // Send a single event from controller to host
                var evt = SimulatedController.CreateEvent(EvtCode.DoorAccessGranted);
                _controller.SendEvent(evt);

                // Wait for event to be received
                Thread.Sleep(500);

                lock (_observer.ReceivedEvents)
                {
                    Assert.AreEqual(1, _observer.ReceivedEvents.Count, "Should have received 1 event");
                    Assert.AreEqual(EvtCode.DoorAccessGranted, _observer.ReceivedEvents[0].EvtCode);
                }
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestMultipleEventsReporting()
        {
            try
            {
                SetupConnection();

                _state.StartEvts();
                Assert.IsTrue(WaitForEvtFlowControl(EvtFlowControl.StartContinuous, 5000), "Expected StartContinuous EvtControl");

                // Send multiple events from controller to host
                var evts = new[]
                {
                    SimulatedController.CreateEvent(EvtCode.ControllerStartup),
                    SimulatedController.CreateEvent(EvtCode.ControllerOnline),
                    SimulatedController.CreateEvent(EvtCode.DoorAccessGranted),
                    SimulatedController.CreateEvent(EvtCode.DoorAccessDenied)
                };
                _controller.SendEvents(evts);

                // Wait for events to be received
                Thread.Sleep(500);

                lock (_observer.ReceivedEvents)
                {
                    Assert.AreEqual(4, _observer.ReceivedEvents.Count, "Should have received 4 events");
                    Assert.AreEqual(EvtCode.ControllerStartup, _observer.ReceivedEvents[0].EvtCode);
                    Assert.AreEqual(EvtCode.ControllerOnline, _observer.ReceivedEvents[1].EvtCode);
                    Assert.AreEqual(EvtCode.DoorAccessGranted, _observer.ReceivedEvents[2].EvtCode);
                    Assert.AreEqual(EvtCode.DoorAccessDenied, _observer.ReceivedEvents[3].EvtCode);
                }
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestDevActionDoorModeChange()
        {
            try
            {
                SetupConnection();

                // Send DevActionReq for door mode change (community supports this action type)
                var devAction = new DevActionReq
                {
                    DevUnid = 1,
                    DevActionType = DevActionType.DoorModeChange
                };
                _state.DevActionReq(devAction);

                Assert.IsTrue(_observer.DevActionRespEvent.WaitOne(5000), "Should get DevActionResp");

                // Verify controller received the request
                Assert.AreEqual(1, _controller.ReceivedDevActions.Count);
                Assert.AreEqual(DevActionType.DoorModeChange, _controller.ReceivedDevActions[0].DevActionType);
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestDbChangeErrorResponse()
        {
            try
            {
                SetupConnection();

                // Configure controller to return an error
                _controller.NextDbChangeError = "Test error: database full";

                var dbChange = new DbChange();
                dbChange.Cred.Add(new Cred { Unid = 1, Name = "Test", Enabled = true });
                SendDbChangeAndWait(dbChange);

                // Verify response has error
                var lastResp = _observer.DbChangeResponses[_observer.DbChangeResponses.Count - 1];
                Assert.IsFalse(string.IsNullOrEmpty(lastResp.Exception), "Exception should be set");
                Assert.AreEqual("Test error: database full", lastResp.Exception);
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestDevActionErrorResponse()
        {
            try
            {
                SetupConnection();

                // Configure controller to return an error
                _controller.NextDevActionError = "Device not found";

                var devAction = new DevActionReq
                {
                    DevUnid = 999,
                    DevActionType = DevActionType.DoorMomentaryUnlock
                };
                _state.DevActionReq(devAction);

                Assert.IsTrue(_observer.DevActionRespEvent.WaitOne(5000), "Should get DevActionResp");

                // Verify response has error
                Assert.AreEqual(1, _observer.DevActionResponses.Count);
                Assert.IsFalse(string.IsNullOrEmpty(_observer.DevActionResponses[0].Exception), "Exception should be set");
                Assert.AreEqual("Device not found", _observer.DevActionResponses[0].Exception);
            }
            finally
            {
                TeardownConnection();
            }
        }

        [TestMethod]
        public void TestEvtControlTracking()
        {
            try
            {
                SetupConnection();

                _state.StartEvts();
                Assert.IsTrue(WaitForEvtFlowControl(EvtFlowControl.StartContinuous, 5000), "Expected StartContinuous EvtControl");

                // EvtControl tracking list should contain at least one item
                lock (_controller.ReceivedEvtControls)
                {
                    Assert.IsTrue(_controller.ReceivedEvtControls.Count > 0, "Expected at least one EvtControl");
                }
            }
            finally
            {
                TeardownConnection();
            }
        }

        private bool WaitForEvtFlowControl(EvtFlowControl flowControl, int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                lock (_controller.ReceivedEvtControls)
                {
                    foreach (var evtControl in _controller.ReceivedEvtControls)
                    {
                        if (evtControl.EvtFlowControlCase == EvtControl.EvtFlowControlOneofCase.EvtFlowControl &&
                            evtControl.EvtFlowControl == flowControl)
                        {
                            return true;
                        }
                    }
                }

                Thread.Sleep(25);
            }

            return false;
        }
    }
}
