using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Mobiles;
using Server.Items;

namespace Server.Mobiles
{
    public class BehaviorMage : BaseCreature
    {
        [Constructible]
        public BehaviorMage() : base(AIType.AI_BehaviorTree, FightMode.Evil, 2, 0, 0.01, 1)
        {
            // Debug = true;

            InitBody();

            SetStr(100, 100);
            SetDex(25, 25);
            SetInt(100, 100);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 40, 50);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.EvalInt, 100.0);
            SetSkill(SkillName.Magery, 100.0);
            SetSkill(SkillName.MagicResist, 100.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Meditation, 100.0);
            SetSkill(SkillName.Wrestling, 100.0);

            AddItem(new Backpack() { Movable = false });

            if (Utility.RandomBool())
            {
                SetSkill(SkillName.Swords, 100.0);
                RangeFight = 1;
                AddToBackpack(new Halberd() { Quality = WeaponQuality.Exceptional, Crafter = this });
                AddToBackpack(new Katana() { Quality = WeaponQuality.Exceptional, Crafter = this });
            }
            else
            {
                SetSkill(SkillName.Archery, 100.0);
                RangeFight = 6;
                AddToBackpack(new HeavyCrossbow() { Quality = WeaponQuality.Exceptional, Crafter = this });
                AddToBackpack(new Bow() { Quality = WeaponQuality.Exceptional, Crafter = this });
                AddToBackpack(new Bolt() { Amount = 300 });
                AddToBackpack(new Arrow() { Amount = 300 });
            }

            ActiveSpeed = 0.01;
            PassiveSpeed = 1;
        }

        public BehaviorMage(Serial serial) : base(serial)
        {
        }
        public override bool CanDestroyObstacles => true;
        public virtual bool GetGender() => Utility.RandomBool();
        public virtual void InitBody()
        {
            InitStats(100, 100, 25);

            SpeechHue = Utility.RandomDyedHue();
            Hue = Race.Human.RandomSkinHue();

            if (Female = GetGender())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
            }
        }
        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
        }
        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
        }
    }
}
