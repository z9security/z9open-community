using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    public class BinaryFormatterException : Exception
    {
        public BinaryFormatterException()
        {

        }

        public BinaryFormatterException(string message) 
            : base(message)
        {
        }

        public BinaryFormatterException(string message, Exception cause)
            : base(message, cause)
        {
        }
        public BinaryFormatterException(Exception cause) 
            : base(cause.Message, cause)
        {
        }
    }

    public class BinaryFormatter : DataFormatter
    {
        private/*final*/ DataFormat format;

	    public BinaryFormatter(DataFormat format)
	    {
		    this.format = format;
	    }

	    public override DecodedRead Decode(BitBuffer bb) /*throws BinaryFormatterException*/
	    {
            BinaryFormat ext = format.ExtBinaryFormat;
		    try
		    {
			    return DoDecode(bb);
		    }
		    catch (BinaryFormatterException eForward)
		    {
			    if (ext.SupportReverseReadCase == BinaryFormat.SupportReverseReadOneofCase.SupportReverseRead && ext.SupportReverseRead)
			    {
				    try
				    {
					    return DoDecode(bb.Reverse());
				    }
				    catch (BinaryFormatterException /*eReverse*/)
				    {
					    throw eForward;
				    }
			    }
			    else
			    {
				    throw eForward;
			    }
		    }
	    }
	
	    private DecodedRead DoDecode(BitBuffer bb) /*throws BinaryFormatterException*/
	    {
            BinaryFormat ext = format.ExtBinaryFormat;

            int numBits = bb.GetSize();
		    if (numBits < ext.MinBits)
			    throw new BinaryFormatterException("Num bits (" + numBits + ") is less than minimum (" + ext.MinBits + ")");
		    if (numBits > ext.MaxBits)
			    throw new BinaryFormatterException("Num bits (" + numBits + ") is greater than maximum (" + ext.MaxBits + ")");
		
		    DecodedRead result = new DecodedRead();
		    foreach (BinaryElement binaryElement in ext.Elements)
		    {
			    DecodedReadElement decodedReadElement = DecodeElement(bb, binaryElement);
			    if (decodedReadElement != null)
			    {	
				    DecodedReadElement combineWith = null;
				
				    // if same field appears multiple times, we add them together.  Normally they would have shift values so the bits don't land on each other.
				    if (binaryElement.TypeCase == BinaryElement.TypeOneofCase.Type && binaryElement.Type == BinaryElementType.Field)
				    {
					    FieldBinaryElement fieldBinaryElement = binaryElement.ExtFieldBinaryElement;
					    foreach (DecodedReadElement existing in result.GetElements())
					    {
						    if (fieldBinaryElement.FieldCase == FieldBinaryElement.FieldOneofCase.Field && existing.GetField().HasValue && existing.GetField().Value == fieldBinaryElement.Field)
						    {
							    combineWith = existing;
							    break;
						    }
					    }
				    }
				
				    if (combineWith != null)
				    {
					    BigInteger newValue = BigInteger.Add(combineWith.GetValue(), decodedReadElement.GetValue());
					    combineWith.SetValue(newValue);
				    }
				    else
				    {
					    result.GetElements().Add(decodedReadElement);
				    }
			    }
		    }
		
		    return result;
	    }

	
	    public override void Encode(BitBuffer bb, DecodedRead value) /*throws BinaryFormatterException*/
	    {
            BinaryFormat ext = format.ExtBinaryFormat;

            int numBits = bb.GetSize();
		    if (numBits < ext.MinBits)
			    throw new BinaryFormatterException("Num bits (" + numBits + ") is less than minimum (" + ext.MinBits + ")");
		    if (numBits > ext.MaxBits)
			    throw new BinaryFormatterException("Num bits (" + numBits + ") is greater than maximum (" + ext.MaxBits + ")");
		
		    foreach (BinaryElement binaryElement in ext.Elements)
		    {
			    DecodedReadElement decodedReadElement = null; // only needed for FieldBinaryElement
			    if (binaryElement.TypeCase == BinaryElement.TypeOneofCase.Type && binaryElement.Type == BinaryElementType.Field)
			    {
				    FieldBinaryElement fieldBinaryElement = binaryElement.ExtFieldBinaryElement;
				    foreach (DecodedReadElement candidateDecodedReadElement in value.GetElements())
				    {
					    if (fieldBinaryElement.FieldCase == FieldBinaryElement.FieldOneofCase.Field && candidateDecodedReadElement.GetField().HasValue && fieldBinaryElement.Field == candidateDecodedReadElement.GetField().Value)
					    {	decodedReadElement = candidateDecodedReadElement;
						    break;
					    }
				    }
				
				    if (decodedReadElement == null)
					    throw new BinaryFormatterException("Missing field in DecodedRead: " + fieldBinaryElement.Field);
			    }
			    EncodeElement(bb, binaryElement, decodedReadElement);
		    }
	    }
	
	    static DecodedReadElement DecodeElement(/*@NonNull*/ BitBuffer bb, /*@NonNull*/ BinaryElement element) /*throws BinaryFormatterException*/
	    {
            if (element.TypeCase != BinaryElement.TypeOneofCase.Type)
                throw new ArgumentException("No type");
		    if (element.Type == BinaryElementType.Field)
			    return DecodeFieldBinaryElement(bb, element);
		    else if (element.Type == BinaryElementType.Parity)
			    DecodeParityBinaryElement(bb, element);
		    else if (element.Type == BinaryElementType.Static)
			    DecodeStaticBinaryElement(bb, element);
		    else
			    throw new ArgumentException("Unknown or unsupported BinaryElementType: " + element.Type);
		
		    return null;
	    }
	
	    static void EncodeElement(BitBuffer bb, BinaryElement element, DecodedReadElement value) /*throws BinaryFormatterException*/
	    {
             if (element.TypeCase != BinaryElement.TypeOneofCase.Type)
                throw new ArgumentException("No type");
		    if (element.Type == BinaryElementType.Field)
			    EncodeFieldBinaryElement(bb, element, value);
			else if (element.Type == BinaryElementType.Parity)
			    EncodeParityBinaryElement(bb, element);
			else if (element.Type == BinaryElementType.Static)
		    	EncodeStaticBinaryElement(bb, element);
		    else
			    throw new ArgumentException("Unknown or unsupported BinaryElementType: " + element.Type);

	    }
	
	    static BigInteger DecodeBigInteger(BitBuffer bb, int start, int len) /*throws BinaryFormatterException*/
	    {
		    try
		    {
			    BigInteger val = BigInteger.Zero;
			    for (int i = 0; i < len; ++i)
			    {	
				    bool bit = bb.Get(start + i);
				    if (bit)
					    val |= (1 << len - 1 - i);
				
			    }
			    return val;
		    }
		    catch (ArgumentOutOfRangeException e)
		    {
			    throw new BinaryFormatterException(e);
		    }
	    }
	
	    static void EncodeBigInteger(BigInteger val, BitBuffer bb, int start, int len) /*throws BinaryFormatterException*/
	    {
		    //if (val.bitLength() > len)
		    //{	// allowing this to truncate and proceed is essential to allowing duplicate fields with different shifts to form a value.
		    //}
		    for (int i = 0; i < len; ++i)
		    {	
			    bool bit = (val & (1 << len - 1 - i)) != 0;
			    try
			    {
				    bb.Set(start + i, bit);
			    }
			    catch (ArgumentOutOfRangeException e)
			    {
				    throw new BinaryFormatterException(Convert.ToString(start + i), e);
			    }
		    }
	    }

	    /** @return true if there are an odd number of bits that are set to 1, false otherwise. */
	    static bool CalcParity(BitBuffer bb, int start, int len) /*throws BinaryFormatterException*/
	    {
		    return CalcParity(bb, start, len, null);
	    }

	    /** @return true if there are an odd number of bits that are set to 1, false otherwise. */
	    static bool CalcParity(BitBuffer bb, int start, int len, /*@CheckForNull*/ BitBuffer mask) /*throws BinaryFormatterException*/
	    {
		    try
		    {
			    bool result = false;
			    for (int i = 0; i < len; ++i)
			    {	
				    try
				    {
					    if (mask != null && !mask.Get(i))
						    continue;
				    }
				    catch (ArgumentOutOfRangeException)
				    {
					    //logger.warn(e, e);
					    continue;
				    }
				    bool bit = bb.Get(start + i);
				    if (bit)
					    result = !result;
				
			    }
			    return result;
		    }
		    catch (ArgumentOutOfRangeException e)
		    {
			    throw new BinaryFormatterException(e);
		    }
	    }
	
	    static DecodedReadElement DecodeFieldBinaryElement(BitBuffer bb, BinaryElement element) /*throws BinaryFormatterException*/
	    {
            FieldBinaryElement ext = element.ExtFieldBinaryElement;
		    BigInteger val = DecodeBigInteger(bb, element.Start, element.Len);
		    DecodedReadElement result = new DecodedReadElement();
            if (ext.FieldCase == FieldBinaryElement.FieldOneofCase.Field)
		        result.SetField(ext.Field);

		    result.SetValue(val);
		    return result;
	    }
	
	    static void EncodeFieldBinaryElement(BitBuffer bb, BinaryElement element, DecodedReadElement value) /*throws BinaryFormatterException*/
	    {
            FieldBinaryElement ext = element.ExtFieldBinaryElement;
		    BigInteger val = value.GetValue();
            if (!value.GetField().HasValue)
                throw new ArgumentNullException();
            if (ext.FieldCase != FieldBinaryElement.FieldOneofCase.Field)
                throw new ArgumentException("No field");
            if (ext.Field != value.GetField().Value)
			    throw new ArgumentException("Incompatible fields: " + ext.Field + " " + value.GetField().Value);

		    EncodeBigInteger(val, bb, element.Start, element.Len);
	    }
	
	    static void EncodeParityBinaryElement(BitBuffer bb, BinaryElement element) /*throws BinaryFormatterException*/
	    {
            ParityBinaryElement ext = element.ExtParityBinaryElement;
            if (element.Len != 1)
			    throw new ArgumentException("Parity len must be 1: " + element.Len);
		    BitBuffer mask = String.IsNullOrEmpty(ext.Mask) ? null : BitBuffer.FromBinaryString(ext.Mask);
		    bool parity = CalcParity(bb, ext.SrcStart, ext.SrcLen, mask);
		    // For odd parity, the parity of the covered bits, WITH the parity bit, should be odd.
		    bb.Set(element.Start, ext.Odd ? !parity : parity);
		
	    }
	
	
	    static void DecodeParityBinaryElement(BitBuffer bb, BinaryElement element) /*throws BinaryFormatterException*/
	    {
            ParityBinaryElement ext = element.ExtParityBinaryElement;
            if (element.Len != 1)
			    throw new ArgumentException("Parity len must be 1: " + element.Len);
            BitBuffer mask = String.IsNullOrEmpty(ext.Mask) ? null : BitBuffer.FromBinaryString(ext.Mask);
		    bool parity = CalcParity(bb, ext.SrcStart, ext.SrcLen, mask);
		    bool bit = bb.Get(element.Start);
		    // For odd parity, the parity of the covered bits, WITH the parity bit, should be odd.
		    if (ext.Odd)
		    {
			    if (parity == bit)
				    throw new BinaryFormatterException("Odd parity failure, expected " + (!parity) + " at location " + element.Start + ", found: " + bit);
		    }
		    else
		    {
			    if (parity != bit)
				    throw new BinaryFormatterException("Even parity failure, expected " + (parity) + " at location " + element.Start + ", found: " + bit);
		    }
	    }
	
	    static void EncodeStaticBinaryElement(BitBuffer bb, BinaryElement element) /*throws BinaryFormatterException*/
	    {
            StaticBinaryElement ext = element.ExtStaticBinaryElement;
            EncodeBigInteger(SpCoreProtoUtil.ToBigInteger(ext.Value), bb, element.Start, element.Len);
	    }
	
	    static void DecodeStaticBinaryElement(BitBuffer bb, BinaryElement element) /*throws BinaryFormatterException*/
	    {
            StaticBinaryElement ext = element.ExtStaticBinaryElement;
            BigInteger val = DecodeBigInteger(bb, element.Start, element.Len);
            if (SpCoreProtoUtil.ToBigInteger(ext.Value) != val)
			    throw new BinaryFormatterException("Expected static value (" + ext.Value + "), actual: " + val);
	    }
    }
}
