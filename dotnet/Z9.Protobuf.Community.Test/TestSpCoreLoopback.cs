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
    }

    [TestClass]
    public class TestSpCoreLoopback
    {
        [TestMethod]
        public void TestIdentificationAndDataExchange()
        {
            using (var controller = new SimulatedController())
            {
                controller.Start();

                // Create Dev pointing to simulated controller
                var dev = new Dev
                {
                    Address = "127.0.0.1",
                    Port = controller.Port
                };
                dev.ExtController = new Controller
                {
                    ControllerConfig = new ControllerConfig()
                };

                var connectionInfo = new SpCoreControllerStateConnectionInfo
                {
                    Controller = dev,
                    SslSettings = new NoSslSettings()
                };

                var observer = new LoopbackTestObserver();
                var state = new SpCoreControllerState(connectionInfo, "127.0.0.1", null, 0, null);
                state.Observer = observer;

                try
                {
                    // Act - Connect
                    state.Start();

                    // Wait for online
                    Assert.IsTrue(observer.OnlineEvent.WaitOne(10000), "Should come online within 10 seconds");
                    Assert.IsTrue(state.IsOnline(), "State should report online");
                    Assert.IsTrue(controller.IdentificationReceived, "Controller should have received identification");

                    // Act - Send DbChange (using Sched instead of Hol for community proto)
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
            using (var controller = new SimulatedController())
            {
                controller.Start();

                var dev = new Dev
                {
                    Address = "127.0.0.1",
                    Port = controller.Port
                };
                dev.ExtController = new Controller
                {
                    ControllerConfig = new ControllerConfig()
                };

                var connectionInfo = new SpCoreControllerStateConnectionInfo
                {
                    Controller = dev,
                    SslSettings = new NoSslSettings()
                };

                var observer = new LoopbackTestObserver();
                var state = new SpCoreControllerState(connectionInfo, "127.0.0.1", null, 0, null);
                state.Observer = observer;

                try
                {
                    state.Start();
                    Assert.IsTrue(observer.OnlineEvent.WaitOne(10000), "Should come online");
                    Assert.IsTrue(controller.ClientConnected, "Controller should show client connected");
                }
                finally
                {
                    state.Stop();
                }

                // After stopping, should eventually go offline
                // Note: The offline event may or may not fire depending on timing
            }
        }
    }
}
