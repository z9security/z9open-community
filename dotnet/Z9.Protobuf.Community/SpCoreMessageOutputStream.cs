using System;
using System.IO;
using System.Linq;
using Z9.Spcore.Proto;
using Google.Protobuf;

namespace Z9.Protobuf
{
    public class SpCoreMessageOutputStream
    {
        private static bool ProtobufReparse = true;
        private static bool TraceMessages = false;

        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Stream _stream;

        public SpCoreMessageOutputStream(Stream stream)
        {
            _stream = stream;
        }

        public void Write(SpCoreMessage message)
        {
            var messageByteArray = message.ToByteArray();

            if (ProtobufReparse)
            {
                try
                {
                    SpCoreMessage.Parser.ParseFrom(messageByteArray);
                }
                catch (Exception e)
                {
                    Logger.Warn("Offending message: " + BitConverter.ToString(messageByteArray));
                    Logger.Error("Unable to reparse outgoing message: " + e, e);
                    throw;
                }
            }

            var header = new SpCoreMessageHeader
            {
                HeaderBytes = SpCoreMessageHeader.STANDARD_HEADER_BYTES,
                Length = messageByteArray.Length
            };

            WriteHeader(header);

            WriteMessageLength(header);

            _stream.Write(messageByteArray, 0, messageByteArray.Length);

            if (TraceMessages)
            {
                if (Logger.IsDebugEnabled) Logger.Debug("Wrote body: " + "length=" + messageByteArray.Length + " bytes=" + BitConverter.ToString(messageByteArray));
            }
        }

        private void WriteMessageLength(SpCoreMessageHeader header)
        {
            var lengthByteArray = ConvertForEndian(BitConverter.GetBytes(header.Length));

            _stream.Write(lengthByteArray, 0, lengthByteArray.Length);

            if (TraceMessages)
            {
                if (Logger.IsDebugEnabled) Logger.Debug("Wrote header length: " + header.Length + " bytes=" + BitConverter.ToString(lengthByteArray));
            }
        }

        private void WriteHeader(SpCoreMessageHeader header)
        {
            var headerByteArray = ConvertForEndian(BitConverter.GetBytes(header.HeaderBytes));

            _stream.Write(headerByteArray, 0, headerByteArray.Length);

            if (TraceMessages)
            {
                if (Logger.IsDebugEnabled) Logger.Debug("Wrote header: " + "length=" + headerByteArray.Length + " bytes=" + BitConverter.ToString(headerByteArray));
            }
        }

        private static byte[] ConvertForEndian(byte[] byteArray)
        {
            return BitConverter.IsLittleEndian ? byteArray.Reverse().ToArray() : byteArray;
        }
    }
}
