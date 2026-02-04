using System;
using System.IO;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    public class SpCoreMessageInputStream
    {
        private static readonly log4net.ILog Logger =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Stream _stream;

        public SpCoreMessageInputStream(Stream stream)
        {
            _stream = stream;
        }

        public SpCoreMessage Read()
        {
            SpCoreMessageHeader header = ReadHeader();

            var buffer = new byte[header.Length];
            ReadFully(buffer);

            return SpCoreMessage.Parser.ParseFrom(buffer);
        }

        private SpCoreMessageHeader ReadHeader()
        {
            SpCoreMessageHeader header = new SpCoreMessageHeader {HeaderBytes = ReadShort()};

            if (header.HeaderBytes != SpCoreMessageHeader.STANDARD_HEADER_BYTES)
            {
                throw new SpCoreIOException("Expected header " +
                                            SpCoreMessageHeader.STANDARD_HEADER_BYTES.ToString("X") + ", was: " +
                                            header.HeaderBytes.ToString("X"));
            }

            header.Length = ReadInt();
            if (header.Length < 0 || header.Length > SpCoreMessageHeader.MAX_LENGTH)
            {
                throw new SpCoreIOException("Header length out of range: " + header.Length);
            }

            return header;
        }

        private short ReadShort()
        {
            var buffer = new byte[2];
            ReadFully(buffer);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }

            return BitConverter.ToInt16(buffer, 0);
        }

        private int ReadInt()
        {
            var buffer = new byte[4];
            ReadFully(buffer);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }

            return BitConverter.ToInt32(buffer, 0);
        }

        private void ReadFully(byte[] destination)
        {
            int remaining = destination.Length;
            var index = 0;

            do
            {
                var received = _stream.Read(destination, index, remaining);
                if (received == 0)
                {
                    Logger.Info("socket.Receive: 0, throwing EndOfStreamException");
                    throw new EndOfStreamException();
                    /* 
                     * Documentation: If the remote host shuts down the Socket connection with the Shutdown method, and 
                     * all available data has been received, the Receive method will complete immediately and return 
                     * zero bytes.
                     */
                }

                index += received;
                remaining -= received;
            } while (remaining > 0);
        }
    }
}