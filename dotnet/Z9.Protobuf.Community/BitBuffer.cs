using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Z9.Protobuf
{
    public class BitBuffer
    {
        public static readonly bool LSB_FIRST = true;
	    public static readonly bool MSB_FIRST = false;
	
	    private /*final*/ byte[] array;
	    /** Size in bits of array. */
	    private /*final*/ int capacity;
	    /** Size in bits of bits actually in the buffer. */
	    private int size;
	
	    /** capacity in bits. */
	    public BitBuffer(int capacity)
	    {
		    this.capacity = capacity;
		    this.array = new byte[capacity / 8 + ((capacity % 8 == 0) ? 0 : 1)];
	    }
	
	    /** capacity, size in bits. */
	    public BitBuffer(int capacity, int size)
	    {
		    this.capacity = capacity;
		    this.array = new byte[capacity / 8 + ((capacity % 8 == 0) ? 0 : 1)];
		    SetSize(size);
	    }
	
	    public BitBuffer(/*@NonNull*/ byte[] array)
	    {
		    this.capacity = this.size = array.Length * 8;
		    this.array = array;
	    }

	    /** size in bits. */
	    public BitBuffer(/*@NonNull*/ byte[] array, int size)
	    {
		    this.capacity = array.Length * 8;
		    this.array = array;
		    SetSize(size);
	    }
	
	    /** @return capacity in bits. */
	    public int GetCapacity()
	    {
		    return capacity;
	    }

	    /** @return size in bits. */
	    public int GetSize()
	    {	return size;
	    }
	
	    /*@NonNull*/
	    public byte[] GetBytes()
	    {	
		    byte[] result = new byte[size / 8 + (size % 8 != 0 ? 1 : 0)];
            Array.Copy(array, result, result.Length);
		    return result;
	    }
	
	    /** size in bits. */
	    public void SetSize(int size)
	    {	
		    if (size < 0 || size > capacity)
			    throw new ArgumentException();
		    this.size = size;
	    }
	
	    public bool Get(int bitIndex)
	    {	
		    if (bitIndex < 0 || bitIndex >= size)
			    throw new ArgumentOutOfRangeException();
		    int byteIndex = bitIndex / 8;
		    int bitInByte = bitIndex % 8;
		    return (array[byteIndex] & (1 << bitInByte) ) != 0;
	    }
	
	    public void Set(int bitIndex, bool value)
	    {
		    if (bitIndex < 0 || bitIndex >= size)
			    throw new ArgumentOutOfRangeException("bitIndex=" + bitIndex + ", size=" + size);
		    int byteIndex = bitIndex / 8;
		    int bitInByte = bitIndex % 8;
		    if (value)
			    array[byteIndex] |= (byte) (1 << bitInByte);
		    else
			    array[byteIndex] &= (byte) (~(1 << bitInByte));
	    }
	
	    public void Append(bool value)
	    {
		    if (size + 1 > capacity)
			    throw new ArgumentOutOfRangeException();
		    int bitIndex = size;
		    size += 1;
		    Set(bitIndex, value);
	    }
	
	    public byte Get(int bitIndex, int len, bool lsbFirst)
	    {	
		    if (len < 0 || len > 8)
			    throw new ArgumentException();
		    if (bitIndex < 0 || bitIndex + len > size)
			    throw new ArgumentOutOfRangeException();
	
		    byte result = 0;
		
		    for (int offset = 0; offset < len; ++offset)
			    if (Get(bitIndex + offset))
                    result |= (byte) (!lsbFirst ? (1 << (len - 1 - offset)) : (1 << offset));
		
		    return result;
	    }
	
	    public void Set(int bitIndex, int len, byte value, bool lsbFirst)
	    {
		    if (len < 0 || len > 8)
			    throw new ArgumentException();
		    if (bitIndex < 0 || bitIndex + len > size)
			    throw new ArgumentOutOfRangeException();
	
		    for (int offset = 0; offset < len; ++offset)
			    Set(bitIndex + offset, (value & (!lsbFirst ? (1 << (len - 1 - offset)) : (1 << offset))) != 0);
	    }
	
	    public void Append(int len, byte value, bool lsbFirst)
	    {
		    if (size + len > capacity)
			    throw new ArgumentOutOfRangeException();
		    int bitIndex = size;
		    size += len;
		    Set(bitIndex, len, value, lsbFirst);
	    }
	
	    /*@NonNull*/
	    public String ToBinaryString()
	    {
            String b = "";
		    for (int i = 0; i < size; ++i)
			    b += (Get(i) ? '1' : '0');
		    return b;
	    }
	
	    /*@NonNull*/
	    public static BitBuffer FromBinaryString(/*@NonNull*/ String binaryString)
	    {
		    int len = binaryString.Length;
		    BitBuffer result = new BitBuffer(len, len);
		    for (int i = 0; i < len; ++i)
		    {	
			    char c = binaryString[i];
			    switch (c)
			    {
				    case '0':
					    result.Set(i, false);
					    break;
				    case '1':
					    result.Set(i, true);
					    break;
				    default:
					    throw new FormatException("Expected '0' or '1': " + c);
			    }
		    }
		    return result;
	    }

	    /*@NonNull*/
	    public BitBuffer Reverse()
	    {
		    BitBuffer result = new BitBuffer(size, size);
		    for (int i = 0; i < size; ++i)
			    result.Set(i, Get(size - i - 1));
		    return result;
	    }
    }
}
