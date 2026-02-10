using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Z9.Spcore.Proto;

namespace Z9.Protobuf.Community.Test
{

/// <summary>
/// Serialization/deserialization round-trip tests for all Z9 Open Community protobuf message types.
/// Each test creates an instance with all properties set to non-default values, serializes to bytes,
/// deserializes back, and asserts all properties match.
///
/// Regression mode (ASSERT_REGRESSION = true): asserts that the serialized hex matches the expected
/// value, catching any backwards-incompatible protocol changes.
/// When false: logs the hex for each message so it can be captured and embedded.
/// </summary>
[TestClass]
public class TestCommunityProtoSerialization
{
    private const bool ASSERT_REGRESSION = true;

    private static T RoundTrip<T>(T message, string expectedHex = null,
        [CallerMemberName] string testName = null) where T : IMessage<T>, new()
    {
        var bytes = message.ToByteArray();
        Assert.IsTrue(bytes.Length > 0, $"{typeof(T).Name} serialized to empty bytes");
        var hex = BitConverter.ToString(bytes).Replace("-", "");

        if (ASSERT_REGRESSION)
        {
            Assert.IsNotNull(expectedHex,
                $"{testName}: No expected hex provided — add the regression hex for this test");
            Assert.AreEqual(expectedHex, hex,
                $"{testName}: Serialized bytes changed — possible backwards-incompatible protocol change");
        }
        else
        {
            Console.WriteLine($"REGRESSION_HEX {testName}: {hex}");
        }

        var parser = new MessageParser<T>(() => new T());
        return parser.ParseFrom(bytes);
    }

    // ==================== Element Types ====================

    [TestMethod]
    public void BigIntegerData_RoundTrip()
    {
        var original = new BigIntegerData { Bytes = ByteString.CopyFrom(1, 2, 3, 4) };
        var result = RoundTrip(original, "0A0401020304");
        CollectionAssert.AreEqual(original.Bytes.ToByteArray(), result.Bytes.ToByteArray());
    }

    [TestMethod]
    public void SqlDateData_RoundTrip()
    {
        var original = new SqlDateData { Year = 2025, Month = 6, Day = 15 };
        var result = RoundTrip(original, "08E90F1006180F");
        Assert.AreEqual(2025, result.Year);
        Assert.AreEqual(6, result.Month);
        Assert.AreEqual(15, result.Day);
    }

    [TestMethod]
    public void SqlTimeData_RoundTrip()
    {
        var original = new SqlTimeData { Hour = 14, Minute = 30, Second = 45 };
        var result = RoundTrip(original, "080E101E182D");
        Assert.AreEqual(14, result.Hour);
        Assert.AreEqual(30, result.Minute);
        Assert.AreEqual(45, result.Second);
    }

    [TestMethod]
    public void DateTimeData_RoundTrip()
    {
        var original = new DateTimeData { Millis = 1700000000000L };
        var result = RoundTrip(original, "0880D095FFBC31");
        Assert.AreEqual(1700000000000L, result.Millis);
    }

    // ==================== Protocol Types ====================

    [TestMethod]
    public void ProtocolCapabilities_RoundTrip()
    {
        var original = new ProtocolCapabilities
        {
            MaxEnumDevMod = 259,
            MaxEnumDevUse = 68,
            MaxEnumDevActionParamsType = 10,
            MaxEnumDevActionType = 108,
            MaxEnumDevAspect = 45,
            MaxEnumEvtCode = 281,
            MaxEnumEvtSubCode = 150,
            SupportsIdentificationPassword = true,
            SupportsIdentificationPasswordUpstream = false,
            MaxEnumDevPlatform = 44,
            MaxEnumTerminationReason = 2
        };
        var result = RoundTrip(original, "1083021844200A286C302D389902489601B00101B80100E0012C800202");
        Assert.AreEqual(259, result.MaxEnumDevMod);
        Assert.AreEqual(68, result.MaxEnumDevUse);
        Assert.AreEqual(10, result.MaxEnumDevActionParamsType);
        Assert.AreEqual(108, result.MaxEnumDevActionType);
        Assert.AreEqual(45, result.MaxEnumDevAspect);
        Assert.AreEqual(281, result.MaxEnumEvtCode);
        Assert.AreEqual(150, result.MaxEnumEvtSubCode);
        Assert.AreEqual(true, result.SupportsIdentificationPassword);
        Assert.AreEqual(false, result.SupportsIdentificationPasswordUpstream);
        Assert.AreEqual(44, result.MaxEnumDevPlatform);
        Assert.AreEqual(2, result.MaxEnumTerminationReason);
    }

    [TestMethod]
    public void Identification_RoundTrip()
    {
        var original = new Identification
        {
            Id = "00:11:22:33:44:55",
            SpCoreDevMod = DevMod.IoControllerCommunity,
            SpCoreDevUse = DevUse.ActuatorDoorStrike,
            ProtocolVersion = "0.1",
            SoftwareVersion = "2.0.0",
            MaxBodyLength = 65536,
            BootId = 987654321L,
            SoftwareVersionTimestamp = "2025-01-15T10:00:00",
            SoftwareVersionBrand = "Z9",
            SoftwareVersionProduct = "Aporta",
            SerialNumber = "SN-12345",
            SoftwareVersionBranch = "main",
            ProtocolCapabilities = new ProtocolCapabilities
            {
                SupportsIdentificationPassword = true,
                SupportsIdentificationPasswordUpstream = false
            },
            Password = "secret123",
            EvtDevRef = new EvtDevRef
            {
                Unid = 1,
                Name = "test-controller",
                DevType = DevType.IoController,
                DevMod = DevMod.IoControllerCommunity
            }
        };
        var result = RoundTrip(original, "0A1130303A31313A32323A33333A34343A353510A40118002203302E312A05322E302E303880800440B1D1F9D6034A13323032352D30312D31355431303A30303A303052025A395A0641706F7274616208534E2D31323334356A046D61696E7206B00101B801008201097365637265743132339A01180801120F746573742D636F6E74726F6C6C6572300150A401");
        Assert.AreEqual("00:11:22:33:44:55", result.Id);
        Assert.AreEqual(DevMod.IoControllerCommunity, result.SpCoreDevMod);
        Assert.AreEqual(DevUse.ActuatorDoorStrike, result.SpCoreDevUse);
        Assert.AreEqual("0.1", result.ProtocolVersion);
        Assert.AreEqual("2.0.0", result.SoftwareVersion);
        Assert.AreEqual(65536, result.MaxBodyLength);
        Assert.AreEqual(987654321L, result.BootId);
        Assert.AreEqual("2025-01-15T10:00:00", result.SoftwareVersionTimestamp);
        Assert.AreEqual("Z9", result.SoftwareVersionBrand);
        Assert.AreEqual("Aporta", result.SoftwareVersionProduct);
        Assert.AreEqual("SN-12345", result.SerialNumber);
        Assert.AreEqual("main", result.SoftwareVersionBranch);
        Assert.IsNotNull(result.ProtocolCapabilities);
        Assert.AreEqual(true, result.ProtocolCapabilities.SupportsIdentificationPassword);
        Assert.AreEqual(false, result.ProtocolCapabilities.SupportsIdentificationPasswordUpstream);
        Assert.AreEqual("secret123", result.Password);
        Assert.IsNotNull(result.EvtDevRef);
        Assert.AreEqual(1, result.EvtDevRef.Unid);
        Assert.AreEqual("test-controller", result.EvtDevRef.Name);
    }

    [TestMethod]
    public void SpCoreMessage_Identification_RoundTrip()
    {
        var original = new SpCoreMessage
        {
            Type = SpCoreMessage.Types.Type.Identification,
            Identification = new Identification
            {
                Id = "panel-1",
                ProtocolVersion = "0.1",
                SoftwareVersion = "1.0.0"
            }
        };
        var result = RoundTrip(original, "080112150A0770616E656C2D312203302E312A05312E302E30");
        Assert.AreEqual(SpCoreMessage.Types.Type.Identification, result.Type);
        Assert.IsNotNull(result.Identification);
        Assert.AreEqual("panel-1", result.Identification.Id);
        Assert.AreEqual("0.1", result.Identification.ProtocolVersion);
    }

    [TestMethod]
    public void SpCoreMessage_Evt_RoundTrip()
    {
        var original = new SpCoreMessage { Type = SpCoreMessage.Types.Type.Evt };
        original.Evt.Add(new Evt
        {
            Unid = 42,
            EvtCode = EvtCode.DoorAccessGranted,
            HwTime = new DateTimeData { Millis = 1700000000000L },
            DbTime = new DateTimeData { Millis = 1700000000001L },
            Consumed = false,
            Priority = 0
        });
        var result = RoundTrip(original, "08052A1B082A12070880D095FFBC311A070881D095FFBC3138307000F00200");
        Assert.AreEqual(SpCoreMessage.Types.Type.Evt, result.Type);
        Assert.AreEqual(1, result.Evt.Count);
        Assert.AreEqual(42L, result.Evt[0].Unid);
        Assert.AreEqual(EvtCode.DoorAccessGranted, result.Evt[0].EvtCode);
    }

