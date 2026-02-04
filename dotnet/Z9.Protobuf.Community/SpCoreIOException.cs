using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Z9.Protobuf
{
    class SpCoreIOException : IOException
    {
        public SpCoreIOException()
        {
        }
        public SpCoreIOException(string message) : base(message)
        {
        }
        public SpCoreIOException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
