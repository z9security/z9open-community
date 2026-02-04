using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Z9.Protobuf
{
    public class SpCoreMessageHeader
    {
        public static readonly short STANDARD_HEADER_BYTES = 0x3e4b;
        public static readonly int MAX_LENGTH = 0x0001ffff; // 131071

        public short HeaderBytes;
        public int Length;
    }
}
