using System;
using System.IO;

namespace ReFrontier
{
    class Program
    {
        static void Main(string[] args)
        {
            Helpers.Print("ReFrontier by MHVuze", false);

            // Assign arguments
            if (args.Length < 1) { Console.WriteLine("ERROR: Not enough arguments specified."); Console.Read(); return; }
            string input = args[0];

            // Check file
            if (File.Exists(input) || Directory.Exists(input))
            {
                FileAttributes inputAttr = File.GetAttributes(input);
                if (inputAttr.HasFlag(FileAttributes.Directory))
                {
                    string[] inputFiles = Directory.GetFiles(input, "*.*", SearchOption.TopDirectoryOnly);
                    ProcessMultipleLevels(inputFiles);
                }
                else
                {
                    string[] inputFiles = { input };
                    ProcessMultipleLevels(inputFiles);
                }
                Console.WriteLine("Done.");
            }
            else
            {
                Console.WriteLine("ERROR: Input file does not exist.");
            }
            Console.Read();
        }

        // Process a file
        static void ProcessFile(string input)
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
                Handlers.UnpackSimpleArchive(input, brInput, 8);
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
                Handlers.UnpackJPK(input, brInput);
            }
            // MHA Header
            else if (fileMagic == 0x0161686D)
            {
                Console.WriteLine("MHA Header detected.");
                Handlers.UnpackMHA(input, brInput);
            }
            // Try to unpack as simple container: i.e. txb, bin, pac, gab
            else
            {
                Console.WriteLine("Trying to unpack as generic simple container.");
                brInput.BaseStream.Seek(0, SeekOrigin.Begin);
                try { Handlers.UnpackSimpleArchive(input, brInput, 4); } catch { }                
            }

            if (fileMagic == 0x1A646365) { ProcessFile(input); return; }
            else Console.WriteLine("==============================");
        }

        // Process file(s) on multiple levels
        static void ProcessMultipleLevels(string[] inputFiles)
        {
            // First level            
            foreach (string inputFile in inputFiles)
            {
                ProcessFile(inputFile);

                // Second level
                FileInfo fileInfo = new FileInfo(inputFile);
                string[] patterns = { "*.bin", "*.jkr" };
                string directory = $"{fileInfo.DirectoryName}\\{Path.GetFileNameWithoutExtension(inputFile)}";

                if (Directory.Exists(directory))
                {
                    var secondLvlInputFiles = Helpers.MyDirectory.GetFiles(directory, patterns, SearchOption.TopDirectoryOnly);
                    foreach (string secondLvlInputFile in secondLvlInputFiles)
                    {
                        ProcessFile(secondLvlInputFile);

                        // Third level
                        fileInfo = new FileInfo(secondLvlInputFile);
                        directory = $"{fileInfo.DirectoryName}\\{Path.GetFileNameWithoutExtension(secondLvlInputFile)}";
                        if (Directory.Exists(directory))
                        {
                            var thirdLvlInputFiles = Helpers.MyDirectory.GetFiles(directory, patterns, SearchOption.TopDirectoryOnly);
                            foreach (string thirdLvlInputFile in thirdLvlInputFiles)
                            {
                                ProcessFile(thirdLvlInputFile);

                                // Fourth level
                                fileInfo = new FileInfo(thirdLvlInputFile);
                                directory = $"{fileInfo.DirectoryName}\\{Path.GetFileNameWithoutExtension(thirdLvlInputFile)}";
                                if (Directory.Exists(directory))
                                {
                                    var fourthLvlInputFiles = Helpers.MyDirectory.GetFiles(directory, patterns, SearchOption.TopDirectoryOnly);
                                    foreach (string fourthLvlInputFile in fourthLvlInputFiles) ProcessFile(fourthLvlInputFile);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
