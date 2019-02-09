using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontierDataTool
{
    public class Structs
    {
        public class ArmorDataEntry
        {
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
    }
}
