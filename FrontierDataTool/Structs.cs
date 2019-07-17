using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontierDataTool
{
    public class Structs
    {
        public class QuestData
        {
            public String title { get; set; }
            public String textMain { get; set; }
            public String textSubA { get; set; }
            public String textSubB { get; set; }

            public Byte unk1 { get; set; }
            public Byte unk2 { get; set; }
            public Byte unk3 { get; set; }
            public Byte unk4 { get; set; }
            public Byte level { get; set; }
            public Byte unk5 { get; set; }
            public Byte courseType { get; set; }    // 6 = Premium, 18 = Free?, 19 = HLC?, 20 = Extra
            public Byte unk7 { get; set; }
            public Byte unk8 { get; set; }
            public Byte unk9 { get; set; }
            public Byte unk10 { get; set; }
            public Byte unk11 { get; set; }
            public Int32 fee { get; set; }
            public Int32 zennyMain { get; set; }
            public Int32 zennyKo { get; set; }
            public Int32 zennySubA { get; set; }
            public Int32 zennySubB { get; set; }
            public Int32 time { get; set; }
            public Int32 unk12 { get; set; }
            public Byte unk13 { get; set; }
            public Byte unk14 { get; set; }
            public Byte unk15 { get; set; }
            public Byte unk16 { get; set; }
            public Byte unk17 { get; set; }
            public Byte unk18 { get; set; }
            public Byte unk19 { get; set; }
            public Byte unk20 { get; set; }
            public String mainGoalType { get; set; }
            public Int16 mainGoalTarget { get; set; }
            public Int16 mainGoalCount { get; set; }
            public String subAGoalType { get; set; }
            public Int16 subAGoalTarget { get; set; }
            public Int16 subAGoalCount { get; set; }
            public String subBGoalType { get; set; }
            public Int16 subBGoalTarget { get; set; }
            public Int16 subBGoalCount { get; set; }

            public Int32 mainGRP { get; set; }
            public Int32 subAGRP { get; set; }
            public Int32 subBGRP { get; set; }
        }

        public enum QuestTypes
        {
            None = 0,
            Hunt = 0x00000001,
            Capture = 0x00000101,
            Kill = 0x00000201,
            Delivery = 0x00000002,
            GuildFlag = 0x00001002,
            Damging = 0x00008004

        }

        public class ArmorDataEntry
        {
            public String equipClass { get; set; }
            public String name { get; set; }
            public Int16 modelIdMale { get; set; }
            public Int16 modelIdFemale { get; set; }
            public Boolean isMaleEquip { get; set; }
            public Boolean isFemaleEquip { get; set; }
            public Boolean isBladeEquip { get; set; }
            public Boolean isGunnerEquip { get; set; }
            public Boolean bool1 { get; set; }
            public Boolean isSPEquip { get; set; }
            public Boolean bool3 { get; set; }
            public Boolean bool4 { get; set; }
            public Byte rarity { get; set; }
            public Byte maxLevel { get; set; }
            public Byte unk1_1 { get; set; }
            public Byte unk1_2 { get; set; }
            public Byte unk1_3 { get; set; }
            public Byte unk1_4 { get; set; }
            public Byte unk2 { get; set; }
            public Int32 zennyCost { get; set; }
            public Int16 unk3 { get; set; }
            public Int16 baseDefense { get; set; }
            public SByte fireRes { get; set; }
            public SByte waterRes { get; set; }
            public SByte thunderRes { get; set; }
            public SByte dragonRes { get; set; }
            public SByte iceRes { get; set; }
            public Int16 unk3_1 { get; set; }
            public Byte baseSlots { get; set; }
            public Byte maxSlots { get; set; }
            public Byte sthEventCrown { get; set; }
            public Byte unk5 { get; set; }
            public Byte unk6 { get; set; }
            public Byte unk7_1 { get; set; }
            public Byte unk7_2 { get; set; }
            public Byte unk7_3 { get; set; }
            public Byte unk7_4 { get; set; }
            public Byte unk8_1 { get; set; }
            public Byte unk8_2 { get; set; }
            public Byte unk8_3 { get; set; }
            public Byte unk8_4 { get; set; }
            public Int16 unk10 { get; set; }
            public String skillId1 { get; set; }
            public SByte skillPts1 { get; set; }
            public String skillId2 { get; set; }
            public SByte skillPts2 { get; set; }
            public String skillId3 { get; set; }
            public SByte skillPts3 { get; set; }
            public String skillId4 { get; set; }
            public SByte skillPts4 { get; set; }
            public String skillId5 { get; set; }
            public SByte skillPts5 { get; set; }
            public Int32 sthHiden { get; set; }
            public Int32 unk12 { get; set; }
            public Byte unk13 { get; set; }
            public Byte unk14 { get; set; }
            public Byte unk15 { get; set; }
            public Byte unk16 { get; set; }
            public Int32 unk17 { get; set; }
            public Int16 unk18 { get; set; }
            public Int16 unk19 { get; set; }
        }

        public class MeleeWeaponEntry
        {
            public String name { get; set; }
            public Int16 modelId { get; set; }
            public String modelIdData { get; set; }
            public Byte rarity { get; set; }
            public String classId { get; set; }
            public Int32 zennyCost { get; set; }
            public Int16 sharpnessId { get; set; }
            public Int16 rawDamage { get; set; }
            public Int16 defense { get; set; }
            public SByte affinity { get; set; }
            public String elementId { get; set; }
            public Int32 eleDamage { get; set; }
            public String ailmentId { get; set; }
            public Int32 ailDamage { get; set; }
            public Byte slots { get; set; }
            public Byte unk3 { get; set; }
            public Byte unk4 { get; set; }
            public Int16 unk5 { get; set; }
            public Int16 unk6 { get; set; }
            public Int16 unk7 { get; set; }
            public Int32 unk8 { get; set; }
            public Int32 unk9 { get; set; }
            public Int16 unk10 { get; set; }
            public Int16 unk11 { get; set; }
            public Byte unk12 { get; set; }
            public Byte unk13 { get; set; }
            public Byte unk14 { get; set; }
            public Byte unk15 { get; set; }
            public Int32 unk16 { get; set; }
            public Int32 unk17 { get; set; }
        }

        public class RangedWeaponEntry
        {
            public String name { get; set; }
            public Int16 modelId { get; set; }
            public String modelIdData { get; set; }
            public Byte rarity { get; set; }
            public Byte maxSlotsMaybe { get; set; }
            public String classId { get; set; }
            public Byte unk2_1 { get; set; }
            public String eqType { get; set; }
            public Byte unk2_3 { get; set; }
            public Byte unk3_1 { get; set; }
            public Byte unk3_2 { get; set; }
            public Byte unk3_3 { get; set; }
            public Byte unk3_4 { get; set; }
            public Byte unk4_1 { get; set; }
            public Byte unk4_2 { get; set; }
            public Byte unk4_3 { get; set; }
            public Byte unk4_4 { get; set; }
            public Byte unk5_1 { get; set; }
            public Byte unk5_2 { get; set; }
            public Byte unk5_3 { get; set; }
            public Byte unk5_4 { get; set; }
            public Int32 zennyCost { get; set; }
            public Int16 rawDamage { get; set; }
            public Int16 defense { get; set; }
            public Byte recoilMaybe { get; set; }
            public Byte slots { get; set; }
            public SByte affinity { get; set; }
            public Byte sortOrderMaybe { get; set; }
            public Byte unk6_1 { get; set; }
            public String elementId { get; set; }
            public Int32 eleDamage { get; set; }
            public Byte unk6_4 { get; set; }
            public Byte unk7_1 { get; set; }
            public Byte unk7_2 { get; set; }
            public Byte unk7_3 { get; set; }
            public Byte unk7_4 { get; set; }
            public Byte unk8_1 { get; set; }
            public Byte unk8_2 { get; set; }
            public Byte unk8_3 { get; set; }
            public Byte unk8_4 { get; set; }
            public Byte unk9_1 { get; set; }
            public Byte unk9_2 { get; set; }
            public Byte unk9_3 { get; set; }
            public Byte unk9_4 { get; set; }
            public Byte unk10_1 { get; set; }
            public Byte unk10_2 { get; set; }
            public Byte unk10_3 { get; set; }
            public Byte unk10_4 { get; set; }
            public Byte unk11_1 { get; set; }
            public Byte unk11_2 { get; set; }
            public Byte unk11_3 { get; set; }
            public Byte unk11_4 { get; set; }
            public Byte unk12_1 { get; set; }
            public Byte unk12_2 { get; set; }
            public Byte unk12_3 { get; set; }
            public Byte unk12_4 { get; set; }
        }
    }
}
