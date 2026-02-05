using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    internal class SpCoreControllerStateProxy : ISpCoreControllerState
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static SpCoreControllerState DeProxy(ISpCoreControllerState state)
        {
            if (state is SpCoreControllerState)
                return state as SpCoreControllerState;
            else if (state is SpCoreControllerStateProxy)
                return (state as SpCoreControllerStateProxy).GetTarget();
            else
                throw new ArgumentException("Cannot obtain SpCoreControllerState");
        }

        SpCoreControllerState _target;

        public SpCoreControllerState GetTarget()
        {
            return _target;
        }

        public void SetTarget(SpCoreControllerState target)
        {
            _target = target;
        }

        ISpCoreControllerState GetValidTarget()
        {
            var result = _target;
            if (result == null)
                throw new Exception("Not connected");
            else
                return result;

        }

        public SpCoreControllerStateProxy(SpCoreControllerState target)
        {
            _target = target;
        }

        public string GetLogPrefix()
        {
            if (_target == null)
                return "[?] ";
            else
                return _target.GetLogPrefix();
        }

        public void ConsumeEvts(HashSet<long> unids)
        {
            GetValidTarget().ConsumeEvts(unids);
        }

        public void DbChange(DbChange dbChange)
        {
            GetValidTarget().DbChange(dbChange);
        }

        public void DevActionReq(DevActionReq devActionReq)
        {
            GetValidTarget().DevActionReq(devActionReq);
        }

        public bool IsOnline()
        {
            var target = _target;
            if (target == null)
                return false;
            else
                return target.IsOnline();

        }

        public bool IsStopping()
        {
            var target = _target;
            if (target == null)
                return false;
            else
                return target.IsStopping();

        }

        public Identification GetIdentification()
        {
            var target = _target;
            if (target == null)
                return null;
            else
                return target.GetIdentification();
        }

        public void StartEvts()
        {
            GetValidTarget().StartEvts();
        }

        public void Stop()
        {

            var target = _target;
            if (target == null)
            {
                Logger.Debug(GetLogPrefix() + "Stop (no target)");
                return;
            }
            else
            {
                Logger.Debug(GetLogPrefix() + "Stop (stopping target)");
                target.Stop();
            }
        }
    }
}
