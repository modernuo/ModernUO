using System;
using Server.Items;
using Server.Items.Holiday;

namespace Server.Mobiles
{
    [Serializable(0, false)]
    public partial class PumpkinHead : BaseCreature
    {
        [Constructible]
        public PumpkinHead()
            : base(Utility.RandomBool() ? AIType.AI_Melee : AIType.AI_Mage)
        {
            Body = 1246 + Utility.Random(2);

            BaseSoundID = 268;

            SetStr(350);
            SetDex(125);
            SetInt(250);

            SetHits(500);
            SetMana(1000);

            SetDamage(10, 15);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 55);
            SetResistance(ResistanceType.Fire, 50);
            SetResistance(ResistanceType.Cold, 50);
            SetResistance(ResistanceType.Poison, 65);
            SetResistance(ResistanceType.Energy, 80);

            SetSkill(SkillName.DetectHidden, 100.0);
            SetSkill(SkillName.Meditation, 120.0);
            SetSkill(SkillName.Necromancy, 100.0);
            SetSkill(SkillName.SpiritSpeak, 120.0);
            SetSkill(SkillName.Magery, 160.0);
            SetSkill(SkillName.EvalInt, 100.0);
            SetSkill(SkillName.MagicResist, 100.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 80.0);

            Fame = 5000;
            Karma = -5000;

            VirtualArmor = 49;
        }

        public override string CorpseName => "a killer pumpkin corpse";
        public override bool AutoDispel => true;
        public override bool BardImmune => true;
        public override bool Unprovokable => true;
        public override bool AreaPeaceImmune => true;
        public override string DefaultName => "a killer pumpkin";

        public override void GenerateLoot()
        {
            if (Utility.RandomDouble() < .05)
            {
                switch (Utility.Random(5))
                {
                    case 0:
                        PackItem(new PaintedEvilClownMask());
                        break;
                    case 1:
                        PackItem(new PaintedDaemonMask());
                        break;
                    case 2:
                        PackItem(new PaintedPlagueMask());
                        break;
                    case 3:
                        PackItem(new PaintedEvilJesterMask());
                        break;
                    case 4:
                        PackItem(new PaintedPorcelainMask());
                        break;
                }
            }

            PackItem(new WrappedCandy());
            AddLoot(LootPack.UltraRich, 2);
        }

        public virtual void Lifted_Callback(Mobile from)
        {
            if (from?.Deleted == false && from is PlayerMobile)
            {
                Combatant = from;
                Warmode = true;
            }
        }

        public override Item NewHarmfulItem() =>
            new PoolOfAcid(TimeSpan.FromSeconds(10), 25, 30)
            {
                Name = "gooey nasty pumpkin hummus",
                Hue = 144
            };

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            if (Utility.RandomBool())
            {
                if (from?.Map != null && Map != Map.Internal && Map == from.Map && from.InRange(this, 12))
                {
                    SpillAcid(willKill ? this : from, willKill ? 3 : 1);
                }
            }

            base.OnDamage(amount, from, willKill);
        }
    }
}