    [TestMethod]
    public void SpCoreMessage_DevStateRecord_RoundTrip()
    {
        var original = new SpCoreMessage { Type = SpCoreMessage.Types.Type.DevStateRecord };
        original.DevStateRecord.Add(new DevStateRecord
        {
            Unid = 10,
            DevUnid = 5,
            DevState = new DevState()
        });
        original.DevStateRecord[0].DevState.DevAspectStates.Add(
            new DevState.Types.DevAspect_DevAspectState_Entry
            {
                Key = DevAspect.Primary,
                Value = new DevAspectState
                {
                    DevAspect = DevAspect.Primary,
                    HwTime = new DateTimeData { Millis = 100 },
                    DbTime = new DateTimeData { Millis = 200 },
                    CommState = CommState.Online
                }
            });
        var result = RoundTrip(original, "0806321A080A10051A140A120800120E10001A020864220308C801800101");
        Assert.AreEqual(1, result.DevStateRecord.Count);
        Assert.AreEqual(10, result.DevStateRecord[0].Unid);
        Assert.AreEqual(5, result.DevStateRecord[0].DevUnid);
        Assert.AreEqual(1, result.DevStateRecord[0].DevState.DevAspectStates.Count);
        Assert.AreEqual(CommState.Online, result.DevStateRecord[0].DevState.DevAspectStates[0].Value.CommState);
    }

    [TestMethod]
    public void SpCoreMessage_Terminate_RoundTrip()
    {
        var original = new SpCoreMessage
        {
            Type = SpCoreMessage.Types.Type.Terminate,
            TerminationReason = TerminationReason.ExitingOrRestarting
        };
        var result = RoundTrip(original, "080EA80301");
        Assert.AreEqual(SpCoreMessage.Types.Type.Terminate, result.Type);
        Assert.AreEqual(TerminationReason.ExitingOrRestarting, result.TerminationReason);
    }

    [TestMethod]
    public void DbChange_RoundTrip()
    {
        var original = new DbChange
        {
            RequestId = 42,
            CredDeleteAll = true,
            CredTemplateDeleteAll = false,
            DataLayoutDeleteAll = true,
            DataFormatDeleteAll = false,
            DevDeleteAll = true,
            PrivDeleteAll = false,
            HolCalDeleteAll = true,
            HolTypeDeleteAll = false,
            SchedDeleteAll = true,
            HolDeleteAll = false
        };
        original.CredDelete.Add(1);
        original.CredDelete.Add(2);
        original.CredTemplateDelete.Add(10);
        original.DataLayoutDelete.Add(20);
        original.DataFormatDelete.Add(30);
        original.DevDelete.Add(40);
        original.PrivDelete.Add(50);
        original.HolCalDelete.Add(60);
        original.HolTypeDelete.Add(70);
        original.SchedDelete.Add(80);
        original.HolDelete.Add(90);
        original.Cred.Add(new Cred { Unid = 100, Name = "TestCred", Enabled = true });
        original.Sched.Add(new Sched { Unid = 200, Name = "TestSched" });

        var result = RoundTrip(original, "082A10011A020102220F586462085465737443726564900101280032010A40014A0114580062011E70017A0128980200A2020132B00201BA02013CC80200D2020146E00201EA020150F2020E520954657374536368656470C801D80900E209015A");
        Assert.AreEqual(42L, result.RequestId);
        Assert.AreEqual(true, result.CredDeleteAll);
        Assert.AreEqual(false, result.CredTemplateDeleteAll);
        Assert.AreEqual(true, result.DataLayoutDeleteAll);
        Assert.AreEqual(false, result.DataFormatDeleteAll);
        Assert.AreEqual(true, result.DevDeleteAll);
        Assert.AreEqual(false, result.PrivDeleteAll);
        Assert.AreEqual(true, result.HolCalDeleteAll);
        Assert.AreEqual(false, result.HolTypeDeleteAll);
        Assert.AreEqual(true, result.SchedDeleteAll);
        Assert.AreEqual(false, result.HolDeleteAll);
        CollectionAssert.AreEqual(new[] { 1, 2 }, result.CredDelete.ToArray());
        CollectionAssert.AreEqual(new[] { 10 }, result.CredTemplateDelete.ToArray());
        CollectionAssert.AreEqual(new[] { 20 }, result.DataLayoutDelete.ToArray());
        CollectionAssert.AreEqual(new[] { 30 }, result.DataFormatDelete.ToArray());
        CollectionAssert.AreEqual(new[] { 40 }, result.DevDelete.ToArray());
        CollectionAssert.AreEqual(new[] { 50 }, result.PrivDelete.ToArray());
        CollectionAssert.AreEqual(new[] { 60 }, result.HolCalDelete.ToArray());
        CollectionAssert.AreEqual(new[] { 70 }, result.HolTypeDelete.ToArray());
        CollectionAssert.AreEqual(new[] { 80 }, result.SchedDelete.ToArray());
        CollectionAssert.AreEqual(new[] { 90 }, result.HolDelete.ToArray());
        Assert.AreEqual(1, result.Cred.Count);
        Assert.AreEqual(100, result.Cred[0].Unid);
        Assert.AreEqual(1, result.Sched.Count);
        Assert.AreEqual(200, result.Sched[0].Unid);
    }

    [TestMethod]
    public void DbChangeResp_RoundTrip()
    {
        var original = new DbChangeResp
        {
            RequestId = 99,
            Exception = "test error"
        };
        original.CredUnids.Add(1);
        original.CredTemplateUnids.Add(2);
        original.DataLayoutUnids.Add(3);
        original.DataFormatUnids.Add(4);
        original.DevUnids.Add(5);
        original.PrivUnids.Add(6);
        original.HolCalUnids.Add(7);
        original.HolTypeUnids.Add(8);
        original.SchedUnids.Add(9);
        original.EncryptionKeyRefUnids.Add(10);
        original.HolUnids.Add(11);
        original.EncryptionKeyUnids.Add(12);

        var result = RoundTrip(original, "0863120A74657374206572726F721A01012201022A01033201043A01057201067A0107820101088A0101098203010AB203010BBA04010C");
        Assert.AreEqual(99L, result.RequestId);
        Assert.AreEqual("test error", result.Exception);
        CollectionAssert.AreEqual(new[] { 1 }, result.CredUnids.ToArray());
        CollectionAssert.AreEqual(new[] { 2 }, result.CredTemplateUnids.ToArray());
        CollectionAssert.AreEqual(new[] { 3 }, result.DataLayoutUnids.ToArray());
        CollectionAssert.AreEqual(new[] { 4 }, result.DataFormatUnids.ToArray());
        CollectionAssert.AreEqual(new[] { 5 }, result.DevUnids.ToArray());
        CollectionAssert.AreEqual(new[] { 6 }, result.PrivUnids.ToArray());
        CollectionAssert.AreEqual(new[] { 7 }, result.HolCalUnids.ToArray());
        CollectionAssert.AreEqual(new[] { 8 }, result.HolTypeUnids.ToArray());
        CollectionAssert.AreEqual(new[] { 9 }, result.SchedUnids.ToArray());
        CollectionAssert.AreEqual(new[] { 10 }, result.EncryptionKeyRefUnids.ToArray());
        CollectionAssert.AreEqual(new[] { 11 }, result.HolUnids.ToArray());
        CollectionAssert.AreEqual(new[] { 12 }, result.EncryptionKeyUnids.ToArray());
    }

    [TestMethod]
    public void EvtControl_RoundTrip()
    {
        var original = new EvtControl { EvtFlowControl = EvtFlowControl.StartContinuous };
        original.ConsumeEvt.Add(100L);
        original.ConsumeEvt.Add(200L);
        var result = RoundTrip(original, "0800120364C801");
        Assert.AreEqual(EvtFlowControl.StartContinuous, result.EvtFlowControl);
        CollectionAssert.AreEqual(new[] { 100L, 200L }, result.ConsumeEvt.ToArray());
    }

    [TestMethod]
    public void DevStateRecordControl_RoundTrip()
    {
        var original = new DevStateRecordControl
        {
            DevStateRecordFlowControl = DevStateRecordFlowControl.SendOneBatch
        };
        var result = RoundTrip(original, "0802");
        Assert.AreEqual(DevStateRecordFlowControl.SendOneBatch, result.DevStateRecordFlowControl);
    }

