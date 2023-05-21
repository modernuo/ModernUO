using ModernUO.Serialization;
using Server.Engines.Plants;
using Server.Items;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Yamadon")]
    [SerializationGenerator(0, false)]
    public partial class Yamandon : BaseCreature
    {
        [Constructible]
        public Yamandon() : base(AIType.AI_Melee)
        {
            Body = 249;

            SetStr(786, 930);
            SetDex(251, 365);
            SetInt(101, 115);

            SetHits(1601, 1800);

            SetDamage(19, 35);

            SetDamageType(ResistanceType.Physical, 70);
            SetDamageType(ResistanceType.Poison, 20);
            SetDamageType(ResistanceType.Energy, 10);

            SetResistance(ResistanceType.Physical, 65, 85);
            SetResistance(ResistanceType.Fire, 70, 90);
            SetResistance(ResistanceType.Cold, 50, 70);
            SetResistance(ResistanceType.Poison, 50, 70);
            SetResistance(ResistanceType.Energy, 50, 70);

            SetSkill(SkillName.Anatomy, 115.1, 130.0);
            SetSkill(SkillName.MagicResist, 117.6, 132.5);
            SetSkill(SkillName.Poisoning, 120.1, 140.0);
            SetSkill(SkillName.Tactics, 117.1, 132.0);
            SetSkill(SkillName.Wrestling, 112.6, 132.5);

            Fame = 22000;
            Karma = -22000;

            if (Utility.RandomDouble() < .50)
            {
                PackItem(Seed.RandomBonsaiSeed());
            }

            PackItem(new Eggs(2));
        }

        public override string CorpseName => "a yamandon corpse";
        public override string DefaultName => "a yamandon";

        public override bool ReacquireOnMovement => true;
        public override Poison PoisonImmune => Poison.Lethal;
        public override Poison HitPoison => Utility.RandomBool() ? Poison.Deadly : Poison.Lethal;
        public override int TreasureMapLevel => 5;
        public override int Hides => 20;

        public override WeaponAbility GetWeaponAbility() => WeaponAbility.DoubleStrike;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich);
            AddLoot(LootPack.FilthyRich, 2);
            AddLoot(LootPack.Gems, 6);
        }

        public override void OnDamagedBySpell(Mobile attacker, int damage)
        {
            base.OnDamagedBySpell(attacker, damage);

            DoCounter(attacker);
        }

        public override void OnGotMeleeAttack(Mobile attacker, int damage)
        {
            base.OnGotMeleeAttack(attacker, damage);

            DoCounter(attacker);
        }

        private void DoCounter(Mobile attacker)
        {
            if (Map == null)
            {
                return;
            }

            if (attacker is BaseCreature creature && creature.BardProvoked)
            {
                return;
            }

            if (Utility.RandomDouble() < 0.2)
            {
                /* Counterattack with Hit Poison Area
                 * 20-25 damage, unresistable
                 * Lethal poison, 100% of the time
                 * Particle effect: Type: "2" From: "0x4061A107" To: "0x0" ItemId: "0x36BD" ItemIdName: "explosion" FromLocation: "(296 615, 17)" ToLocation: "(296 615, 17)" Speed: "1" Duration: "10" FixedDirection: "True" Explode: "False" Hue: "0xA6" RenderMode: "0x0" Effect: "0x1F78" ExplodeEffect: "0x1" ExplodeSound: "0x0" Serial: "0x4061A107" Layer: "255" Unknown: "0x0"
                 * Doesn't work on provoked monsters
                 */

                Mobile target = null;

                if (attacker is BaseCreature baseCreature)
                {
                    var m = baseCreature.GetMaster();

                    if (m != null)
                    {
                        target = m;
                    }
                }

                if (target?.InRange(this, 18) != true)
                {
                    target = attacker;
                }

                Animate(10, 4, 1, true, false, 0);

                var eable = target.GetMobilesInRange<Mobile>(8);

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

                    AOS.Damage(m, this, Utility.RandomMinMax(20, 25), true, 0, 0, 0, 100, 0);

                    m.FixedParticles(0x36BD, 1, 10, 0x1F78, 0xA6, 0, (EffectLayer)255);
                    m.ApplyPoison(this, Poison.Lethal);
                }

                eable.Free();
            }
        }

        public override int GetAttackSound() => 1260;

        public override int GetAngerSound() => 1262;

        public override int GetDeathSound() => 1259;

        public override int GetHurtSound() => 1263;

        public override int GetIdleSound() => 1261;
    }
}
