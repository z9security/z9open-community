using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    public interface ISpCoreControllerStateMgr
    {
        ISpCoreControllerState Add(Dev controller, ISpCoreControllerStateObserver observer, ISslSettings sslSettings, ISpCoreTakeoverStorage takeoverStorage, X509Certificate2 identificationCertificate);
        
        /// <summary>
        /// Stop all controllers and clear them from the state observers
        /// </summary>
        void StopAll();

        /// <summary>
        /// Shuts down this instance of State Controller. A new State Controller needs to be created to begin communicating with Z9 Sp-Core controllers.
        /// </summary>
        void Shutdown();
    }

    /// <summary>
    /// The top-level object for the API for connecting to and communicating with Z9 Sp-Core controllers.
    /// </summary>
    [Guid("37F6EDB6-EDBF-4939-995E-77FF7DA75369"), ComVisible(true)]
    public class SpCoreControllerStateMgr : ISpCoreControllerStateMgr, IDisposable
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        
        private ConcurrentDictionary<ISpCoreControllerState, ISpCoreControllerState> _controllerStates = new ConcurrentDictionary<ISpCoreControllerState, ISpCoreControllerState>();
        private ConcurrentDictionary<SpCoreControllerState, SpCoreControllerState> _pendingIncomingStates = new ConcurrentDictionary<SpCoreControllerState, SpCoreControllerState>();

        private readonly string _callbackHostAddress;
        private readonly string _callbackHostAddress_Secondary;
        private readonly int _callbackHostPort;
        private readonly X509Certificate2 _sslCertificate;
        private readonly ISslSettings _defaultSslSettings;
        private readonly bool _noTakeover;
        private ISpCoreControllerMgrObserver _observer;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private SemaphoreSlim _shutdownListenerCompleted = new SemaphoreSlim(0, 1);

        public SpCoreControllerStateMgr(ISslSettings defaultSslSettings)
        {
            _defaultSslSettings = defaultSslSettings;

            LogBanner();
        }

        public SpCoreControllerStateMgr(SpCoreControllerStateMgrConfig config, ISslSettings defaultSslSettings, ISpCoreControllerMgrObserver observer)
        {
            if (config.ListenPort > 0)
            {
                Task.Factory.StartNew(async () => await Listen(config.ListenPort, _cancellationTokenSource.Token), TaskCreationOptions.LongRunning);
                _callbackHostAddress = config.CallbackHostAddress;
                _callbackHostAddress_Secondary = config.CallbackHostAddress_Secondary;
                _callbackHostPort = config.ListenPort;
                _sslCertificate = config.Certificate;
            }
            else
            {
                // Not sure what should be done here in this case. We are not going to be listening for connections
                Logger.Warn($"SpCoreControllerStateMgr: ListenPort is not > 0 : {config.ListenPort}");
                _shutdownListenerCompleted.Release();
            }
            _defaultSslSettings = defaultSslSettings;
            _noTakeover = config.NoTakeover;
            _observer = observer;

            LogBanner();
        }

        void LogBanner()
        {
            Logger.Info("");
            Logger.Info("--------------------------------------------------------------------------------------------------------------------");
            Logger.Info("Starting Z9 Security Access-Control SDK");
            Logger.Info("z9/op=n                                                                              https://z9open.com");
            Logger.Info("--------------------------------------------------------------------------------------------------------------------");
            Logger.Info("");
            Logger.Info($"Assembly Name: {Assembly.GetExecutingAssembly().GetName().Name} Version: {Assembly.GetExecutingAssembly().GetName().Version}");
            Logger.Info("");

        }

        /// <summary>
        /// Add a controller to connect to.
        /// </summary>
        /// <param name="controller">A Dev with the connection parameters for the controller.  Note that this Dev is not sent to the controller as configuration, DbChange must be used for that purpose.</param>
        /// <param name="observer">The observer</param>
        /// <param name="sslSettings">The SSL settings for connecting to the controller</param>
        /// <returns>The state class with management methods for the controller</returns>
        public ISpCoreControllerState Add(Dev controller, ISpCoreControllerStateObserver observer, ISslSettings sslSettings, ISpCoreTakeoverStorage takeoverStorage, X509Certificate2 identificationCertificate)
        {
            var connectionInfo = new SpCoreControllerStateConnectionInfo
            {
                Controller = controller,
                SslSettings = sslSettings,
                TakeoverStorage = takeoverStorage,
                IdentificationCertificate = identificationCertificate
            };

            return Add(connectionInfo, observer);
        }

            

        /// <summary>
        /// Add a controller to connect to.
        /// </summary>
        /// <param name="controller">A Dev with the connection parameters for the controller.  Note that this Dev is not sent to the controller as configuration, DbChange must be used for that purpose.</param>
        /// <param name="observer">The observer</param>
        /// <param name="sslSettings">The SSL settings for connecting to the controller</param>
        /// <returns>The state class with management methods for the controller</returns>
        public ISpCoreControllerState Add(SpCoreControllerStateConnectionInfo connectionInfo, ISpCoreControllerStateObserver observer)
        {
            var controller = connectionInfo.Controller;
            SpCoreControllerState newControllerState = new SpCoreControllerState(connectionInfo, _callbackHostAddress, _callbackHostAddress_Secondary, _callbackHostPort, _sslCertificate) {Observer = observer};
            ISpCoreControllerState result = newControllerState.Outgoing ? newControllerState as ISpCoreControllerState : new SpCoreControllerStateProxy(newControllerState);

            if (!newControllerState.Outgoing)
                new IncomingOfflineDetectionThread(result).Start();

            RemoveAllStopping();

            foreach (var controllerStatePossibleProxy in _controllerStates.Values)
            {
                var controllerState = SpCoreControllerStateProxy.DeProxy(controllerStatePossibleProxy);
                if (controllerState.IsStopping())
                    continue;
                if (controllerState.Outgoing && controllerState.ConnectionInfo.Controller.AddressCase == Dev.AddressOneofCase.Address && !String.IsNullOrEmpty(controllerState.ConnectionInfo.Controller.Address) && controllerState.ConnectionInfo.Controller.Address == controller.Address && controllerState.ConnectionInfo.Controller.Port == controller.Port)
                {
                    Logger.Warn($"Duplicate controller being added: {controller.Address}:{controller.Port}");
                }

                if (controllerState.ConnectionInfo.Controller.MacAddressCase == Dev.MacAddressOneofCase.MacAddress && controller.MacAddressCase == Dev.MacAddressOneofCase.MacAddress && controllerState.ConnectionInfo.Controller.MacAddress == controller.MacAddress)
                {
                    Logger.Warn($"Duplicate controller being added: {controller.MacAddress}");
                }
            }

            _controllerStates.TryAdd(result, result);
            newControllerState.Start();

            Logger.Info($"Add: {newControllerState.StateName}");

            return result;
        }

        void RemoveAllStopping()
        {
            List<ISpCoreControllerState> toRemove = new List<ISpCoreControllerState>();

            foreach (var controllerState in _controllerStates.Values)
            {
                if (controllerState.IsStopping())
                    toRemove.Add(controllerState);
                else
                    Logger.Debug(controllerState.GetLogPrefix() + $"of type {controllerState.GetType()} is not stopping, leaving in manager.");

            }

            foreach (var controllerState in toRemove)
            {
                if (controllerState.IsStopping())
                {
                    Logger.Info(controllerState.GetLogPrefix() + $"of type {controllerState.GetType()} is stopped/stopping, removing from manager.");
                    if (!_controllerStates.TryRemove(controllerState, out ISpCoreControllerState removed))
                        Logger.Warn(controllerState.GetLogPrefix() + "failed to remove from manager.");
                }
            }
        }

        public void StopAll()
        {
            foreach (var state in _controllerStates.Values)
            {
                if (state.IsStopping())
                    continue;

                state.Stop();
            }

            RemoveAllStopping();
        }

        public void Shutdown()
        {
            // Stop listener from accepting new connection first
            _cancellationTokenSource.Cancel();

            _shutdownListenerCompleted.Wait();
            _shutdownListenerCompleted.Release(); // Allow disposal to occur if called

            StopAll();
        }

        public async Task Listen(int listenPort, CancellationToken cancellationToken)
        {
            if (listenPort == 0)
                throw new Exception("listenPort=0");

            Logger.Info($"Listen: listenPort={listenPort} is being opened for new requests");

            var listener = new TcpListener(IPAddress.Any, listenPort);

            try
            {
                listener.Start();
            }
            catch (Exception exception) 
            {
                throw new Exception($"Listen: Unable to open port {listenPort} for incoming connections.", exception);
            }

            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await WaitingForClient(listener, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception exception)
                    {
                        Logger.Error($"Listen: Error while waiting for incoming connection.", exception);
                        await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                    }
                }
            }
            finally
            {
                listener.Stop();
                Logger.Info($"Listen: listenPort={listenPort} has been terminated");
                _shutdownListenerCompleted.Release();
            }
        }

        private async Task WaitingForClient(TcpListener listener, CancellationToken cancellationToken)
        {
            var client = await listener.AcceptTcpClientAsync().WithCancellation(cancellationToken);

            Logger.Info($"Listen: Incoming connection received from {(IPEndPoint) client.Client.RemoteEndPoint}");

            var host = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

            // dummy placeholder controller
            var controller = new Dev
            {
                DevType = DevType.IoController,
                DevUse = DevUse.IoControllerSecondary, // this means it is a "secondary" controller that gets its configuration from us.
                Enabled = true,
                Address = host, // for now
                Name = "Pending from " + host,
                ExtController = new Controller
                {
                    ControllerConfig = new ControllerConfig
                    {
                        DevInitiatesConnection = true
                    }
                }
            };
            SpCoreProtoUtil.InitRequired(controller);

            controller.DevModConfig = new DevModConfig
            {
                Type = DevModConfigType.ControllerZ9Spcore,
            };

            SpCoreControllerStateConnectionInfo connectionInfo = new SpCoreControllerStateConnectionInfo()
            {
                Controller = controller,
                SslSettings = _defaultSslSettings
                // no takeover will be happening with this state, and we don't know the identification certificate yet, since we don't know what controller it is yet.
            };


            var pendingState = new SpCoreControllerState(connectionInfo, _callbackHostAddress, _callbackHostAddress_Secondary, _callbackHostPort, _sslCertificate)
            {
                Observer = new PendingIncomingStateObserver(this)
            };

            _pendingIncomingStates.TryAdd(pendingState, pendingState);
            pendingState.Start();

            Logger.Info($"Add (pending): {pendingState.StateName}");

            pendingState.OnConnected(client, controller.Address);

        }

        private class PendingIncomingStateObserver : ISpCoreControllerStateObserver
        {
            private SpCoreControllerStateMgr _mgr;

            public PendingIncomingStateObserver(SpCoreControllerStateMgr mgr)
            {
                _mgr = mgr;
            }

            void ISpCoreControllerStateObserver.OnOffline(ISpCoreControllerState state)
            {
                // TODO: can we even ever get this?
            }

            void ISpCoreControllerStateObserver.OnOnline(ISpCoreControllerState pendingState)
            {
                var pendingStateCast = pendingState as SpCoreControllerState;

                var id = pendingState.GetIdentification().Id;

                // now we should know the id/mac address, so we can connect it up with the "real" state.
                Logger.Info(pendingStateCast.LogPrefix + $"OnOnline (pending): {id}");

                SpCoreControllerStateProxy matchingProxy = null;
                SpCoreControllerState matchingUnderlying = null;

                foreach (var controllerStatePossibleProxy in _mgr._controllerStates.Values)
                {
                    if (!(controllerStatePossibleProxy is SpCoreControllerStateProxy))
                        continue;
                    var controllerStateUnderlying = SpCoreControllerStateProxy.DeProxy(controllerStatePossibleProxy);

                    if (controllerStateUnderlying.Outgoing)
                        continue;
                    if (controllerStateUnderlying.ConnectionInfo.Controller.MacAddress == id)
                    {
                        if (controllerStateUnderlying.IsStopping())
                        {
                            Logger.Info(pendingStateCast.LogPrefix + $"OnOnline: Found matching configured controller for id {id}, but it is stopping, so skipping: {controllerStateUnderlying.StateName}");
                            continue;
                        }

                        Logger.Info(pendingStateCast.LogPrefix + $"OnOnline: Found matching configured controller for id {id}: {controllerStateUnderlying.StateName}");

                        matchingProxy = controllerStatePossibleProxy as SpCoreControllerStateProxy;
                        matchingUnderlying = controllerStateUnderlying;
                        break;
                    }
                }

                SpCoreControllerState removedPendingState;
                _mgr._pendingIncomingStates.TryRemove(pendingState as SpCoreControllerState, out removedPendingState);

                if (matchingProxy == null || matchingUnderlying == null)
                {
                    var identification = pendingState.GetIdentification();
                    Logger.Info(pendingStateCast.LogPrefix + $"Listen: No matching configured controller found for id {id}");

                    UnknownControllerRejectedOptions options = new UnknownControllerRejectedOptions();

                    if (_mgr._observer == null)
                        Logger.Debug(pendingStateCast.LogPrefix + "No observer");
                    else
                        Logger.Debug(pendingStateCast.LogPrefix + "Invoking observer.OnUnknownControllerRejected");

                    // notify SDK client of this - they might want to auto-add or similar.  Also allow them to prevent the use of STFU.
                    _mgr._observer?.OnUnknownControllerRejected(identification, options);

                    Logger.Info(pendingStateCast.LogPrefix + "Stopping pending state (async)");
                    Task.Run(() => {
                        pendingState.Stop(); // seems to block, or at least take a while, if we call it synchronously, so do it in the background.
                        Logger.Info(pendingStateCast.LogPrefix + "Stopped pending state");
                    });
                    return;
                }


                // now we have to change the proxy to point to the "connected" one.
                Logger.Info(pendingStateCast.LogPrefix + $"OnOnline: Changing Target for SpCoreControllerStateProxy for id {id} from {matchingUnderlying.GetType()} {matchingUnderlying.StateName} to {pendingStateCast.GetType()} {pendingStateCast.StateName}");

                matchingProxy.SetTarget(pendingStateCast);

                // re-link observer

                pendingStateCast.Observer = matchingUnderlying.Observer;
                matchingUnderlying.Observer = null;

                Logger.Debug(pendingStateCast.LogPrefix + $"OnOnline: invoking Stop on {matchingUnderlying.GetType()} {matchingUnderlying.StateName}");

                matchingUnderlying.Stop();

                // copy some essential connection info from matchingUnderlying to pendingStateCast:
                pendingStateCast.ConnectionInfo.Controller = matchingUnderlying.ConnectionInfo.Controller;
                pendingStateCast.ConnectionInfo.IdentificationCertificate = matchingUnderlying.ConnectionInfo.IdentificationCertificate;
                pendingStateCast.ConnectionInfo.Licenses = matchingUnderlying.ConnectionInfo.Licenses;

                bool identificationChallengeInProgress;
                try
                {
                    identificationChallengeInProgress = !pendingStateCast.CheckIdentification();
                }
                catch (Exception e)
                {
                    Logger.Warn(pendingStateCast.LogPrefix + $"OnOnline: CheckIdentification failed {e}");
                    matchingUnderlying.Stop();
                    pendingStateCast.Observer.OnOffline(matchingProxy);
                    return;
                }

                pendingStateCast.SendIdentification();

                Logger.Info(pendingStateCast.LogPrefix + $"identificationChallengeInProgress={identificationChallengeInProgress}");

                if (!identificationChallengeInProgress)
                    pendingStateCast.Observer?.OnOnline(matchingProxy);
            }

            // we don't care about any of these.
            void ISpCoreControllerStateObserver.OnDbChangeResp(ISpCoreControllerState state, DbChange dbChange, DbChangeResp dbChangeResp) { }
            void ISpCoreControllerStateObserver.OnDevActionResp(ISpCoreControllerState state, DevActionReq devActionReq, DevActionResp devActionResp) { }
            void ISpCoreControllerStateObserver.OnEvts(ISpCoreControllerState state, List<Evt> evts) { }

        }

        public void Dispose()
        {
            Shutdown();

            _cancellationTokenSource?.Dispose();
            _shutdownListenerCompleted?.Dispose();
        }
    }
}
