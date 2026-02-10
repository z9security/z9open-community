using System;
using System.Collections.Generic;
using System.Reflection;
using Google.Protobuf.Reflection;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    public class Messages
    {
        private static readonly Dictionary<string, string> Strings = new Dictionary<string, string>
        {
            // CommonDataFormats
            { "CommonDataFormats._26_BIT_WIEGAND_RAW", "26-bit Wiegand (Raw)" },
            { "CommonDataFormats._26_BIT_WIEGAND_RAW_NO_PARITY", "26-bit Wiegand (Raw, No Parity Check)" },
            { "CommonDataFormats._26_BIT_WIEGAND_WITH_FACILITY_CODE", "26-bit Wiegand w/Facility Code" },
            { "CommonDataFormats._32_BIT_RAW", "32-bit (Raw)" },
            { "CommonDataFormats._34_BIT_WIEGAND_RAW", "34-bit Wiegand (Raw)" },
            { "CommonDataFormats._34_BIT_WIEGAND_WITH_FACILITY_CODE", "34-bit Wiegand w/Facility Code" },
            { "CommonDataFormats._35_BIT_CORPORATE_1000_WITH_FACILITY_CODE", "35-bit Corporate 1000" },
            { "CommonDataFormats._37_BIT_WIEGAND_RAW", "37-bit Wiegand (Raw)" },
            { "CommonDataFormats._37_BIT_WIEGAND_WITH_FACILITY_CODE", "37-bit Wiegand w/Facility Code" },
            { "CommonDataFormats._40_BIT_RAW", "40-bit (Raw)" },
            { "CommonDataFormats._48_BIT_CORPORATE_1000_RAW", "48-bit Corporate 1000 (Raw)" },
            { "CommonDataFormats._48_BIT_CORPORATE_1000_WITH_FACILITY_CODE", "48-bit Corporate 1000" },
            { "CommonDataFormats._50_BIT_WIEGAND_WITH_FACILITY_CODE", "50-bit Wiegand w/Facility Code" },
            { "CommonDataFormats._64_BIT_RAW", "64-bit (Raw)" },
            { "CommonDataFormats.CORPORATE_1000_WITH_FACILITY_CODE", "35-bit Corporate 1000" },
            // CredTemplates
            { "CredTemplate.CARD_PIN", "Card and PIN" },
            // EvtCode
            { "EvtCode.CONTROLLER_STARTUP", "Controller Startup" },
            { "EvtCode.CONTROLLER_ONLINE", "Controller Online" },
            { "EvtCode.CONTROLLER_OFFLINE", "Controller Offline" },
            { "EvtCode.CRED_READER_ONLINE", "Reader Online" },
            { "EvtCode.CRED_READER_OFFLINE", "Reader Offline" },
            { "EvtCode.RAW_CRED_READ", "Card Read" },
            { "EvtCode.DOOR_ACCESS_GRANTED", "Access Granted" },
            { "EvtCode.DOOR_ACCESS_DENIED", "Access Denied" },
            { "EvtCode.DOOR_FORCED", "Door Forced Open" },
            { "EvtCode.DOOR_NOT_FORCED", "Door Forced Open Restored" },
            { "EvtCode.DOOR_HELD", "Door Held Open" },
            { "EvtCode.DOOR_NOT_HELD", "Door Held Open Restored" },
            { "EvtCode.DOOR_OPENED", "Door Opened" },
            { "EvtCode.DOOR_CLOSED", "Door Closed" },
            { "EvtCode.DOOR_LOCKED", "Door Locked" },
            { "EvtCode.DOOR_UNLOCKED", "Door Unlocked" },
            { "EvtCode.DOOR_MODE_STATIC_STATE_UNLOCKED", "Access Mode: Unlocked" },
            { "EvtCode.DOOR_MODE_STATIC_STATE_LOCKED", "Access Mode: No Access" },
            { "EvtCode.DOOR_MODE_CARD_ONLY", "Access Mode: Card Only" },
            { "EvtCode.DOOR_MODE_CARD_AND_CONFIRMING_PIN", "Access Mode: Card and PIN" },
            { "EvtCode.DOOR_MODE_UNIQUE_PIN_ONLY", "Access Mode: PIN Only" },
            { "EvtCode.DOOR_MODE_CARD_ONLY_OR_UNIQUE_PIN", "Access Mode: Card or PIN" },
            { "EvtCode.EXIT_REQUESTED", "Exit Requested" },
            { "EvtCode.MOMENTARY_UNLOCK", "Access Point Momentarily Unlocked" },
            { "EvtCode.POWER_PRIMARY", "On Main Power" },
            { "EvtCode.POWER_OFF_PRIMARY", "Off Main Power" },
            { "EvtCode.POWER_NONE", "No Power" },
            { "EvtCode.BATTERY_OK", "Battery Restored" },
            { "EvtCode.BATTERY_LOW", "Battery Low" },
            { "EvtCode.BATTERY_FAIL", "Battery Failure" },
            { "EvtCode.TAMPER_NORMAL", "Tamper Restored" },
            { "EvtCode.TAMPER", "Tamper" },
            { "EvtCode.SCHED_ACTIVE", "Schedule Active" },
            { "EvtCode.SCHED_INACTIVE", "Schedule Inactive" },
            { "EvtCode.EXTERNAL", "External Event" },
            { "EvtCode.CRED_READER_POWER_CYCLE", "Reader Power Cycled" },
            { "EvtCode.BATTERY_CRITICAL", "Battery Critical" },
            // EvtSubCode
            { "EvtSubCode.ACCESS_DENIED_INACTIVE", "Inactive" },
            { "EvtSubCode.ACCESS_DENIED_NOT_EFFECTIVE", "Not Yet Effective" },
            { "EvtSubCode.ACCESS_DENIED_EXPIRED", "Expired" },
            { "EvtSubCode.ACCESS_DENIED_NO_PRIV", "No Privileges" },
            { "EvtSubCode.ACCESS_DENIED_OUTSIDE_SCHED", "Outside Schedule" },
            { "EvtSubCode.ACCESS_DENIED_UNKNOWN_CRED_NUM", "Unknown Card" },
            { "EvtSubCode.ACCESS_DENIED_UNKNOWN_CRED_NUM_FORMAT", "Unknown Format" },
            { "EvtSubCode.ACCESS_DENIED_UNKNOWN_CRED_UNIQUE_PIN", "Unknown PIN" },
            { "EvtSubCode.ACCESS_DENIED_INCORRECT_FACILITY_CODE", "Incorrect Facility Code" },
            { "EvtSubCode.ACCESS_DENIED_DOOR_MODE_STATIC_LOCKED", "No Access" },
            { "EvtSubCode.ACCESS_DENIED_DOOR_MODE_DOESNT_ALLOW_CARD", "No Card Access" },
            { "EvtSubCode.ACCESS_DENIED_DOOR_MODE_DOESNT_ALLOW_UNIQUE_PIN", "No PIN Access" },
            { "EvtSubCode.ACCESS_DENIED_NO_CONFIRMING_PIN_FOR_CRED", "No Confirming PIN Defined" },
            { "EvtSubCode.ACCESS_DENIED_INCORRECT_CONFIRMING_PIN", "Incorrect Confirming PIN" },
            { "EvtSubCode.EXTERNAL", "Other" },
        };

        public static string GetString(string key)
        {
            if (Strings.TryGetValue(key, out var value))
                return value;
            // Fallback: return key with formatting
            int dot = key.LastIndexOf('.');
            if (dot >= 0)
                key = key.Substring(dot + 1);
            return key.Replace("_", " ").Trim();
        }

        public static string GetEvtCodeText(EvtCode evtCode)
        {
            var key = GetEnumKey(evtCode);
            return key != null ? GetString(key) : evtCode.ToString();
        }

        public static string GetEvtSubCodeText(EvtSubCode evtSubCode)
        {
            var key = GetEnumKey(evtSubCode);
            return key != null ? GetString(key) : evtSubCode.ToString();
        }

        private static string GetEnumKey<T>(T value) where T : Enum
        {
            var member = typeof(T).GetField(value.ToString());
            var attr = member?.GetCustomAttribute<OriginalNameAttribute>();
            if (attr == null) return null;
            // Convert "EvtCode_CONTROLLER_STARTUP" to "EvtCode.CONTROLLER_STARTUP"
            var idx = attr.Name.IndexOf('_');
            if (idx < 0) return attr.Name;
            return attr.Name.Substring(0, idx) + "." + attr.Name.Substring(idx + 1);
        }
    }
}
