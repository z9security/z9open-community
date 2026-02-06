using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    public class DecodedReadElement
    {
        private DataFormatField? field;
	    private BigInteger value;
	
	    public DecodedReadElement()
	    {
	    }
	
	    public DecodedReadElement(DataFormatField field, BigInteger value)
	    {
		    this.field = field;
		    this.value = value;
	    }

	    public DataFormatField? GetField()
	    {
		    return field;
	    }
	
	    public void SetField(DataFormatField? field)
	    {
		    this.field = field;
	    }
	
	    public BigInteger GetValue()
	    {
		    return value;
	    }
	
	    public void SetValue(BigInteger value)
	    {
		    this.value = value;
	    }
    }
    public class DecodedRead
    {
        private List<DecodedReadElement> elements = new List<DecodedReadElement>();

	    public DecodedRead()
	    {	
	    }
	
	    public List<DecodedReadElement> GetElements()
	    {
		    return elements;
	    }

	    public void SetElements(List<DecodedReadElement> elements)
	    {
		    this.elements = elements;
	    }
	
	    public List<DecodedReadElement> GetElementsMatchingField(DataFormatField f)
	    {
		    List<DecodedReadElement> result = new List<DecodedReadElement>();
		    foreach (DecodedReadElement e in elements)
		    {
			    if (e.GetField() == f)
				    result.Add(e);
		    }
		
		    return result;
	    }
    }
}
