using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    /// <summary>
    /// Utilities for dealing with Sp-Core protobuf data structures.
    /// </summary>
    public class SpCoreProtoUtil
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static bool IsSpCoreDevMod(DevMod devMod)
        {
            switch (devMod)
            {
                case DevMod.IoControllerZ9Spcore:
                    return true;
                default:
                    return false;
            }
        }

        //static DateTime Epoch = new DateTime(1970, 1, 1);
        static DateTime EpochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Convert a DateTimeData to a DateTime
        /// </summary>
        /// <param name="dtd"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(DateTimeData dtd)
        {
           return EpochUtc.AddMilliseconds(dtd.Millis);
        }

        /// <summary>
        /// Converts a DateTime to a DateTimeData
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTimeData ToDateTimeData(DateTime dateTime)
        {
            if (dateTime == null)
                return null;

            DateTimeData result = new DateTimeData();

            result.Millis = (long) dateTime.ToUniversalTime().Subtract(EpochUtc).TotalMilliseconds;

            return result;
        }

        public static SqlTimeData ToSqlTimeData(int hour, int minute, int second)
        {
            SqlTimeData result = new SqlTimeData();
            result.Hour = hour;
            result.Minute = minute;
            result.Second = second;
            return result;
        }

        public static Boolean IsEmpty(DoorMode value)
        {
            return
                value.StaticStateCase != DoorMode.StaticStateOneofCase.StaticState &&
                value.AllowUniquePinCase != DoorMode.AllowUniquePinOneofCase.AllowUniquePin &&
                value.AllowCardCase != DoorMode.AllowCardOneofCase.AllowCard &&
                value.RequireConfirmingPinWithCardCase != DoorMode.RequireConfirmingPinWithCardOneofCase.RequireConfirmingPinWithCard
            ;
        }

        public static BigIntegerData ToBigIntegerData(UInt32 value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            BigIntegerData result = new BigIntegerData();

            int trimLeadingBytes = 0;
            for (int i = 0; i < bytes.Length; ++i)
            {
                if (bytes[i] == 0)
                    ++trimLeadingBytes;
                else
                    break;
            }

            int trimmedLen = bytes.Length - trimLeadingBytes;
            byte[] trimmedBytes = new byte[trimmedLen];
            for (int i = 0; i < trimmedLen; ++i)
                trimmedBytes[i] = bytes[trimLeadingBytes + i];
            
            result.Bytes = ByteString.CopyFrom(trimmedBytes);
            return result;
        }

        public static BigIntegerData ToBigIntegerData(UInt64 value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            BigIntegerData result = new BigIntegerData();

            int trimLeadingBytes = 0;
            for (int i = 0; i < bytes.Length; ++i)
            {
                if (bytes[i] == 0)
                    ++trimLeadingBytes;
                else
                    break;
            }

            int trimmedLen = bytes.Length - trimLeadingBytes;
            byte[] trimmedBytes = new byte[trimmedLen];
            for (int i = 0; i < trimmedLen; ++i)
                trimmedBytes[i] = bytes[trimLeadingBytes + i];

            result.Bytes = ByteString.CopyFrom(trimmedBytes);
            return result;
        }

        public static BigIntegerData ToBigIntegerData(BigInteger value)
        {
            byte[] bytes = value.ToByteArray();
            Array.Reverse(bytes); // BigInteger.ToByteArray always returns little-endian
            BigIntegerData result = new BigIntegerData();

            int trimLeadingBytes = 0;
            for (int i = 0; i < bytes.Length; ++i)
            {
                if (bytes[i] == 0)
                    ++trimLeadingBytes;
                else
                    break;
            }

            int trimmedLen = bytes.Length - trimLeadingBytes;
            byte[] trimmedBytes = new byte[trimmedLen];
            for (int i = 0; i < trimmedLen; ++i)
                trimmedBytes[i] = bytes[trimLeadingBytes + i];

            result.Bytes = ByteString.CopyFrom(trimmedBytes);
            return result;
        }

        public static BigInteger ToBigInteger(BigIntegerData data)
        {
            byte[] bytes = data.Bytes.ToByteArray();

            int trimLeadingBytes = 0;
            for (int i = 0; i < bytes.Length; ++i)
            {
                if (bytes[i] == 0)
                    ++trimLeadingBytes;
                else
                    break;
            }

            // trim the bytes, but make sure we have 1 extra byte of 0 on the most-significant side so that 
            // BigInteger will be unsigned (avoid having it interpret MSB of 1 as a sign bit)
            int trimmedLen = bytes.Length - trimLeadingBytes;
            byte[] trimmedBytes = new byte[trimmedLen + 1]; // +1 for the extra byte
            for (int i = 0; i < trimmedLen; ++i)
                trimmedBytes[i + 1] = bytes[trimLeadingBytes + i];

            Array.Reverse(trimmedBytes); // BigInteger byte array constructor expects little-endian

            return new BigInteger(trimmedBytes);
        }

        public static UInt64 ToLong(BigIntegerData value)
        {
            UInt64 result = 0;
            foreach (byte b in value.Bytes)
            {
                result = result << 8;
                result += b;
            }

            // TODO: check for overflow

            return result;
        }

        public static void InitRequired(DataLayout o)
        {
            if (o.EnabledCase == DataLayout.EnabledOneofCase.None)
                o.Enabled = false;
            if (o.PriorityCase == DataLayout.PriorityOneofCase.None)
                o.Priority = 0;
        }

        public static void InitRequired(DataFormat o)
        {
        }

        public static void InitRequired(Dev o, bool logWarning = false)
        {
            if (o.EnabledCase == Dev.EnabledOneofCase.None)
            {
                if (logWarning)
                    Logger.Warn("Defaulting Dev.Enabled to false");

                o.Enabled = false;
            }

            if (o.DevTypeCase == Dev.DevTypeOneofCase.DevType)
            {
                switch (o.DevType)
                {
                    case DevType.IoController:
                        if (o.ExtController == null)
                        {
                            if (logWarning)
                                Logger.Warn("Creating Dev.ExtController");

                            o.ExtController = new Controller();
                        }
                        if (o.ExtController.ControllerConfig == null)
                            o.ExtController.ControllerConfig = new ControllerConfig(); // not actually required, but leaving for backwards compatibilty 
                        InitRequired(o.ExtController.ControllerConfig, logWarning);
                        break;
                    case DevType.Sensor:
                        if (o.ExtSensor == null)
                        {
                            if (logWarning)
                                Logger.Warn("Creating Dev.ExtSensor");

                            o.ExtSensor = new Sensor();
                        }
                        if (o.ExtSensor.SensorConfig == null)
                            o.ExtSensor.SensorConfig = new SensorConfig();  // not actually required, but leaving for backwards compatibilty 
                        InitRequired(o.ExtSensor.SensorConfig, logWarning);
                        break;
                    case DevType.Actuator:
                        if (o.ExtActuator == null)
                        {
                            if (logWarning)
                                Logger.Warn("Creating Dev.ExtActuator");

                            o.ExtActuator = new Actuator();
                        }
                        if (o.ExtActuator.ActuatorConfig == null)
                            o.ExtActuator.ActuatorConfig = new ActuatorConfig();    // not actually required, but leaving for backwards compatibilty 
                        InitRequired(o.ExtActuator.ActuatorConfig, logWarning);
                        break;
                    case DevType.CredReader:
                        if (o.ExtCredReader == null)
                        {
                            if (logWarning)
                                Logger.Warn("Creating Dev.ExtCredReader");

                            o.ExtCredReader = new CredReader();
                        }
                        if (o.ExtCredReader.CredReaderConfig == null)
                            o.ExtCredReader.CredReaderConfig = new CredReaderConfig();
                        InitRequired(o.ExtCredReader.CredReaderConfig, logWarning);
                        break;
                    case DevType.Door:
                        if (o.ExtDoor == null)
                        {
                            if (logWarning)
                                Logger.Warn("Creating Dev.ExtDoor");

                            o.ExtDoor = new Door();
                        }
                        break;
                }
            }
            else
            {
                if (logWarning)
                    Logger.Warn("Dev.DevType is not set");
            }
        }

        public static void InitRequired(ControllerConfig o, bool logWarning = false)
        {
        }

        public static void InitRequired(SensorConfig o, bool logWarning = false)
        {
            if (o.InvertCase == SensorConfig.InvertOneofCase.None)
            {
                if (logWarning)
                    Logger.Warn("Defaulting SensorConfig.Invert to false");

                o.Invert = false;
            }
        }

        public static void InitRequired(ActuatorConfig o, bool logWarning = false)
        {
            if (o.InvertCase == ActuatorConfig.InvertOneofCase.None)
            {
                if (logWarning)
                    Logger.Warn("Defaulting ActuatorConfig.Invert to false");

                o.Invert = false;
            }
        }

        public static void InitRequired(CredReaderConfig o, bool logWarning = false)
        {
        }

        public static void InitRequired(CredTemplate o)
        {
            if (o.PriorityCase == CredTemplate.PriorityOneofCase.None)
                o.Priority = 0;
        }

        public static void InitRequired(Cred o, bool logWarning = false)
        {
            if (o.EnabledCase == Cred.EnabledOneofCase.None)
            {
                if (logWarning)
                    Logger.Warn("Defaulting Cred.Enabled to false");

                o.Enabled = false;
            }
        }

   		public static void InitRequired(HolCal o, bool logWarning = false)
        {
        }

   		public static void InitRequired(HolType o, bool logWarning = false)
        {
        }
        
        public static void InitRequired(Hol o, bool logWarning = false)
        {
            // What about o.Date ? This is required, but it really makes no sense if somebody has not set it...

            if (o.PreserveSchedDayCase == Hol.PreserveSchedDayOneofCase.None)
            {
                if (logWarning)
                    Logger.Warn("Defaulting Hol.PreserveSchedDay to false");

                o.PreserveSchedDay = false;
            }

            if (o.AllHolTypesCase == Hol.AllHolTypesOneofCase.None)
            {
                if (logWarning)
                    Logger.Warn("Defaulting Hol.AllHolTypes to false");

                o.AllHolTypes = false;
            }

            if (o.NumDaysCase == Hol.NumDaysOneofCase.None)
            {
                if (logWarning)
                    Logger.Warn("Defaulting Hol.NumDays to 0");

                o.NumDays = 0;
            }
            if (o.RepeatCase == Hol.RepeatOneofCase.None)
                o.Repeat = false;
        }

        public static void InitRequired(Sched o, bool logWarning = false)
        {
            foreach (SchedElement e in o.Elements)
                InitRequired(e, logWarning);
        }

        public static void InitRequired(SchedElement o, bool logWarning = false)
        {
            // Q) what about  o.Start o.Stop ? These are required, but it really makes no sense if somebody has not set it...

            if (o.HolidaysCase == SchedElement.HolidaysOneofCase.None)
            {
                if (logWarning)
                    Logger.Warn("Defaulting SchedElement.Holidays to false");

                o.Holidays = false;
            }

            if (o.PlusDaysCase == SchedElement.PlusDaysOneofCase.None)
            {
                if (logWarning)
                    Logger.Warn("Defaulting SchedElement.PlusDays to 0");

                o.PlusDays = 0;
            }
        }

        public static void InitRequired(Priv o, bool logWarning = false)
        {
            if (o.EnabledCase == Priv.EnabledOneofCase.None)
            {
                if (logWarning)
                    Logger.Warn("Defaulting Priv.Enabled to false");

                o.Enabled = false;
            }
        }

        public static void InitRequired(EncryptionKey o)
        {
            // TODO: anything?
        }

        public static void InitRequired(EncryptionKeyRef o)
        {
            // TODO: anything?
        }

        public static void InitRequired(GeneralDevRestriction o)
        {
            // nothing to do
        }

        public static void InitRequired(SpecificDevRestriction o)
        {
            if (o.InvertCase == SpecificDevRestriction.InvertOneofCase.None)
                o.Invert = false;
        }

        public static void InitRequired(SchedRestriction o)
        {
            if (o.InvertCase == SchedRestriction.InvertOneofCase.None)
                o.Invert = false;
        }

        public static void InitRequired(CredPrivBinding o)
        {
            // nothing to do
        }

        public static void InitRequired(CardPin o)
        {
        }
        
        public static void InitRequired(CardPinTemplate o)
        {
            if (o.CredComponentPresenceCase == CardPinTemplate.CredComponentPresenceOneofCase.None)
                o.CredComponentPresence = CredComponentPresence.Absent;
            if (o.CredNumPresenceCase == CardPinTemplate.CredNumPresenceOneofCase.None)
                o.CredNumPresence = CredComponentPresence.Absent;
            if (o.PinPresenceCase == CardPinTemplate.PinPresenceOneofCase.None)
                o.PinPresence = CredComponentPresence.Absent;
        }

        public static bool SetNameAuto(Dev dev, Dev physicalParent, Dev logicalParent)
        {
            string parentName = null;

	        if (logicalParent != null)
	        {
                parentName = logicalParent.Name;
            }
	        else if (physicalParent != null)
	        {
                parentName = physicalParent.Name;
            }

            string detail = StringFormatter.DevTypeAndSubTypeAndUseToStr(dev);

            string newName;

	        if (parentName != null)
                newName = parentName + " - " + detail;
	        else
		        newName = detail;

	        if (dev.NameCase == Dev.NameOneofCase.Name && newName.Equals(dev.Name))
		        return false;

	        dev.Name = newName;

	        return true;
        }

        public static bool FindInDbChange(DbChange dbChange, int unid, out CredTemplate result)
        {
            foreach (CredTemplate o in dbChange.CredTemplate)
            {
                if (o.UnidCase == CredTemplate.UnidOneofCase.Unid && o.Unid == unid)
                {
                    result = o;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public static bool FindInDbChange(DbChange dbChange, int unid, out DataFormat result)
        {
            foreach (DataFormat o in dbChange.DataFormat)
            {
                if (o.UnidCase == DataFormat.UnidOneofCase.Unid && o.Unid == unid)
                {
                    result = o;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public static bool FindInDbChange(DbChange dbChange, int unid, out DataLayout result)
        {
            foreach (DataLayout o in dbChange.DataLayout)
            {
                if (o.UnidCase == DataLayout.UnidOneofCase.Unid && o.Unid == unid)
                {
                    result = o;
                    return true;
                }
            }

            result = null;
            return false;
        }


        public static bool FindDeleteInDbChange_CredTemplate(DbChange dbChange, int unid)
        {
            foreach (int deleteUnid in dbChange.CredTemplateDelete)
            {
                if (deleteUnid == unid)
                    return true;
            }

            return false;
        }

        public static bool FindDeleteInDbChange_DataFormat(DbChange dbChange, int unid)
        {
            foreach (int deleteUnid in dbChange.DataFormatDelete)
            {
                if (deleteUnid == unid)
                    return true;
            }

            return false;
        }

        public static bool FindDeleteInDbChange_DataLayout(DbChange dbChange, int unid)
        {
            foreach (int deleteUnid in dbChange.DataLayoutDelete)
            {
                if (deleteUnid == unid)
                    return true;
            }

            return false;
        }

 
    }


}
