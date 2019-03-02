using CsvHelper;
using LibReFrontier;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FrontierDataTool
{
    class Program
    {
        // Define offset pointers
        // --- mhfdat.bin ---
        // Strings
        static int soStringHead = 0x64; static int soStringBody = 0x68; static int soStringArm = 0x6C; static int soStringWaist = 0x70; static int soStringLeg = 0x74;
        static int eoStringHead = 0x60; static int eoStringBody = 0x64; static int eoStringArm = 0x68; static int eoStringWaist = 0x6C; static int eoStringLeg = 0x70;
        static int soStringRanged = 0x84; static int soStringMelee = 0x88;
        static int eoStringRanged = 0x88; static int eoStringMelee = 0x174;

        // Armor
        static int soHead = 0x50; static int soBody = 0x54; static int soArm = 0x58; static int soWaist = 0x5C; static int soLeg = 0x60;
        static int eoHead = 0xE8; static int eoBody = 0x50; static int eoArm = 0x54; static int eoWaist = 0x58; static int eoLeg = 0x5C;

        // Weapons
        static int soRanged = 0x80; static int soMelee = 0x7C;
        static int eoRanged = 0x7C; static int eoMelee = 0x90;

        // --- mhfpac.bin ---
        // Strings
        static int soStringSkillPt = 0xA20;
        static int eoStringSkillPt = 0xA1C;

        public static List<KeyValuePair<int, int>> dataPointersArmor = new List<KeyValuePair<int, int>>()
        {
            new KeyValuePair<int, int>(soHead, eoHead),
            new KeyValuePair<int, int>(soBody, eoBody),
            new KeyValuePair<int, int>(soArm, eoArm),
            new KeyValuePair<int, int>(soWaist, eoWaist),
            new KeyValuePair<int, int>(soLeg, eoLeg)
        };

        public static List<KeyValuePair<int, int>> stringPointersArmor = new List<KeyValuePair<int, int>>()
        {
            new KeyValuePair<int, int>(soStringHead, eoStringHead),
            new KeyValuePair<int, int>(soStringBody, eoStringBody),
            new KeyValuePair<int, int>(soStringArm, eoStringArm),
            new KeyValuePair<int, int>(soStringWaist, eoStringWaist),
            new KeyValuePair<int, int>(soStringLeg, eoStringLeg)
        };

        public static string[] elementIds = new string[] { "なし", "火", "水", "雷", "龍", "氷", "炎", "光", "雷極", "天翔", "熾凍", "黒焔", "奏", "闇", "紅魔", "風", "響", "灼零", "皇鳴" };
        public static string[] ailmentIds = new string[] { "なし", "毒", "麻痺", "睡眠", "爆破" };
        public static string[] wClassIds = new string[] { "大剣", "ヘビィボウガン", "ハンマー", "ランス", "片手剣", "ライトボウガン", "双剣", "太刀", "狩猟笛", "ガンランス", "弓", "穿龍棍", "スラッシュアックスＦ", "マグネットスパイク" };
        public static string[] aClassIds = new string[] { "頭", "胴", "腕", "腰", "脚" };
        public enum eqType { 通常 = 0, ＳＰ = 1, 剛種 = 2, 進化 = 4, ＨＣ = 8 };

        static void Main(string[] args)
        {
            if (args.Length < 2) { Console.WriteLine("Too few arguments."); return; }

            if (args[0] == "dump") DumpData(args[1], args[2]);      // mhfpac.bin mhfdat.bin
            if (args[0] == "modshop") ModShop(args[1]);             // mhfdat.bin
            Console.WriteLine("Done"); Console.Read();
        }

        // Dump weapon and armor data
        static void DumpData(string mhfpac, string mhfdat)
        {
            // Get skill name dictionary
            MemoryStream msInput = new MemoryStream(File.ReadAllBytes(mhfpac));
            BinaryReader brInput = new BinaryReader(msInput);
            brInput.BaseStream.Seek(soStringSkillPt, SeekOrigin.Begin); int sOffset = brInput.ReadInt32();
            brInput.BaseStream.Seek(eoStringSkillPt, SeekOrigin.Begin); int eOffset = brInput.ReadInt32();

            brInput.BaseStream.Seek(sOffset, SeekOrigin.Begin);
            List<KeyValuePair<int, string>> skillId = new List<KeyValuePair<int, string>>();
            int id = 0;
            while (brInput.BaseStream.Position < eOffset)
            {
                string name = StringFromPointer(brInput);
                skillId.Add(new KeyValuePair<int, string>(id, name));
                id++;
            }

            // Dump equip data
            msInput = new MemoryStream(File.ReadAllBytes(mhfdat));
            brInput = new BinaryReader(msInput);

            // Dump armor data
            int totalCount = 0;
            for (int i = 0; i < 5; i++)
            {
                // Get raw data
                brInput.BaseStream.Seek(dataPointersArmor[i].Key, SeekOrigin.Begin); sOffset = brInput.ReadInt32();
                brInput.BaseStream.Seek(dataPointersArmor[i].Value, SeekOrigin.Begin); eOffset = brInput.ReadInt32();
                int entryCount = (eOffset - sOffset) / 0x48;
                totalCount += entryCount;
            }
            Console.WriteLine($"Total armor count: {totalCount}");

            Structs.ArmorDataEntry[] armorEntries = new Structs.ArmorDataEntry[totalCount];
            int currentCount = 0;
            for (int i = 0; i < 5; i++)
            {
                // Get raw data
                brInput.BaseStream.Seek(dataPointersArmor[i].Key, SeekOrigin.Begin); sOffset = brInput.ReadInt32();
                brInput.BaseStream.Seek(dataPointersArmor[i].Value, SeekOrigin.Begin); eOffset = brInput.ReadInt32();
                int entryCount = (eOffset - sOffset) / 0x48;
                brInput.BaseStream.Seek(sOffset, SeekOrigin.Begin);
                Console.WriteLine($"{aClassIds[i]} count: {entryCount}");

                for (int j = 0; j < entryCount; j++)
                {
                    Structs.ArmorDataEntry entry = new Structs.ArmorDataEntry();
                    entry.equipClass = aClassIds[i];
                    entry.modelIdMale = brInput.ReadInt16();
                    entry.modelIdFemale = brInput.ReadInt16();
                    byte bitfield = brInput.ReadByte();
                    entry.isMaleEquip = (bitfield & (1 << 1 - 1)) != 0;
                    entry.isFemaleEquip = (bitfield & (1 << 2 - 1)) != 0;
                    entry.isBladeEquip = (bitfield & (1 << 3 - 1)) != 0;
                    entry.isGunnerEquip = (bitfield & (1 << 4 - 1)) != 0;
                    entry.bool1 = (bitfield & (1 << 5 - 1)) != 0;
                    entry.isSPEquip = (bitfield & (1 << 6 - 1)) != 0;
                    entry.bool3 = (bitfield & (1 << 7 - 1)) != 0;
                    entry.bool4 = (bitfield & (1 << 8 - 1)) != 0;
                    entry.rarity = brInput.ReadByte();
                    entry.maxLevel = brInput.ReadByte();
                    entry.unk1_1 = brInput.ReadByte();
                    entry.unk1_2 = brInput.ReadByte();
                    entry.unk1_3 = brInput.ReadByte();
                    entry.unk1_4 = brInput.ReadByte();
                    entry.unk2 = brInput.ReadByte();
                    entry.zennyCost = brInput.ReadInt32();
                    entry.unk3 = brInput.ReadInt16();
                    entry.baseDefense = brInput.ReadInt16();
                    entry.fireRes = brInput.ReadSByte();
                    entry.waterRes = brInput.ReadSByte();
                    entry.thunderRes = brInput.ReadSByte();
                    entry.dragonRes = brInput.ReadSByte();
                    entry.iceRes = brInput.ReadSByte();
                    entry.unk3_1 = brInput.ReadInt16();
                    entry.baseSlots = brInput.ReadByte();
                    entry.maxSlots = brInput.ReadByte();
                    entry.sthEventCrown = brInput.ReadByte();
                    entry.unk5 = brInput.ReadByte();
                    entry.unk6 = brInput.ReadByte();
                    entry.unk7_1 = brInput.ReadByte();
                    entry.unk7_2 = brInput.ReadByte();
                    entry.unk7_3 = brInput.ReadByte();
                    entry.unk7_4 = brInput.ReadByte();
                    entry.unk8_1 = brInput.ReadByte();
                    entry.unk8_2 = brInput.ReadByte();
                    entry.unk8_3 = brInput.ReadByte();
                    entry.unk8_4 = brInput.ReadByte();
                    entry.unk10 = brInput.ReadInt16();
                    entry.skillId1 = skillId[brInput.ReadByte()].Value;
                    entry.skillPts1 = brInput.ReadSByte();
                    entry.skillId2 = skillId[brInput.ReadByte()].Value;
                    entry.skillPts2 = brInput.ReadSByte();
                    entry.skillId3 = skillId[brInput.ReadByte()].Value;
                    entry.skillPts3 = brInput.ReadSByte();
                    entry.skillId4 = skillId[brInput.ReadByte()].Value;
                    entry.skillPts4 = brInput.ReadSByte();
                    entry.skillId5 = skillId[brInput.ReadByte()].Value;
                    entry.skillPts5 = brInput.ReadSByte();
                    entry.sthHiden = brInput.ReadInt32();
                    entry.unk12 = brInput.ReadInt32();
                    entry.unk13 = brInput.ReadByte();
                    entry.unk14 = brInput.ReadByte();
                    entry.unk15 = brInput.ReadByte();
                    entry.unk16 = brInput.ReadByte();
                    entry.unk17 = brInput.ReadInt32();
                    entry.unk18 = brInput.ReadInt16();
                    entry.unk19 = brInput.ReadInt16();

                    armorEntries[j + currentCount] = entry;
                }

                // Get strings
                brInput.BaseStream.Seek(stringPointersArmor[i].Key, SeekOrigin.Begin); sOffset = brInput.ReadInt32();
                brInput.BaseStream.Seek(sOffset, SeekOrigin.Begin);
                for (int j = 0; j < entryCount - 1; j++)
                {
                    string name = StringFromPointer(brInput);
                    armorEntries[j + currentCount].name = name;
                }
                currentCount += entryCount;
            }

            // Write armor csv
            using (var textWriter = new StreamWriter($"Armor.csv", false, Encoding.GetEncoding("shift-jis")))
            {
                var writer = new CsvWriter(textWriter);
                writer.Configuration.Delimiter = "\t";
                writer.WriteRecords(armorEntries);
            }

            // Dump melee weapon data
            brInput.BaseStream.Seek(soMelee, SeekOrigin.Begin); sOffset = brInput.ReadInt32();
            brInput.BaseStream.Seek(eoMelee, SeekOrigin.Begin); eOffset = brInput.ReadInt32();
            int entryCountMelee = (eOffset - sOffset) / 0x34;
            brInput.BaseStream.Seek(sOffset, SeekOrigin.Begin);
            Console.WriteLine($"Melee count: {entryCountMelee}");

            Structs.MeleeWeaponEntry[] meleeEntries = new Structs.MeleeWeaponEntry[entryCountMelee];
            for (int i = 0; i < entryCountMelee; i++)
            {
                Structs.MeleeWeaponEntry entry = new Structs.MeleeWeaponEntry();
                entry.modelId = brInput.ReadInt16();
                entry.modelIdData = GetModelIdData(entry.modelId);
                entry.rarity = brInput.ReadByte();
                entry.classId = wClassIds[brInput.ReadByte()];
                entry.zennyCost = brInput.ReadInt32();
                entry.sharpnessId = brInput.ReadInt16();
                entry.rawDamage = brInput.ReadInt16();
                entry.defense = brInput.ReadInt16();
                entry.affinity = brInput.ReadSByte();
                entry.elementId = elementIds[brInput.ReadByte()];
                entry.eleDamage = brInput.ReadByte() * 10;
                entry.ailmentId = ailmentIds[brInput.ReadByte()];
                entry.ailDamage = brInput.ReadByte() * 10;
                entry.slots = brInput.ReadByte();
                entry.unk3 = brInput.ReadByte();
                entry.unk4 = brInput.ReadByte();
                entry.unk5 = brInput.ReadInt16();
                entry.unk6 = brInput.ReadInt16();
                entry.unk7 = brInput.ReadInt16();
                entry.unk8 = brInput.ReadInt32();
                entry.unk9 = brInput.ReadInt32();
                entry.unk10 = brInput.ReadInt16();
                entry.unk11 = brInput.ReadInt16();
                entry.unk12 = brInput.ReadByte();
                entry.unk13 = brInput.ReadByte();
                entry.unk14 = brInput.ReadByte();
                entry.unk15 = brInput.ReadByte();
                entry.unk16 = brInput.ReadInt32();
                entry.unk17 = brInput.ReadInt32();

                meleeEntries[i] = entry;
            }

            // Get strings
            brInput.BaseStream.Seek(soStringMelee, SeekOrigin.Begin); sOffset = brInput.ReadInt32();
            brInput.BaseStream.Seek(sOffset, SeekOrigin.Begin);
            for (int j = 0; j < entryCountMelee - 1; j++)
            {
                string name = StringFromPointer(brInput);
                meleeEntries[j].name = name;
            }

            // Write csv
            using (var textWriter = new StreamWriter("Melee.csv", false, Encoding.GetEncoding("shift-jis")))
            {
                var writer = new CsvWriter(textWriter);
                writer.Configuration.Delimiter = "\t";
                writer.WriteRecords(meleeEntries);
            }

            // Dump ranged weapon data
            brInput.BaseStream.Seek(soRanged, SeekOrigin.Begin); sOffset = brInput.ReadInt32();
            brInput.BaseStream.Seek(eoRanged, SeekOrigin.Begin); eOffset = brInput.ReadInt32();
            int entryCountRanged = (eOffset - sOffset) / 0x3C;
            brInput.BaseStream.Seek(sOffset, SeekOrigin.Begin);
            Console.WriteLine($"Ranged count: {entryCountRanged}");

            Structs.RangedWeaponEntry[] rangedEntries = new Structs.RangedWeaponEntry[entryCountRanged];
            for (int i = 0; i < entryCountRanged; i++)
            {
                Structs.RangedWeaponEntry entry = new Structs.RangedWeaponEntry();
                entry.modelId = brInput.ReadInt16();
                entry.modelIdData = GetModelIdData(entry.modelId);
                entry.rarity = brInput.ReadByte();
                entry.maxSlotsMaybe = brInput.ReadByte();
                entry.classId = wClassIds[brInput.ReadByte()];
                entry.unk2_1 = brInput.ReadByte();
                entry.eqType = brInput.ReadByte().ToString(); //Enum.GetName(typeof(eqType), brInput.ReadByte());
                entry.unk2_3 = brInput.ReadByte();
                entry.unk3_1 = brInput.ReadByte();
                entry.unk3_2 = brInput.ReadByte();
                entry.unk3_3 = brInput.ReadByte();
                entry.unk3_4 = brInput.ReadByte();
                entry.unk4_1 = brInput.ReadByte();
                entry.unk4_2 = brInput.ReadByte();
                entry.unk4_3 = brInput.ReadByte();
                entry.unk4_4 = brInput.ReadByte();
                entry.unk5_1 = brInput.ReadByte();
                entry.unk5_2 = brInput.ReadByte();
                entry.unk5_3 = brInput.ReadByte();
                entry.unk5_4 = brInput.ReadByte();
                entry.zennyCost = brInput.ReadInt32();
                entry.rawDamage = brInput.ReadInt16();
                entry.defense = brInput.ReadInt16();
                entry.recoilMaybe = brInput.ReadByte();
                entry.slots = brInput.ReadByte();
                entry.affinity = brInput.ReadSByte();
                entry.sortOrderMaybe = brInput.ReadByte();
                entry.unk6_1 = brInput.ReadByte();
                entry.elementId = elementIds[brInput.ReadByte()];
                entry.eleDamage = brInput.ReadByte() * 10;
                entry.unk6_4 = brInput.ReadByte();
                entry.unk7_1 = brInput.ReadByte();
                entry.unk7_2 = brInput.ReadByte();
                entry.unk7_3 = brInput.ReadByte();
                entry.unk7_4 = brInput.ReadByte();
                entry.unk8_1 = brInput.ReadByte();
                entry.unk8_2 = brInput.ReadByte();
                entry.unk8_3 = brInput.ReadByte();
                entry.unk8_4 = brInput.ReadByte();
                entry.unk9_1 = brInput.ReadByte();
                entry.unk9_2 = brInput.ReadByte();
                entry.unk9_3 = brInput.ReadByte();
                entry.unk9_4 = brInput.ReadByte();
                entry.unk10_1 = brInput.ReadByte();
                entry.unk10_2 = brInput.ReadByte();
                entry.unk10_3 = brInput.ReadByte();
                entry.unk10_4 = brInput.ReadByte();
                entry.unk11_1 = brInput.ReadByte();
                entry.unk11_2 = brInput.ReadByte();
                entry.unk11_3 = brInput.ReadByte();
                entry.unk11_4 = brInput.ReadByte();
                entry.unk12_1 = brInput.ReadByte();
                entry.unk12_2 = brInput.ReadByte();
                entry.unk12_3 = brInput.ReadByte();
                entry.unk12_4 = brInput.ReadByte();

                rangedEntries[i] = entry;
            }

            // Get strings
            brInput.BaseStream.Seek(soStringRanged, SeekOrigin.Begin); sOffset = brInput.ReadInt32();
            brInput.BaseStream.Seek(sOffset, SeekOrigin.Begin);
            for (int j = 0; j < entryCountRanged - 1; j++)
            {
                string name = StringFromPointer(brInput);
                rangedEntries[j].name = name;
            }

            // Write csv
            using (var textWriter = new StreamWriter("Ranged.csv", false, Encoding.GetEncoding("shift-jis")))
            {
                var writer = new CsvWriter(textWriter);
                writer.Configuration.Delimiter = "\t";
                writer.WriteRecords(rangedEntries);
            }
        }

        // Add all-items shop to file, change item prices, change armor prices
        static void ModShop(string file)
        {
            MemoryStream msInput = new MemoryStream(File.ReadAllBytes(file));
            BinaryReader brInput = new BinaryReader(msInput);
            BinaryWriter brOutput = new BinaryWriter(File.Open(file, FileMode.Open));

            // Patch item prices
            brInput.BaseStream.Seek(0xFC, SeekOrigin.Begin); int sOffset = brInput.ReadInt32();
            brInput.BaseStream.Seek(0xA70, SeekOrigin.Begin); int eOffset = brInput.ReadInt32();
            int count = (eOffset - sOffset) / 0x24;
            Console.WriteLine($"Patching prices for {count} items starting at 0x{sOffset.ToString("X8")}");
            for (int i = 0; i < count; i++)
            {
                brOutput.BaseStream.Seek(sOffset + (i * 0x24) + 12, SeekOrigin.Begin);
                brInput.BaseStream.Seek(sOffset + (i * 0x24) + 12, SeekOrigin.Begin);
                Int32 buyPrice = brInput.ReadInt32() / 10;
                brOutput.Write(buyPrice);

                brOutput.BaseStream.Seek(sOffset + (i * 0x24) + 16, SeekOrigin.Begin);
                brInput.BaseStream.Seek(sOffset + (i * 0x24) + 16, SeekOrigin.Begin);
                Int32 sellPrice = brInput.ReadInt32() * 5;
                brOutput.Write(sellPrice);
            }

            // Patch equip prices
            for (int i = 0; i < 5; i++)
            {
                brInput.BaseStream.Seek(dataPointersArmor[i].Key, SeekOrigin.Begin); sOffset = brInput.ReadInt32();
                brInput.BaseStream.Seek(dataPointersArmor[i].Value, SeekOrigin.Begin); eOffset = brInput.ReadInt32();
                count = (eOffset - sOffset) / 0x48;
                Console.WriteLine($"Patching prices for {count} armor pieces starting at 0x{sOffset.ToString("X8")}");
                for (int j = 0; j < count; j++)
                {
                    brOutput.BaseStream.Seek(sOffset + (j * 0x48) + 12, SeekOrigin.Begin);
                    brOutput.Write((Int32)50);
                }
            }

            brOutput.Close();
            brInput.Close();

            // Generate shop array
            count = 16700;
            byte[] shopArray = new byte[(count * 8) + 5 * 32];
            int blockSize = (count / 5) * 8;

            for (int i = 0; i < count; i++)
            {
                byte[] id = BitConverter.GetBytes((Int16)(i + 1));
                byte[] item = new byte[8];
                Array.Copy(id, item, 2);
                Array.Copy(item, 0, shopArray, i * 8, 8);
            }

            // Append modshop data to file          
            byte[] inputArray = File.ReadAllBytes(file);
            byte[] outputArray = new byte[inputArray.Length + shopArray.Length];
            Array.Copy(inputArray, outputArray, inputArray.Length);
            Array.Copy(shopArray, 0, outputArray, inputArray.Length, shopArray.Length);

            // Find and modify item shop data pointer
            byte[] needle = new byte[] { 0x0F, 01, 01, 00, 00, 00, 00, 00, 03, 01, 01, 00, 00, 00, 00, 00 };
            int offsetData = Helpers.GetOffsetOfArray(outputArray, needle);
            if (offsetData != -1)
            {
                Console.WriteLine($"Found shop inventory to modify at 0x{offsetData.ToString("X8")}.");
                byte[] offsetArray = BitConverter.GetBytes(offsetData);
                offsetArray.Reverse();
                int offsetPointer = Helpers.GetOffsetOfArray(outputArray, offsetArray);
                if (offsetPointer != -1)
                {
                    Console.WriteLine($"Found pointer at 0x{offsetPointer.ToString("X8")}.");
                    byte[] patchedPointer = BitConverter.GetBytes(inputArray.Length);
                    patchedPointer.Reverse();
                    Array.Copy(patchedPointer, 0, outputArray, offsetPointer, patchedPointer.Length);

                    File.WriteAllBytes(file, outputArray);
                }
                else Console.WriteLine("Could not find pointer, please check manually and correct code.");
            }
            else Console.WriteLine("Could not find needle, please check manually and correct code.");
        }

        static string StringFromPointer(BinaryReader brInput)
        {
            int off = brInput.ReadInt32();
            long pos = brInput.BaseStream.Position;
            brInput.BaseStream.Seek(off, SeekOrigin.Begin);
            string str = Helpers.ReadNullterminatedString(brInput, Encoding.GetEncoding("shift-jis"));
            brInput.BaseStream.Seek(pos, SeekOrigin.Begin);
            return str;
        }

        static string GetModelIdData(int id)
        {
            string str = "";
            if (id >= 0 && id < 1000) str = $"we{id.ToString("D3")}";
            else if (id < 2000) str = $"wf{(id - 1000).ToString("D3")}";
            else if (id < 3000) str = $"wg{(id - 2000).ToString("D3")}";
            else if (id < 4000) str = $"wh{(id - 3000).ToString("D3")}";
            else if (id < 5000) str = $"wi{(id - 4000).ToString("D3")}";
            else if (id < 7000) str = $"wk{(id - 6000).ToString("D3")}";
            else if (id < 8000) str = $"wl{(id - 7000).ToString("D3")}";
            else if (id < 9000) str = $"wm{(id - 8000).ToString("D3")}";
            else if (id < 10000) str = $"wg{(id - 9000).ToString("D3")}";
            else str = "Unmapped";
            return str;
        }
    }
}
