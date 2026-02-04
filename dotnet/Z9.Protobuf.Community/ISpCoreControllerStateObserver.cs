using System.Collections.Generic;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    /// <summary>
    /// An observer for ISpCoreControllerState.
    /// </summary>
    public interface ISpCoreControllerStateObserver
    {
        /// <summary>
        /// Called when the controller is online
        /// </summary>
        /// <param name="state"></param>
        void OnOnline(ISpCoreControllerState state);

        /// <summary>
        /// Called when the controller is offline.
        /// </summary>
        /// <param name="state"></param>
        void OnOffline(ISpCoreControllerState state);

        /// <summary>
        /// Called asynchronously with the result of a DbChange
        /// </summary>
        /// <param name="state"></param>
        /// <param name="dbChange"></param>
        /// <param name="dbChangeResp"></param>
        void OnDbChangeResp(ISpCoreControllerState state, DbChange dbChange, DbChangeResp dbChangeResp);

        /// <summary>
        /// Called asynchronously with the result of a DevActionReq
        /// </summary>
        /// <param name="state"></param>
        /// <param name="devActionReq"></param>
        /// <param name="devActionResp"></param>
        void OnDevActionResp(ISpCoreControllerState state, DevActionReq devActionReq, DevActionResp devActionResp);

        /// <summary>
        /// Called with events as they arrive from the controller.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="evts"></param>
        void OnEvts(ISpCoreControllerState state, List<Evt> evts);
    }
}
