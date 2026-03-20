using System;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    /// <summary>
    /// Base-class implementation of ISpCoreHostConnectionObserver with all methods implemented with empty stubs.
    /// </summary>
    public class SpCoreHostConnectionObserverBase : ISpCoreHostConnectionObserver
    {
        public virtual void OnOnline(SpCoreHostConnection connection, Identification hostIdentification)
        {
        }

        public virtual void OnOffline(SpCoreHostConnection connection, Exception exception)
        {
        }

        public virtual string OnDbChange(SpCoreHostConnection connection, DbChange dbChange)
        {
            return null;
        }

        public virtual string OnDevActionReq(SpCoreHostConnection connection, DevActionReq req)
        {
            return null;
        }

        public virtual void OnEvtControl(SpCoreHostConnection connection, EvtControl evtControl)
        {
        }

        public virtual void OnDevStateRecordControl(SpCoreHostConnection connection, DevStateRecordControl control)
        {
        }
    }
}
