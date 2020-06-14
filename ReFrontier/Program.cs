using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LibReFrontier;

namespace ReFrontier
{
    class Program
    {
        static bool createLog = false;
        static bool recursive = true;
        static bool repack = false;
        static bool decryptOnly = false;
        static bool noDecryption = false;
        static bool encrypt = false;
        static bool autoClose = false;
        static bool cleanUp = false;
        static bool compress = false;
        static bool ignoreJPK = false;
        static bool stageContainer = false;
        static bool autoStage = false;

        //[STAThread]
        static void Main(string[] args)
        {
            Helpers.Print("ReFrontier by MHVuze", false);

            // Assign arguments
            if (args.Length < 1)
            {
                Helpers.Print("Usage: ReFrontier <file> (options)\n" +
                    "Options:\n" +
                    "-log: Write log file (required for repacking)\n" +
                    "-nonRecursive: Do not unpack recursively\n" +
                    "-pack: Repack directory (requires log file)\n" +
                    "-decryptOnly: Decrypt ecd files without unpacking\n" +
                    "-noDecryption: Don't decrypt ecd files, no unpacking\n" +
                    "-compress [type],[level]: Pack file with jpk [type] at compression [level]\n" +
                    "-encrypt: Encrypt input file with ecd algorithm\n" +
                    "-close: Close window after finishing process\n" +
                    "-cleanUp: Delete simple archives after unpacking\n" +
                    "-ignoreJPK: Do not decompress JPK files\n" +
                    "-stageContainer: Unpack file as stage-specific container\n" +
                    "-autoStage: Automatically attempt to unpack containers that might be stage-specific", 
                    false);
                Console.Read();
                return;
            }

            string input = args[0];
            if (args.Any("-log".Contains)) { createLog = true; repack = false; }
            if (args.Any("-nonRecursive".Contains)) { recursive = false; repack = false; }
            if (args.Any("-pack".Contains)) repack = true;
            if (args.Any("-decryptOnly".Contains)) { decryptOnly = true; repack = false; }
            if (args.Any("-noDecryption".Contains)) { noDecryption = true; repack = false; }
            if (args.Any("-encrypt".Contains)) { encrypt = true; repack = false; }
            if (args.Any("-close)".Contains)) autoClose = true;
            if (args.Any("-cleanUp)".Contains)) cleanUp = true;
            if (args.Any("-compress)".Contains)) { compress = true; repack = false; }
            if (args.Any("-ignoreJPK".Contains)) { ignoreJPK = true; repack = false; }
            if (args.Any("-stageContainer".Contains)) { stageContainer = true; repack = false; }
            if (args.Any("-autoStage".Contains)) { autoStage = true; repack = false; }

            // Check file
            if (File.Exists(input) || Directory.Exists(input))
            {
                FileAttributes inputAttr = File.GetAttributes(input);
                // Directories
                if (inputAttr.HasFlag(FileAttributes.Directory))
                {
                    if (!repack && !encrypt)
                    {
                        string[] inputFiles = Directory.GetFiles(input, "*.*", SearchOption.AllDirectories);
                        ProcessMultipleLevels(inputFiles);
                    }
                    else if (repack) Pack.ProcessPackInput(input);
                    else if (compress) Console.WriteLine("A directory was specified while in compression mode. Stopping.");
                    else if (encrypt) Console.WriteLine("A directory was specified while in encryption mode. Stopping.");
                }
                // Single file
                else
                {
                    if (!repack && !encrypt && !compress)
                    {
                        string[] inputFiles = { input };
                        ProcessMultipleLevels(inputFiles);
                    }
                    else if (repack) Console.WriteLine("A single file was specified while in repacking mode. Stopping.");
                    else if (compress) 
                    {
                        string pattern = @"-compress (\d+),(\d+)";
                        try
                        {
                            Match match = Regex.Matches(string.Join(" ", args, 1, args.Length - 1), pattern)[0];
                            ushort type = ushort.Parse(match.Groups[1].Value);
                            int level = int.Parse(match.Groups[2].Value) * 100;
                            Pack.JPKEncode(type, input, $"output\\{Path.GetFileName(input)}", level);
                        }
                        catch
                        {
                            Console.WriteLine("ERROR: Check compress input. Example: -compress 3,50");
                        }
                    }
                    else if (encrypt)
                    {
                        byte[] buffer = File.ReadAllBytes(input);
                        byte[] bufferMeta = File.ReadAllBytes($"{input}.meta");
                        buffer = Crypto.encEcd(buffer, bufferMeta);
                        File.WriteAllBytes(input, buffer);
                        Helpers.Print("File encrypted.", false);
                        Helpers.GetUpdateEntry(input);
                    }
                }
                Console.WriteLine("Done.");
            }
            else Console.WriteLine("ERROR: Input file does not exist.");
           if (!autoClose) Console.Read();
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

            // Since stage containers have no file magic, check for them first
            if (stageContainer == true)
            {
                brInput.BaseStream.Seek(0, SeekOrigin.Begin);
                try { Unpack.UnpackStageContainer(input, brInput, createLog, cleanUp); } catch { }
            }
            // MOMO Header: snp, snd
            else if (fileMagic == 0x4F4D4F4D)
            {
                Console.WriteLine("MOMO Header detected.");
                Unpack.UnpackSimpleArchive(input, brInput, 8, createLog, cleanUp, autoStage);
            }
            // ECD Header
            else if (fileMagic == 0x1A646365)
            {
                Console.WriteLine("ECD Header detected.");
                if (noDecryption == false)
                {
                    byte[] buffer = File.ReadAllBytes(input);
                    Crypto.decEcd(buffer);

                    byte[] ecdHeader = new byte[0x10];
                    Array.Copy(buffer, 0, ecdHeader, 0, 0x10);
                    byte[] bufferStripped = new byte[buffer.Length - 0x10];
                    Array.Copy(buffer, 0x10, bufferStripped, 0, buffer.Length - 0x10);

                    File.WriteAllBytes(input, bufferStripped);
                    if (createLog) File.WriteAllBytes($"{input}.meta", ecdHeader);
                    Console.WriteLine("File decrypted.");
                }
                else
                {
                    Helpers.Print("Not decrypting due to flag.", false);
                    return;
                }
            }
            // EXF Header
            else if (fileMagic == 0x1A667865)
            {
                Console.WriteLine("EXF Header detected.");
                byte[] buffer = File.ReadAllBytes(input);
                Crypto.decExf(buffer);
                byte[] bufferStripped = new byte[buffer.Length - 0x10];
                Array.Copy(buffer, 0x10, bufferStripped, 0, buffer.Length - 0x10);
                File.WriteAllBytes(input, bufferStripped);
                Console.WriteLine("File decrypted.");
            }
            // JKR Header
            else if (fileMagic == 0x1A524B4A)
            {
                Console.WriteLine("JKR Header detected.");
                if (!ignoreJPK) { Unpack.UnpackJPK(input); Console.WriteLine("File decompressed."); }
            }
            // MHA Header
            else if (fileMagic == 0x0161686D)
            {
                Console.WriteLine("MHA Header detected.");
                Unpack.UnpackMHA(input, brInput, createLog);
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
                brInput.BaseStream.Seek(0, SeekOrigin.Begin);
                try { Unpack.UnpackSimpleArchive(input, brInput, 4, createLog, cleanUp, autoStage); } catch { }                
            }

            if (fileMagic == 0x1A646365 && !decryptOnly) { Console.WriteLine("=============================="); ProcessFile(input); return; }
            else Console.WriteLine("==============================");
        }

        // Process file(s) on multiple levels
        static void ProcessMultipleLevels(string[] inputFiles)
        {
            // CurrentLevel        
            foreach (string inputFile in inputFiles)
            {
                ProcessFile(inputFile);

                // Disable stage processing files unpacked from parent
                if (stageContainer == true) stageContainer = false;

                FileInfo fileInfo = new FileInfo(inputFile);
                string[] patterns = { "*.bin", "*.jkr", "*.ftxt", "*.snd" };
                string directory = $"{fileInfo.DirectoryName}\\{Path.GetFileNameWithoutExtension(inputFile)}";

                if (Directory.Exists(directory) && recursive)
                {
                    //Process All Successive Levels
                    ProcessMultipleLevels(Helpers.MyDirectory.GetFiles(directory, patterns, SearchOption.TopDirectoryOnly));
                }
            }
        }
    }
}
