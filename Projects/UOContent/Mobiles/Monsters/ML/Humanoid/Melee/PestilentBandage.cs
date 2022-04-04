using Server.Items;

namespace Server.Mobiles
{
    public class PestilentBandage : BaseCreature
    {
        [Constructible]
        public PestilentBandage() : base(AIType.AI_Melee) // NEED TO CHECK
        {
            Body = 154;
            Hue = 0x515;
            BaseSoundID = 471;

            SetStr(691, 740);
            SetDex(141, 180);
            SetInt(51, 80);

            SetHits(415, 445);

            SetDamage(13, 23);

            SetDamageType(ResistanceType.Physical, 40);
            SetDamageType(ResistanceType.Cold, 20);
            SetDamageType(ResistanceType.Poison, 40);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 50, 60);
            SetResistance(ResistanceType.Poison, 20, 30);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.Poisoning, 0.0, 10.0);
            SetSkill(SkillName.Anatomy, 0);
            SetSkill(SkillName.MagicResist, 75.0, 80.0);
            SetSkill(SkillName.Tactics, 80.0, 85.0);
            SetSkill(SkillName.Wrestling, 70.0, 75.0);

            Fame = 20000;
            Karma = -20000;

            // VirtualArmor = 28; // Don't know what it should be

            PackItem(new Bandage(5)); // How many?
        }

        public PestilentBandage(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a pestilent bandage corpse";
        // Neither Stratics nor UOGuide have much description
        // beyond being a "Grey Mummy". Body, Sound and
        // Hue are all guessed until they can be verified.
        // Loot and Fame/Karma are also guesses at this point.
        //
        // They also apparently have a Poison Attack, which I've stolen from Yamandons.

        public override string DefaultName => "a pestilent bandage";

        public override Poison HitPoison => Poison.Lethal;
        public override bool CanHeal => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich); // Need to verify
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
