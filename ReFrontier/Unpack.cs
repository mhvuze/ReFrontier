using LibReFrontier;
using ReFrontier.jpk;
using System;
using System.IO;
using System.Text;

namespace ReFrontier
{
    class Unpack
    {
        public static void UnpackSimpleArchive(string input, BinaryReader brInput, int magicSize, bool createLog, bool cleanUp, bool autoStage)
        {
            FileInfo fileInfo = new FileInfo(input);
            string outputDir = $"{fileInfo.DirectoryName}\\{Path.GetFileNameWithoutExtension(input)}";

            // Abort if too small
            if (fileInfo.Length < 12)
            {
                Console.WriteLine("File is too small. Skipping.");
                return;
            }

            uint count = brInput.ReadUInt32();

            // Calculate complete size of extracted data to avoid extracting plausible files that aren't archives
            int completeSize = magicSize;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    brInput.BaseStream.Seek(magicSize, SeekOrigin.Current);
                    int entrySize = brInput.ReadInt32();
                    completeSize += entrySize;
                }
            }
            catch
            {
                Console.WriteLine("Caught file-based error during simple container check.");
            }

            // Very fragile check for stage container
            brInput.BaseStream.Seek(4, SeekOrigin.Begin);
            int checkUnk = brInput.ReadInt32();
            long checkZero = brInput.ReadInt64();
            if (checkUnk < 9999 && checkZero == 0)
            {
                if (autoStage == true)
                {
                    brInput.BaseStream.Seek(0, SeekOrigin.Begin);
                    UnpackStageContainer(input, brInput, createLog, cleanUp);
                }
                else
                {
                    Console.WriteLine($"Skipping. Not a valid simple container, but could be stage-specific. Try:\nReFrontier.exe {fileInfo.FullName} -stageContainer");
                }
                return;
            }

            if (completeSize > fileInfo.Length || count == 0 || count > 9999)
            {
                Console.WriteLine("Skipping. Not a valid simple container.");
                return;
            }

            Console.WriteLine("Trying to unpack as generic simple container.");
            brInput.BaseStream.Seek(magicSize, SeekOrigin.Begin);

            // Write to log file if desired; needs some other solution because it creates useless logs even if !createLog
            Directory.CreateDirectory(outputDir);
            StreamWriter logOutput = new StreamWriter($"{outputDir}\\{Path.GetFileNameWithoutExtension(input)}.log");
            if (createLog) { logOutput.WriteLine("SimpleArchive"); logOutput.WriteLine(input.Remove(0, input.LastIndexOf('\\') + 1)); logOutput.WriteLine(count); }

