using CsvHelper;
using Force.Crc32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FrontierTextTool
{
    class Program
    {
        static bool verbose = false;
        static bool autoClose = false;

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length < 2) { Console.WriteLine("Too few arguments."); return; }

            if (args.Any("-verbose".Contains)) verbose = true;
            if (args.Any("-close)".Contains)) autoClose = true;

            if (args[0] == "dump") DumpAndHash(args[1], Convert.ToInt32(args[2]), Convert.ToInt32(args[3]));
            if (args[0] == "insert") InsertStrings(args[1], args[2]);
            Console.WriteLine("Done");
            if (!autoClose) Console.Read();
        }

        public class StringDatabase
        {
            public UInt32 offset { get; set; }
            public UInt32 hash { get; set; }
            public string eString { get; set; }
        }

        // insert src\mhfpac.bin csv\mhfpac_01.csv
        static void InsertStrings(string inputFile, string inputCsv)
        {
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
                            offset = csv.GetField<UInt32>("Offset"),
                            hash = csv.GetField<UInt32>("Hash"),
                            eString = csv.GetField("eString")
                        };
                        stringDatabase.Add(record);
                    }
                }
            }

            // Create byte array with translated strings
            int eStringsLength = 0;
            foreach (var obj in stringDatabase)
            {
                if (obj.eString != "") eStringsLength += obj.eString.Length + 1;
            }

            Console.WriteLine($"Filling array of size {eStringsLength.ToString("X8")}...");
            byte[] eStringsArray = new byte[eStringsLength];
            int pos = 0;
            foreach (var obj in stringDatabase)
            {
                if (obj.eString != "")
                {
                    // Write string to string array
                    if (verbose) Console.WriteLine($"String: '{obj.eString}', Length: {obj.eString.Length}");
                    byte[] eStringArray = Encoding.GetEncoding("shift-jis").GetBytes(obj.eString);
                    Array.Copy(eStringArray, 0, eStringsArray, pos, obj.eString.Length);

                    // Replace offsets in binary file
                    byte[] pointerToReplace = BitConverter.GetBytes((Int32)obj.offset);
                    byte[] newPointer = BitConverter.GetBytes((Int32)(inputArray.Length + pos));
                    int pointerCount = 0;
                    for (int p = 0; p < inputArray.Length; p++)
                    {
                        if (!DetectPatch(inputArray, p, pointerToReplace)) continue;

                        // Skip if within first 10kb to preserve index just in case
                        if (p > 1000)
                        {
                            if (verbose) Console.WriteLine($"Remapping pointer at 0x{p.ToString("X8")}");
                            pointerCount++;
                            for (int w = 0; w < pointerToReplace.Length; w++)
                            {
                                inputArray[p + w] = newPointer[w];
                            }
                        }
                    }
                    pos += obj.eString.Length + 1;
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
        }

        // dump mhfpac.bin 4416 1289334
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

        // Find and replace binary
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DetectPatch(byte[] sequence, int position, byte[] PatchFind)
        {
            if (position + PatchFind.Length > sequence.Length) return false;
            for (int p = 0; p < PatchFind.Length; p++)
            {
                if (PatchFind[p] != sequence[position + p]) return false;
            }
            return true;
        }
    }
}
