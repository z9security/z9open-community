using System;
using System.Threading;

namespace Z9.Protobuf
{
    internal class IncomingOfflineDetectionThread : IConnectionThread
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ISpCoreControllerState _controllerState;

        Thread _thread;
        bool _stopping;

        public IncomingOfflineDetectionThread(ISpCoreControllerState controllerState)
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
                if (_controllerState.IsOnline())
                    break;

                if (!notifiedConnectTimeout)
                {
                    DateTime now = DateTime.Now;
                    if (now.CompareTo(offlineAfter) > 0)
                    {
                        var underlying = SpCoreControllerStateProxy.DeProxy(_controllerState);
                        if (underlying != null)
                        {
                            underlying.Observer?.OnOffline(_controllerState);
                        }
                        notifiedConnectTimeout = true;
                        break;
                    }
                }

                Thread.Sleep(1000);
            }
        }

    }
}