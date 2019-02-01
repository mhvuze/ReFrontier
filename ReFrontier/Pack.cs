using Force.Crc32;
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
                        byte[] fileData = File.ReadAllBytes($"{input}\\{listFileNames[i]}");
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
            UInt32 u32;
            UInt16 u16;
            u32 = 0x1A524B4A;
            br.Write(u32);
            u16 = 0x108; // see it in all files
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
                    //encoder = new JPKEncodeLz();
                    break;
                case 4:
                    //encoder = new JPKEncodeHFI();
                    break;
            }

            if (encoder != null)
            {
                DateTime sta, fin;
                sta = DateTime.Now;
                encoder.ProcessOnEncode(buffer, fsot, level, null);
                fin = DateTime.Now;
                //Console.WriteLine("\r\nResult length " + fsot.Length + " bytes. Elapsed time " + (fin - sta).ToString("%m\\:ss\\.ff"));
            }
            else
            {
                Console.WriteLine("Invalid type: " + type);
                fsot.Close();
                File.Delete(otPath);
            }
        }
    }
}
