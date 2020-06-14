using LibReFrontier;
using ReFrontier.jpk;
using System;
using System.Collections.Generic;
using System.IO;

namespace ReFrontier
{
    public class Pack
    {
        public static void ProcessPackInput(string input)
        {
            string logFile = $"{input}\\{input.Remove(0, input.LastIndexOf('\\')+1)}.log";
            if (!File.Exists(logFile))
            {
                logFile = $"{input}.log";
                if (!File.Exists(logFile)) Console.WriteLine("ERROR: Log file does not exist."); return;
            }
            string[] logContent = File.ReadAllLines(logFile);

            // Simple Archive
            if (logContent[0] == "SimpleArchive")
            {
                string fileName = logContent[1];
                int count = int.Parse(logContent[2]);
                Console.WriteLine($"Simple archive with {count} entries.");

                // Entries
                List<string> listFileNames = new List<string>();
                //List<int> listFileOffsets = new List<int>();
                //List<int> listFileSizes = new List<int>();
                //List<int> listFileMagics = new List<int>();

                for (int i = 3; i < logContent.Length; i++)
                {
                    string[] columns = logContent[i].Split(',');
                    listFileNames.Add(columns[0]);
                    //listFileOffsets.Add(int.Parse(columns[1]));
                    //listFileSizes.Add(int.Parse(columns[2]));
                    //listFileMagics.Add(int.Parse(columns[3]));
                }

                Directory.CreateDirectory("output");
                fileName = $"output\\{fileName}";
                using (BinaryWriter bwOutput = new BinaryWriter(File.Open(fileName, FileMode.Create)))
                {
                    bwOutput.Write(count);
                    int offset = 0x04 + count * 0x08;
                    for (int i = 0; i < count; i++)
                    {
                        Console.WriteLine($"{input}\\{listFileNames[i]}");
                        byte[] fileData = new byte[0];
                        if (listFileNames[i] != "null") { fileData = File.ReadAllBytes($"{input}\\{listFileNames[i]}"); }
                        bwOutput.BaseStream.Seek(0x04 + i * 0x08, SeekOrigin.Begin);
                        bwOutput.Write(offset);
                        bwOutput.Write(fileData.Length);
                        bwOutput.BaseStream.Seek(offset, SeekOrigin.Begin);
                        bwOutput.Write(fileData);
                        offset += fileData.Length;
                    }
                }
                Helpers.GetUpdateEntry(fileName);
            }
            // MHA; this doesn't do file data padding for now, but seems the game works just fine without it
            else if (logContent[0] == "MHA")
            {
                string fileName = logContent[1];
                int count = int.Parse(logContent[2]);
                Int16 unk1 = Int16.Parse(logContent[3]);
                Int16 unk2 = Int16.Parse(logContent[4]);
                Console.WriteLine($"MHA with {count} entries (unk1: {unk1}, unk2: {unk2}).");

                // Entries
                List<string> listFileNames = new List<string>();
                List<int> listFileIds = new List<int>();

                for (int i = 0; i < count; i++)
                {
                    string[] columns = logContent[i + 5].Split(',');  // 5 = Account for meta data entries before
                    listFileNames.Add(columns[0]);
                    listFileIds.Add(int.Parse(columns[1]));
                }

                // Set up memory streams for segments
                MemoryStream entryMetaBlock = new MemoryStream();
                MemoryStream entryNamesBlock = new MemoryStream();

                Directory.CreateDirectory("output");
                fileName = $"output\\{fileName}";
                using (BinaryWriter bwOutput = new BinaryWriter(File.Open(fileName, FileMode.Create)))
                {
                    // Header
                    bwOutput.Write((Int32)23160941);    // MHA magic
                    bwOutput.Write((Int32)0);           // pointerEntryMetaBlock
                    bwOutput.Write(count);
                    bwOutput.Write((Int32)0);           // pointerEntryNamesBlock
                    bwOutput.Write((Int32)0);           // entryNamesBlockLength
                    bwOutput.Write(unk1);
                    bwOutput.Write(unk2);

                    int pointerEntryNamesBlock = 0x18;   // 0x18 = Header length
                    int stringOffset = 0;
                    for (int i = 0; i < count; i++)
                    {
                        Console.WriteLine($"{input}\\{listFileNames[i]}");
                        byte[] fileData = File.ReadAllBytes($"{input}\\{listFileNames[i]}");
                        bwOutput.Write(fileData);

                        entryMetaBlock.Write(BitConverter.GetBytes(stringOffset), 0, 4);
                        entryMetaBlock.Write(BitConverter.GetBytes(pointerEntryNamesBlock), 0, 4);
                        entryMetaBlock.Write(BitConverter.GetBytes(fileData.Length), 0, 4);
                        entryMetaBlock.Write(BitConverter.GetBytes(fileData.Length), 0, 4); // write psize if necessary
                        entryMetaBlock.Write(BitConverter.GetBytes(listFileIds[i]), 0, 4);

                        System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
                        byte[] arrayFileName = enc.GetBytes(listFileNames[i]);
                        entryNamesBlock.Write(arrayFileName, 0, arrayFileName.Length);
                        entryNamesBlock.WriteByte(0);
                        stringOffset += arrayFileName.Length + 1;

                        pointerEntryNamesBlock += fileData.Length; // update with psize if necessary
                    }

                    bwOutput.Write(entryNamesBlock.ToArray());
                    bwOutput.Write(entryMetaBlock.ToArray());

                    // Update offsets
                    bwOutput.Seek(4, SeekOrigin.Begin);
                    bwOutput.Write((Int32)(pointerEntryNamesBlock + entryNamesBlock.Length));
                    bwOutput.Write(count);
                    bwOutput.Write(pointerEntryNamesBlock);
                    bwOutput.Write((Int32)entryNamesBlock.Length);
                }
            }
            Console.WriteLine("==============================");
        }

        public static void JPKEncode(UInt16 atype, string inPath, string otPath, int level)
        {
            Directory.CreateDirectory("output");

            UInt16 type = atype;
            byte[] buffer = File.ReadAllBytes(inPath);
            int insize = buffer.Length;
            if (File.Exists(otPath)) File.Delete(otPath);
            FileStream fsot = File.Create(otPath);
            BinaryWriter br = new BinaryWriter(fsot);
            UInt32 u32 = 0x1A524B4A;
            UInt16 u16 = 0x108;
            br.Write(u32);
            br.Write(u16);
            br.Write(type);
            u32 = 0x10;
            br.Write(u32);
            br.Write(insize);
            IJPKEncode encoder = null;
            switch (type)
            {
                case 0:
                    encoder = new JPKEncodeRW();
                    break;
                case 2:
                    //encoder = new JPKEncodeHFIRW();
                    break;
                case 3:
                    encoder = new JPKEncodeLz();
                    break;
                case 4:
                    encoder = new JPKEncodeHFI();
                    break;
            }

            if (encoder != null)
            {
                DateTime sta, fin;
                sta = DateTime.Now;
                encoder.ProcessOnEncode(buffer, fsot, level, null);
                fin = DateTime.Now;
                Helpers.Print($"File compressed using type {type} (level {level / 100}): {fsot.Length} bytes ({1 - (decimal)fsot.Length / insize:P} saved) in {(fin - sta).ToString("%m\\:ss\\.ff")}", false);
                fsot.Close();
            }
            else
            {
                Console.WriteLine("Unsupported/invalid type: " + type);
                fsot.Close();
                File.Delete(otPath);
            }
        }
    }
}
