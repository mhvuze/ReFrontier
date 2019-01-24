using System;
using System.IO;

namespace ReFrontier.jpk
{
    class JPKDecodeRW : IJPKDecode
    {
        public void ProcessOnDecode(Stream inStream, byte[] outBuffer)
        {
            int index = 0;
            while (inStream.Position < inStream.Length && index < outBuffer.Length)
            {
                outBuffer[index++] = ReadByte(inStream);
            }
        }

        public byte ReadByte(Stream s)
        {
            int value = s.ReadByte();
            if (value < 0)
                throw new NotImplementedException();
            return (byte)value;
        }
    }
}
