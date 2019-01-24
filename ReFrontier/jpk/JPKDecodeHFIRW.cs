using System.IO;

namespace ReFrontier.jpk
{
    class JPKDecodeHFIRW : JPKDecodeHFI
    {
        public override void ProcessOnDecode(Stream inStream, byte[] outBuffer)
        {
            int index = 0;
            while (index < outBuffer.Length)
                outBuffer[index++] = base.ReadByte(inStream);
        }
    }
}