            for (int i = 0; i < count; i++)
            {
                int entryOffset = brInput.ReadInt32();
                int entrySize = brInput.ReadInt32();

                // Skip if size is zero
                if (entrySize == 0)
                {
                    Console.WriteLine($"Offset: 0x{entryOffset.ToString("X8")}, Size: 0x{entrySize.ToString("X8")} (SKIPPED)");
                    if (createLog) logOutput.WriteLine($"null,{entryOffset},{entrySize},0");
                    continue;
                }

                // Read file to array
                brInput.BaseStream.Seek(entryOffset, SeekOrigin.Begin);
                byte[] entryData = brInput.ReadBytes(entrySize);

                // Check file header and get extension
                byte[] header = new byte[4];
                Array.Copy(entryData, header, 4);
                uint headerInt = BitConverter.ToUInt32(header, 0);
                string extension = Enum.GetName(typeof(Helpers.Extensions), headerInt);
                if (extension == null) extension = Helpers.CheckForMagic(headerInt, entryData);
                if (extension == null) extension = "bin";

                // Print info
                Console.WriteLine($"Offset: 0x{entryOffset.ToString("X8")}, Size: 0x{entrySize.ToString("X8")} ({extension})");
                if (createLog) logOutput.WriteLine($"{(i + 1).ToString("D4")}_{entryOffset.ToString("X8")}.{extension},{entryOffset},{entrySize},{headerInt}");

                // Extract file
                File.WriteAllBytes($"{outputDir}\\{(i + 1).ToString("D4")}_{entryOffset.ToString("X8")}.{extension}", entryData);

                // Move to next entry block
                brInput.BaseStream.Seek(magicSize + (i + 1) * 0x08, SeekOrigin.Begin);
            }
            // Clean up
            logOutput.Close();
            if (!createLog) File.Delete($"{outputDir}\\{Path.GetFileNameWithoutExtension(input)}.log");
            if (cleanUp) File.Delete(input);
            //if (Directory.GetFiles(outputDir) == null) Directory.Delete(outputDir);
        }

        public static void UnpackMHA(string input, BinaryReader brInput, bool createLog)
        {
            FileInfo fileInfo = new FileInfo(input);
            string outputDir = $"{fileInfo.DirectoryName}\\{Path.GetFileNameWithoutExtension(input)}";
            Directory.CreateDirectory(outputDir);

            StreamWriter logOutput = new StreamWriter($"{outputDir}\\{Path.GetFileNameWithoutExtension(input)}.log");
            if (createLog) { logOutput.WriteLine("MHA"); logOutput.WriteLine(input.Remove(0, input.LastIndexOf('\\') + 1)); }

            // Read header
            int pointerEntryMetaBlock = brInput.ReadInt32();
            int count = brInput.ReadInt32();
            int pointerEntryNamesBlock = brInput.ReadInt32();
            int entryNamesBlockLength = brInput.ReadInt32();
            Int16 unk1 = brInput.ReadInt16();
            Int16 unk2 = brInput.ReadInt16();
            if (createLog) { logOutput.WriteLine(count); logOutput.WriteLine(unk1); logOutput.WriteLine(unk2); }

            // File Data
            for (int i = 0; i < count; i++)
            {
                // Get meta
                brInput.BaseStream.Seek(pointerEntryMetaBlock + i * 0x14, SeekOrigin.Begin);
                int stringOffset = brInput.ReadInt32();
                int entryOffset = brInput.ReadInt32();
                int entrySize = brInput.ReadInt32();
                int pSize = brInput.ReadInt32();        // Padded size
                int fileId = brInput.ReadInt32();

                // Get name
                brInput.BaseStream.Seek(pointerEntryNamesBlock + stringOffset, SeekOrigin.Begin);
                string entryName = Helpers.ReadNullterminatedString(brInput, Encoding.UTF8);
                if (createLog) logOutput.WriteLine(entryName + "," + fileId);

                // Extract file
                brInput.BaseStream.Seek(entryOffset, SeekOrigin.Begin);
                byte[] entryData = brInput.ReadBytes(entrySize);
                File.WriteAllBytes($"{outputDir}\\{entryName}", entryData);

                Console.WriteLine($"{entryName}, String Offset: 0x{stringOffset.ToString("X8")}, Offset: 0x{entryOffset.ToString("X8")}, Size: 0x{entrySize.ToString("X8")}, pSize: 0x{pSize.ToString("X8")}, File ID: 0x{fileId.ToString("X8")}");
            }

            logOutput.Close();
            if (!createLog) File.Delete($"{outputDir}\\{Path.GetFileNameWithoutExtension(input)}.log");
        }

        public static void UnpackJPK(string input)
        {
            byte[] buffer = File.ReadAllBytes(input);
            MemoryStream ms = new MemoryStream(buffer);
            BinaryReader br = new BinaryReader(ms);
            if (br.ReadUInt32() == 0x1A524B4A)
            {
                IJPKDecode decoder = null;
                ms.Seek(0x2, SeekOrigin.Current);
                int type = br.ReadUInt16();
                Console.WriteLine($"JPK Type: {type}");
                switch (type)
                {
                    case 0:
                        decoder = new JPKDecodeRW();
                        break;
                    case 2:
                        decoder = new JPKDecodeHFIRW();
                        break;
                    case 3:
                        decoder = new JPKDecodeLz();
                        break;
                    case 4:
                        decoder = new JPKDecodeHFI();
                        break;
                }
                if (decoder != null)
                {
                    // Decompres file
                    int startOffset = br.ReadInt32();
                    int outSize = br.ReadInt32();
                    byte[] outBuffer = new byte[outSize];
                    ms.Seek(startOffset, SeekOrigin.Begin);
                    decoder.ProcessOnDecode(ms, outBuffer);

                    // Get extension
                    byte[] header = new byte[4];
                    Array.Copy(outBuffer, header, 4);
                    uint headerInt = BitConverter.ToUInt32(header, 0);                    
                    string extension = Enum.GetName(typeof(Helpers.Extensions), headerInt);
                    if (extension == null) extension = Helpers.CheckForMagic(headerInt, outBuffer);
                    if (extension == null) extension = "bin";

                    FileInfo fileInfo = new FileInfo(input);
                    string output = $"{fileInfo.DirectoryName}\\{Path.GetFileNameWithoutExtension(input)}.{extension}";
                    File.Delete(input);
                    File.WriteAllBytes(output, outBuffer);
                }
            }
            br.Close();
            ms.Close();
        }

        public static void UnpackStageContainer(string input, BinaryReader brInput, bool createLog, bool cleanUp)
        {
            Console.WriteLine("Trying to unpack as stage-specific container.");

            FileInfo fileInfo = new FileInfo(input);
            string outputDir = $"{fileInfo.DirectoryName}\\{Path.GetFileNameWithoutExtension(input)}";
            Directory.CreateDirectory(outputDir);

            StreamWriter logOutput = new StreamWriter($"{outputDir}\\{Path.GetFileNameWithoutExtension(input)}.log");
            if (createLog) { logOutput.WriteLine("StageContainer"); logOutput.WriteLine(input.Remove(0, input.LastIndexOf('\\') + 1)); }

            // First three segments
            for (int i = 0; i < 3; i ++)
            {
                int offset = brInput.ReadInt32();
                int size = brInput.ReadInt32();

                if (size == 0)
                {
                    Console.WriteLine($"Offset: 0x{offset.ToString("X8")}, Size: 0x{size.ToString("X8")} (SKIPPED)");
                    if (createLog) logOutput.WriteLine($"null,{offset},{size},0");
                    continue;
                }

                brInput.BaseStream.Seek(offset, SeekOrigin.Begin);
                byte[] data = brInput.ReadBytes(size);

                // Get extension
                byte[] header = new byte[4];
                Array.Copy(data, header, 4);
                uint headerInt = BitConverter.ToUInt32(header, 0);
                string extension = Enum.GetName(typeof(Helpers.Extensions), headerInt);
                if (extension == null) extension = Helpers.CheckForMagic(headerInt, data);
                if (extension == null) extension = "bin";

                // Print info
                Console.WriteLine($"Offset: 0x{offset.ToString("X8")}, Size: 0x{size.ToString("X8")} ({extension})");
                if (createLog) logOutput.WriteLine($"{(i + 1).ToString("D4")}_{offset.ToString("X8")}.{extension},{offset},{size},{headerInt}");

                // Extract file
                File.WriteAllBytes($"{outputDir}\\{(i + 1).ToString("D4")}_{offset.ToString("X8")}.{extension}", data);

                // Move to next entry block
                brInput.BaseStream.Seek((i + 1) * 0x08, SeekOrigin.Begin);
            }

            // Rest
            int restCount = brInput.ReadInt32();
            int unkHeader = brInput.ReadInt32();
            if (createLog) logOutput.WriteLine(restCount + "," + unkHeader);
            for (int i = 3; i < restCount + 3; i++)
            {
                int offset = brInput.ReadInt32();
                int size = brInput.ReadInt32();
                int unk = brInput.ReadInt32();

                if (size == 0)
                {
                    Console.WriteLine($"Offset: 0x{offset.ToString("X8")}, Size: 0x{size.ToString("X8")}, Unk: 0x{unk.ToString("X8")} (SKIPPED)");
                    if (createLog) logOutput.WriteLine($"null,{offset},{size},{unk},0");
                    continue;
                }

                brInput.BaseStream.Seek(offset, SeekOrigin.Begin);
                byte[] data = brInput.ReadBytes(size);

                // Get extension
                byte[] header = new byte[4];
                Array.Copy(data, header, 4);
                uint headerInt = BitConverter.ToUInt32(header, 0);
                string extension = Enum.GetName(typeof(Helpers.Extensions), headerInt);
                if (extension == null) extension = Helpers.CheckForMagic(headerInt, data);
                if (extension == null) extension = "bin";

                // Print info
                Console.WriteLine($"Offset: 0x{offset.ToString("X8")}, Size: 0x{size.ToString("X8")}, Unk: 0x{unk.ToString("X8")} ({extension})");
                if (createLog) logOutput.WriteLine($"{(i + 1).ToString("D4")}_{offset.ToString("X8")}.{extension},{offset},{size},{unk}, {headerInt}");

                // Extract file
                File.WriteAllBytes($"{outputDir}\\{(i + 1).ToString("D4")}_{offset.ToString("X8")}.{extension}", data);

                // Move to next entry block
                brInput.BaseStream.Seek(0x18 + 0x08 + (i - 3 + 1) * 0x0C, SeekOrigin.Begin); // 0x18 = first three segments, 0x08 = header for this segment
            }

            // Clean up
            logOutput.Close();
            if (!createLog) File.Delete($"{outputDir}\\{Path.GetFileNameWithoutExtension(input)}.log");
            if (cleanUp) File.Delete(input);
        }

        public static void PrintFTXT(string input, BinaryReader brInput)
        {
            FileInfo fileInfo = new FileInfo(input);
            string outputPath = $"{fileInfo.DirectoryName}\\{Path.GetFileNameWithoutExtension(input)}.txt";
            if (File.Exists(outputPath)) File.Delete(outputPath);
            StreamWriter txtOutput = new StreamWriter(outputPath, true, Encoding.GetEncoding("shift-jis"));

            // Read header
            brInput.BaseStream.Seek(10, SeekOrigin.Current);
            int stringCount = brInput.ReadInt16();
            int textBlockSize = brInput.ReadInt32();

            for (int i = 0; i < stringCount; i++)
            {
                string str = Helpers.ReadNullterminatedString(brInput, Encoding.GetEncoding("shift-jis"));
                txtOutput.WriteLine(str.Replace("\n", "<NEWLINE>"));
            }

            txtOutput.Close();
        }
    }
}
