using System.Text;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    /// <summary>
    /// String formatting utilities.
    /// </summary>
    public class StringFormatter
    {
        public static string EvtToStr(Evt value)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(value.HwTime);
            builder.Append(" - ");

            builder.Append(EvtCodeAndSubCodeToStr(value));

            if (value.EvtDevRef != null && value.EvtDevRef.NameCase == EvtDevRef.NameOneofCase.Name)
            {
                builder.Append(" - ");
                builder.Append(value.EvtDevRef.Name);
            }

            if (value.DataCase == Evt.DataOneofCase.Data && !string.IsNullOrEmpty(value.Data))
            {
                builder.Append(" - ");
                builder.Append(value.Data);
            }
            return builder.ToString();
        }

        public static string EvtCodeAndSubCodeToStr(Evt value)
        {
            StringBuilder builder = new StringBuilder();

            if (value.EvtCodeCase == Evt.EvtCodeOneofCase.EvtCode)
            {
                if (value.EvtCode == EvtCode.External && value.ExternalEvtCodeTextCase == Evt.ExternalEvtCodeTextOneofCase.ExternalEvtCodeText)
                {
                    builder.Append(value.ExternalEvtCodeText);
                }
                else
                {
                    builder.Append(value.EvtCode.ToString());
                }
            }

            if (value.EvtSubCodeCase == Evt.EvtSubCodeOneofCase.EvtSubCode)
            {
                builder.Append(" (");

                if (value.EvtSubCode == EvtSubCode.External && value.ExternalSubCodeTextCase == Evt.ExternalSubCodeTextOneofCase.ExternalSubCodeText)
                {
                    builder.Append(value.ExternalSubCodeText);
                }
                else
                {
                    builder.Append(value.EvtSubCode.ToString());
                }
                builder.Append(")");
            }

            return builder.ToString();
        }

        public static string GetNameOrDetails(EvtCredRef evtCredRef)
        {
            if (evtCredRef == null)
                return null;

            if (evtCredRef.NameCase == EvtCredRef.NameOneofCase.Name)
                return evtCredRef.Name;
            if (evtCredRef.CredNum != null)
                return SpCoreProtoUtil.ToBigInteger(evtCredRef.CredNum).ToString();

            return null;
        }

        public static string FormatName(string last, string first, string middle)
        {
            StringBuilder builder = new StringBuilder();
            if (last != null)
                builder.Append(last);
            if (!string.IsNullOrEmpty(first) || !string.IsNullOrEmpty(middle))
            {
                if (!string.IsNullOrEmpty(last))
                    builder.Append(", ");
                if (!string.IsNullOrEmpty(first))
                    builder.Append(first);
                if (!string.IsNullOrEmpty(middle))
                {
                    if (!string.IsNullOrEmpty(first))
                        builder.Append(" ");
                    builder.Append(middle);
                }
            }

            return builder.ToString();
        }

        public static string DevTypeAndSubTypeAndUseToStr(Dev value)
        {
            if (value.DevUseCase == Dev.DevUseOneofCase.DevUse)
                return value.DevUse.ToString();

            if (value.DevSubTypeCase == Dev.DevSubTypeOneofCase.DevSubType)
                return value.DevSubType.ToString();

            if (value.DevTypeCase == Dev.DevTypeOneofCase.DevType)
                return value.DevType.ToString();

            return "";
        }
    }
}
