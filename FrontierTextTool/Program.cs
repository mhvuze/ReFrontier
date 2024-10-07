using CsvHelper;
using ReFrontier;
using LibReFrontier;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            if (args[0] == "cleanTrados") CleanTrados(args[1]);
            if (args[0] == "insertCAT") InsertCatFile(args[1], args[2]);
            if (!autoClose) { Console.WriteLine("Done"); Console.Read(); }
        }

        public class StringDatabase
        {
            public UInt32 Offset { get; set; }
            public UInt32 Hash { get; set; }
            public string jString { get; set; }
            public string eString { get; set; }
        }

        // Update MHFUP_00.DAT
        static void UpdateList(string updEntry)
        {
            string file = updEntry.Split(',')[3].Split('\\')[1];
            string[] lines = File.ReadAllLines("src\\MHFUP_00.DAT");
            for (int i = 0; i < lines.Count(); i++)
            {
                if (lines[i].Contains(file)) lines[i] = updEntry.Replace("output", "dat");
            }
            File.WriteAllLines("src\\MHFUP_00.DAT", lines);
            FileUploadSFTP(File.ReadAllBytes("src\\MHFUP_00.DAT"), $"/var/www/html/mhfo/MHFUP_00.DAT");
        }

        // Insert CAT file to csv
        static void InsertCatFile(string catFile, string csvFile)
        {
            Console.WriteLine($"Processing {catFile}...");
            CleanTrados(catFile);
            string[] catStrings = File.ReadAllLines(catFile, Encoding.UTF8);

            var stringDb = new List<StringDatabase>();
            using (var reader = new StreamReader(csvFile, Encoding.GetEncoding("shift-jis")))
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
                        stringDb.Add(record);
                    }
                }
            }

            // Copy catStrings to new db
            for (int i = 0; i < stringDb.Count; i++)
            {
                Console.Write($"\rUpdating entry {i + 1}/{stringDb.Count}");
                if (stringDb[i].jString != catStrings[i]) stringDb[i].eString = catStrings[i];
                // Allow for deletions
                else if (stringDb[i].jString == catStrings[i] && stringDb[i].eString != "") stringDb[i].eString = "";
            }
            Console.WriteLine();

            // Using this approach because csvHelper would always escape some strings which might mess up in-game when copy-pasting where required
            string fileName = "csv\\" + Path.GetFileName(csvFile);
            //string fileName = "test.csv";
            if (File.Exists(fileName)) File.Delete(fileName);
            StreamWriter txtOutput = new StreamWriter(fileName, true, Encoding.GetEncoding("shift-jis"));
            txtOutput.WriteLine("Offset\tHash\tjString\teString");
            foreach (var obj in stringDb) txtOutput.WriteLine($"{obj.Offset}\t{obj.Hash}\t{obj.jString}\t{obj.eString}");
            txtOutput.Close();

            if (!Directory.Exists("backup")) Directory.CreateDirectory("backup");
            File.Move(catFile, $"backup\\{Path.GetFileNameWithoutExtension(catFile)}_{DateTime.Now.ToString("yyyyMMdd_HHmm")}.txt");
        }

        // Clean pollution caused by Trados or other CAT
        static void CleanTrados(string file)
        {
            string text = File.ReadAllText(file, Encoding.UTF8);
            text = text.Replace(": ~", ":~");
            text = text.Replace("。 ", "。");
            text = text.Replace("！ ", "！");
            text = text.Replace("？ ", "？");
            text = text.Replace("： ", "：");
            text = text.Replace("． ", "．");
            text = text.Replace("． ", "．");
            text = text.Replace("」 ", "」");
            text = text.Replace("「 ", "「");
            text = text.Replace("） ", "）");
            text = text.Replace("（ ", "（");
            File.WriteAllText(file, text, Encoding.UTF8);
            Console.WriteLine("Cleaned up");
        }

        // Append translations and update pointers
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
                            eString = csv.GetField("eString").
                            Replace("<TAB>", "\t"). // Replace tab
                            Replace("<CLINE>", "\r\n"). // Replace carriage return
                            Replace("<NLINE>", "\n") // Replace new line
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
            for (int i = 0; i < eStringsCount; i++) 
                offsetDict.Add((int)eStringsOffsets[i], inputArray.Length + eStringLengths.Take(i).Sum());

            if (verbose) Console.WriteLine($"Filling array of size {eStringsLength:X8}...");
            byte[] eStringsArray = new byte[eStringsLength];
            for (int i = 0, j = 0; i < stringDatabase.Count; i++)
            {
                if (stringDatabase[i].eString != "")
                {
                    // Write string to string array
                    int test = eStringLengths.Take(j).Sum();
                    if (verbose) Console.WriteLine($"String: '{stringDatabase[i].eString}', Length: {eStringLengths[j] - 1}");
                    byte[] eStringArray = Encoding.GetEncoding("shift-jis").GetBytes(stringDatabase[i].eString);
                    Array.Copy(eStringArray, 0, eStringsArray, eStringLengths.Take(j).Sum(), eStringLengths[j] - 1);
                    j++;
                }
            }

            // Replace offsets in binary file
            for (int p = 0; p < inputArray.Length; p += 4)
            {
                if (p + 4 > inputArray.Length) continue;
                int cur = BitConverter.ToInt32(inputArray, p);
                if (offsetDict.ContainsKey(cur) && p > 10000)
                {
                    int replacement = 0;
                    offsetDict.TryGetValue(cur, out replacement);
                    byte[] newPointer = BitConverter.GetBytes(replacement);
                    for (int w = 0; w < 4; w++)
                        inputArray[p + w] = newPointer[w];
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
            Pack.JPKEncode(0, outputFile, outputFile, 15);
            byte[] buffer = File.ReadAllBytes(outputFile);
            byte[] bufferMeta = File.ReadAllBytes($"{outputFile}.meta");
            buffer = Crypto.encEcd(buffer, bufferMeta);
            File.WriteAllBytes(outputFile, buffer);

            // Update list
            string updEntry = Helpers.GetUpdateEntry(outputFile);
            UpdateList(updEntry);

            // Upload to ftp
            FileUploadSFTP(buffer, $"/var/www/html/mhfo/dat/{Path.GetFileName(inputFile)}");
        }

        // dump mhfpac.bin 4416 1278872
        // dump mhfdat.bin 3072 3328538
        static void DumpAndHash(string input, int startOffset, int endOffset)
        {
            byte[] buffer = File.ReadAllBytes(input);
            MemoryStream msInput = new MemoryStream(buffer);
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
                string str = Helpers.ReadNullterminatedString(brInput, Encoding.GetEncoding("shift-jis")).
                    Replace("\t", "<TAB>"). // Replace tab
                    Replace("\r\n", "<CLINE>"). // Replace carriage return
                    Replace("\n", "<NLINE>"); // Replace new line
                txtOutput.WriteLine($"{off}\t{Helpers.GetCrc32(Encoding.GetEncoding("shift-jis").GetBytes(str))}\t{str}\t");
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
                Console.Write($"\rUpdating entry {i+1}/{stringDbOld.Count}");
                if (stringDbOld[i].eString != "")
                {
                    var matchedNewObjs = stringDbNew.Where(x => x.Hash.Equals(stringDbOld[i].Hash));
                    if (matchedNewObjs.Count() > 0)
                    {
                        foreach (var obj in matchedNewObjs) obj.eString = stringDbOld[i].eString;
                    }
                }
            }
            Console.WriteLine();

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