using CsvHelper;
using Force.Crc32;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace FrontierTextTool
{
    class Program
    {
        static bool verbose = false;
        static bool autoClose = false;

        //[STAThread]
        static void Main(string[] args)
        {
            if (args.Length < 2) { Console.WriteLine("Too few arguments."); return; }

            if (args.Any("-verbose".Contains)) verbose = true;
            if (args.Any("-close)".Contains)) autoClose = true;

            if (args[0] == "dump") DumpAndHash(args[1], Convert.ToInt32(args[2]), Convert.ToInt32(args[3]));
            if (args[0] == "insert") InsertStrings(args[1], args[2]);
            if (args[0] == "merge") Merge(args[1], args[2]);
            if (!autoClose) { Console.WriteLine("Done"); Console.Read(); }
        }

        public class StringDatabase
        {
            public UInt32 Offset { get; set; }
            public UInt32 Hash { get; set; }
            public string jString { get; set; }
            public string eString { get; set; }
        }

        // insert src\mhfpac.bin csv\mhfpac_01.csv
        static void InsertStrings(string inputFile, string inputCsv)
        {
            Console.WriteLine($"Processing {inputFile}...");
            byte[] inputArray = File.ReadAllBytes(inputFile);

            // Read csv
            var stringDatabase = new List<StringDatabase>();
            using (var reader = new StreamReader(inputCsv, Encoding.GetEncoding("shift-jis")))
            {
                using (var csv = new CsvReader(reader))
                {
                    csv.Configuration.Delimiter = "\t";
                    csv.Configuration.IgnoreQuotes = true;
                    csv.Configuration.MissingFieldFound = null;
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        var record = new StringDatabase
                        {
                            Offset = csv.GetField<UInt32>("Offset"),
                            Hash = csv.GetField<UInt32>("Hash"),
                            eString = csv.GetField("eString")
                        };
                        stringDatabase.Add(record);
                    }
                }
            }

            // Get info for translation array and get all offsets that need to be remapped
            List<UInt32> eStringsOffsets = new List<uint>();
            List<Int32> eStringLengths = new List<int>();
            foreach (var obj in stringDatabase)
            {
                if (obj.eString != "")
                {
                    eStringsOffsets.Add(obj.Offset);
                    eStringLengths.Add(GetNullterminatedStringLength(obj.eString));
                }
            }
            int eStringsLength = eStringLengths.Sum();
            int eStringsCount = eStringLengths.Count;

            // Create dictionary with offset replacements
            Dictionary<int, int> offsetDict = new Dictionary<int, int>();
            for (int i = 0; i < eStringsCount; i++) offsetDict.Add((int)eStringsOffsets[i], inputArray.Length + eStringLengths.Take(i).Sum());

            if (verbose) Console.WriteLine($"Filling array of size {eStringsLength.ToString("X8")}...");
            byte[] eStringsArray = new byte[eStringsLength];
            for (int i = 0; i < stringDatabase.Count; i++)
            {
                if (stringDatabase[i].eString != "")
                {
                    // Write string to string array
                    int test = eStringLengths.Take(i).Sum();
                    if (verbose) Console.WriteLine($"String: '{stringDatabase[i].eString}', Length: {eStringLengths[i] - 1}");
                    byte[] eStringArray = Encoding.GetEncoding("shift-jis").GetBytes(stringDatabase[i].eString);
                    Array.Copy(eStringArray, 0, eStringsArray, eStringLengths.Take(i).Sum(), eStringLengths[i] - 1);
                }
            }

            // Replace offsets in binary file
            for (int p = 0; p < inputArray.Length; p += 4)
            {
                if (p + 4 > inputArray.Length) continue;
                int cur = BitConverter.ToInt32(inputArray, p);
                if (offsetDict.ContainsKey(cur) && p > 10000)
                {
                    int replacement = 0; offsetDict.TryGetValue(cur, out replacement);
                    byte[] newPointer = BitConverter.GetBytes(replacement);
                    for (int w = 0; w < 4; w++) inputArray[p + w] = newPointer[w];
                }
            }

            // Combine arrays
            byte[] outputArray = new byte[inputArray.Length + eStringsLength];
            Array.Copy(inputArray, outputArray, inputArray.Length);
            Array.Copy(eStringsArray, 0, outputArray, inputArray.Length, eStringsArray.Length);

            // Output file
            Directory.CreateDirectory("output");
            string outputFile = $"output\\{Path.GetFileName(inputFile)}";
            File.WriteAllBytes(outputFile, outputArray);

            // Pack with jpk type 0 and encrypt file with ecd
            ReFrontier.Pack.JPKEncode(0, outputFile, outputFile, 15);
            byte[] buffer = File.ReadAllBytes(outputFile);
            byte[] bufferMeta = File.ReadAllBytes($"{outputFile}.meta");
            buffer = ReFrontier.Crypto.encEcd(buffer, bufferMeta);
            File.WriteAllBytes(outputFile, buffer);
            ReFrontier.Helpers.PrintUpdateEntry(outputFile);

            // Upload to ftp
            FileUploadSFTP(buffer, $"/var/www/html/mhfo/dat/{Path.GetFileName(inputFile)}");
        }

        // dump mhfpac.bin 4416 1278872
        // dump mhfdat.bin 3072 3328538
        static void DumpAndHash(string input, int startOffset, int endOffset)
        {
            MemoryStream msInput = new MemoryStream(File.ReadAllBytes(input));
            BinaryReader brInput = new BinaryReader(msInput);

            Console.WriteLine($"Strings at: 0x{startOffset.ToString("X8")} - 0x{endOffset.ToString("X8")}. Size 0x{(endOffset - startOffset).ToString("X8")}");

            string fileName = Path.GetFileNameWithoutExtension(input);
            if (File.Exists($"{fileName}.csv")) File.Delete($"{fileName}.csv");
            StreamWriter txtOutput = new StreamWriter($"{fileName}.csv", true, Encoding.GetEncoding("shift-jis"));
            txtOutput.WriteLine("Offset\tHash\tjString\teString");

            brInput.BaseStream.Seek(startOffset, SeekOrigin.Begin);
            while (brInput.BaseStream.Position < endOffset)
            {
                long off = brInput.BaseStream.Position;
                string str = ReFrontier.Helpers.ReadNullterminatedString(brInput, Encoding.GetEncoding("shift-jis")).
                    Replace("\t", "<TAB>"). // Replace tab
                    Replace("\r\n", "<CLINE>"). // Replace carriage return
                    Replace("\n", "<NLINE>"); // Replace new line
                txtOutput.WriteLine($"{off}\t{Crc32Algorithm.Compute(Encoding.GetEncoding("shift-jis").GetBytes(str))}\t{str}\t");
            }
            txtOutput.Close();
        }

        // Merge old and updated csvs
        static void Merge(string oldCsv, string newCsv)
        {
            // Read csv
            var stringDbOld = new List<StringDatabase>();
            using (var reader = new StreamReader(oldCsv, Encoding.GetEncoding("shift-jis")))
            {
                using (var csv = new CsvReader(reader))
                {
                    csv.Configuration.Delimiter = "\t";
                    csv.Configuration.IgnoreQuotes = true;
                    csv.Configuration.MissingFieldFound = null;
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        var record = new StringDatabase
                        {
                            Hash = csv.GetField<UInt32>("Hash"),
                            eString = csv.GetField("eString")
                        };
                        stringDbOld.Add(record);
                    }
                }
            }

            var stringDbNew = new List<StringDatabase>();
            using (var reader = new StreamReader(newCsv, Encoding.GetEncoding("shift-jis")))
            {
                using (var csv = new CsvReader(reader))
                {
                    csv.Configuration.Delimiter = "\t";
                    csv.Configuration.IgnoreQuotes = true;
                    csv.Configuration.MissingFieldFound = null;
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        var record = new StringDatabase
                        {
                            Offset = csv.GetField<UInt32>("Offset"),
                            Hash = csv.GetField<UInt32>("Hash"),
                            eString = csv.GetField("eString"),
                            jString = csv.GetField("jString")
                        };
                        stringDbNew.Add(record);
                    }
                }
            }

            // Copy eStrings to new db
            for (int i = 0; i < stringDbOld.Count; i++)
            {
                Console.Write($"\rUpdating entry {i}/{stringDbOld.Count}");
                if (stringDbOld[i].eString != "")
                {
                    var matchedNewObjs = stringDbNew.Where(x => x.Hash.Equals(stringDbOld[i].Hash));
                    if (matchedNewObjs.Count() > 0)
                    {
                        foreach (var obj in matchedNewObjs) obj.eString = stringDbOld[i].eString;
                    }
                }
            }

            // Using this approach because csvHelper would always escape some strings which might mess up in-game when copy-pasting where required
            string fileName = "csv\\" + Path.GetFileName(oldCsv);
            if (File.Exists(fileName)) File.Delete(fileName);
            StreamWriter txtOutput = new StreamWriter(fileName, true, Encoding.GetEncoding("shift-jis"));
            txtOutput.WriteLine("Offset\tHash\tjString\teString");
            foreach (var obj in stringDbNew) txtOutput.WriteLine($"{obj.Offset}\t{obj.Hash}\t{obj.jString}\t{obj.eString}");
            txtOutput.Close();

            File.Delete(newCsv);
        }

        // Get byte length of string (avoids issues with special spacing characters)
        public static int GetNullterminatedStringLength(string input)
        {
            return Encoding.GetEncoding("shift-jis").GetBytes(input).Length + 1;
        }

        // Upload to ftp
        public static void FileUploadSFTP(byte[] buffer, string path)
        {
            var host = "192.168.2.121";
            var port = 22;
            var username = "root";
            var password = "coconut";

            using (var client = new SftpClient(host, port, username, password))
            {
                client.Connect();
                if (client.IsConnected)
                {
                    Console.WriteLine($"Connected. Uploading to {path}...");
                    using (var ms = new MemoryStream(buffer))
                    {
                        client.BufferSize = (uint)ms.Length;
                        client.UploadFile(ms, path);
                    }
                }
                else
                {
                    Console.WriteLine("Could not connect.");
                    return;
                }
            }
        }
    }
}