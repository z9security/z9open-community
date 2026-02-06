using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    public class CommonCredTemplates
    {
        static readonly bool DEFAULT_UNIQUE_PIN = true;
        static readonly string CARD_PIN_ANY_DATA_LAYOUT_TAG = "CARD_PIN";

        public static CredTemplate CARD_PIN()
        {
            CredTemplate result = new CredTemplate();
            SpCoreProtoUtil.InitRequired(result);
            result.CardPinTemplate = new CardPinTemplate();
            SpCoreProtoUtil.InitRequired(result.CardPinTemplate);

            result.Name = Messages.GetString("CredTemplate.CARD_PIN");
            result.Tag = CARD_PIN_ANY_DATA_LAYOUT_TAG;

            result.CardPinTemplate.CredComponentPresence = CredComponentPresence.Required;
            result.CardPinTemplate.CredNumPresence = CredComponentPresence.Required;
            result.CardPinTemplate.PinPresence = CredComponentPresence.Optional;
            result.CardPinTemplate.PinUnique = DEFAULT_UNIQUE_PIN;

            result.CardPinTemplate.AnyDataLayout = true;
            return result;
        }
    }
}
