using System;
using Server.Items;

namespace Server.Mobiles
{
    public class Savage : BaseCreature
    {
        [Constructible]
        public Savage() : base(AIType.AI_Melee)
        {
            Name = NameList.RandomName("savage");

            if (Female = Utility.RandomBool())
            {
                Body = 184;
            }
            else
            {
                Body = 183;
            }

            SetStr(96, 115);
            SetDex(86, 105);
            SetInt(51, 65);

            SetDamage(23, 27);

            SetDamageType(ResistanceType.Physical, 100);

            SetSkill(SkillName.Fencing, 60.0, 82.5);
            SetSkill(SkillName.Macing, 60.0, 82.5);
            SetSkill(SkillName.Poisoning, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 57.5, 80.0);
            SetSkill(SkillName.Swords, 60.0, 82.5);
            SetSkill(SkillName.Tactics, 60.0, 82.5);

            Fame = 1000;
            Karma = -1000;

            PackItem(new Bandage(Utility.RandomMinMax(1, 15)));

            if (Female && Utility.RandomDouble() < 0.1)
            {
                PackItem(new TribalBerry());
            }
            else if (!Female && Utility.RandomDouble() < 0.1)
            {
                PackItem(new BolaBall());
            }

            AddItem(new Spear());
            AddItem(new BoneArms());
            AddItem(new BoneLegs());

            if (Utility.RandomBool())
            {
                AddItem(new SavageMask());
            }
            else if (Utility.RandomDouble() < 0.1)
            {
                AddItem(new OrcishKinMask());
            }
        }

        public Savage(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a savage corpse";

        public override int Meat => 1;
        public override bool AlwaysMurderer => true;
        public override bool ShowFameTitle => false;

        public override OppositionGroup OppositionGroup => OppositionGroup.SavagesAndOrcs;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
        }

        public override bool IsEnemy(Mobile m)
        {
            if (m.BodyMod == 183 || m.BodyMod == 184)
            {
                return false;
            }

            return base.IsEnemy(m);
        }

        public override void AggressiveAction(Mobile aggressor, bool criminal)
        {
            base.AggressiveAction(aggressor, criminal);

            if (aggressor.BodyMod == 183 || aggressor.BodyMod == 184)
            {
                AOS.Damage(aggressor, 50, 0, 100, 0, 0, 0);
                aggressor.BodyMod = 0;
                aggressor.HueMod = -1;
                aggressor.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
                aggressor.PlaySound(0x307);
                aggressor.SendLocalizedMessage(1040008); // Your skin is scorched as the tribal paint burns away!

                if (aggressor is PlayerMobile mobile)
                {
                    mobile.SavagePaintExpiration = TimeSpan.Zero;
                }
            }
        }

        public override void AlterMeleeDamageTo(Mobile to, ref int damage)
        {
            if (to is Dragon or WhiteWyrm or SwampDragon or Drake or Nightmare or Hiryu or LesserHiryu or Daemon)
            {
                damage *= 3;
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
