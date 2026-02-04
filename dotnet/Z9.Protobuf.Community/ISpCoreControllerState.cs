using System;
using System.Collections.Generic;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    /// <summary>
    /// The library's representation of the state of the connection to a controller.
    /// </summary>
    public interface ISpCoreControllerState
    {
        /// <summary>
        /// Whether the controller corresponding to this state is online
        /// </summary>
        /// <returns>true if online, false otherwise</returns>
        bool IsOnline();

        /// <summary>
        /// Whether this state is in the process of stopping, or stopped.
        /// </summary>
        /// <returns>true if stopping/stopped, false otherwise</returns>
        bool IsStopping();

        /// <summary>
        /// Get log prefix for logging.
        /// </summary>
        /// <returns>the log prefix</returns>
        string GetLogPrefix();

        Identification GetIdentification();

        /// <summary>
        /// Perform a DB change on the controller.  The result will come back asynchronously in the observer's OnDbChangeResp method.
        /// </summary>
        /// <param name="dbChange"></param>
        void DbChange(DbChange dbChange);

        /// <summary>
        /// Execute a dev action on the controller.  The result will come back asynchronously in the observer's OnDevActionResp method.
        /// </summary>
        /// <param name="devActionReq"></param>
        void DevActionReq(DevActionReq devActionReq);

        /// <summary>
        /// Tells the controller to start sending events.
        /// </summary>
        void StartEvts();

        /// <summary>
        /// Tells the controller to consume (mark as consumed) the events with the specified unids.
        /// </summary>
        /// <param name="unids"></param>
        void ConsumeEvts(HashSet<Int64> unids);

        /// <summary>
        /// Stop communicating with the controller, terminate the use of this object.
        /// </summary>
        void Stop();
    }
}
