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
            Console.WriteLine("ReFrontier by MHVuze\n");

            // Assign arguments
            if (args.Length < 2) { Console.WriteLine("ERROR: Not enough arguments specified."); return; }

            string mode = args[0];
            string input = args[1];

            // Check mode
            if (mode != "txb" && mode != "bin" && mode != "pac" && mode != "gab" && mode != "snp")
            {
                Console.WriteLine("ERROR: Unsupported mode specified.");
                return;
            }

            // Check file
            if (File.Exists(input))
            {
                // Set strings
                FileInfo fileInfo = new FileInfo(input);
                string output_dir = fileInfo.DirectoryName + "\\" + Path.GetFileNameWithoutExtension(input);

                // Read file to memory
                MemoryStream ms_input = new MemoryStream(File.ReadAllBytes(input));
                BinaryReader br_input = new BinaryReader(ms_input);

                // Extract txb, bin, pac, gab
                if (mode == "txb" || mode == "bin" || mode == "pac" || mode == "gab")
                {
                    int count = br_input.ReadInt32();
                    // REMLATER Check if file is encrypted
                    if (count == 0x1A646365) { Console.WriteLine("WARNING: Encrypted file detected! Shutting down."); return; }

                    for (int i = 0; i < count; i++)
                    {
                        int offset = br_input.ReadInt32();
                        int size = br_input.ReadInt32();

                        // Skip if size is zero
                        if (size == 0) { Console.WriteLine("Offset: 0x" + offset.ToString("X8") + ", Size: 0x" + size.ToString("X8") + " (SKIPPED)"); continue; }

                        // Print to console
                        Console.WriteLine("Offset: 0x" + offset.ToString("X8") + ", Size: 0x" + size.ToString("X8"));

                        // Read file to array
                        br_input.BaseStream.Seek(offset, SeekOrigin.Begin);
                        byte[] file_data = br_input.ReadBytes(size);

                        // Check file header
                        string extension = "";
                        byte[] header = new Byte[4];
                        Array.Copy(file_data, header, 4);

                        if (BitConverter.ToInt32(header, 0) == 0x474e5089)
                            extension = ".png";
                        else if (BitConverter.ToInt32(header, 0) == 0x1A524B4A)
                            extension = ".jkr";
                        else
                            extension = ".bin";

                        // Extract file
                        Directory.CreateDirectory(output_dir);
                        File.WriteAllBytes(output_dir + "\\" + (i + 1).ToString("D4") + "_" + offset.ToString("X8") + extension, file_data);

                        // Save info to log
                        // using (StreamWriter log = new StreamWriter(Path.GetDirectoryName(input) + "\\" + Path.GetFileNameWithoutExtension(input) + ".log", true, Encoding.UTF8))  { log.WriteLine((i + 1).ToString("D4") + "," + extension + "," + offset.ToString("X8") + "," + size.ToString("X8")); }

                        // Move to next entry block
                        br_input.BaseStream.Seek(0x04 + (i + 1) * 0x08, SeekOrigin.Begin);
                    }
                }

                // Extract snp
                else if (mode == "snp")
                {
                    int magic = br_input.ReadInt32();
                    if (magic != 0x4F4D4F4D) { Console.WriteLine("WARNING: Invalid file specified! Shutting down."); return; }

                    int count = br_input.ReadInt32();

                    for (int i = 0; i < count; i++)
                    {
                        int offset = br_input.ReadInt32();
                        int size = br_input.ReadInt32();

                        // Skip if size is zero
                        if (size == 0) { Console.WriteLine("Offset: 0x" + offset.ToString("X8") + ", Size: 0x" + size.ToString("X8") + " (SKIPPED)"); continue; }

                        // Print to console
                        Console.WriteLine("Offset: 0x" + offset.ToString("X8") + ", Size: 0x" + size.ToString("X8"));

                        // Read file to array
                        br_input.BaseStream.Seek(offset, SeekOrigin.Begin);
                        byte[] file_data = br_input.ReadBytes(size);

                        // Check file header
                        string extension = "";
                        byte[] header = new Byte[4];
                        Array.Copy(file_data, header, 4);

                        if (BitConverter.ToInt32(header, 0) == 0x5367674F)
                            extension = ".ogg";
                        else if (BitConverter.ToInt32(header, 0) == 0x1A524B4A)
                            extension = ".jkr";
                        else
                            extension = ".bin";

                        // Extract file
                        Directory.CreateDirectory(output_dir);
                        File.WriteAllBytes(output_dir + "\\" + (i + 1).ToString("D4") + "_" + offset.ToString("X8") + extension, file_data);

                        // Save info to log
                        // using (StreamWriter log = new StreamWriter(Path.GetDirectoryName(input) + "\\" + Path.GetFileNameWithoutExtension(input) + ".log", true, Encoding.UTF8))  { log.WriteLine((i + 1).ToString("D4") + "," + extension + "," + offset.ToString("X8") + "," + size.ToString("X8")); }

                        // Move to next entry block
                        br_input.BaseStream.Seek(0x08 + (i + 1) * 0x08, SeekOrigin.Begin);
                    }
                }

                // More file types here...
            }
            else
            {
                Console.WriteLine("ERROR: Input file does not exist.");
                return;
            }
        }
    }
}
