using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Z9.Spcore.Proto;

namespace Z9.Protobuf.Community.Test
{
    // adapted from parts of Java z9.drivers.access.dataformat.binary.TestBinaryFormatter
    [TestClass]
    public class TestBinaryFormatter
    {
        [TestMethod]
        public void Test26BitWiegand()
        {
            TestDecode26bitWiegand(new BigInteger(4497999), "00100010010100010010011111");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(68), new BigInteger(41551), "00100010010100010010011111");

            TestDecode26bitWiegand(new BigInteger(4518618), "00100010011110010110110101");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(68), new BigInteger(62170), "00100010011110010110110101");

            TestDecode26bitWiegand(new BigInteger(4503646), "10100010010111000010111101");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(68), new BigInteger(47198), "10100010010111000010111101");

            TestDecode26bitWiegand(new BigInteger(4503069), "10100010010110110000111011");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(68), new BigInteger(46621), "10100010010110110000111011");

            TestDecode26bitWiegand(new BigInteger(4494103), "00100010010010011000101111");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(68), new BigInteger(37655), "00100010010010011000101111");

            TestDecode26bitWiegand(new BigInteger(4500001), "00100010010101010001000011");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(68), new BigInteger(43553), "00100010010101010001000011");

            TestDecode26bitWiegand(new BigInteger(4497963), "00100010010100010001010110");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(68), new BigInteger(41515), "00100010010100010001010110");

            TestDecode26bitWiegand(new BigInteger(4517431), "10100010011101110001101111");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(68), new BigInteger(60983), "10100010011101110001101111");

            TestDecode26bitWiegand(new BigInteger(4518618), "00100010011110010110110101");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(68), new BigInteger(62170), "00100010011110010110110101");

            TestDecode26bitWiegand(new BigInteger(4499974), "00100010010101010000001101");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(68), new BigInteger(43526), "00100010010101010000001101");

            TestDecode26bitWiegand(new BigInteger(4484915), "00100010001101111001100111");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(68), new BigInteger(28467), "00100010001101111001100111");


            // EM4100 reader reading RFID tags:
            // card numbers determined empirically
            // blue tag:
            TestDecode26bitWiegand(new BigInteger(12959281), "11100010110111110001100011");
            // black round tag:
            TestDecode26bitWiegand(new BigInteger(16659308), "11111111000110011011011001");

            // Grey tokens:
            TestDecode26bitWiegand(new BigInteger(8383478), "00111111111101011111101100");  // 11910392-12
            TestDecode26bitWiegand(new BigInteger(7574481), "10111001110010011110100011");  // 11845314-10
            TestDecode26bitWiegand(new BigInteger(7574480), "10111001110010011110100000");  // 11845314-10
            TestDecode26bitWiegand(new BigInteger(8383481), "00111111111101011111110010");  // 11910392-12	black twistie tie
            TestDecode26bitWiegand(new BigInteger(8383482), "00111111111101011111110100");  // 11910392-12	yellow twistie tie
            TestDecode26bitWiegand(new BigInteger(8383477), "00111111111101011111101010");  // 11910392-12	paper clip
            TestDecode26bitWiegand(new BigInteger(8383479), "00111111111101011111101111");  // 11910392-12
            TestDecode26bitWiegand(new BigInteger(7574483), "10111001110010011110100110");  // 11845314-10
            TestDecode26bitWiegand(new BigInteger(8383480), "00111111111101011111110001");  // 11910392-12
            TestDecode26bitWiegand(new BigInteger(7574479), "10111001110010011110011111");  // 11845314-10

            // Same grey tokens, interpreted with facility code:
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(127), new BigInteger(60406), "00111111111101011111101100");  // 11910392-12
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(115), new BigInteger(37841), "10111001110010011110100011");  // 11845314-10
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(115), new BigInteger(37840), "10111001110010011110100000");  // 11845314-10
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(127), new BigInteger(60409), "00111111111101011111110010");  // 11910392-12	black twistie tie
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(127), new BigInteger(60410), "00111111111101011111110100");  // 11910392-12	yellow twistie tie
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(127), new BigInteger(60405), "00111111111101011111101010");  // 11910392-12	paper clip
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(127), new BigInteger(60407), "00111111111101011111101111");  // 11910392-12
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(115), new BigInteger(37843), "10111001110010011110100110");  // 11845314-10
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(127), new BigInteger(60408), "00111111111101011111110001");  // 11910392-12
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(115), new BigInteger(37839), "10111001110010011110011111");  // 11845314-10

            TestDecode26bitWiegand(new BigInteger(9446189), "11001000000100011001011011");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(144), new BigInteger(9005), "11001000000100011001011011");


            // Sample HID iClass cards:
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(1), new BigInteger(556), "10000000100000010001011001");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(1), new BigInteger(558), "10000000100000010001011100");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(1), new BigInteger(551), "10000000100000010001001110");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(1), new BigInteger(5), "10000000100000000000001011");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(200), new BigInteger(35628), "01100100010001011001011001");

            // Clear HID 125 khz prox cards with mag stripe (mag stripe not used here):
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(1), new BigInteger(5281), "00000000100010100101000011");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(1), new BigInteger(5282), "00000000100010100101000101");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(1), new BigInteger(5283), "00000000100010100101000110");
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(1), new BigInteger(5278), "00000000100010100100111101");

            // HID PhotoProx cards, facility code 233.  card numbers determined empirically with an HID reader, they are not printed on the cards
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(233), new BigInteger(30328), "01110100101110110011110001"); // hot stamp: 30015*
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(233), new BigInteger(30333), "01110100101110110011111011"); // hot stamp: 30016
                                                                                                                                       //                                                                             0010000000000101110100101110110011111011    // output of Proxmark3 - matches
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(233), new BigInteger(30335), "01110100101110110011111110"); // hot stamp: 30017
                                                                                                                                       //                                                                             0010000000000101110100101110110011111110    // output of Proxmark3 - matches
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(233), new BigInteger(30338), "01110100101110110100000101"); // hot stamp: 30018
                                                                                                                                       //                                                                             0010000000000101110100101110110100000101    // output of Proxmark3 - matches
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(233), new BigInteger(30336), "01110100101110110100000000"); // hot stamp: 30019*

            // HID Prox Card II shipped with Proxmark3:
            // values determined empirically
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(113), new BigInteger(7068), "10111000100011011100111000"); // hot stamp: 7068

            // card Jon used for OSDP testing
            TestDecode26bitWiegand_WithFacilityCode(new BigInteger(11), new BigInteger(637), "10000101100000010011111010"); //
        }

        private void TestDecode26bitWiegand(BigInteger value, String encodeBits)
        {
            TestDecode26bitWiegand(value, encodeBits, encodeBits);
	    }

        private void TestDecode26bitWiegand(BigInteger value, String encodeBits, String decodeBits)
        {
            DataFormat format = CommonDataFormats._26_BIT_WIEGAND_RAW();

            TestDecode(format, value, encodeBits, decodeBits);
        }

        private void TestDecode26bitWiegand_WithFacilityCode(BigInteger facilityCode, BigInteger value, String encodeBits)
        {
            TestDecode26bitWiegand_WithFacilityCode(facilityCode, value, encodeBits, encodeBits);
	    }

        private void TestDecode26bitWiegand_WithFacilityCode(BigInteger facilityCode, BigInteger value, String encodeBits, String decodeBits)
        {
            DataFormat format = CommonDataFormats._26_BIT_WIEGAND_WITH_FACILITY_CODE();

            TestDecode_WithFacilityCode(format, facilityCode, value, encodeBits, decodeBits);
        }

        private void TestDecode(DataFormat format, BigInteger value, String encodeBits, String decodeBits)
        {
            BinaryFormat ext = format.ExtBinaryFormat;

            BinaryFormatter formatter = new BinaryFormatter(format);
            DecodedRead decodedRead = formatter.Decode(BitBuffer.FromBinaryString(decodeBits));
            Assert.AreEqual(decodedRead.GetElements().Count, 1);
            DecodedReadElement decodedReadElement = decodedRead.GetElements()[0];
            Assert.AreEqual(decodedReadElement.GetField(), DataFormatField.CredNum);
            Assert.AreEqual(decodedReadElement.GetValue(), value);

		    {
			    BitBuffer bb = new BitBuffer(ext.MaxBits, ext.MaxBits);
                formatter.Encode(bb, decodedRead);
                Assert.AreEqual(bb.ToBinaryString(), encodeBits);
            }
	    }

        private void TestDecode_WithFacilityCode(DataFormat format, BigInteger facilityCode, BigInteger value, String encodeBits, String decodeBits)
        {
            BinaryFormat ext = format.ExtBinaryFormat;

            BinaryFormatter formatter = new BinaryFormatter(format);
            DecodedRead decodedRead = formatter.Decode(BitBuffer.FromBinaryString(decodeBits));
            Assert.AreEqual(decodedRead.GetElements().Count, 2);
		    {
                DecodedReadElement decodedReadElement = decodedRead.GetElements()[0];
                Assert.AreEqual(decodedReadElement.GetField(), DataFormatField.FacilityCode);
                Assert.AreEqual(decodedReadElement.GetValue(), facilityCode);
            }
		    {
                DecodedReadElement decodedReadElement = decodedRead.GetElements()[1];
                Assert.AreEqual(decodedReadElement.GetField(), DataFormatField.CredNum);
                Assert.AreEqual(decodedReadElement.GetValue(), value);
            }

            {
                BitBuffer bb = new BitBuffer(ext.MaxBits, ext.MaxBits);
                formatter.Encode(bb, decodedRead);
                Assert.AreEqual(bb.ToBinaryString(), encodeBits);
            }
        }
    }
}
