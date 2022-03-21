using System;
using Server.Collections;
using Server.Items;
using Server.Mobiles;
using Server.Spells.Necromancy;

namespace Server.Spells.Chivalry
{
    public class DispelEvilSpell : PaladinSpell
    {
        private static readonly SpellInfo _info = new(
            "Dispel Evil",
            "Dispiro Malas",
            -1,
            9002
        );

        public DispelEvilSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(0.25);

        public override double RequiredSkill => 35.0;
        public override int RequiredMana => 10;
        public override int RequiredTithing => 10;
        public override int MantraNumber => 1060721; // Dispiro Malas
        public override bool BlocksMovement => false;

        public override bool DelayedDamage => false;

        public override void SendCastEffect()
        {
            Caster.FixedEffect(0x37C4, 10, 7, 4, 3); // At player
        }

        public override void OnCast()
        {
            if (CheckSequence())
            {
                Caster.PlaySound(0xF5);
                Caster.PlaySound(0x299);
                Caster.FixedParticles(0x37C4, 1, 25, 9922, 14, 3, EffectLayer.Head);

                var dispelSkill = ComputePowerValue(2);

                var chiv = Caster.Skills.Chivalry.Value;

                var eable = Caster.GetMobilesInRange(8);
                using var queue = PooledRefQueue<Mobile>.Create();
                foreach (var m in eable)
                {
                    if (Caster != m && SpellHelper.ValidIndirectTarget(Caster, m) && Caster.CanBeHarmful(m, false))
                    {
                        queue.Enqueue(m);
                    }
                }
                eable.Free();

                while (queue.Count > 0)
                {
                    var m = queue.Dequeue();

                    if (m is BaseCreature bc)
                    {
                        if (bc.Summoned && !bc.IsAnimatedDead)
                        {
                            var dispelChance = (50.0 + 100 * (chiv - bc.DispelDifficulty) / (bc.DispelFocus * 2)) / 100;
                            dispelChance *= dispelSkill / 100.0;

                            if (dispelChance > Utility.RandomDouble())
                            {
                                Effects.SendLocationParticles(
                                    EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration),
                                    0x3728,
                                    8,
                                    20,
                                    5042
                                );
                                Effects.PlaySound(m, 0x201);

                                m.Delete();
                                continue;
                            }
                        }

                        var evil = !bc.Controlled && bc.Karma < 0;

                        if (evil)
                        {
                            // TODO: Is this right?
                            var fleeChance = (100 - Math.Sqrt(m.Fame / 2.0)) * chiv * dispelSkill;
                            fleeChance /= 1000000;

                            if (fleeChance > Utility.RandomDouble())
                            {
                                bc.BeginFlee(TimeSpan.FromSeconds(30.0));
                            }
                        }
                    }

                    var context = TransformationSpellHelper.GetContext(m);
                    if (context?.Spell is NecromancerSpell) // Trees are not evil! TODO: OSI confirm?
                    {
                        // transformed ..

                        var drainChance = 0.5 * (Caster.Skills.Chivalry.Value / Math.Max(m.Skills.Necromancy.Value, 1));

                        if (drainChance > Utility.RandomDouble())
                        {
                            var drain = 5 * dispelSkill / 100;

                            m.Stam -= drain;
                            m.Mana -= drain;
                        }
                    }
                }
            }

            FinishSequence();
        }
    }
}
