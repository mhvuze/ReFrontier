using System;
using System.IO;
using System.Linq;

namespace ReFrontier.jpk
{
    class JPKEncodeHFI : JPKEncodeLz
    {
        private static Int16 m_hfTableLen = 0x1fe;
        private Int16[] m_hfTable = new Int16[m_hfTableLen];
        private Int16[] m_Paths = new Int16[0x100];
        private Int16[] m_Lengths = new Int16[0x100];

        private void dbg(string s, params Object[] o)
        {
            //Debug.WriteLine(s, o);
        }
        private void dbg(string s)
        {
            //Debug.WriteLine(s);
        }

        private int m_filled = 0;
        private int m_depth = 0;
        private void GetPaths(int strt, int lev, int pth)
        {
            //dbg("fill strt={0:x} lev={1} pth={2:x}", strt, lev, pth);
            int maxlev = 30;
            if (lev >= maxlev) return;
            if (lev >= m_depth) m_depth = lev;
            //bool done = false;
            if (strt < m_hfTableLen)
            {
                int val = m_hfTable[strt];
                if (val < 0x100)
                {
                    //dbg("val={0:x} strt={1:x} pth={2:x} lev={3:x}", val, strt, pth, lev);
                    m_Paths[val] = (short)pth;
                    m_Lengths[val] = (short)lev;
                    m_filled++;
                    return;
                }
                strt = val;
            }
            GetPaths(2 * (strt - 0x100), lev + 1, (pth << 1));
            GetPaths(2 * (strt - 0x100) + 1, lev + 1, (pth << 1) | 1);
        }
        private void FillTable()
        {
            Array.Clear(m_Paths, 0, m_Paths.Length);
            Array.Clear(m_Lengths, 0, m_Lengths.Length);
            Int16[] rndseq = new short[0x100];
            for (Int16 i = 0; i < rndseq.Length; i++) rndseq[i] = i;
            Random rnd = new Random();
            rndseq = rndseq.OrderBy(x => rnd.Next()).ToArray();
            //string s = "";
            /*for (int i = 0; i < rndseq.Length; i++) {
              s += String.Format(" {0:x}={1:x}", i, rndseq[i]);
              //if (i % 16 == 15) { dbg(s); s = ""; }
            }*/
            for (int i = 0; i < 0x100; i++) m_hfTable[i] = rndseq[i];
            for (int i = 0x100; i < m_hfTableLen; i++) m_hfTable[i] = (Int16)i;
            /*for (int i = 0; i < m_hfTable.Length; i++) {
              s += String.Format(" {0:x}={1:x}", i, m_hfTable[i]);
              if (i % 16 == 15) { dbg(s); s = ""; }
            }*/
            //dbg(s);
            GetPaths(m_hfTableLen, 0, 0);
            //s = "";
            //dbg("m_filled=" + m_filled + " m_depth=" + m_depth);
            /*for (int i = 0; i < m_Paths.Length; i++) {
              s += String.Format("  {0:x},{1:x},{2:x}", i, m_Lengths[i], m_Paths[i]);
              if (m_Lengths[i] == 0) s += "???";
              if (i % 8 == 7) {
                dbg(s);
                s = "";
              }
            }*/
        }

        public override void ProcessOnEncode(byte[] inBuffer, Stream outStream, int level = 16, ShowProgress progress = null)
        {
            FillTable();
            BinaryWriter br = new BinaryWriter(outStream);
            br.Write(m_hfTableLen);
            for (int i = 0; i < m_hfTableLen; i++) br.Write(m_hfTable[i]);
            base.ProcessOnEncode(inBuffer, outStream, level, progress);
            FlushWrite(outStream);
        }

        private byte m_bits = 0;
        private int m_bitcount = 0;
        private void WriteBit(Stream s, byte b)
        {
            if (m_bitcount == 8)
            {
                s.WriteByte(m_bits);
                m_bits = 0;
                m_bitcount = 0;
            }
            m_bits <<= 1;
            m_bits |= b;
            m_bitcount++;
        }
        private void WriteBits(Stream s, Int16 bits, Int16 len)
        {
            while (len > 0)
            {
                len--;
                WriteBit(s, (byte)((bits >> len) & 1));
            }
        }
        private void FlushWrite(Stream s)
        {
            if (m_bitcount > 0)
            {
                m_bits <<= (8 - m_bitcount);
                s.WriteByte(m_bits);
            }
        }
        public override void WriteByte(Stream s, byte b)
        {
            Int16 bits = m_Paths[b];
            Int16 len = m_Lengths[b];
            WriteBits(s, bits, len);
        }
    }
}
