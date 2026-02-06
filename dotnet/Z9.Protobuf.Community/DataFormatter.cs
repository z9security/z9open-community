using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    public abstract class DataFormatter
    {
        public abstract DecodedRead Decode(BitBuffer bb) /* throws DataFormatterException */;
	    public abstract void Encode(BitBuffer bb, DecodedRead value) /*throws DataFormatterException*/ ;

	    /**
	     * Factory method to construct a {@link DataFormatter} given a {@link DataFormat}.
	     * 
	     * @param keyLookup only used for {@link SmartFormat}/{@link SmartFormatter}.
	     */
	    public static DataFormatter ForDataFormat(DataFormat dataFormat)
	    {
            if (dataFormat.DataFormatTypeCase == DataFormat.DataFormatTypeOneofCase.DataFormatType)
                throw new Exception("no DataFormatType");
		    if (dataFormat.DataFormatType == DataFormatType.Binary)
		    {	return new BinaryFormatter(dataFormat);
		    }
		    else
			    throw new ArgumentException("Unknown or unsupported data format (community edition only supports Binary): " + dataFormat);
	    }
    }
}
