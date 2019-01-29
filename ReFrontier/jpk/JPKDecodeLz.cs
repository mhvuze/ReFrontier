using System;
using System.Diagnostics;
using System.IO;

namespace ReFrontier.jpk
{
    class JPKDecodeLz : IJPKDecode
    {
        private int m_shiftIndex = 0;
        private byte m_flag = 0;

        private void jpkcpy_lz(byte[] outBuffer, int offset, int length, ref int index)
        {
            for (int i = 0; i < length; i++, index++)
            {
                outBuffer[index] = outBuffer[index - offset - 1];
            }
        }

        private byte jpkbit_lz(Stream s)
        {
            m_shiftIndex--;
            if (m_shiftIndex < 0)
            {
                m_shiftIndex = 7;
                //Debug.WriteLine("flag read from {0:X8}", s.Position);
                m_flag = this.ReadByte(s);
            }
            return (byte)((m_flag >> m_shiftIndex) & 1);
        }

        public virtual void ProcessOnDecode(Stream inStream, byte[] outBuffer)//implements jpkdec_lz
        {
            int outIndex = 0;
            while (inStream.Position < inStream.Length && outIndex < outBuffer.Length)
            {
                if (jpkbit_lz(inStream) == 0)
                {
                    outBuffer[outIndex++] = ReadByte(inStream);
                    continue;
                }
                else
                {
                    if (jpkbit_lz(inStream) == 0)
                    {
                        //Debug.WriteLine("case 0");
                        byte len = (byte)((jpkbit_lz(inStream) << 1) | jpkbit_lz(inStream));
                        byte off = ReadByte(inStream);
                        jpkcpy_lz(outBuffer, off, len + 3, ref outIndex);
                        continue;
                    }
                    else
                    {

                        byte hi = ReadByte(inStream);
                        byte lo = ReadByte(inStream);
                        int len = (hi & 0xE0) >> 5;
                        int off = ((hi & 0x1F) << 8) | lo;
                        if (len != 0)
                        {
                            //Debug.WriteLine("case 1");
                            jpkcpy_lz(outBuffer, off, len + 2, ref outIndex);
                            continue;
                        }
                        else
                        {
                            if (jpkbit_lz(inStream) == 0)
                            {
                                //Debug.WriteLine("case 2");
                                len = (byte)((jpkbit_lz(inStream) << 3) | (jpkbit_lz(inStream) << 2) | (jpkbit_lz(inStream) << 1) | jpkbit_lz(inStream));
                                jpkcpy_lz(outBuffer, off, len + 2 + 8, ref outIndex);
                                continue;
                            }
                            else
                            {
                                byte temp = ReadByte(inStream);
                                if (temp == 0xFF)
                                {
                                    //Debug.WriteLine("case 3");
                                    for (int i = 0; i < off + 0x1B; i++)
                                        outBuffer[outIndex++] = ReadByte(inStream);
                                    continue;
                                }
                                else
                                {
                                    //Debug.WriteLine("case 4");
                                    jpkcpy_lz(outBuffer, off, temp + 0x1a, ref outIndex);
                                }
                            }
                        }
                    }
                }
            }
        }

        public virtual byte ReadByte(Stream s)
        {
            int value = s.ReadByte();
            if (value < 0)
                throw new NotImplementedException();
            return (byte)value;
        }
    }
}
