using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class BlackSolenInfiltratorWarrior : BaseCreature
    {
        [Constructible]
        public BlackSolenInfiltratorWarrior() : base(AIType.AI_Melee)
        {
            Body = 806;
            BaseSoundID = 959;
            Hue = 0x453;

            SetStr(206, 230);
            SetDex(121, 145);
            SetInt(66, 90);

            SetHits(96, 107);

            SetDamage(5, 15);

            SetDamageType(ResistanceType.Physical, 80);
            SetDamageType(ResistanceType.Poison, 20);

            SetResistance(ResistanceType.Physical, 20, 35);
            SetResistance(ResistanceType.Fire, 20, 35);
            SetResistance(ResistanceType.Cold, 10, 25);
            SetResistance(ResistanceType.Poison, 20, 35);
            SetResistance(ResistanceType.Energy, 10, 25);

            SetSkill(SkillName.MagicResist, 80.0);
            SetSkill(SkillName.Tactics, 80.0);
            SetSkill(SkillName.Wrestling, 80.0);

            Fame = 3000;
            Karma = -3000;

            VirtualArmor = 40;

            SolenHelper.PackPicnicBasket(this);

            PackItem(new ZoogiFungus(Utility.RandomDouble() < 0.05 ? 13 : 3));
        }

        public override string CorpseName => "a solen infiltrator corpse";
        public override string DefaultName => "a black solen infiltrator";

        public override int GetAngerSound() => 0xB5;

        public override int GetIdleSound() => 0xB5;

        public override int GetAttackSound() => 0x289;

        public override int GetHurtSound() => 0xBC;

        public override int GetDeathSound() => 0xE4;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average, 2);
            AddLoot(LootPack.Gems, Utility.RandomMinMax(1, 4));
        }

        public override bool IsEnemy(Mobile m)
        {
            if (SolenHelper.CheckBlackFriendship(m))
            {
                return false;
            }

            return base.IsEnemy(m);
        }

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            SolenHelper.OnBlackDamage(from);

            base.OnDamage(amount, from, willKill);
        }
    }
}
