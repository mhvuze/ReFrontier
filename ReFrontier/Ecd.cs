using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// by enler
namespace ReFrontier
{
    class Ecd
    {
        static UInt32 LoadUInt32BE(byte[] buffer, int offset)
        {
            UInt32 value = (UInt32)((buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3]);
            return value;
        }

        // from addr 0x10292DCC
        static byte[] rndBuf = new byte[] {0x4A,0x4B,0x52,0x2E,0x00,0x00,0x00,0x01,0x00,0x01,0x0D,0xCD,0x00,0x00,0x00,0x01,
            0x00,0x01,0x0D,0xCD,0x00,0x00,0x00,0x01,0x00,0x01,0x0D,0xCD,0x00,0x00,0x00,0x01,
            0x00,0x19,0x66,0x0D,0x00,0x00,0x00,0x03,0x7D,0x2B,0x89,0xDD,0x00,0x00,0x00,0x01 };

        static UInt32 ecd_getrnd(int index, ref UInt32 rnd)
        {
            rnd = rnd * LoadUInt32BE(rndBuf, 8 * index) + LoadUInt32BE(rndBuf, 8 * index + 4);
            return rnd;
        }

        public static void dececd(byte[] buffer)
        {
            UInt32 fsize = BitConverter.ToUInt32(buffer, 8);
            UInt32 crc32 = BitConverter.ToUInt32(buffer, 12);
            int index = BitConverter.ToUInt16(buffer, 4);
            UInt32 rnd = (crc32 << 16) | (crc32 >> 16) | 1;

            UInt32 xorpad = ecd_getrnd(index, ref rnd);

            byte r8 = (byte)xorpad;

            for (int i = 0; i < fsize; i++)
            {
                xorpad = ecd_getrnd(index, ref rnd);

                byte data = buffer[0x10 + i];
                UInt32 r11 = (UInt32)(data ^ r8);
                UInt32 r12 = (r11 >> 4) & 0xFF;
                for (int j = 0; j < 8; j++)
                {
                    UInt32 r10 = xorpad ^ r11;
                    r11 = r12;
                    r12 = r12 ^ r10;
                    r12 = r12 & 0xFF;
                    xorpad = xorpad >> 4;
                }

                r8 = (byte)((r12 & 0xF) | ((r11 & 0xF) << 4));
                buffer[0x10 + i] = r8;
            }
        }
    }
}
