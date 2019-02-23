using Force.Crc32;
using System;

namespace LibReFrontier
{
    // With major help from enler
    public class Crypto
    {
        static UInt32 LoadUInt32BE(byte[] buffer, int offset)
        {
            UInt32 value = (UInt32)((buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3]);
            return value;
        }

        // from addr 0x10292DCC
        static byte[] rndBufEcd = new byte[] { 0x4A, 0x4B, 0x52, 0x2E, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x0D,
            0xCD, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x0D, 0xCD, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x0D, 0xCD,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x19, 0x66, 0x0D, 0x00, 0x00, 0x00, 0x03, 0x7D, 0x2B, 0x89, 0xDD, 0x00,
            0x00, 0x00, 0x01 };

        // from addr 0x1025F4E0
        static byte[] rndBufExf = new byte[] { 0x4A, 0x4B, 0x52, 0x2E, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x0D,
            0xCD, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x0D, 0xCD, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x0D, 0xCD,
            0x00, 0x00, 0x00, 0x01, 0x02, 0xE9, 0x0E, 0xDD, 0x00, 0x00, 0x00, 0x03 };

        static UInt32 getRndEcd(int index, ref UInt32 rnd)
        {
            rnd = rnd * LoadUInt32BE(rndBufEcd, 8 * index) + LoadUInt32BE(rndBufEcd, 8 * index + 4);
            return rnd;
        }

        static byte[] CreateXorkeyExf(byte[] header)
        {
            byte[] keyBuffer = new byte[16];
            int index = BitConverter.ToUInt16(header, 4);
            UInt32 tempVal = BitConverter.ToUInt32(header, 0xc);
            UInt32 value = BitConverter.ToUInt32(header, 0xc);
            for (int i = 0; i < 4; i++)
            {
                tempVal = tempVal * LoadUInt32BE(rndBufExf, index * 8) + LoadUInt32BE(rndBufExf, index * 8 + 4);
                UInt32 key = tempVal ^ value;
                byte[] tempKey = BitConverter.GetBytes(key);
                Array.Copy(tempKey, 0, keyBuffer, i * 4, 4);
            }
            return keyBuffer;
        }

        public static void decEcd(byte[] buffer)
        {
            UInt32 fsize = BitConverter.ToUInt32(buffer, 8);
            UInt32 crc32 = BitConverter.ToUInt32(buffer, 12);
            int index = BitConverter.ToUInt16(buffer, 4);
            UInt32 rnd = (crc32 << 16) | (crc32 >> 16) | 1;

            UInt32 xorpad = getRndEcd(index, ref rnd);

            byte r8 = (byte)xorpad;

            for (int i = 0; i < fsize; i++)
            {
                xorpad = getRndEcd(index, ref rnd);

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

        public static byte[] encEcd(byte[] buffer, byte[] bufferMeta)
        {
            // Update meta data
            int fsize = buffer.Length;
            UInt32 crc32w = Crc32Algorithm.Compute(buffer);
            int index = BitConverter.ToUInt16(bufferMeta, 4);

            // Write meta data
            byte[] buf = new byte[16 + fsize];
            byte[] bufnum;
            Array.Copy(bufferMeta, buf, bufferMeta.Length);
            bufnum = BitConverter.GetBytes(fsize);
            Array.Copy(bufnum, 0, buf, 8, 4);
            bufnum = BitConverter.GetBytes(crc32w);
            Array.Copy(bufnum, 0, buf, 12, 4);

            // Fill data with nullspace
            int i;
            for (i = 16 + fsize; i < buf.Length; i++) buf[i] = 0;

            // Encrypt data
            UInt32 rnd = (crc32w << 16) | (crc32w >> 16) | 1;
            UInt32 xorpad = getRndEcd(index, ref rnd);
            byte r8 = (byte)xorpad;

            for (i = 0; i < fsize; i++)
            {
                xorpad = getRndEcd(index, ref rnd);
                byte data = buffer[i];
                UInt32 r11 = 0;
                UInt32 r12 = 0;
                for (int j = 0; j < 8; j++)
                {
                    UInt32 r10 = xorpad ^ r11;
                    r11 = r12;
                    r12 ^= r10;
                    r12 = r12 & 0xFF;
                    xorpad = xorpad >> 4;
                }

                UInt32 dig2 = data;
                UInt32 dig1 = (dig2 >> 4) & 0xFF;
                dig1 ^= r11;
                dig2 ^= r12;
                dig1 ^= dig2;

                byte rr = (byte)((dig2 & 0xF) | ((dig1 & 0xF) << 4));
                rr = (byte)(rr ^ r8);
                buf[16 + i] = rr;
                r8 = data;
            }
            return buf;
        }

        public static void decExf(byte[] buffer)
        {
            byte[] header = new byte[16];
            Array.Copy(buffer, header, header.Length);
            if (BitConverter.ToUInt32(header, 0) == 0x1a667865)
            {
                byte[] keybuf = CreateXorkeyExf(header);
                for (int i = 16; i < buffer.Length - header.Length; i++)
                {
                    UInt32 r28 = (UInt32)(i - 0x10);
                    byte r8 = buffer[i];
                    int index = (int)(r28 & 0xf);
                    UInt32 r4 = r8 ^ r28;
                    UInt32 r12 = keybuf[index];
                    UInt32 r0 = (r4 & 0xf0) >> 4;
                    UInt32 r7 = keybuf[r0];
                    UInt32 r9 = r4 >> 4;
                    UInt32 r5 = r7 >> 4;
                    r9 = r9 ^ r12;
                    UInt32 r26 = r5 ^ r4;
                    r26 = (UInt32)(r26 & ~0xf0) | ((r9 & 0xf) << 4);
                    buffer[i] = (byte)r26;
                }
            }
        }
    }
}
