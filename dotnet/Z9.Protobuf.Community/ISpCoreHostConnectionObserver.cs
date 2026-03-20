using System;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    /// <summary>
    /// An observer for SpCoreHostConnection (controller-side connection to a host).
    /// </summary>
    public interface ISpCoreHostConnectionObserver
    {
        /// <summary>
        /// Called when the connection is online and the host has identified itself.
        /// </summary>
        void OnOnline(SpCoreHostConnection connection, Identification hostIdentification);

        /// <summary>
        /// Called when the connection goes offline.
        /// </summary>
        void OnOffline(SpCoreHostConnection connection, Exception exception);

        /// <summary>
        /// Called when a DbChange is received from the host. Return an error string, or null for success.
        /// The connection will automatically send the DbChangeResp.
        /// </summary>
        string OnDbChange(SpCoreHostConnection connection, DbChange dbChange);

        /// <summary>
        /// Called when a DevActionReq is received from the host. Return an error string, or null for success.
        /// The connection will automatically send the DevActionResp.
        /// </summary>
        string OnDevActionReq(SpCoreHostConnection connection, DevActionReq req);

        /// <summary>
        /// Called when an EvtControl is received from the host.
        /// </summary>
        void OnEvtControl(SpCoreHostConnection connection, EvtControl evtControl);

        /// <summary>
        /// Called when a DevStateRecordControl is received from the host.
        /// </summary>
        void OnDevStateRecordControl(SpCoreHostConnection connection, DevStateRecordControl control);
    }
}
