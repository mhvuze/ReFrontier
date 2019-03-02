using Force.Crc32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibReFrontier
{
    public class Helpers
    {
        // Read null-terminated string
        public static string ReadNullterminatedString(BinaryReader brInput, Encoding encoding)
        {
            var charByteList = new List<byte>();
            string str = "";
            if (brInput.BaseStream.Position == brInput.BaseStream.Length)
            {
                byte[] charByteArray = charByteList.ToArray();
                str = encoding.GetString(charByteArray);
                return str;
            }
            byte b = brInput.ReadByte();
            while ((b != 0x00) && (brInput.BaseStream.Position != brInput.BaseStream.Length))
            {
                charByteList.Add(b);
                b = brInput.ReadByte();
            }
            byte[] char_bytes = charByteList.ToArray();
            str = encoding.GetString(char_bytes);
            return str;
        }

        // Multi-filter GetFiles https://stackoverflow.com/a/3754470/5343630
        public static class MyDirectory
        {
            public static string[] GetFiles(string path,
                                string[] searchPatterns,
                                SearchOption searchOption = SearchOption.TopDirectoryOnly)
            {
                return searchPatterns.AsParallel()
                       .SelectMany(searchPattern =>
                              Directory.EnumerateFiles(path, searchPattern, searchOption))
                              .ToArray();
            }
        }

        // Print to console with seperator
        public static void Print(string input, bool printBefore)
        {
            if (!printBefore)
            {
                Console.WriteLine(input);
                Console.WriteLine("==============================");
            }
            else
            {
                Console.WriteLine("\n==============================");
                Console.WriteLine(input);
            }
        }

        // String to byte array
        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        // Byte array to string
        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        // CRC32 byte array - just to remove dependency from TextTool
        public static uint GetCrc32(byte[] array)
        {
            return Crc32Algorithm.Compute(array);
        }

        // Return info for MHFUP_00.DAT
        public static string GetUpdateEntry(string fileName)
        {
            DateTime date = File.GetLastWriteTime(fileName);
            string dateHex2 = date.Subtract(new DateTime(1601, 1, 1)).Ticks.ToString("X16").Substring(0, 8);
            string dateHex1 = date.Subtract(new DateTime(1601, 1, 1)).Ticks.ToString("X16").Substring(8);
            byte[] repackData = File.ReadAllBytes(fileName);
            uint crc32 = Crc32Algorithm.Compute(repackData);
            Console.WriteLine($"{crc32.ToString("X8")},{dateHex1},{dateHex2},{fileName.Replace("output", "dat")},{repackData.Length},0");
            return $"{crc32.ToString("X8")},{dateHex1},{dateHex2},{fileName},{repackData.Length},0";
        }

        // Search for byte array
        public static int GetOffsetOfArray(byte[] haystack, byte[] needle)
        {
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                if (MatchArrays(haystack, needle, i))
                {
                    return i;
                }
            }
            return -1;
        }

        static bool MatchArrays(byte[] haystack, byte[] needle, int start)
        {
            if (needle.Length + start > haystack.Length)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < needle.Length; i++)
                {
                    if (needle[i] != haystack[i + start])
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        // Header <-> extensions
        public enum Extensions
        {
            dds = 542327876,
            ftxt = 0x000B0000,  // custom extension
            gfx2 = 846751303,   // WiiU texture
            jkr = 0x1A524B4A,
            ogg = 0x5367674F,
            pmo = 7302512,      // iOS MHFU model
            png = 0x474e5089,
            tmh = 1213027374    // iOS MHFU texture
        }

        // Get file extension for files without unique 4-byte magic
        public static string CheckForMagic(uint headerInt, byte[] data)
        {
            byte[] header;
            string extension = null;

            if (headerInt == 1)
            {
                header = new byte[12];
                Array.Copy(data, header, 12);
                headerInt = BitConverter.ToUInt32(header, 8);
                if (headerInt == data.Length) extension = "fmod";
            }
            else if (headerInt == 0xC0000000)
            {
                header = new byte[12];
                Array.Copy(data, header, 12);
                headerInt = BitConverter.ToUInt32(header, 8);
                if (headerInt == data.Length) extension = "fskl";
            }

            return extension;
        }
    }
}
