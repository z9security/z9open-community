
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    /// <summary>
    /// An observer for SpCoreControllerStateMgr
    /// </summary>
    public interface ISpCoreControllerMgrObserver
    {
        /// <summary>
        /// called when an unknown controller calls in.
        /// </summary>
        /// <param name="id"></param>
        void OnUnknownControllerRejected(Identification identification, UnknownControllerRejectedOptions options);
    }
}
