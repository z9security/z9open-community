using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    public class CommonDataFormats
    {

        /** Raw 26-bit Wiegand.  All 24 data bits included in credential number. */
	   public static DataFormat _26_BIT_WIEGAND_RAW() {return NewBinaryFormat(Messages.GetString("CommonDataFormats._26_BIT_WIEGAND_RAW"), 26, 26,
			    new BinaryElement[] {
                    NewFieldBinaryElement(1, 24, DataFormatField.CredNum),
			        NewParityBinaryElement(0, false, 1, 12), // even parity on first 12 bits
			        NewParityBinaryElement(26 - 1, true, 26 - 12 - 1, 12) // odd parity on last 12 bits
                }
			    );}

	    /** Raw 26-bit Wiegand, partity not checked.  All 26 data bits included in credential number. */
	   public static DataFormat _26_BIT_WIEGAND_RAW_NO_PARITY() {return NewBinaryFormat(Messages.GetString("CommonDataFormats._26_BIT_WIEGAND_RAW_NO_PARITY"), 26, 26,
			    new BinaryElement[] {NewFieldBinaryElement(0, 26, DataFormatField.CredNum)}
			    );}

	    /** 26-bit Wiegand with facility code.  8 facility code bits, 16 credential number bits. */
	   public static DataFormat _26_BIT_WIEGAND_WITH_FACILITY_CODE() {return NewBinaryFormat(Messages.GetString("CommonDataFormats._26_BIT_WIEGAND_WITH_FACILITY_CODE"), 26, 26,
			    new BinaryElement[] {
                    NewFieldBinaryElement(1, 8, DataFormatField.FacilityCode),
			        NewFieldBinaryElement(1 + 8, 16, DataFormatField.CredNum),
			        NewParityBinaryElement(0, false, 1, 12), // even parity on first 12 bits
			        NewParityBinaryElement(26 - 1, true, 26 - 12 - 1, 12) // odd parity on last 12 bits
                }
			    );}

	    /**
	     * 40-bit raw.  This can be used for EM 4001 tags - the 40 bits sent by and ID-12 or compatible serial reader, without the checksum.
	     * See <a href="http://www.sparkfun.com/datasheets/Sensors/ID-12-Datasheet.pdf">http://www.sparkfun.com/datasheets/Sensors/ID-12-Datasheet.pdf</a>
	     */
	   public static DataFormat _40_BIT_RAW() {return NewBinaryFormat(Messages.GetString("CommonDataFormats._40_BIT_RAW"), 40, 40, new BinaryElement[] {NewFieldBinaryElement(0, 40, DataFormatField.CredNum)});}

	    /**
	     * 32-bit raw.  This can be used for the 32 bits sent as the CSN (UID) for MIFARE cards (Classic 1k and Classic 4k, for example) which use a 4-byte CSN (UID).
	     */
	    public static DataFormat _32_BIT_RAW() {return NewBinaryFormat(Messages.GetString("CommonDataFormats._32_BIT_RAW"), 32, 32, new BinaryElement[] {NewFieldBinaryElement(0, 32, DataFormatField.CredNum)});}

	    /**
	     * 64-bit raw.
	     */
	    public static DataFormat _64_BIT_RAW() {return NewBinaryFormat(Messages.GetString("CommonDataFormats._64_BIT_RAW"), 64, 64, new BinaryElement[] {NewFieldBinaryElement(0, 64, DataFormatField.CredNum)});}


        // TODO: 34/35/37-bit formats.

        /** Raw 37-bit Wiegand, aka H10302.  All 35 data bits included in credential number. */
        public static DataFormat _37_BIT_WIEGAND_RAW()
        {
            return NewBinaryFormat(Messages.GetString("CommonDataFormats._37_BIT_WIEGAND_RAW"), 37, 37,
                new BinaryElement[]
                {
                    NewFieldBinaryElement(1, 35, DataFormatField.CredNum),
                    NewParityBinaryElement(0, false, 1, 18), // even parity on first 18 bits
                    NewParityBinaryElement(37 - 1, true, 37 - 18 - 1, 18) // odd parity on last 18 bits
                }
               );
        }

        /** 37-bit Wiegand with Facility Code, aka H10304 */
        /** 37-bit Wiegand with facility code. parity not checked.  16 facility code bits, 19 credential number bits. */
        /** Matches three iClass W16K CH cards that R. Rozwod has which all appear to be from same batch  */
        /** Second set of 8 bits all have different values on these cards (so not used in this format), */
        /** But I do think a "standard" 37 bit format would have 16 FC bits... */
        /** but first set of 8 are same (bits 1 through  9 for FC) and the 19 credential number bits match up with what is stamped on the cards */
        public static DataFormat _37_BIT_WIEGAND_WITH_FACILITY_CODE() {return NewBinaryFormat(Messages.GetString("CommonDataFormats._37_BIT_WIEGAND_WITH_FACILITY_CODE"), 37, 37,
			    new BinaryElement[]
                {   NewFieldBinaryElement(1, 16, DataFormatField.FacilityCode),
			        NewFieldBinaryElement(17, 19, DataFormatField.CredNum),
                    NewParityBinaryElement(0, false, 1, 18), // even parity on first 18 bits
			        NewParityBinaryElement(37 - 1, true, 37 - 18 - 1, 18) // odd parity on last 18 bits
                }
                );}

        /** 48 bit Corporate 1000, 3 parity bits. 22 Facility Code bits, 23 credential number bits */
        public static DataFormat _48_BIT_CORPORATE_1000_WITH_FACILITY_CODE()
        {
            return NewBinaryFormat(Messages.GetString("CommonDataFormats._48_BIT_CORPORATE_1000_WITH_FACILITY_CODE"), 48, 48,
                    new BinaryElement[] {
                        NewFieldBinaryElement(2, 22, DataFormatField.FacilityCode),
                        NewFieldBinaryElement(24, 23, DataFormatField.CredNum),
                        NewParityBinaryElement(1, false, 3, 44, "11011011011011011011011011011011011011011011"), // even parity with mask
                        NewParityBinaryElement(48 - 1, true, 2, 44, "11011011011011011011011011011011011011011011"), // odd parity with mask
                        NewParityBinaryElement(0, true, 1, 47) // odd parity on everything
                    }
                );
        }

        /** 48 bit Corporate 1000, 3 parity bits. All 45 data bits included in credential number. */
        public static DataFormat _48_BIT_CORPORATE_1000_RAW()
        {
            return NewBinaryFormat(Messages.GetString("CommonDataFormats._48_BIT_CORPORATE_1000_RAW"), 48, 48,
                    new BinaryElement[] {
                        NewFieldBinaryElement(2, 45, DataFormatField.CredNum),
                        NewParityBinaryElement(1, false, 3, 44, "11011011011011011011011011011011011011011011"), // even parity with mask
                        NewParityBinaryElement(48 - 1, true, 2, 44, "11011011011011011011011011011011011011011011"), // odd parity with mask
                        NewParityBinaryElement(0, true, 1, 47) // odd parity on everything
                    }
                );
        }

        /** 35-bit Corporate 1000 with facility code.  12 facility code bits, 20 credential number bits - See: http://www.pagemac.com/azure/data_formats.php */
        public static DataFormat CORPORATE_1000_WITH_FACILITY_CODE()
	    {
            return NewBinaryFormat(Messages.GetString("CommonDataFormats.CORPORATE_1000_WITH_FACILITY_CODE"), 35, 35,
                    new BinaryElement[]
                    {
			            NewFieldBinaryElement(2, 12, DataFormatField.FacilityCode),
			            NewFieldBinaryElement(2 + 12, 20, DataFormatField.CredNum),
			            NewParityBinaryElement(1, false, 2, 32, "11011011011011011011011011011011"), // even parity with mask
			            NewParityBinaryElement(35 - 1, true, 1, 33, "110110110110110110110110110110110"), // odd parity with mask
			            NewParityBinaryElement(0, true, 1, 34) // odd parity on everything
                    }
			        );
	    }

        /** 35-bit Corporate 1000 with facility code.  12 facility code bits, 20 credential number bits - See: http://www.pagemac.com/azure/data_formats.php */
        public static DataFormat _35_BIT_CORPORATE_1000_WITH_FACILITY_CODE()
        {
            return NewBinaryFormat(Messages.GetString("CommonDataFormats._35_BIT_CORPORATE_1000_WITH_FACILITY_CODE"), 35, 35,
                    new BinaryElement[]
                    {
                        NewFieldBinaryElement(2, 12, DataFormatField.FacilityCode),
                        NewFieldBinaryElement(14, 20, DataFormatField.CredNum),
                        NewParityBinaryElement(1, false, 2, 32, "11011011011011011011011011011011"), // even parity with mask
                        NewParityBinaryElement(35 - 1, true, 1, 33, "110110110110110110110110110110110"), // odd parity with mask
                        NewParityBinaryElement(0, true, 1, 34) // odd parity on everything
                    }
                );
        }

        /** 50-bit Wiegand with Facility Code. */
        public static DataFormat _50_BIT_WIEGAND_WITH_FACILITY_CODE()
        {
            return NewBinaryFormat(Messages.GetString("CommonDataFormats._50_BIT_WIEGAND_WITH_FACILITY_CODE"), 50, 50,
                new BinaryElement[]
                    {
                        NewFieldBinaryElement(1, 16, DataFormatField.FacilityCode),
                        NewFieldBinaryElement(17, 32, DataFormatField.CredNum),
                        NewParityBinaryElement(0, false, 1, 24), // even parity on first 24 bits
                        NewParityBinaryElement(50 - 1, true, 25, 24) // odd parity on last 24 bits
                    }
                );
        }

        /** 34-bit Wiegand raw. */
        public static DataFormat _34_BIT_WIEGAND_RAW()
        {
            return NewBinaryFormat(Messages.GetString("CommonDataFormats._34_BIT_WIEGAND_RAW"), 34, 34,
                new BinaryElement[]
                    {
                        NewFieldBinaryElement(1, 32, DataFormatField.CredNum),
                        NewParityBinaryElement(0, false, 1, 16), // even parity on first 16 bits
                        NewParityBinaryElement(34 - 1, true, 17, 16) // odd parity on last 16 bits
                    }
                );
        }

        /** 34-bit Wiegand with Facility Code, aka H10306. */
        public static DataFormat _34_BIT_WIEGAND_WITH_FACILITY_CODE()
        {
            return NewBinaryFormat(Messages.GetString("CommonDataFormats._34_BIT_WIEGAND_WITH_FACILITY_CODE"), 34, 34,
                new BinaryElement[]
                    {
                        NewFieldBinaryElement(1, 16, DataFormatField.FacilityCode),
                        NewFieldBinaryElement(17, 16, DataFormatField.CredNum),
                        NewParityBinaryElement(0, false, 1, 16), // even parity on first 16 bits
                        NewParityBinaryElement(34 - 1, true, 17, 16) // odd parity on last 16 bits
                    }
                );
        }


        public static DataFormat NewBinaryFormat(String name, int minBits, int maxBits, BinaryElement[] binaryElements)
        {
            DataFormat result = new DataFormat();
            BinaryFormat ext = result.ExtBinaryFormat = new BinaryFormat();
            result.DataFormatType = DataFormatType.Binary;
            result.Name = name;
            ext.MinBits = minBits;
            ext.MaxBits = maxBits;
            ext.SupportReverseRead = false;
            int index = 0;
            foreach (BinaryElement e in binaryElements)
            {
                e.Num = index++;
                ext.Elements.Add(e);
            }
            return result;
        }

        public static BinaryElement NewFieldBinaryElement(int start, int len, DataFormatField field)
        {
            BinaryElement result = new BinaryElement();
            result.Type = BinaryElementType.Parity;
            FieldBinaryElement ext = result.ExtFieldBinaryElement = new FieldBinaryElement();
            result.Type = BinaryElementType.Field;
            result.Start = start;
            result.Len = len;
            ext.Field = field;
            return result;
        }

        public static BinaryElement NewParityBinaryElement(int pos, bool odd, int srcStart, int srcLen)
        {
            BinaryElement result = new BinaryElement();
            result.Type = BinaryElementType.Parity;
            ParityBinaryElement ext = result.ExtParityBinaryElement = new ParityBinaryElement();
            result.Start = pos;
            result.Len = 1;
            ext.Odd = odd;
            ext.SrcStart = srcStart;
            ext.SrcLen = srcLen;
            return result;
        }

        public static BinaryElement NewParityBinaryElement(int pos, bool odd, int srcStart, int srcLen, String mask)
	    {
		    BinaryElement result = new BinaryElement();
            result.Type = BinaryElementType.Parity;
            ParityBinaryElement ext = result.ExtParityBinaryElement = new ParityBinaryElement();
            result.Start = pos;
            result.Len = 1;
            ext.Odd = odd;
            ext.SrcStart = srcStart;
            ext.SrcLen = srcLen;
            ext.Mask = mask;
            return result;
        }

	    public static bool Equivalent(/*@NonNull*/ DataFormat a, /*@NonNull*/ DataFormat b)
	    {
            if (a.DataFormatTypeCase != b.DataFormatTypeCase)
                return false;
            if (a.DataFormatTypeCase != DataFormat.DataFormatTypeOneofCase.DataFormatType)
                return true;
		    if (a.DataFormatType != b.DataFormatType)
			    return false;
		    switch (a.DataFormatType)
		    {
			    case DataFormatType.Binary:
				    return Equivalent(a.ExtBinaryFormat, b.ExtBinaryFormat);
			    default:
				    return false;
		    }
	    }

	    public static bool Equivalent(/*@NonNull*/ BinaryFormat a, /*@NonNull*/ BinaryFormat b)
	    {
            if (a.MinBits != b.MinBits)
		       return false;
            if (a.MaxBits != b.MaxBits)
		       return false;
            if ((a.Elements == null) != (b.Elements == null))
                return false;
            if (a.Elements != null)
            {
		        if (a.Elements.Count != b.Elements.Count)
			        return false;

		        // TODO: be independent of the order
		        for (int i = 0; i < a.Elements.Count; ++i)
		        {
			        BinaryElement elementA = a.Elements[i];
			        BinaryElement elementB = b.Elements[i];
			        if (!Equivalent(elementA, elementB))
				        return false;
		        }
            }
		    return true;
	    }

	    static bool Equivalent(/*@NonNull*/ BinaryElement a, /*@NonNull*/ BinaryElement b)
	    {
            if (a.TypeCase != b.TypeCase)
                return false;
            if (a.TypeCase != BinaryElement.TypeOneofCase.Type)
                return true;
		    if (a.Type != b.Type)
			    return false;
            if (a.Start != b.Start)
                return false;
            if (a.Len != b.Len)
                return false;
            switch (a.Type)
		    {
			    case BinaryElementType.Static:
				    return Equivalent(a.ExtStaticBinaryElement, b.ExtStaticBinaryElement);
			    case BinaryElementType.Parity:
				    return Equivalent((ParityBinaryElement) a.ExtParityBinaryElement, (ParityBinaryElement) b.ExtParityBinaryElement);
			    case BinaryElementType.Field:
				    return Equivalent((FieldBinaryElement) a.ExtFieldBinaryElement, (FieldBinaryElement) b.ExtFieldBinaryElement);
				default:
					throw new Exception();
		    }
	    }

	    static bool Equivalent(/*@NonNull*/ StaticBinaryElement a, /*@NonNull*/ StaticBinaryElement b)
	    {
            if (a.Value != b.Value)
			    return false;

		    return true;
	    }

	    static bool Equivalent(/*@NonNull*/ ParityBinaryElement a, /*@NonNull*/ ParityBinaryElement b)
	    {
            if (a.OddCase != b.OddCase)
                return false;
		    if (a.OddCase == ParityBinaryElement.OddOneofCase.Odd && a.Odd != b.Odd)
			    return false;
            if (a.SrcStartCase != b.SrcStartCase)
                return false;
		    if (a.SrcStartCase == ParityBinaryElement.SrcStartOneofCase.SrcStart && a.SrcStart != b.SrcStart)
			    return false;
            if (a.SrcLenCase != b.SrcLenCase)
                return false;
            if (a.SrcLenCase == ParityBinaryElement.SrcLenOneofCase.SrcLen && a.SrcLen != b.SrcLen)
			    return false;
            if (a.MaskCase != b.MaskCase)
                return false;
            if (a.MaskCase == ParityBinaryElement.MaskOneofCase.Mask && a.Mask != b.Mask)
                return false;

		    return true;
	    }

	    static bool Equivalent(/*@NonNull*/ FieldBinaryElement a, /*@NonNull*/ FieldBinaryElement b)
	    {
            if (a.FieldCase != b.FieldCase)
                return false;
            if (a.FieldCase == FieldBinaryElement.FieldOneofCase.Field && a.Field != b.Field)
                return false;

		    return true;
	    }

    }
}