    [TestMethod]
    public void DevActionReq_RoundTrip()
    {
        var original = new DevActionReq
        {
            RequestId = 77,
            DevActionType = DevActionType.DoorMomentaryUnlock,
            DevUnid = 5,
            DevActionParams = new DevActionParams
            {
                Unid = 1,
                Type = DevActionParamsType.DoorMomentaryUnlock,
                ExtDoorMomentaryUnlockDevActionParams = new DoorMomentaryUnlockDevActionParams()
            }
        };
        var result = RoundTrip(original, "084D1011180522070801180ADA0600");
        Assert.AreEqual(77L, result.RequestId);
        Assert.AreEqual(DevActionType.DoorMomentaryUnlock, result.DevActionType);
        Assert.AreEqual(5, result.DevUnid);
        Assert.IsNotNull(result.DevActionParams);
        Assert.AreEqual(DevActionParamsType.DoorMomentaryUnlock, result.DevActionParams.Type);
        Assert.IsNotNull(result.DevActionParams.ExtDoorMomentaryUnlockDevActionParams);
    }

    [TestMethod]
    public void DevActionResp_RoundTrip()
    {
        var original = new DevActionResp
        {
            RequestId = 88,
            Exception = "device not found"
        };
        var result = RoundTrip(original, "08581210646576696365206E6F7420666F756E64");
        Assert.AreEqual(88L, result.RequestId);
        Assert.AreEqual("device not found", result.Exception);
    }

    // ==================== Credential Types ====================

    [TestMethod]
    public void CardPin_RoundTrip()
    {
        var original = new CardPin
        {
            CredNum = new BigIntegerData { Bytes = ByteString.CopyFrom(0, 1, 0xE2, 0x40) },
            FacilityCode = 100,
            Pin = "1234",
            PinUnique = true
        };
        var result = RoundTrip(original, "0A060A040001E24010642204313233342801");
        CollectionAssert.AreEqual(original.CredNum.Bytes.ToByteArray(), result.CredNum.Bytes.ToByteArray());
        Assert.AreEqual(100, result.FacilityCode);
        Assert.AreEqual("1234", result.Pin);
        Assert.AreEqual(true, result.PinUnique);
    }

    [TestMethod]
    public void CardPinTemplate_RoundTrip()
    {
        var original = new CardPinTemplate
        {
            CredComponentPresence = CredComponentPresence.Required,
            CredNumPresence = CredComponentPresence.Required,
            PinPresence = CredComponentPresence.Optional,
            PinUnique = true,
            MinPinLength = 4,
            MaxPinLength = 8,
            FacilityCode = 200,
            DataLayoutUnid = 1,
            AnyDataLayout = false
        };
        var result = RoundTrip(original, "08011001180220012804300838C80140017800");
        Assert.AreEqual(CredComponentPresence.Required, result.CredComponentPresence);
        Assert.AreEqual(CredComponentPresence.Required, result.CredNumPresence);
        Assert.AreEqual(CredComponentPresence.Optional, result.PinPresence);
        Assert.AreEqual(true, result.PinUnique);
        Assert.AreEqual(4, result.MinPinLength);
        Assert.AreEqual(8, result.MaxPinLength);
        Assert.AreEqual(200, result.FacilityCode);
        Assert.AreEqual(1, result.DataLayoutUnid);
        Assert.AreEqual(false, result.AnyDataLayout);
    }

    [TestMethod]
    public void Cred_RoundTrip()
    {
        var original = new Cred
        {
            Version = 3,
            Tag = "tag-123",
            Uuid = "uuid-456",
            Unid = 100,
            Name = "John Doe",
            Enabled = true,
            Effective = new DateTimeData { Millis = 1700000000000L },
            Expires = new DateTimeData { Millis = 1800000000000L },
            CredTemplateUnid = 5,
            CardPin = new CardPin
            {
                CredNum = new BigIntegerData { Bytes = ByteString.CopyFrom(0, 1, 0xE2, 0x40) },
                Pin = "5678",
                PinUnique = true
            },
            DoorAccessModifiers = new DoorAccessModifiers { ExtDoorTime = true }
        };
        original.PrivBindings.Add(new CredPrivBinding { Unid = 1, PrivUnid = 10 });
        original.PrivBindings.Add(new CredPrivBinding { Unid = 2, DevAsDoorAccessPrivUnid = 20 });

        var result = RoundTrip(original, "080332077461672D3132333A08757569642D343536586462084A6F686E20446F65900101A201070880D095FFBC31AA01070880A0F1C2B134B80105F201100A060A040001E24022043536373828019202021801A20204400A6001A2020448146002");
        Assert.AreEqual(3, result.Version);
        Assert.AreEqual("tag-123", result.Tag);
        Assert.AreEqual("uuid-456", result.Uuid);
        Assert.AreEqual(100, result.Unid);
        Assert.AreEqual("John Doe", result.Name);
        Assert.AreEqual(true, result.Enabled);
        Assert.AreEqual(1700000000000L, result.Effective.Millis);
        Assert.AreEqual(1800000000000L, result.Expires.Millis);
        Assert.AreEqual(5, result.CredTemplateUnid);
        Assert.IsNotNull(result.CardPin);
        Assert.AreEqual("5678", result.CardPin.Pin);
        Assert.AreEqual(true, result.CardPin.PinUnique);
        Assert.IsNotNull(result.DoorAccessModifiers);
        Assert.AreEqual(true, result.DoorAccessModifiers.ExtDoorTime);
        Assert.AreEqual(2, result.PrivBindings.Count);
        Assert.AreEqual(10, result.PrivBindings[0].PrivUnid);
        Assert.AreEqual(20, result.PrivBindings[1].DevAsDoorAccessPrivUnid);
    }

    [TestMethod]
    public void CredTemplate_RoundTrip()
    {
        var original = new CredTemplate
        {
            Version = 2,
            Tag = "ct-tag",
            Uuid = "ct-uuid",
            Name = "Standard Card",
            Unid = 50,
            CardPinTemplate = new CardPinTemplate
            {
                CredComponentPresence = CredComponentPresence.Required,
                CredNumPresence = CredComponentPresence.Required,
                PinPresence = CredComponentPresence.Absent
            },
            Priority = 1
        };
        var result = RoundTrip(original, "0802320663742D7461673A0763742D75756964520D5374616E64617264204361726460327A06080110011800900101");
        Assert.AreEqual(2, result.Version);
        Assert.AreEqual("ct-tag", result.Tag);
        Assert.AreEqual("ct-uuid", result.Uuid);
        Assert.AreEqual("Standard Card", result.Name);
        Assert.AreEqual(50, result.Unid);
        Assert.IsNotNull(result.CardPinTemplate);
        Assert.AreEqual(CredComponentPresence.Required, result.CardPinTemplate.CredComponentPresence);
        Assert.AreEqual(1, result.Priority);
    }

    [TestMethod]
    public void CredPrivBinding_RoundTrip()
    {
        var original = new CredPrivBinding
        {
            Unid = 7,
            PrivUnid = 15,
            DevAsDoorAccessPrivUnid = 25,
            SchedRestriction = new SchedRestriction { SchedUnid = 3, Invert = false }
        };
        var result = RoundTrip(original, "3A0408031000400F48196007");
        Assert.AreEqual(7, result.Unid);
        Assert.AreEqual(15, result.PrivUnid);
        Assert.AreEqual(25, result.DevAsDoorAccessPrivUnid);
        Assert.IsNotNull(result.SchedRestriction);
        Assert.AreEqual(3, result.SchedRestriction.SchedUnid);
        Assert.AreEqual(false, result.SchedRestriction.Invert);
    }

    [TestMethod]
    public void DoorAccessModifiers_RoundTrip()
    {
        var original = new DoorAccessModifiers { ExtDoorTime = true };
        var result = RoundTrip(original, "1801");
        Assert.AreEqual(true, result.ExtDoorTime);
    }

    // ==================== Data Format Types ====================

