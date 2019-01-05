using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReFrontier
{
    class Helpers
    {
        // Read null-terminated string
        public static string ReadNullterminatedString(BinaryReader brInput)
        {
            var charByteList = new List<byte>();
            string str = "";
            if (brInput.BaseStream.Position == brInput.BaseStream.Length)
            {
                byte[] charByteArray = charByteList.ToArray();
                str = Encoding.UTF8.GetString(charByteArray);
                return str;
            }
            byte b = brInput.ReadByte();
            while ((b != 0x00) && (brInput.BaseStream.Position != brInput.BaseStream.Length))
            {
                charByteList.Add(b);
                b = brInput.ReadByte();
            }
            byte[] char_bytes = charByteList.ToArray();
            str = Encoding.UTF8.GetString(char_bytes);
            return str;
        }

        // Multi-filter GetFiles https://stackoverflow.com/a/3754470/5343630
        public static class MyDirectory
        {
            public static IEnumerable<string> GetFiles(string path,
                                string[] searchPatterns,
                                SearchOption searchOption = SearchOption.TopDirectoryOnly)
            {
                return searchPatterns.AsParallel()
                       .SelectMany(searchPattern =>
                              Directory.EnumerateFiles(path, searchPattern, searchOption));
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
    }
}
