using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    public class Logging
    {
        public static string DevRefToString(Dev value)
        {
	        string s;

            s = "Dev[";

	        if (value.DevTypeCase == Dev.DevTypeOneofCase.DevType)
		        s += " devType=" + value.DevType;

	        if (value.NameCase == Dev.NameOneofCase.Name)
		        s += " name=" + value.Name;

	        if (value.UnidCase == Dev.UnidOneofCase.Unid)
		        s += " unid=" + value.Unid;

	        if (value.TagCase == Dev.TagOneofCase.Tag)
		        s += " tag=" + value.Tag;

	        if (value.UuidCase == Dev.UuidOneofCase.Uuid)
		        s += " uuid=" + value.Uuid;

	        s += "]";

	        return s;
        }

        public static string EvtDevRefToString(EvtDevRef value)
        {
            string s;

            s = "EvtDevRef[";

            if (value.DevTypeCase == EvtDevRef.DevTypeOneofCase.DevType)
                s += " devType=" + value.DevType;

            if (value.NameCase == EvtDevRef.NameOneofCase.Name)
                s += " name=" + value.Name;

            if (value.UnidCase == EvtDevRef.UnidOneofCase.Unid)
                s += " unid=" + value.Unid;

            if (value.TagCase == EvtDevRef.TagOneofCase.Tag)
                s += " tag=" + value.Tag;

            if (value.UuidCase == EvtDevRef.UuidOneofCase.Uuid)
                s += " uuid=" + value.Uuid;

            s += "]";

            return s;
        }

        public static string EvtCredRefToString(EvtCredRef value)
        {
            string s;

            s = "EvtCredRef[";

            if (value.CredTemplateRef != null)
                s += " credTemplate=" + EvtCredTemplateRefToString(value.CredTemplateRef);

            if (value.NameCase == EvtCredRef.NameOneofCase.Name)
                s += " name=" + value.Name;

            if (value.CredNum != null)
                s += " credNum=" + SpCoreProtoUtil.ToBigInteger(value.CredNum);

            if (value.FacilityCodeCase == EvtCredRef.FacilityCodeOneofCase.FacilityCode)
                s += " facilityCode=" + value.FacilityCode;

            if (value.UnidCase == EvtCredRef.UnidOneofCase.Unid)
                s += " unid=" + value.Unid;

            if (value.TagCase == EvtCredRef.TagOneofCase.Tag)
                s += " tag=" + value.Tag;

            if (value.UuidCase == EvtCredRef.UuidOneofCase.Uuid)
                s += " uuid=" + value.Uuid;

            s += "]";

            return s;
        }

        public static string EvtCredTemplateRefToString(EvtCredTemplateRef value)
        {
            string s;

            s = "EvtCredTemplateRef[";

            if (value.NameCase == EvtCredTemplateRef.NameOneofCase.Name)
                s += " name=" + value.Name;

            if (value.UnidCase == EvtCredTemplateRef.UnidOneofCase.Unid)
                s += " unid=" + value.Unid;

            if (value.TagCase == EvtCredTemplateRef.TagOneofCase.Tag)
                s += " tag=" + value.Tag;

            if (value.UuidCase == EvtCredTemplateRef.UuidOneofCase.Uuid)
                s += " uuid=" + value.Uuid;

            s += "]";

            return s;
        }

        public static string EvtToString(Evt evt)
        {
            string s;

            s = "Evt[";

            if (evt.UnidCase == Evt.UnidOneofCase.Unid)
                s += " unid=" + evt.Unid;

            if (evt.PriorityCase == Evt.PriorityOneofCase.Priority && evt.Priority != 0)
                s += " priority=" + evt.Priority;

            // TODO: fgColor, bgColor

            if (evt.EvtCodeCase == Evt.EvtCodeOneofCase.EvtCode)
                s += " code=" + evt.EvtCode;
            if (evt.ExternalEvtCodeTextCase == Evt.ExternalEvtCodeTextOneofCase.ExternalEvtCodeText)
                s += " [" + evt.ExternalEvtCodeText + "]";
            if (evt.ExternalEvtCodeIdCase == Evt.ExternalEvtCodeIdOneofCase.ExternalEvtCodeId)
                s += " externalevtcodeid=" + evt.ExternalEvtCodeId;

            if (evt.EvtSubCodeCase == Evt.EvtSubCodeOneofCase.EvtSubCode)
                s += " subCode=" + evt.EvtSubCode;
            if (evt.ExternalSubCodeTextCase == Evt.ExternalSubCodeTextOneofCase.ExternalSubCodeText)
                s += " [" + evt.ExternalSubCodeText + "]";


            if (evt.EvtModifiers != null)
                s += " modifiers=" + EvtModifiersToString(evt.EvtModifiers);

            if (evt.EvtDevRef != null)
                s += " dev=" + EvtDevRefToString(evt.EvtDevRef);

            if (evt.EvtControllerRef != null)
                s += " controller=" + EvtDevRefToString(evt.EvtControllerRef);

            if (evt.EvtCredRef != null)
                s += " cred=" + EvtCredRefToString(evt.EvtCredRef);

            if (evt.HwTime != null)
                s += " hwTime=[" + SpCoreProtoUtil.ToDateTime(evt.HwTime) + "]";

            if (evt.DataCase == Evt.DataOneofCase.Data)
                s += " data=" + evt.Data;

            s += "]";

            return s;
        }

        public static String EvtModifiersToString(EvtModifiers m)
        {
            string s;

            s = "[";

            if (m.UsedCardCase == EvtModifiers.UsedCardOneofCase.UsedCard && m.UsedCard)
                s += " usedCard=" + m.UsedCard;
            if (m.UsedPinCase == EvtModifiers.UsedPinOneofCase.UsedPin && m.UsedPin)
                s += " usedPin=" + m.UsedPin;

            s += "]";

            return s;
        }
    }
}