    [TestMethod]
    public void DataFormat_BinaryFormat_RoundTrip()
    {
        var original = new DataFormat
        {
            Version = 1,
            Tag = "df-tag",
            Uuid = "df-uuid",
            Name = "26-bit Wiegand",
            Unid = 10,
            DataFormatType = DataFormatType.Binary,
            ExtBinaryFormat = new BinaryFormat
            {
                MinBits = 26,
                MaxBits = 26,
                SupportReverseRead = false
            }
        };
        original.ExtBinaryFormat.Elements.Add(new BinaryElement
        {
            Unid = 1, Num = 1, Type = BinaryElementType.Parity, Start = 0, Len = 1,
            ExtParityBinaryElement = new ParityBinaryElement
            {
                Odd = false, SrcStart = 1, SrcLen = 12, Mask = "111111111111"
            }
        });
        original.ExtBinaryFormat.Elements.Add(new BinaryElement
        {
            Unid = 2, Num = 2, Type = BinaryElementType.Field, Start = 1, Len = 8,
            ExtFieldBinaryElement = new FieldBinaryElement
            {
                Field = DataFormatField.FacilityCode
            }
        });
        original.ExtBinaryFormat.Elements.Add(new BinaryElement
        {
            Unid = 3, Num = 3, Type = BinaryElementType.Field, Start = 9, Len = 16,
            ExtFieldBinaryElement = new FieldBinaryElement
            {
                Field = DataFormatField.CredNum
            }
        });
        original.ExtBinaryFormat.Elements.Add(new BinaryElement
        {
            Unid = 4, Num = 4, Type = BinaryElementType.Static, Start = 25, Len = 1,
            ExtStaticBinaryElement = new StaticBinaryElement
            {
                Value = new BigIntegerData { Bytes = ByteString.CopyFrom(0) }
            }
        });

        var result = RoundTrip(original, "0801320664662D7461673A0764662D75756964520E32362D6269742057696567616E64600A7001AA065F081A101A1A2108011001180120002801AA061408001001180C220C3131313131313131313131311A0F08021002180220012808A2060208011A0F08031003180220092810A2060208001A1208041004180020192801B206050A030A01002000");
        Assert.AreEqual(1, result.Version);
        Assert.AreEqual("df-tag", result.Tag);
        Assert.AreEqual("df-uuid", result.Uuid);
        Assert.AreEqual("26-bit Wiegand", result.Name);
        Assert.AreEqual(10, result.Unid);
        Assert.AreEqual(DataFormatType.Binary, result.DataFormatType);
        Assert.IsNotNull(result.ExtBinaryFormat);
        Assert.AreEqual(26, result.ExtBinaryFormat.MinBits);
        Assert.AreEqual(26, result.ExtBinaryFormat.MaxBits);
        Assert.AreEqual(false, result.ExtBinaryFormat.SupportReverseRead);
        Assert.AreEqual(4, result.ExtBinaryFormat.Elements.Count);

        // Parity element
        var parity = result.ExtBinaryFormat.Elements[0];
        Assert.AreEqual(BinaryElementType.Parity, parity.Type);
        Assert.AreEqual(0, parity.Start);
        Assert.AreEqual(1, parity.Len);
        Assert.IsNotNull(parity.ExtParityBinaryElement);
        Assert.AreEqual(false, parity.ExtParityBinaryElement.Odd);
        Assert.AreEqual(1, parity.ExtParityBinaryElement.SrcStart);
        Assert.AreEqual(12, parity.ExtParityBinaryElement.SrcLen);
        Assert.AreEqual("111111111111", parity.ExtParityBinaryElement.Mask);

        // Field element (facility code)
        var fc = result.ExtBinaryFormat.Elements[1];
        Assert.AreEqual(BinaryElementType.Field, fc.Type);
        Assert.IsNotNull(fc.ExtFieldBinaryElement);
        Assert.AreEqual(DataFormatField.FacilityCode, fc.ExtFieldBinaryElement.Field);

        // Field element (cred num)
        var cn = result.ExtBinaryFormat.Elements[2];
        Assert.AreEqual(DataFormatField.CredNum, cn.ExtFieldBinaryElement.Field);

        // Static element
        var stat = result.ExtBinaryFormat.Elements[3];
        Assert.AreEqual(BinaryElementType.Static, stat.Type);
        Assert.IsNotNull(stat.ExtStaticBinaryElement);
        Assert.IsNotNull(stat.ExtStaticBinaryElement.Value);
    }

    [TestMethod]
    public void FieldBinaryElement_WithStaticValue_RoundTrip()
    {
        var original = new FieldBinaryElement
        {
            Field = DataFormatField.FacilityCode,
            StaticValue = new BigIntegerData { Bytes = ByteString.CopyFrom(0, 0xC8) }
        };
        var result = RoundTrip(original, "08012A040A0200C8");
        Assert.AreEqual(DataFormatField.FacilityCode, result.Field);
        Assert.IsNotNull(result.StaticValue);
        CollectionAssert.AreEqual(original.StaticValue.Bytes.ToByteArray(), result.StaticValue.Bytes.ToByteArray());
    }

    [TestMethod]
    public void DataLayout_BasicDataLayout_RoundTrip()
    {
        var original = new DataLayout
        {
            Version = 1,
            Tag = "dl-tag",
            Uuid = "dl-uuid",
            Name = "Standard Wiegand",
            Unid = 20,
            LayoutType = DataLayoutType.Basic,
            Priority = 1,
            Enabled = true,
            ExtBasicDataLayout = new BasicDataLayout { DataFormatUnid = 10 }
        };
        var result = RoundTrip(original, "08013206646C2D7461673A07646C2D7575696452105374616E646172642057696567616E64601470007801880101A20602080A");
        Assert.AreEqual(1, result.Version);
        Assert.AreEqual("dl-tag", result.Tag);
        Assert.AreEqual("Standard Wiegand", result.Name);
        Assert.AreEqual(20, result.Unid);
        Assert.AreEqual(DataLayoutType.Basic, result.LayoutType);
        Assert.AreEqual(1, result.Priority);
        Assert.AreEqual(true, result.Enabled);
        Assert.IsNotNull(result.ExtBasicDataLayout);
        Assert.AreEqual(10, result.ExtBasicDataLayout.DataFormatUnid);
    }

    // ==================== Encryption Types ====================

    [TestMethod]
    public void EncryptionKey_RoundTrip()
    {
        var original = new EncryptionKey
        {
            Unid = 1,
            Tag = "ek-tag",
            Uuid = "ek-uuid",
            Version = 1,
            Algorithm = "AES",
            Size = 128,
            KeyIdentifier = "SCBK",
            Bytes = ByteString.CopyFrom(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16)
        };
        var result = RoundTrip(original, "08011206656B2D7461671A07656B2D7575696420012A034145533080013A045343424B42100102030405060708090A0B0C0D0E0F10");
        Assert.AreEqual(1, result.Unid);
        Assert.AreEqual("ek-tag", result.Tag);
        Assert.AreEqual("ek-uuid", result.Uuid);
        Assert.AreEqual(1, result.Version);
        Assert.AreEqual("AES", result.Algorithm);
        Assert.AreEqual(128, result.Size);
        Assert.AreEqual("SCBK", result.KeyIdentifier);
        Assert.AreEqual(16, result.Bytes.Length);
    }

    [TestMethod]
    public void EncryptionKeyRef_RoundTrip()
    {
        var original = new EncryptionKeyRef
        {
            Unid = 2,
            Tag = "ekr-tag",
            Uuid = "ekr-uuid",
            Version = 1,
            Algorithm = "RSA",
            Size = 2048,
            KeystoreIdentifier = "keystore-1",
            KeyIdentifier = "key-1"
        };
        var result = RoundTrip(original, "08021207656B722D7461671A08656B722D7575696420012A035253413080103A0A6B657973746F72652D3142056B65792D31");
        Assert.AreEqual(2, result.Unid);
        Assert.AreEqual("ekr-tag", result.Tag);
        Assert.AreEqual("RSA", result.Algorithm);
        Assert.AreEqual(2048, result.Size);
        Assert.AreEqual("keystore-1", result.KeystoreIdentifier);
        Assert.AreEqual("key-1", result.KeyIdentifier);
    }

    // ==================== Privilege Types ====================

    [TestMethod]
    public void Priv_DoorAccessPriv_RoundTrip()
    {
        var original = new Priv
        {
            Version = 1,
            Tag = "priv-tag",
            Uuid = "priv-uuid",
            Name = "All Doors",
            Unid = 30,
            Enabled = true,
            PrivType = PrivType.Door,
            ExternalId = "ext-priv-1",
            ExtDoorAccessPriv = new DoorAccessPriv()
        };
        original.ExtDoorAccessPriv.Elements.Add(new DoorAccessPrivElement
        {
            Unid = 1,
            DoorUnid = 5,
            SchedRestriction = new SchedRestriction { SchedUnid = 10, Invert = false }
        });
        original.ExtDoorAccessPriv.Elements.Add(new DoorAccessPrivElement
        {
            Unid = 2,
            DoorUnid = 6
        });

        var result = RoundTrip(original, "08013208707269762D7461673A09707269762D757569645209416C6C20446F6F7273601E7001900100A2010A6578742D707269762D31AA06120A0A080120053A04080A10000A0408022006");
        Assert.AreEqual(1, result.Version);
        Assert.AreEqual("priv-tag", result.Tag);
        Assert.AreEqual("priv-uuid", result.Uuid);
        Assert.AreEqual("All Doors", result.Name);
        Assert.AreEqual(30, result.Unid);
        Assert.AreEqual(true, result.Enabled);
        Assert.AreEqual(PrivType.Door, result.PrivType);
        Assert.AreEqual("ext-priv-1", result.ExternalId);
        Assert.IsNotNull(result.ExtDoorAccessPriv);
        Assert.AreEqual(2, result.ExtDoorAccessPriv.Elements.Count);
        Assert.AreEqual(5, result.ExtDoorAccessPriv.Elements[0].DoorUnid);
        Assert.IsNotNull(result.ExtDoorAccessPriv.Elements[0].SchedRestriction);
        Assert.AreEqual(10, result.ExtDoorAccessPriv.Elements[0].SchedRestriction.SchedUnid);
        Assert.AreEqual(6, result.ExtDoorAccessPriv.Elements[1].DoorUnid);
    }

    // ==================== Device Types ====================

