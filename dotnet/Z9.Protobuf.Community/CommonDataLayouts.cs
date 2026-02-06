using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    public class CommonDataLayouts
    {
        public static DataLayout ForDataFormat(DataFormat dataFormat)
        {
            if (dataFormat.UnidCase != DataFormat.UnidOneofCase.Unid)
                throw new ArgumentException("DataFormat has no Unid");
            DataLayout result = new DataLayout();
            SpCoreProtoUtil.InitRequired(result);
            result.LayoutType = DataLayoutType.Basic;
            result.ExtBasicDataLayout = new BasicDataLayout
            {
                DataFormatUnid = dataFormat.Unid
            };
            result.Name = dataFormat.Name;
            result.Enabled = true;
            return result;
        }

        public static DataLayout ForDataFormatUnid(int dataFormatUnid)
        {
            DataLayout result = new DataLayout();
            SpCoreProtoUtil.InitRequired(result);
            result.LayoutType = DataLayoutType.Basic;
            result.ExtBasicDataLayout = new BasicDataLayout
            {
                DataFormatUnid = dataFormatUnid
            };
            result.Name = "<<" + dataFormatUnid + ">>"; // placeholder - caller should set a better name
            result.Enabled = true;
            return result;
        }
    }
}
