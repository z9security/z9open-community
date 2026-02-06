using System;
using System.Collections.Generic;

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
    }
}