    [TestMethod]
    public void Dev_Controller_RoundTrip()
    {
        var original = new Dev
        {
            Version = 1,
            Tag = "dev-tag",
            Uuid = "dev-uuid",
            Name = "Main Controller",
            ExternalId = "ext-1",
            Unid = 1,
            Address = "192.168.1.100",
            LogicalAddress = 0,
            MacAddress = "00:11:22:33:44:55",
            Enabled = true,
            Port = 9723,
            Speed = 9600,
            DevType = DevType.IoController,
            DevMod = DevMod.IoControllerCommunity,
            DevPlatform = DevPlatform.Community,
            ExternalDevModText = "Community Controller",
            ExternalDevModId = "comm-ctrl",
            DevModConfig = new DevModConfig { Unid = 1, Version = 1, Type = DevModConfigType.ControllerZ9Spcore },
            DevUse = DevUse.ActuatorDoorStrike,
            PhysicalParentUnid = 0,
            LogicalParentUnid = 0,
            TimeZone = "America/New_York",
            IgnoreDaylightSavings = false,
            ExtController = new Controller
            {
                ControllerConfig = new ControllerConfig
                {
                    Version = 1,
                    Username = "admin",
                    Password = "pass123",
                    DevInitiatesConnection = true,
                    Unid = 1,
                    EncryptionKeyRefUnid = 10,
                    EncryptionKeyRefNextUnid = 11,
                    DisableEncryption = false
                }
            }
        };
        original.LogicalChildrenUnid.Add(2);
        original.LogicalChildrenUnid.Add(3);
        original.PhysicalChildrenUnid.Add(4);

        var result = RoundTrip(original, "080132076465762D7461673A086465762D75756964520F4D61696E20436F6E74726F6C6C65726A056578742D317001AA010D3139322E3136382E312E313030B00100BA011130303A31313A32323A33333A34343A3535C80101D001FB4BD801804BE001018002A401980211A20214436F6D6D756E69747920436F6E74726F6C6C6572AA0209636F6D6D2D6374726CB2020608011001180DB80200C80200D002008A0310416D65726963612F4E65775F596F726B900300A203020203AA030104BA061E0A1C0801120561646D696E1A077061737331323320012801580A600B6800");
        Assert.AreEqual(1, result.Version);
        Assert.AreEqual("dev-tag", result.Tag);
        Assert.AreEqual("dev-uuid", result.Uuid);
        Assert.AreEqual("Main Controller", result.Name);
        Assert.AreEqual("ext-1", result.ExternalId);
        Assert.AreEqual(1, result.Unid);
        Assert.AreEqual("192.168.1.100", result.Address);
        Assert.AreEqual(0, result.LogicalAddress);
        Assert.AreEqual("00:11:22:33:44:55", result.MacAddress);
        Assert.AreEqual(true, result.Enabled);
        Assert.AreEqual(9723, result.Port);
        Assert.AreEqual(9600, result.Speed);
        Assert.AreEqual(DevType.IoController, result.DevType);
        Assert.AreEqual(DevMod.IoControllerCommunity, result.DevMod);
        Assert.AreEqual(DevPlatform.Community, result.DevPlatform);
        Assert.AreEqual("Community Controller", result.ExternalDevModText);
        Assert.AreEqual("comm-ctrl", result.ExternalDevModId);
        Assert.IsNotNull(result.DevModConfig);
        Assert.AreEqual(DevModConfigType.ControllerZ9Spcore, result.DevModConfig.Type);
        Assert.AreEqual("America/New_York", result.TimeZone);
        Assert.AreEqual(false, result.IgnoreDaylightSavings);
        CollectionAssert.AreEqual(new[] { 2, 3 }, result.LogicalChildrenUnid.ToArray());
        CollectionAssert.AreEqual(new[] { 4 }, result.PhysicalChildrenUnid.ToArray());
        Assert.IsNotNull(result.ExtController);
        Assert.IsNotNull(result.ExtController.ControllerConfig);
        Assert.AreEqual("admin", result.ExtController.ControllerConfig.Username);
        Assert.AreEqual("pass123", result.ExtController.ControllerConfig.Password);
        Assert.AreEqual(true, result.ExtController.ControllerConfig.DevInitiatesConnection);
        Assert.AreEqual(10, result.ExtController.ControllerConfig.EncryptionKeyRefUnid);
        Assert.AreEqual(11, result.ExtController.ControllerConfig.EncryptionKeyRefNextUnid);
        Assert.AreEqual(false, result.ExtController.ControllerConfig.DisableEncryption);
    }

    [TestMethod]
    public void Dev_CredReader_RoundTrip()
    {
        var original = new Dev
        {
            Unid = 2,
            Name = "Front Door Reader",
            Address = "0",
            Enabled = true,
            DevType = DevType.CredReader,
            DevMod = DevMod.CredReaderOsdp,
            PhysicalParentUnid = 1,
            LogicalParentUnid = 5,
            ExtCredReader = new CredReader
            {
                CredReaderConfig = new CredReaderConfig
                {
                    Version = 1,
                    Username = "reader",
                    Password = "rpass",
                    DevInitiatesConnection = false,
                    Unid = 2,
                    CommType = CredReaderCommType.OsdpHalfDuplex,
                    TamperType = CredReaderTamperType.Osdp,
                    LedType = CredReaderLedType.Osdp,
                    EncryptionKeyRefUnid = 20,
                    EncryptionKeyRefNextUnid = 21,
                    SerialPortAddress = "/dev/ttyUSB0",
                    DisableEncryption = true
                }
            }
        };

        var result = RoundTrip(original, "521146726F6E7420446F6F72205265616465727002AA010130C80101E001048002E301C80201D00205C206350A33080112067265616465721A05727061737320002802300638025802980114A00115AA010C2F6465762F74747955534230B00101");
        Assert.AreEqual(DevType.CredReader, result.DevType);
        Assert.AreEqual(DevMod.CredReaderOsdp, result.DevMod);
        Assert.IsNotNull(result.ExtCredReader);
        var config = result.ExtCredReader.CredReaderConfig;
        Assert.IsNotNull(config);
        Assert.AreEqual(CredReaderCommType.OsdpHalfDuplex, config.CommType);
        Assert.AreEqual(CredReaderTamperType.Osdp, config.TamperType);
        Assert.AreEqual(CredReaderLedType.Osdp, config.LedType);
        Assert.AreEqual("/dev/ttyUSB0", config.SerialPortAddress);
        Assert.AreEqual(true, config.DisableEncryption);
    }

    [TestMethod]
    public void Dev_Door_RoundTrip()
    {
        var original = new Dev
        {
            Unid = 5,
            Name = "Front Door",
            Address = "door-1",
            Enabled = true,
            DevType = DevType.Door,
            DevMod = DevMod.DoorCommunity,
            ExtDoor = new Door
            {
                DoorConfig = new DoorConfig
                {
                    Version = 1,
                    Username = "door-user",
                    Password = "door-pass",
                    DevInitiatesConnection = false,
                    DefaultDoorMode = new DoorMode
                    {
                        StaticState = DoorModeStaticState.AccessControlled,
                        AllowUniquePin = true,
                        AllowCard = true,
                        RequireConfirmingPinWithCard = false
                    },
                    ActivateStrikeOnRex = true,
                    StrikeTime = 5000,
                    ExtendedStrikeTime = 10000,
                    HeldTime = 3000,
                    ExtendedHeldTime = 6000,
                    EncryptionKeyRefUnid = 30,
                    EncryptionKeyRefNextUnid = 31,
                    DisableEncryption = false
                }
            }
        };

        var result = RoundTrip(original, "520A46726F6E7420446F6F727005AA0106646F6F722D31C80101E0010580028302DA063F0A3D08011209646F6F722D757365721A09646F6F722D70617373200032080802100118012000580170882778904E8001B8178801F02EF8021E80031F980300");
        Assert.AreEqual(DevType.Door, result.DevType);
        Assert.AreEqual(DevMod.DoorCommunity, result.DevMod);
        Assert.IsNotNull(result.ExtDoor);
        var doorConfig = result.ExtDoor.DoorConfig;
        Assert.IsNotNull(doorConfig);
        Assert.AreEqual("door-user", doorConfig.Username);
        Assert.IsNotNull(doorConfig.DefaultDoorMode);
        Assert.AreEqual(DoorModeStaticState.AccessControlled, doorConfig.DefaultDoorMode.StaticState);
        Assert.AreEqual(true, doorConfig.DefaultDoorMode.AllowUniquePin);
        Assert.AreEqual(true, doorConfig.DefaultDoorMode.AllowCard);
        Assert.AreEqual(false, doorConfig.DefaultDoorMode.RequireConfirmingPinWithCard);
        Assert.AreEqual(true, doorConfig.ActivateStrikeOnRex);
        Assert.AreEqual(5000, doorConfig.StrikeTime);
        Assert.AreEqual(10000, doorConfig.ExtendedStrikeTime);
        Assert.AreEqual(3000, doorConfig.HeldTime);
        Assert.AreEqual(6000, doorConfig.ExtendedHeldTime);
    }

