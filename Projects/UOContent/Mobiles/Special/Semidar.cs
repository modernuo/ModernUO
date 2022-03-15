using System;
using Server.Engines.CannedEvil;
using Server.Items;

namespace Server.Mobiles
{
    public class Semidar : BaseChampion
    {
        [Constructible]
        public Semidar() : base(AIType.AI_Mage)
        {
            Body = 174;
            BaseSoundID = 0x4B0;

            SetSpeed(0.1,  0.6);

            SetStr(502, 600);
            SetDex(102, 200);
            SetInt(601, 750);

            SetHits(1500);
            SetStam(103, 250);

            SetDamage(29, 35);

            SetDamageType(ResistanceType.Physical, 75);
            SetDamageType(ResistanceType.Fire, 25);

            SetResistance(ResistanceType.Physical, 20, 30);
            SetResistance(ResistanceType.Fire, 50, 60);
            SetResistance(ResistanceType.Cold, 20, 30);
            SetResistance(ResistanceType.Poison, 20, 30);
            SetResistance(ResistanceType.Energy, 10, 20);

            SetSkill(SkillName.EvalInt, 95.1, 100.0);
            SetSkill(SkillName.Magery, 90.1, 105.0);
            SetSkill(SkillName.Meditation, 95.1, 100.0);
            SetSkill(SkillName.MagicResist, 120.2, 140.0);
            SetSkill(SkillName.Tactics, 90.1, 105.0);
            SetSkill(SkillName.Wrestling, 90.1, 105.0);

            Fame = 24000;
            Karma = -24000;

            VirtualArmor = 20;
        }

        public Semidar(Serial serial) : base(serial)
        {
        }

        public override ChampionSkullType SkullType => ChampionSkullType.Pain;

        public override Type[] UniqueList => new[] { typeof(GladiatorsCollar) };

        public override Type[] SharedList => new[]
            { typeof(RoyalGuardSurvivalKnife), typeof(ANecromancerShroud), typeof(LieutenantOfTheBritannianRoyalGuard) };

        public override Type[] DecorativeList => new[] { typeof(LavaTile), typeof(DemonSkull) };

        public override MonsterStatuetteType[] StatueTypes => Array.Empty<MonsterStatuetteType>();

        public override string DefaultName => "Semidar";

        public override bool Unprovokable => true;
        public override Poison PoisonImmune => Poison.Lethal;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 4);
            AddLoot(LootPack.FilthyRich);
        }

        public override void CheckReflect(Mobile caster, ref bool reflect)
        {
            if (caster.Body.IsMale)
            {
                reflect = true; // Always reflect if caster isn't female
            }
        }

        public override void AlterDamageScalarFrom(Mobile caster, ref double scalar)
        {
            if (caster.Body.IsMale)
            {
                scalar = 20; // Male bodies always reflect.. damage scaled 20x
            }
        }

        public void DrainLife()
        {
            if (Map == null)
            {
                return;
            }

            var eable = GetMobilesInRange<Mobile>(2);

            foreach (var m in eable)
            {
                if (m == this || !(CanBeHarmful(m) || m.Player && m.Alive))
                {
                    continue;
                }

                if (m is not BaseCreature bc || !(bc.Controlled || bc.Summoned || bc.Team != Team))
                {
                    continue;
                }

                DoHarmful(m);

                m.FixedParticles(0x374A, 10, 15, 5013, 0x496, 0, EffectLayer.Waist);
                m.PlaySound(0x231);

                m.SendMessage("You feel the life drain out of you!");

                var toDrain = Utility.RandomMinMax(10, 40);

                Hits += toDrain;
                m.Damage(toDrain, this);
            }

            eable.Free();
        }

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (Utility.RandomDouble() <= 0.25)
            {
                DrainLife();
            }
        }

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);

            if (Utility.RandomDouble() <= 0.25)
            {
                DrainLife();
            }
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
