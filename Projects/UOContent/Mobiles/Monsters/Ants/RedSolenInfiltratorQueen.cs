using Server.Items;

namespace Server.Mobiles
{
    public class RedSolenInfiltratorQueen : BaseCreature
    {
        [Constructible]
        public RedSolenInfiltratorQueen() : base(AIType.AI_Melee)
        {
            Body = 783;
            BaseSoundID = 959;

            SetStr(326, 350);
            SetDex(141, 165);
            SetInt(96, 120);

            SetHits(151, 162);

            SetDamage(10, 15);

            SetDamageType(ResistanceType.Physical, 70);
            SetDamageType(ResistanceType.Poison, 30);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 30, 35);
            SetResistance(ResistanceType.Cold, 25, 35);
            SetResistance(ResistanceType.Poison, 35, 40);
            SetResistance(ResistanceType.Energy, 25, 30);

            SetSkill(SkillName.MagicResist, 90.0);
            SetSkill(SkillName.Tactics, 90.0);
            SetSkill(SkillName.Wrestling, 90.0);

            Fame = 6500;
            Karma = -6500;

            VirtualArmor = 50;

            SolenHelper.PackPicnicBasket(this);

            PackItem(new ZoogiFungus(Utility.RandomDouble() < 0.95 ? 4 : 16));
        }

        public RedSolenInfiltratorQueen(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a solen infiltrator corpse";
        public override string DefaultName => "a red solen infiltrator";

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

            base.OnDamage(amount, from, willKill);
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