    [TestMethod]
    public void Dev_Actuator_RoundTrip()
    {
        var original = new Dev
        {
            Unid = 3,
            Name = "Door Strike",
            Address = "0",
            Enabled = true,
            DevType = DevType.Actuator,
            DevMod = DevMod.ActuatorDigital,
            DevUse = DevUse.ActuatorDoorStrike,
            ExtActuator = new Actuator
            {
                ActuatorConfig = new ActuatorConfig
                {
                    Version = 1,
                    Username = "act-user",
                    Password = "act-pass",
                    DevInitiatesConnection = false,
                    Unid = 3,
                    Invert = true,
                    EncryptionKeyRefUnid = 40,
                    EncryptionKeyRefNextUnid = 41,
                    DisableEncryption = false
                }
            }
        };

        var result = RoundTrip(original, "520B446F6F7220537472696B657003AA010130C80101E00103800212B80200A206250A23080112086163742D757365721A086163742D7061737320002803300170287829800100");
        Assert.AreEqual(DevType.Actuator, result.DevType);
        Assert.AreEqual(DevMod.ActuatorDigital, result.DevMod);
        Assert.AreEqual(DevUse.ActuatorDoorStrike, result.DevUse);
        Assert.IsNotNull(result.ExtActuator);
        Assert.IsNotNull(result.ExtActuator.ActuatorConfig);
        Assert.AreEqual(true, result.ExtActuator.ActuatorConfig.Invert);
    }

    [TestMethod]
    public void Dev_Sensor_RoundTrip()
    {
        var original = new Dev
        {
            Unid = 4,
            Name = "Door Contact",
            Address = "0",
            Enabled = true,
            DevType = DevType.Sensor,
            DevMod = DevMod.SensorDigital,
            DevUse = DevUse.SensorDoorContact,
            ExtSensor = new Sensor
            {
                SensorConfig = new SensorConfig
                {
                    Version = 1,
                    Username = "sensor-user",
                    Password = "sensor-pass",
                    DevInitiatesConnection = false,
                    Unid = 4,
                    Invert = false,
                    EncryptionKeyRefUnid = 50,
                    EncryptionKeyRefNextUnid = 51,
                    DisableEncryption = true
                }
            }
        };

        var result = RoundTrip(original, "520C446F6F7220436F6E746163747004AA010130C80101E0010280020EB8020A9A072D0A2B0801120B73656E736F722D757365721A0B73656E736F722D70617373200028044000B00132B80133C00101");
        Assert.AreEqual(DevType.Sensor, result.DevType);
        Assert.AreEqual(DevUse.SensorDoorContact, result.DevUse);
        Assert.IsNotNull(result.ExtSensor);
        Assert.IsNotNull(result.ExtSensor.SensorConfig);
        Assert.AreEqual(false, result.ExtSensor.SensorConfig.Invert);
        Assert.AreEqual(true, result.ExtSensor.SensorConfig.DisableEncryption);
    }

    [TestMethod]
    public void DoorMode_RoundTrip()
    {
        var original = new DoorMode
        {
            StaticState = DoorModeStaticState.AccessControlled,
            AllowUniquePin = true,
            AllowCard = true,
            RequireConfirmingPinWithCard = false
        };
        var result = RoundTrip(original, "0802100118012000");
        Assert.AreEqual(DoorModeStaticState.AccessControlled, result.StaticState);
        Assert.AreEqual(true, result.AllowUniquePin);
        Assert.AreEqual(true, result.AllowCard);
        Assert.AreEqual(false, result.RequireConfirmingPinWithCard);
    }

    // ==================== Device State Types ====================

    [TestMethod]
    public void DevAspectState_AllFields_RoundTrip()
    {
        var original = new DevAspectState
        {
            Unid = 1,
            DevAspect = DevAspect.Primary,
            HwTime = new DateTimeData { Millis = 1700000000000L },
            DbTime = new DateTimeData { Millis = 1700000000001L },
            CommState = CommState.Online,
            CommStateStale = false,
            TamperState = TamperState.Normal,
            TamperStateStale = false,
            ActivityState = ActivityState.Active,
            ActivityStateStale = true,
            DoorMode = new DoorMode
            {
                StaticState = DoorModeStaticState.Unlocked,
                AllowUniquePin = false,
                AllowCard = false,
                RequireConfirmingPinWithCard = false
            },
            DoorModeStale = false,
            ExternalState = "custom-state",
            ExternalStateStale = true
        };
        var result = RoundTrip(original, "080110001A070880D095FFBC3122070881D095FFBC31800101880100D00100D80100D00201D80201E202080801100018002000E80200A2030C637573746F6D2D7374617465A80301");
        Assert.AreEqual(1, result.Unid);
        Assert.AreEqual(DevAspect.Primary, result.DevAspect);
        Assert.AreEqual(1700000000000L, result.HwTime.Millis);
        Assert.AreEqual(1700000000001L, result.DbTime.Millis);
        Assert.AreEqual(CommState.Online, result.CommState);
        Assert.AreEqual(false, result.CommStateStale);
        Assert.AreEqual(TamperState.Normal, result.TamperState);
        Assert.AreEqual(false, result.TamperStateStale);
        Assert.AreEqual(ActivityState.Active, result.ActivityState);
        Assert.AreEqual(true, result.ActivityStateStale);
        Assert.IsNotNull(result.DoorMode);
        Assert.AreEqual(DoorModeStaticState.Unlocked, result.DoorMode.StaticState);
        Assert.AreEqual(false, result.DoorModeStale);
        Assert.AreEqual("custom-state", result.ExternalState);
        Assert.AreEqual(true, result.ExternalStateStale);
    }

    [TestMethod]
    public void DevState_RoundTrip()
    {
        var original = new DevState();
        original.DevAspectStates.Add(new DevState.Types.DevAspect_DevAspectState_Entry
        {
            Key = DevAspect.Primary,
            Value = new DevAspectState
            {
                DevAspect = DevAspect.Primary,
                HwTime = new DateTimeData { Millis = 100 },
                DbTime = new DateTimeData { Millis = 200 },
                CommState = CommState.Online
            }
        });
        original.DevAspectStates.Add(new DevState.Types.DevAspect_DevAspectState_Entry
        {
            Key = DevAspect.DoorForced,
            Value = new DevAspectState
            {
                DevAspect = DevAspect.DoorForced,
                HwTime = new DateTimeData { Millis = 300 },
                DbTime = new DateTimeData { Millis = 400 },
                ActivityState = ActivityState.Active
            }
        });

        var result = RoundTrip(original, "0A120800120E10001A020864220308C8018001010A130805120F10051A0308AC022203089003D00201");
        Assert.AreEqual(2, result.DevAspectStates.Count);
        Assert.AreEqual(DevAspect.Primary, result.DevAspectStates[0].Key);
        Assert.AreEqual(CommState.Online, result.DevAspectStates[0].Value.CommState);
        Assert.AreEqual(DevAspect.DoorForced, result.DevAspectStates[1].Key);
        Assert.AreEqual(ActivityState.Active, result.DevAspectStates[1].Value.ActivityState);
    }

    [TestMethod]
    public void DevStateRecord_RoundTrip()
    {
        var original = new DevStateRecord
        {
            Unid = 42,
            DevUnid = 5,
            DevState = new DevState()
        };
        original.DevState.DevAspectStates.Add(new DevState.Types.DevAspect_DevAspectState_Entry
        {
            Key = DevAspect.DoorOpen,
            Value = new DevAspectState
            {
                DevAspect = DevAspect.DoorOpen,
                HwTime = new DateTimeData { Millis = 500 },
                DbTime = new DateTimeData { Millis = 600 },
                ActivityState = ActivityState.Inactive
            }
        });

        var result = RoundTrip(original, "082A10051A150A130810120F10101A0308F403220308D804D00200");
        Assert.AreEqual(42, result.Unid);
        Assert.AreEqual(5, result.DevUnid);
        Assert.IsNotNull(result.DevState);
        Assert.AreEqual(1, result.DevState.DevAspectStates.Count);
        Assert.AreEqual(DevAspect.DoorOpen, result.DevState.DevAspectStates[0].Key);
    }

    // ==================== Device Action Types ====================

    [TestMethod]
    public void DevActionParams_DoorMode_RoundTrip()
    {
        var original = new DevActionParams
        {
            Unid = 1,
            Type = DevActionParamsType.DoorMode,
            ExtDoorModeDevActionParams = new DoorModeDevActionParams
            {
                ResetToDefault = false,
                DoorMode = new DoorMode
                {
                    StaticState = DoorModeStaticState.Unlocked,
                    AllowUniquePin = false,
                    AllowCard = false,
                    RequireConfirmingPinWithCard = false
                }
            }
        };
        var result = RoundTrip(original, "08011801D2060C080012080801100018002000");
        Assert.AreEqual(DevActionParamsType.DoorMode, result.Type);
        Assert.IsNotNull(result.ExtDoorModeDevActionParams);
        Assert.AreEqual(false, result.ExtDoorModeDevActionParams.ResetToDefault);
        Assert.IsNotNull(result.ExtDoorModeDevActionParams.DoorMode);
        Assert.AreEqual(DoorModeStaticState.Unlocked, result.ExtDoorModeDevActionParams.DoorMode.StaticState);
    }

