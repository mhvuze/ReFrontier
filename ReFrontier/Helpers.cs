using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ReFrontier
{
    class Helpers
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

        // Header <-> extensions
        public enum Extensions
        {
            dds = 542327876,
            ftxt = 0x000B0000,  // custom extension
            jkr = 0x1A524B4A,
            ogg = 0x5367674F,
            png = 0x474e5089
        }
    }
}
