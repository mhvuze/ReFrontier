using System;
using System.IO;
using System.Linq;

namespace ReFrontier
{
    class Program
    {
        static void Main(string[] args)
        {
            Helpers.Print("ReFrontier by MHVuze", false);

            // Assign arguments
            if (args.Length < 1) { Console.WriteLine("ERROR: Not enough arguments specified."); Console.Read(); return; }
            string input = args[0]; bool createLog = false; bool repack = false;
            if (args.Any("-log".Contains)) createLog = true; if (args.Any("-pack".Contains)) repack = true;
            if (createLog) Console.WriteLine("Writing log file."); if (repack) Console.WriteLine("Repacking mode.");

            // Check file
            if (File.Exists(input) || Directory.Exists(input))
            {
                FileAttributes inputAttr = File.GetAttributes(input);
                if (inputAttr.HasFlag(FileAttributes.Directory))
                {
                    if (!repack)
                    {
                        string[] inputFiles = Directory.GetFiles(input, "*.*", SearchOption.TopDirectoryOnly);
                        ProcessMultipleLevels(inputFiles, createLog);
                    }
                    else Pack.ProcessPackInput(input);
                }
                else
                {
                    if (!repack)
                    {
                        string[] inputFiles = { input };
                        ProcessMultipleLevels(inputFiles, createLog);
                    }
                    else Console.WriteLine("A single file was specified while in repacking mode. Stopping.");
                }
                Console.WriteLine("Done.");
            }
            else Console.WriteLine("ERROR: Input file does not exist.");
            Console.Read();
        }

        // Process a file
        static void ProcessFile(string input, bool createLog)
        {
            Helpers.Print($"Processing {input}", false);

            // Read file to memory
            MemoryStream msInput = new MemoryStream(File.ReadAllBytes(input));
            BinaryReader brInput = new BinaryReader(msInput);
            if (msInput.Length == 0) { Console.WriteLine("File is empty. Skipping."); return; }

            int fileMagic = brInput.ReadInt32();

            // MOMO Header: snp, snd
            if (fileMagic == 0x4F4D4F4D)
            {
                Console.WriteLine("MOMO Header detected.");
                Unpack.UnpackSimpleArchive(input, brInput, 8, createLog);
            }
            // ECD Header
            else if (fileMagic == 0x1A646365)
            {
                Console.WriteLine("ECD Header detected.");
                byte[] buffer = File.ReadAllBytes(input);
                Crypto.decEcd(buffer);
                byte[] bufferStripped = new byte[buffer.Length - 0x10];
                Array.Copy(buffer, 0x10, bufferStripped, 0, buffer.Length - 0x10);
                File.WriteAllBytes(input, bufferStripped);
                Helpers.Print("File decrypted. Processing output.", false);
            }
            // EXF Header
            else if (fileMagic == 0x1A667865)
            {
                Console.WriteLine("EXF Header detected.");
                byte[] buffer = File.ReadAllBytes(input);
                /*Crypto.decexf(buffer);
                byte[] bufferStripped = new byte[buffer.Length - 0x10];
                Array.Copy(buffer, 0x10, bufferStripped, 0, buffer.Length - 0x10);
                File.WriteAllBytes("out.bin", bufferStripped);
                Helpers.Print("File decrypted. Processing output.", false);*/
            }
            // JKR Header
            else if (fileMagic == 0x1A524B4A)
            {
                Console.WriteLine("JKR Header detected.");
                Unpack.UnpackJPK(input, brInput);
            }
            // MHA Header
            else if (fileMagic == 0x0161686D)
            {
                Console.WriteLine("MHA Header detected.");
                Unpack.UnpackMHA(input, brInput);
            }
            // MHF Text file
            else if (fileMagic == 0x000B0000)
            {
                Console.WriteLine("MHF Text file detected.");
                Unpack.PrintFTXT(input, brInput);
            }
            // Try to unpack as simple container: i.e. txb, bin, pac, gab
            else
            {
                Console.WriteLine("Trying to unpack as generic simple container.");
                brInput.BaseStream.Seek(0, SeekOrigin.Begin);
                try { Unpack.UnpackSimpleArchive(input, brInput, 4, createLog); } catch { }                
            }

            if (fileMagic == 0x1A646365) { ProcessFile(input, createLog); return; }
            else Console.WriteLine("==============================");
        }

        // Process file(s) on multiple levels
        static void ProcessMultipleLevels(string[] inputFiles, bool createLog)
        {
            // CurrentLevel        
            foreach (string inputFile in inputFiles)
            {
                ProcessFile(inputFile, createLog);

                FileInfo fileInfo = new FileInfo(inputFile);
                string[] patterns = { "*.bin", "*.jkr", "*.ftxt" };
                string directory = $"{fileInfo.DirectoryName}\\{Path.GetFileNameWithoutExtension(inputFile)}";

                if (Directory.Exists(directory))
                {
                    //Process All Successive Levels
                    ProcessMultipleLevels(Helpers.MyDirectory.GetFiles(directory, patterns, SearchOption.TopDirectoryOnly), createLog);
                }
            }
        }
    }
}