    [TestMethod]
    public void DevActionParams_DoorMomentaryUnlock_RoundTrip()
    {
        var original = new DevActionParams
        {
            Unid = 2,
            Type = DevActionParamsType.DoorMomentaryUnlock,
            ExtDoorMomentaryUnlockDevActionParams = new DoorMomentaryUnlockDevActionParams()
        };
        var result = RoundTrip(original, "0802180ADA0600");
        Assert.AreEqual(DevActionParamsType.DoorMomentaryUnlock, result.Type);
        Assert.IsNotNull(result.ExtDoorMomentaryUnlockDevActionParams);
    }

    // ==================== Event Types ====================

    [TestMethod]
    public void Evt_AllFields_RoundTrip()
    {
        var original = new Evt
        {
            Unid = 1000L,
            HwTime = new DateTimeData { Millis = 1700000000000L },
            DbTime = new DateTimeData { Millis = 1700000000001L },
            HwTimeZone = "America/New_York",
            EvtCode = EvtCode.DoorAccessGranted,
            ExternalEvtCodeText = "ext-code",
            ExternalEvtCodeId = "ext-code-id",
            EvtSubCode = EvtSubCode.AccessDeniedNoPriv,
            ExternalSubCodeText = "ext-sub",
            ExternalSubCodeId = "ext-sub-id",
            EvtModifiers = new EvtModifiers
            {
                InaccurateHwTime = false,
                UsedCard = true,
                UsedPin = false
            },
            Priority = 0,
            Data = "extra-data",
            EvtDevRef = new EvtDevRef
            {
                Unid = 5,
                Name = "Front Door",
                Address = "door-1",
                LogicalAddress = 0,
                DevPlatform = DevPlatform.Community,
                DevType = DevType.Door,
                ExternalDevTypeText = "ext-dt",
                ExternalDevTypeId = "ext-dt-id",
                DevMod = DevMod.DoorCommunity,
                ExternalDevModText = "ext-dm",
                ExternalDevModId = "ext-dm-id",
                DevUse = DevUse.ActuatorDoorStrike,
                Tag = "devref-tag",
                Uuid = "devref-uuid",
                ExternalId = "devref-ext"
            },
            EvtControllerRef = new EvtDevRef
            {
                Unid = 1,
                Name = "Controller",
                DevType = DevType.IoController,
                DevMod = DevMod.IoControllerCommunity
            },
            EvtCredRef = new EvtCredRef
            {
                Unid = 100,
                CredTemplateRef = new EvtCredTemplateRef
                {
                    Unid = 50,
                    Name = "Standard Card",
                    Tag = "ct-tag",
                    Uuid = "ct-uuid"
                },
                Name = "John Doe",
                CredNum = new BigIntegerData { Bytes = ByteString.CopyFrom(0, 0x30, 0x39) },
                FacilityCode = 200,
                Tag = "cred-tag",
                Uuid = "cred-uuid"
            },
            EvtSchedRef = new EvtSchedRef
            {
                Unid = 10,
                Name = "Business Hours",
                Invert = false,
                Tag = "sched-tag",
                Uuid = "sched-uuid"
            },
            Consumed = false,
            Uuid = "evt-uuid"
        };

        var result = RoundTrip(original, "08E80712070880D095FFBC311A070881D095FFBC313210416D65726963612F4E65775F596F726B383042086578742D636F64654A0B6578742D636F64652D6964500A5A076578742D737562620A6578742D7375622D69646A065800680170007000A2010A65787472612D64617461CA016F0805120A46726F6E7420446F6F721A06646F6F722D312000281130053A066578742D647442096578742D64742D696450830262066578742D646D6A096578742D646D2D6964700082010A6465767265662D7461678A010B6465767265662D7575696492010A6465767265662D657874D201130801120A436F6E74726F6C6C6572300150A401F2014F086412220832120D5374616E6461726420436172641A0663742D746167220763742D7575696432084A6F686E20446F653A050A0300303940C8015A08637265642D7461676209637265642D7575696492022B080A120E427573696E65737320486F7572731800220973636865642D7461672A0A73636865642D75756964F002009203086576742D75756964");
        Assert.AreEqual(1000L, result.Unid);
        Assert.AreEqual(1700000000000L, result.HwTime.Millis);
        Assert.AreEqual(1700000000001L, result.DbTime.Millis);
        Assert.AreEqual("America/New_York", result.HwTimeZone);
        Assert.AreEqual(EvtCode.DoorAccessGranted, result.EvtCode);
        Assert.AreEqual("ext-code", result.ExternalEvtCodeText);
        Assert.AreEqual("ext-code-id", result.ExternalEvtCodeId);
        Assert.AreEqual(EvtSubCode.AccessDeniedNoPriv, result.EvtSubCode);
        Assert.AreEqual("ext-sub", result.ExternalSubCodeText);
        Assert.AreEqual("ext-sub-id", result.ExternalSubCodeId);
        Assert.IsNotNull(result.EvtModifiers);
        Assert.AreEqual(false, result.EvtModifiers.InaccurateHwTime);
        Assert.AreEqual(true, result.EvtModifiers.UsedCard);
        Assert.AreEqual(false, result.EvtModifiers.UsedPin);
        Assert.AreEqual(0, result.Priority);
        Assert.AreEqual("extra-data", result.Data);

        // EvtDevRef
        Assert.IsNotNull(result.EvtDevRef);
        Assert.AreEqual(5, result.EvtDevRef.Unid);
        Assert.AreEqual("Front Door", result.EvtDevRef.Name);
        Assert.AreEqual("door-1", result.EvtDevRef.Address);
        Assert.AreEqual(DevPlatform.Community, result.EvtDevRef.DevPlatform);
        Assert.AreEqual(DevType.Door, result.EvtDevRef.DevType);
        Assert.AreEqual("ext-dt", result.EvtDevRef.ExternalDevTypeText);
        Assert.AreEqual("ext-dt-id", result.EvtDevRef.ExternalDevTypeId);
        Assert.AreEqual(DevMod.DoorCommunity, result.EvtDevRef.DevMod);
        Assert.AreEqual("ext-dm", result.EvtDevRef.ExternalDevModText);
        Assert.AreEqual("ext-dm-id", result.EvtDevRef.ExternalDevModId);
        Assert.AreEqual(DevUse.ActuatorDoorStrike, result.EvtDevRef.DevUse);
        Assert.AreEqual("devref-tag", result.EvtDevRef.Tag);
        Assert.AreEqual("devref-uuid", result.EvtDevRef.Uuid);
        Assert.AreEqual("devref-ext", result.EvtDevRef.ExternalId);

        // EvtControllerRef
        Assert.IsNotNull(result.EvtControllerRef);
        Assert.AreEqual(1, result.EvtControllerRef.Unid);
        Assert.AreEqual(DevMod.IoControllerCommunity, result.EvtControllerRef.DevMod);

        // EvtCredRef
        Assert.IsNotNull(result.EvtCredRef);
        Assert.AreEqual(100, result.EvtCredRef.Unid);
        Assert.AreEqual("John Doe", result.EvtCredRef.Name);
        Assert.AreEqual(200, result.EvtCredRef.FacilityCode);
        Assert.AreEqual("cred-tag", result.EvtCredRef.Tag);
        Assert.IsNotNull(result.EvtCredRef.CredTemplateRef);
        Assert.AreEqual(50, result.EvtCredRef.CredTemplateRef.Unid);
        Assert.AreEqual("Standard Card", result.EvtCredRef.CredTemplateRef.Name);

        // EvtSchedRef
        Assert.IsNotNull(result.EvtSchedRef);
        Assert.AreEqual(10, result.EvtSchedRef.Unid);
        Assert.AreEqual("Business Hours", result.EvtSchedRef.Name);
        Assert.AreEqual(false, result.EvtSchedRef.Invert);

        Assert.AreEqual(false, result.Consumed);
        Assert.AreEqual("evt-uuid", result.Uuid);
    }

    // ==================== Schedule & Holiday Types ====================

