using Server.Ethics;
using Server.Factions;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Silverserpant")]
    public class SilverSerpent : BaseCreature
    {
        [Constructible]
        public SilverSerpent() : base(AIType.AI_Melee)
        {
            Body = 92;
            BaseSoundID = 219;

            SetStr(161, 360);
            SetDex(151, 300);
            SetInt(21, 40);

            SetHits(97, 216);

            SetDamage(5, 21);

            SetDamageType(ResistanceType.Physical, 50);
            SetDamageType(ResistanceType.Poison, 50);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 5, 10);
            SetResistance(ResistanceType.Cold, 5, 10);
            SetResistance(ResistanceType.Poison, 100);
            SetResistance(ResistanceType.Energy, 5, 10);

            SetSkill(SkillName.Poisoning, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 95.1, 100.0);
            SetSkill(SkillName.Tactics, 80.1, 95.0);
            SetSkill(SkillName.Wrestling, 85.1, 100.0);

            Fame = 7000;
            Karma = -7000;

            VirtualArmor = 40;
        }

        public SilverSerpent(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a silver serpent corpse";
        public override Faction FactionAllegiance => TrueBritannians.Instance;
        public override Ethic EthicAllegiance => Ethic.Hero;

        public override string DefaultName => "a silver serpent";

        public override bool DeathAdderCharmable => true;

        public override int Meat => 1;
        public override Poison PoisonImmune => Poison.Lethal;
        public override Poison HitPoison => Poison.Lethal;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
            AddLoot(LootPack.Gems, 2);
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

            if (BaseSoundID == -1)
            {
                BaseSoundID = 219;
            }
        }
    }
}
