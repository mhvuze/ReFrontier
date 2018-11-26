using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ReFrontier
{
    class Program
    {
        static void Main(string[] args)
        {
            // Print header
            Console.WriteLine("ReFrontier by MHVuze");

            // Assign arguments
            if (args.Length < 1) { Console.WriteLine("ERROR: Not enough arguments specified."); Console.Read(); return; }

            string input = args[0];

            // Check file
            if (File.Exists(input))
            {
                // Read file to memory
                MemoryStream msInput = new MemoryStream(File.ReadAllBytes(input));
                BinaryReader brInput = new BinaryReader(msInput);

                int fileMagic = brInput.ReadInt32();

                // MOMO Header: snp, snd
                if (fileMagic == 0x4F4D4F4D)
                {
                    Console.WriteLine("MOMO Header detected.");
                    Handlers.UnpackSimpleArchive(input, brInput, 8);
                }
                // ECD Header
                else if (fileMagic == 0x1A646365)
                {
                    Console.WriteLine("ECD Header detected.");
                    Console.WriteLine("File is encrypted. Handling not implemented yet.");
                }
                // JKR Header
                else if (fileMagic == 0x1A524B4A)
                {
                    Console.WriteLine("JKR Header detected.");
                    Console.WriteLine("Handling not implemented yet.");
                }
                // MHA Header
                else if (fileMagic == 0x0161686D)
                {
                    Console.WriteLine("MHA Header detected.");
                    Handlers.UnpackMHA(input, brInput);
                }
                // Try to unpack as simple archive: i.e. txb, some bin, some pac, gab
                else
                {
                    Console.WriteLine("Trying to unpack as generic simple archive.");
                    brInput.BaseStream.Seek(0, SeekOrigin.Begin);
                    Handlers.UnpackSimpleArchive(input, brInput, 4);
                }
            }
            else
            {
                Console.WriteLine("ERROR: Input file does not exist.");
                Console.Read();
                return;
            }
            Console.Read();
        }
    }
}
