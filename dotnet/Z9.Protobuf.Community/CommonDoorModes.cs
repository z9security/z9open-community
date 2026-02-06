using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    public class CommonDoorModes
    {
        /** Empty door mode (not valid) - can be used to mean "null" door mode. */
        public static DoorMode EMPTY() { return NewDoorMode(); }
        /** Unlocked - free access. */
        public static DoorMode STATIC_STATE_UNLOCKED() { return NewDoorMode(DoorModeStaticState.Unlocked, false, false, false); }
        /** No access. */
        public static DoorMode STATIC_STATE_LOCKED() { return NewDoorMode(DoorModeStaticState.Locked, false, false, false); }
        /** Card only. */
        public static DoorMode CARD_ONLY() { return NewDoorMode(DoorModeStaticState.AccessControlled, false, true, false); }
        /** Card and PIN. */
        public static DoorMode CARD_AND_CONFIRMING_PIN() { return NewDoorMode(DoorModeStaticState.AccessControlled, false, true, true); }
        /** PIN only. */
        public static DoorMode UNIQUE_PIN_ONLY() { return NewDoorMode(DoorModeStaticState.AccessControlled, true, false, false); }
        /** Card or PIN. */
        public static DoorMode CARD_ONLY_OR_UNIQUE_PIN() { return NewDoorMode(DoorModeStaticState.AccessControlled, true, true, false); }

        public static DoorMode NewDoorMode()
        {
            DoorMode result = new DoorMode();
            return result;
        }

        public static DoorMode NewDoorMode(
            DoorModeStaticState staticState,
            Boolean allowUniquePin,
            Boolean allowCard,
            Boolean requireConfirmingPinWithCard)
        {
            DoorMode result = new DoorMode();
            result.StaticState = staticState;
            result.AllowUniquePin = allowUniquePin;
            result.AllowCard = allowCard;
            result.RequireConfirmingPinWithCard = requireConfirmingPinWithCard;
            return result;
        }

        /** All common door modes except EMPTY. */
        public static DoorMode[] ALL()
        {
            return new DoorMode[] {
                STATIC_STATE_UNLOCKED(),
                STATIC_STATE_LOCKED(),
                CARD_ONLY(),
                CARD_AND_CONFIRMING_PIN(),
                UNIQUE_PIN_ONLY(),
                CARD_ONLY_OR_UNIQUE_PIN(),
            };
        }

        public static Boolean RequiresPinPad(DoorModeType t)
        {
            switch (t)
            {
                case DoorModeType.CardAndConfirmingPin:
                case DoorModeType.UniquePinOnly:
                case DoorModeType.CardOnlyOrUniquePin:
                    return true;
                default:
                    return false;
            }
        }

        public static Boolean RequiresCardReader(DoorModeType t)
        {
            switch (t)
            {
                case DoorModeType.CardOnly:
                case DoorModeType.CardAndConfirmingPin:
                case DoorModeType.CardOnlyOrUniquePin:
                    return true;
                default:
                    return false;
            }
        }

        /** All common door modes except EMPTY, and ones requiring a PIN pad. */
        public static DoorMode[] NO_PIN_PAD()
        {
            return new DoorMode[] {
                STATIC_STATE_UNLOCKED(),
                STATIC_STATE_LOCKED(),
                CARD_ONLY(),
            };
        }

        /** String used in place of a common mode for command processing to reset to default. */
        public static readonly String RESET = "RESET";


        public static DoorMode ForDoorModeType(DoorModeType doorModeType)
        {
            switch (doorModeType)
            {
                case DoorModeType.StaticStateUnlocked: return STATIC_STATE_UNLOCKED();
                case DoorModeType.StaticStateLocked: return STATIC_STATE_LOCKED();
                case DoorModeType.CardOnly: return CARD_ONLY();
                case DoorModeType.CardAndConfirmingPin: return CARD_AND_CONFIRMING_PIN();
                case DoorModeType.UniquePinOnly: return UNIQUE_PIN_ONLY();
                case DoorModeType.CardOnlyOrUniquePin: return CARD_ONLY_OR_UNIQUE_PIN();
                default:
                    return null;
            }
        }

        /** So we can have consistent message keys for these common modes. */
        public static DoorModeType? DoorModeToDoorModeType(DoorMode doorMode)
        {
            if (doorMode == null)
                return null;
            if (SpCoreProtoUtil.IsEmpty(doorMode))
                return null;

            if (doorMode.StaticStateCase == DoorMode.StaticStateOneofCase.StaticState)
            {
                switch (doorMode.StaticState)
                {
                    case DoorModeStaticState.Locked:
                        return DoorModeType.StaticStateLocked;
                    case DoorModeStaticState.Unlocked:
                        return DoorModeType.StaticStateUnlocked;
                    default:
                        break;
                }
            }

            if (doorMode.AllowCardCase == DoorMode.AllowCardOneofCase.AllowCard && doorMode.AllowCard)
            {
                if (doorMode.RequireConfirmingPinWithCardCase == DoorMode.RequireConfirmingPinWithCardOneofCase.RequireConfirmingPinWithCard && doorMode.RequireConfirmingPinWithCard)
                {
                    return DoorModeType.CardAndConfirmingPin;
                }
                else if (doorMode.AllowUniquePinCase == DoorMode.AllowUniquePinOneofCase.AllowUniquePin && doorMode.AllowUniquePin)
                {
                    return DoorModeType.CardOnlyOrUniquePin;
                }
                else
                {
                    return DoorModeType.CardOnly;
                }
            }
            else if (doorMode.AllowUniquePinCase == DoorMode.AllowUniquePinOneofCase.AllowUniquePin && doorMode.AllowUniquePin)
            {
                return DoorModeType.UniquePinOnly;
            }
            else
            {
                return null; // should never happen
            }
        }

        public static DoorModeType? EvtCodeToDoorModeType(EvtCode evtCode)
        {
            switch (evtCode)
            {
                case EvtCode.DoorModeStaticStateUnlocked: return DoorModeType.StaticStateUnlocked;
                case EvtCode.DoorModeStaticStateLocked: return DoorModeType.StaticStateLocked;
                case EvtCode.DoorModeCardOnly: return DoorModeType.CardOnly;
                case EvtCode.DoorModeCardAndConfirmingPin: return DoorModeType.CardAndConfirmingPin;
                case EvtCode.DoorModeUniquePinOnly: return DoorModeType.UniquePinOnly;
                case EvtCode.DoorModeCardOnlyOrUniquePin: return DoorModeType.CardOnlyOrUniquePin;
                default:
                    return null;
            }
        }

        public static DoorMode DoorModeToClosestCommonDoorMode(DoorMode doorMode)
        {
            if (doorMode == null)
                return null;
            if (SpCoreProtoUtil.IsEmpty(doorMode))
                return null;

            DoorModeType? t = DoorModeToDoorModeType(doorMode);
            if (!t.HasValue)
                return null;
            return ForDoorModeType(t.Value);
        }

        public static bool AllowsCardOnly(DoorModeType doormode)
        {
            switch (doormode)
            {
                case DoorModeType.CardOnly:
                case DoorModeType.CardOnlyOrUniquePin:
                    return true;
                default:
                    return false;
            }
        }
    }
}
