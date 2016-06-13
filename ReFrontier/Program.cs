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
            if (mode != "c" && mode != "txb" && mode != "bin" && mode != "pac" && mode != "gab" && mode != "snp" && mode != "snd")
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
                string log_name = output_dir + ".log";
                string create_dir = fileInfo.DirectoryName + "\\new_output";

                // Delete log if present
                if (mode != "c" && File.Exists(log_name)) { File.Delete(log_name); }

                // Read file to memory
                MemoryStream ms_input = new MemoryStream(File.ReadAllBytes(input));
                BinaryReader br_input = new BinaryReader(ms_input);

                // Extract txb, bin, pac, gab
                if (mode == "txb" || mode == "bin" || mode == "pac" || mode == "gab")
                {

                    int count = br_input.ReadInt32();
                    // REMLATER Check if file is encrypted
                    if (count == 0x1A646365) { Console.WriteLine("WARNING: Encrypted file detected! Shutting down."); return; }

                    // Write basic info to log
                    using (StreamWriter log = new StreamWriter(log_name, true, Encoding.UTF8)) { log.WriteLine(mode); log.WriteLine(count); }

                    for (int i = 0; i < count; i++)
                    {
                        int offset = br_input.ReadInt32();
                        int size = br_input.ReadInt32();

                        // Skip if size is zero
                        if (size == 0)
                        {
                            Console.WriteLine("Offset: 0x" + offset.ToString("X8") + ", Size: 0x" + size.ToString("X8") + " (SKIPPED)");
                            using (StreamWriter log = new StreamWriter(log_name, true, Encoding.UTF8)) { log.WriteLine((i + 1).ToString("D4") + ",none," + offset.ToString("X8") + "," + size.ToString("X8")); }
                            continue;
                        }

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
                        using (StreamWriter log = new StreamWriter(log_name, true, Encoding.UTF8))  { log.WriteLine((i + 1).ToString("D4") + "," + extension + "," + offset.ToString("X8") + "," + size.ToString("X8")); }

                        // Move to next entry block
                        br_input.BaseStream.Seek(0x04 + (i + 1) * 0x08, SeekOrigin.Begin);
                    }
                }

                // Extract snp, snd
                else if (mode == "snp" || mode == "snd")
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
                        // ...

                        // Move to next entry block
                        br_input.BaseStream.Seek(0x08 + (i + 1) * 0x08, SeekOrigin.Begin);
                    }
                }

                // More file types here...

                // Create archive based on log
                else if (mode == "c")
                {
                    Directory.CreateDirectory(create_dir);

                    // Read info from log file
                    StreamReader log = new StreamReader(input, Encoding.UTF8, false);
                    string archive_type = log.ReadLine();
                    int count = Convert.ToInt32(log.ReadLine());
                    string create_file = create_dir + "\\" + Path.GetFileNameWithoutExtension(input) + "." + archive_type;

                    // Read entries from log file
                    List<string> l_entry_no = new List<string>();
                    List<string> l_entry_type = new List<string>();
                    List<string> l_entry_offset = new List<string>();
                    List<string> l_entry_size = new List<string>();

                    while (!log.EndOfStream)
                    {
                        var line = log.ReadLine();
                        var columns = line.Split(',');

                        l_entry_no.Add(columns[0]);
                        l_entry_type.Add(columns[1]);
                        l_entry_offset.Add(columns[2]);
                        l_entry_size.Add(columns[3]);
                    }

                    Console.WriteLine("Archive type: " + archive_type + ", File count: " + count);

                    // Write txb
                    if (archive_type == "txb")
                    {
                        File.WriteAllBytes(create_file, new byte[0x04 + count * 0x08]);
                        BinaryWriter writer = new BinaryWriter(File.Open(create_file, FileMode.Open));
                        writer.Write(count);

                        // Process entries
                        int offset = 0x04 + count * 0x08;
                        for (int i = 0; i < count; i++)
                        {
                            // REMLATER Add support for size zero files
                            byte[] entry_data = File.ReadAllBytes(output_dir + "\\" + l_entry_no[i] + "_" + l_entry_offset[i] + l_entry_type[i]);

                            writer.BaseStream.Seek(0x04 + i * 0x08, SeekOrigin.Begin);
                            writer.Write(offset);
                            writer.Write(entry_data.Length);

                            writer.BaseStream.Seek(offset, SeekOrigin.Begin);
                            writer.Write(entry_data);

                            Console.WriteLine("Offset: 0x" + offset.ToString("X8") + ", Size: 0x" + entry_data.Length.ToString("X8"));

                            offset += entry_data.Length;
                        }
                    }

                    // More archive types here...
                }
            }
            else
            {
                Console.WriteLine("ERROR: Input file does not exist.");
                return;
            }
        }
    }
}
