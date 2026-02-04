using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Z9.Protobuf
{
    /// <summary>
    /// Thrown when a controller sends us a TERMINATE message, which will cause us to disconnect.
    /// </summary>
    class SpCoreReceivedTerminateMessageException : Exception
    {
        public Spcore.Proto.TerminationReason Reason { get; set; }

        public SpCoreReceivedTerminateMessageException(Spcore.Proto.TerminationReason reason)
        {
            Reason = reason;
        }
        public SpCoreReceivedTerminateMessageException(Spcore.Proto.TerminationReason reason, string message) : base(message)
        {
            Reason = reason;
        }
        public SpCoreReceivedTerminateMessageException(Spcore.Proto.TerminationReason reason, string message, Exception innerException) : base(message, innerException)
        {
            Reason = reason;
        }
    }
}
