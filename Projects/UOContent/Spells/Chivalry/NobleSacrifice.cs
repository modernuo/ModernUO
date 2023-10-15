using System;
using Server.Collections;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Spells.Fourth;
using Server.Spells.Mysticism;
using Server.Spells.Necromancy;

namespace Server.Spells.Chivalry
{
    public class NobleSacrificeSpell : PaladinSpell
    {
        private static readonly SpellInfo _info = new(
            "Noble Sacrifice",
            "Dium Prostra",
            -1,
            9002
        );

        public NobleSacrificeSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.5);

        public override double RequiredSkill => 65.0;
        public override int RequiredMana => 20;
        public override int RequiredTithing => 30;
        public override int MantraNumber => 1060725; // Dium Prostra
        public override bool BlocksMovement => false;

        public override void OnCast()
        {
            if (CheckSequence())
            {
                var eable = Caster.GetMobilesInRange(3);
                using var pool = PooledRefQueue<Mobile>.Create();

                foreach (var m in eable)
                {
                    if (m is not BaseCreature { IsAnimatedDead: true } && Caster != m && m.InLOS(Caster) &&
                        Caster.CanBeBeneficial(m, false, true) && m is not Golem)
                    {
                        pool.Enqueue(m);
                    }
                }

                Caster.PlaySound(0x244);
                Caster.FixedParticles(0x3709, 1, 30, 9965, 5, 7, EffectLayer.Waist);
                Caster.FixedParticles(0x376A, 1, 30, 9502, 5, 3, EffectLayer.Waist);

                /* Attempts to Resurrect, Cure and Heal all targets in a radius around the caster.
                 * If any target is successfully assisted, the Paladin's current
                 * Hit Points, Mana and Stamina are set to 1.
                 * Amount of damage healed is affected by the Caster's Karma, from 8 to 24 hit points.
                 */

                var sacrifice = false;

                // TODO: Is there really a resurrection chance?
                var resChance = 0.1 + 0.9 * Caster.Karma / 10000;

                while (pool.Count > 0)
                {
                    var m = pool.Dequeue();

                    if (!m.Alive)
                    {
                        if (m.Region?.IsPartOf("Khaldun") == true)
                        {
                            // The veil of death in this area is too strong and resists thy efforts to restore life.
                            Caster.SendLocalizedMessage(1010395);
                        }
                        else if (resChance > Utility.RandomDouble())
                        {
                            m.FixedParticles(0x375A, 1, 15, 5005, 5, 3, EffectLayer.Head);
                            m.CloseGump<ResurrectGump>();
                            m.SendGump(new ResurrectGump(m, Caster));
                            sacrifice = true;
                        }
                    }
                    else
                    {
                        bool sendEffect = false;

                        if (m.Poisoned && m.CurePoison(Caster))
                        {
                            Caster.DoBeneficial(m);

                            if (Caster != m)
                            {
                                Caster.SendLocalizedMessage(1010058); // You have cured the target of all poisons!
                            }

                            m.SendLocalizedMessage(1010059); // You have been cured of all poisons.
                            sendEffect = true;
                            sacrifice = true;
                        }

                        if (m.Hits < m.HitsMax)
                        {
                            var toHeal = Math.Clamp(ComputePowerValue(10) + Utility.RandomMinMax(0, 2), 8, 24);

                            Caster.DoBeneficial(m);
                            m.Heal(toHeal, Caster);
                            sendEffect = true;
                        }

                        var mod = m.GetStatMod("[Magic] Str Curse");
                        if (mod?.Offset < 0)
                        {
                            m.RemoveStatMod("[Magic] Str Curse");
                            sendEffect = true;
                        }

                        mod = m.GetStatMod("[Magic] Dex Curse");
                        if (mod?.Offset < 0)
                        {
                            m.RemoveStatMod("[Magic] Dex Curse");
                            sendEffect = true;
                        }

                        mod = m.GetStatMod("[Magic] Int Curse");
                        if (mod?.Offset < 0)
                        {
                            m.RemoveStatMod("[Magic] Int Curse");
                            sendEffect = true;
                        }

                        if (m.Paralyzed)
                        {
                            m.Paralyzed = false;
                            sendEffect = true;
                        }

                        sendEffect = EvilOmenSpell.EndEffect(m) || sendEffect;
                        sendEffect = StrangleSpell.RemoveCurse(m) || sendEffect;
                        sendEffect = CorpseSkinSpell.RemoveCurse(m) || sendEffect;
                        sendEffect = CurseSpell.RemoveEffect(m) || sendEffect;
                        sendEffect = MortalStrike.EndWound(m) || sendEffect;
                        sendEffect = MindRotSpell.ClearMindRotScalar(m) || sendEffect;
                        sendEffect = BloodOathSpell.RemoveCurse(m) || sendEffect;
                        sendEffect = SpellPlagueSpell.RemoveEffect(m) || sendEffect;

                        // TODO: Move these into their respective end effect methods
                        BuffInfo.RemoveBuff(m, BuffIcon.Clumsy);
                        BuffInfo.RemoveBuff(m, BuffIcon.FeebleMind);
                        BuffInfo.RemoveBuff(m, BuffIcon.Weaken);
                        BuffInfo.RemoveBuff(m, BuffIcon.Curse);
                        BuffInfo.RemoveBuff(m, BuffIcon.MassCurse);
                        BuffInfo.RemoveBuff(m, BuffIcon.MortalStrike);
                        BuffInfo.RemoveBuff(m, BuffIcon.Strangle);
                        BuffInfo.RemoveBuff(m, BuffIcon.EvilOmen);

                        if (sendEffect)
                        {
                            m.FixedParticles(0x375A, 1, 15, 5005, 5, 3, EffectLayer.Head);
                            sacrifice = true;
                        }
                    }
                }

                if (sacrifice)
                {
                    Caster.PlaySound(Caster.Body.IsFemale ? 0x150 : 0x423);
                    Caster.Hits = 1;
                    Caster.Stam = 1;
                    Caster.Mana = 1;
                }
            }

            FinishSequence();
        }
    }
}