    [TestMethod]
    public void Sched_RoundTrip()
    {
        var original = new Sched
        {
            Version = 1,
            Tag = "sched-tag",
            Uuid = "sched-uuid",
            Name = "Business Hours",
            ExternalId = "ext-sched-1",
            Unid = 10
        };
        original.Elements.Add(new SchedElement
        {
            Unid = 1,
            Holidays = false,
            Start = new SqlTimeData { Hour = 8, Minute = 0, Second = 0 },
            Stop = new SqlTimeData { Hour = 17, Minute = 0, Second = 0 },
            PlusDays = 0
        });
        original.Elements[0].SchedDays.Add(SchedDay.Mon);
        original.Elements[0].SchedDays.Add(SchedDay.Tues);
        original.Elements[0].SchedDays.Add(SchedDay.Wed);
        original.Elements[0].SchedDays.Add(SchedDay.Thur);
        original.Elements[0].SchedDays.Add(SchedDay.Fri);
        original.Elements.Add(new SchedElement
        {
            Unid = 2,
            Holidays = true,
            Start = new SqlTimeData { Hour = 0, Minute = 0, Second = 0 },
            Stop = new SqlTimeData { Hour = 23, Minute = 59, Second = 59 },
            PlusDays = 0
        });
        original.Elements[1].HolTypesUnid.Add(1);
        original.Elements[1].HolTypesUnid.Add(2);

        var result = RoundTrip(original, "0801320973636865642D7461673A0A73636865642D75756964520E427573696E65737320486F7572736A0B6578742D73636865642D31700A82011D08011205000102030418002A060808100018003206081110001800380082011A08021801220201022A0608001000180032060817103B183B3800");
        Assert.AreEqual(1, result.Version);
        Assert.AreEqual("sched-tag", result.Tag);
        Assert.AreEqual("Business Hours", result.Name);
        Assert.AreEqual("ext-sched-1", result.ExternalId);
        Assert.AreEqual(10, result.Unid);
        Assert.AreEqual(2, result.Elements.Count);

        var weekday = result.Elements[0];
        Assert.AreEqual(false, weekday.Holidays);
        Assert.AreEqual(8, weekday.Start.Hour);
        Assert.AreEqual(17, weekday.Stop.Hour);
        Assert.AreEqual(0, weekday.PlusDays);
        CollectionAssert.AreEqual(
            new[] { SchedDay.Mon, SchedDay.Tues, SchedDay.Wed, SchedDay.Thur, SchedDay.Fri },
            weekday.SchedDays.ToArray());

        var holiday = result.Elements[1];
        Assert.AreEqual(true, holiday.Holidays);
        CollectionAssert.AreEqual(new[] { 1, 2 }, holiday.HolTypesUnid.ToArray());
    }

    [TestMethod]
    public void SchedRestriction_RoundTrip()
    {
        var original = new SchedRestriction { SchedUnid = 10, Invert = true };
        var result = RoundTrip(original, "080A1001");
        Assert.AreEqual(10, result.SchedUnid);
        Assert.AreEqual(true, result.Invert);
    }

    [TestMethod]
    public void HolCal_RoundTrip()
    {
        var original = new HolCal
        {
            Version = 1,
            Tag = "hc-tag",
            Uuid = "hc-uuid",
            Name = "US Holidays",
            Unid = 1
        };
        var result = RoundTrip(original, "0801320668632D7461673A0768632D75756964520B555320486F6C69646179736001");
        Assert.AreEqual(1, result.Version);
        Assert.AreEqual("hc-tag", result.Tag);
        Assert.AreEqual("US Holidays", result.Name);
        Assert.AreEqual(1, result.Unid);
    }

    [TestMethod]
    public void HolType_RoundTrip()
    {
        var original = new HolType
        {
            Version = 1,
            Tag = "ht-tag",
            Uuid = "ht-uuid",
            Name = "Federal Holiday",
            ExternalId = "ext-ht-1",
            Unid = 2
        };
        var result = RoundTrip(original, "0801320668742D7461673A0768742D75756964520F4665646572616C20486F6C696461796A086578742D68742D317002");
        Assert.AreEqual("ht-tag", result.Tag);
        Assert.AreEqual("Federal Holiday", result.Name);
        Assert.AreEqual("ext-ht-1", result.ExternalId);
        Assert.AreEqual(2, result.Unid);
    }

    [TestMethod]
    public void Hol_RoundTrip()
    {
        var original = new Hol
        {
            Version = 1,
            Tag = "hol-tag",
            Uuid = "hol-uuid",
            Name = "Christmas",
            Unid = 5,
            AllHolTypes = false,
            Date = new SqlDateData { Year = 2025, Month = 12, Day = 25 },
            NumDays = 1,
            Repeat = true,
            NumYearsRepeat = 10,
            PreserveSchedDay = false,
            HolCalUnid = 1
        };
        original.HolTypesUnid.Add(2);
        original.HolTypesUnid.Add(3);

        var result = RoundTrip(original, "08013207686F6C2D7461673A08686F6C2D7575696452094368726973746D6173600572020203780082010708E90F100C181988010190010198010AA00100A80101");
        Assert.AreEqual(1, result.Version);
        Assert.AreEqual("hol-tag", result.Tag);
        Assert.AreEqual("Christmas", result.Name);
        Assert.AreEqual(5, result.Unid);
        Assert.AreEqual(false, result.AllHolTypes);
        Assert.AreEqual(2025, result.Date.Year);
        Assert.AreEqual(12, result.Date.Month);
        Assert.AreEqual(25, result.Date.Day);
        Assert.AreEqual(1, result.NumDays);
        Assert.AreEqual(true, result.Repeat);
        Assert.AreEqual(10, result.NumYearsRepeat);
        Assert.AreEqual(false, result.PreserveSchedDay);
        Assert.AreEqual(1, result.HolCalUnid);
        CollectionAssert.AreEqual(new[] { 2, 3 }, result.HolTypesUnid.ToArray());
    }

    // ==================== Restriction Types ====================

    [TestMethod]
    public void EvtCodeRestriction_RoundTrip()
    {
        var original = new EvtCodeRestriction { Invert = false, EmptyIsNone = true };
        original.EvtCodes.Add(EvtCode.DoorAccessGranted);
        original.EvtCodes.Add(EvtCode.DoorAccessDenied);
        var result = RoundTrip(original, "0A02303110001801");
        Assert.AreEqual(false, result.Invert);
        Assert.AreEqual(true, result.EmptyIsNone);
        Assert.AreEqual(2, result.EvtCodes.Count);
        Assert.AreEqual(EvtCode.DoorAccessGranted, result.EvtCodes[0]);
        Assert.AreEqual(EvtCode.DoorAccessDenied, result.EvtCodes[1]);
    }

    [TestMethod]
    public void EvtRestriction_RoundTrip()
    {
        var original = new EvtRestriction
        {
            EvtCodeRestriction = new EvtCodeRestriction { Invert = false },
            GeneralDevRestriction = new GeneralDevRestriction
            {
                DevModRestriction = new DevModRestriction { Invert = false },
                DevTypeRestriction = new DevTypeRestriction { Invert = false },
                DevSubTypeRestriction = new DevSubTypeRestriction { Invert = true },
                DevUseRestriction = new DevUseRestriction { Invert = false },
                EnabledRestriction = new EnabledRestriction { EnabledValue = true, Invert = false }
            }
        };
        original.EvtCodeRestriction.EvtCodes.Add(EvtCode.DoorForced);
        original.GeneralDevRestriction.DevModRestriction.DevMods.Add(DevMod.DoorCommunity);
        original.GeneralDevRestriction.DevTypeRestriction.DevTypes.Add(DevType.Door);
        original.GeneralDevRestriction.DevSubTypeRestriction.DevSubTypes.Add(DevSubType.IoControllerSub);
        original.GeneralDevRestriction.DevUseRestriction.DevUses.Add(DevUse.SensorDoorContact);

        var result = RoundTrip(original, "0A050A013410003A230A060A028302100012050A010710011A050A0105100022050A010A1000720408011000");
        Assert.IsNotNull(result.EvtCodeRestriction);
        Assert.AreEqual(1, result.EvtCodeRestriction.EvtCodes.Count);
        Assert.AreEqual(EvtCode.DoorForced, result.EvtCodeRestriction.EvtCodes[0]);
        Assert.IsNotNull(result.GeneralDevRestriction);
        Assert.IsNotNull(result.GeneralDevRestriction.DevModRestriction);
        Assert.AreEqual(DevMod.DoorCommunity, result.GeneralDevRestriction.DevModRestriction.DevMods[0]);
        Assert.AreEqual(DevType.Door, result.GeneralDevRestriction.DevTypeRestriction.DevTypes[0]);
        Assert.AreEqual(true, result.GeneralDevRestriction.DevSubTypeRestriction.Invert);
        Assert.AreEqual(DevSubType.IoControllerSub, result.GeneralDevRestriction.DevSubTypeRestriction.DevSubTypes[0]);
        Assert.AreEqual(DevUse.SensorDoorContact, result.GeneralDevRestriction.DevUseRestriction.DevUses[0]);
        Assert.AreEqual(true, result.GeneralDevRestriction.EnabledRestriction.EnabledValue);
    }

    [TestMethod]
    public void SpecificDevRestriction_RoundTrip()
    {
        var original = new SpecificDevRestriction { Invert = true };
        original.DevsUnid.Add(1);
        original.DevsUnid.Add(5);
        original.DevsUnid.Add(10);
        var result = RoundTrip(original, "0A0301050A1001");
        Assert.AreEqual(true, result.Invert);
        CollectionAssert.AreEqual(new[] { 1, 5, 10 }, result.DevsUnid.ToArray());
    }
}
}
