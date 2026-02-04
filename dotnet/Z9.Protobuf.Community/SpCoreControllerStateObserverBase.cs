using System.Collections.Generic;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    /// <summary>
    /// Base-class implementation of ISpCoreControllerStateObserver with all methods implemented with empty stubs.
    /// </summary>
    public class SpCoreControllerStateObserverBase : ISpCoreControllerStateObserver
    {
        public virtual void OnOnline(ISpCoreControllerState state)
        {
        }

        public virtual void OnOffline(ISpCoreControllerState state)
        {
        }

        public virtual void OnDbChangeResp(ISpCoreControllerState state, DbChange dbChange, DbChangeResp dbChangeResp)
        {
        }

        public virtual void OnDevActionResp(ISpCoreControllerState state, DevActionReq devActionReq, DevActionResp devActionResp)
        {
        }

        public virtual void OnEvts(ISpCoreControllerState state, List<Evt> evts)
        {
        }
    }
}
