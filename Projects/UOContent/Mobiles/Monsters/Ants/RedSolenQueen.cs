using ModernUO.Serialization;
using Server.Items;
using Server.Network;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class RedSolenQueen : BaseCreature
    {
        [SerializableField(0, setter: "private")]
        private bool _burstSac;

        [Constructible]
        public RedSolenQueen() : base(AIType.AI_Melee)
        {
            Body = 783;
            BaseSoundID = 959;

            SetStr(296, 320);
            SetDex(121, 145);
            SetInt(76, 100);

            SetHits(151, 162);

            SetDamage(10, 15);

            SetDamageType(ResistanceType.Physical, 70);
            SetDamageType(ResistanceType.Poison, 30);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 30, 35);
            SetResistance(ResistanceType.Cold, 25, 35);
            SetResistance(ResistanceType.Poison, 35, 40);
            SetResistance(ResistanceType.Energy, 25, 30);

            SetSkill(SkillName.MagicResist, 70.0);
            SetSkill(SkillName.Tactics, 90.0);
            SetSkill(SkillName.Wrestling, 90.0);

            Fame = 4500;
            Karma = -4500;

            VirtualArmor = 45;

            SolenHelper.PackPicnicBasket(this);

            PackItem(new ZoogiFungus(Utility.RandomDouble() < 0.95 ? 5 : 25));

            if (Utility.RandomDouble() < 0.05)
            {
                PackItem(new BallOfSummoning());
            }
        }

        public override string CorpseName => "a solen queen corpse";

        public override string DefaultName => "a red solen queen";

        public override int GetAngerSound() => 0x259;

        public override int GetIdleSound() => 0x259;

        public override int GetAttackSound() => 0x195;

        public override int GetHurtSound() => 0x250;

        public override int GetDeathSound() => 0x25B;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
        }

        public override bool IsEnemy(Mobile m)
        {
            if (SolenHelper.CheckRedFriendship(m))
            {
                return false;
            }

            return base.IsEnemy(m);
        }

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            SolenHelper.OnRedDamage(from);

            if (!willKill)
            {
                if (!BurstSac)
                {
                    if (Hits < 50)
                    {
                        PublicOverheadMessage(MessageType.Regular, 0x3B2, true, "* The solen's acid sac is burst open! *");
                        BurstSac = true;
                    }
                }
                else if (from != null && from != this && InRange(from, 1))
                {
                    SpillAcid(from, 1);
                }
            }

            base.OnDamage(amount, from, willKill);
        }

        public override bool OnBeforeDeath()
        {
            SpillAcid(4);

            return base.OnBeforeDeath();
        }
    }
}
