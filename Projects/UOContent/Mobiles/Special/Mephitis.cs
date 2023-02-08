using System;
using Server.Engines.CannedEvil;
using Server.Items;

namespace Server.Mobiles
{
    public class Mephitis : BaseChampion
    {
        [Constructible]
        public Mephitis() : base(AIType.AI_Melee)
        {
            Body = 173;
            BaseSoundID = 0x183;

            SetStr(505, 1000);
            SetDex(102, 300);
            SetInt(402, 600);

            SetHits(3000);
            SetStam(105, 600);

            SetDamage(21, 33);

            SetDamageType(ResistanceType.Physical, 50);
            SetDamageType(ResistanceType.Poison, 50);

            SetResistance(ResistanceType.Physical, 75, 80);
            SetResistance(ResistanceType.Fire, 60, 70);
            SetResistance(ResistanceType.Cold, 60, 70);
            SetResistance(ResistanceType.Poison, 100);
            SetResistance(ResistanceType.Energy, 60, 70);

            SetSkill(SkillName.MagicResist, 70.7, 140.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 97.6, 100.0);

            Fame = 22500;
            Karma = -22500;

            VirtualArmor = 80;
        }

        public Mephitis(Serial serial) : base(serial)
        {
        }

        public override ChampionSkullType SkullType => ChampionSkullType.Venom;

        public override Type[] UniqueList => new[] { typeof(Calm) };

        public override Type[] SharedList => new[]
        {
            typeof(OblivionsNeedle), typeof(ANecromancerShroud), typeof(EmbroideredOakLeafCloak),
            typeof(TheMostKnowledgePerson)
        };

        public override Type[] DecorativeList => new[] { typeof(Web), typeof(MonsterStatuette) };

        public override MonsterStatuetteType[] StatueTypes => new[] { MonsterStatuetteType.Spider };

        public override string DefaultName => "Mephitis";

        public override Poison PoisonImmune => Poison.Lethal;
        public override Poison HitPoison => Poison.Lethal;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 4);
        }

        public override void OnGotMeleeAttack(Mobile attacker, int damage)
        {
            base.OnGotMeleeAttack(attacker, damage);

            // TODO: Web ability
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
