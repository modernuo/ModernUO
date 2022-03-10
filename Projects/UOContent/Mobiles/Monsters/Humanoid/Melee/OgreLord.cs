using Server.Ethics;
using Server.Factions;
using Server.Items;

namespace Server.Mobiles
{
    public class OgreLord : BaseCreature
    {
        [Constructible]
        public OgreLord() : base(AIType.AI_Melee)
        {
            Body = 83;
            BaseSoundID = 427;

            SetStr(767, 945);
            SetDex(66, 75);
            SetInt(46, 70);

            SetHits(476, 552);

            SetDamage(20, 25);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.MagicResist, 125.1, 140.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 15000;
            Karma = -15000;

            VirtualArmor = 50;

            PackItem(new Club());
        }

        public OgreLord(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "an ogre lords corpse";
        public override Faction FactionAllegiance => Minax.Instance;
        public override Ethic EthicAllegiance => Ethic.Evil;

        public override string DefaultName => "an ogre lord";

        public override bool CanRummageCorpses => true;
        public override Poison PoisonImmune => Poison.Regular;
        public override int TreasureMapLevel => 3;
        public override int Meat => 2;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich, 2);
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
