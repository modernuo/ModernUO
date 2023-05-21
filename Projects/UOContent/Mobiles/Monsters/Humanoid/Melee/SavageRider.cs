using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class SavageRider : BaseCreature
    {
        [Constructible]
        public SavageRider() : base(AIType.AI_Melee)
        {
            Name = NameList.RandomName("savage rider");

            if (Female = Utility.RandomBool())
            {
                Body = 186;
            }
            else
            {
                Body = 185;
            }

            SetStr(151, 170);
            SetDex(92, 130);
            SetInt(51, 65);

            SetDamage(29, 34);

            SetDamageType(ResistanceType.Physical, 100);

            SetSkill(SkillName.Fencing, 72.5, 95.0);
            SetSkill(SkillName.Healing, 60.3, 90.0);
            SetSkill(SkillName.Macing, 72.5, 95.0);
            SetSkill(SkillName.Poisoning, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 72.5, 95.0);
            SetSkill(SkillName.Swords, 72.5, 95.0);
            SetSkill(SkillName.Tactics, 72.5, 95.0);

            Fame = 1000;
            Karma = -1000;

            PackItem(new Bandage(Utility.RandomMinMax(1, 15)));

            if (Utility.RandomDouble() < 0.1)
            {
                PackItem(new BolaBall());
            }

            AddItem(new TribalSpear());
            AddItem(new BoneArms());
            AddItem(new BoneLegs());
            // TODO: BEAR MASK

            new SavageRidgeback().Rider = this;
        }

        public override string CorpseName => "a savage corpse";

        public override int Meat => 1;
        public override bool AlwaysMurderer => true;
        public override bool ShowFameTitle => false;

        public override OppositionGroup OppositionGroup => OppositionGroup.SavagesAndOrcs;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
        }

        public override bool OnBeforeDeath()
        {
            var mount = Mount;

            if (mount != null)
            {
                mount.Rider = null;
            }

            if (mount is Mobile mobile)
            {
                mobile.Delete();
            }

            return base.OnBeforeDeath();
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
    }
}
