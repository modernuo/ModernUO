using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai
{
    public class CursedSoul : BaseCreature
    {
        [Constructible]
        public CursedSoul() : base(AIType.AI_Melee, FightMode.Aggressor)
        {
            Body = 3;
            BaseSoundID = 471;

            SetStr(20, 40);
            SetDex(40, 60);
            SetInt(15, 25);

            SetHits(10, 20);

            SetDamage(3, 7);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 15, 20);
            SetResistance(ResistanceType.Fire, 8, 12);

            SetSkill(SkillName.Wrestling, 35.0, 39.0);
            SetSkill(SkillName.Tactics, 5.0, 15.0);
            SetSkill(SkillName.MagicResist, 10.0);

            Fame = 200;
            Karma = -200;

            PackItem(
                Utility.Random(10) switch
                {
                    0 => new LeftArm(),
                    1 => new RightArm(),
                    2 => new Torso(),
                    3 => new Bone(),
                    4 => new RibCage(),
                    5 => new RibCage(),
                    _ => new BonePile() // 6-9
                }
            );
        }

        public CursedSoul(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a cursed soul corpse";
        public override string DefaultName => "a cursed soul";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
