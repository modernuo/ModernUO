using System;
using Server.Engines.ConPVP;
using Server.Items;
using Server.Spells.Fourth;
using Server.Spells.Mysticism;
using Server.Spells.Necromancy;
using Server.Targeting;

namespace Server.Spells.Chivalry
{
    public class RemoveCurseSpell : PaladinSpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo _info = new(
            "Remove Curse",
            "Extermo Vomica",
            -1,
            9002
        );

        public RemoveCurseSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        // https://uo.com/wiki/ultima-online-wiki/publish-notes/publish-108/ - 1.5 -> 2.0
        // According to tests, this includes the 0.25s added penalty, so we are adjusting to 1.75s base.
        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(Core.EJ ? 1.75 : 1.5);

        public override double RequiredSkill => 5.0;
        public override int RequiredMana => 20;
        public override int RequiredTithing => 10;
        public override int MantraNumber => 1060726; // Extermo Vomica

        public void Target(Mobile m)
        {
            if (m == null)
            {
                return;
            }

            if (CheckBSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                /* Attempts to remove all Curse effects from Target.
                 * Curses include Mage spells such as Clumsy, Weaken, Feeblemind and Paralyze
                 * as well as all Necromancer curses.
                 * Chance of removing curse is affected by Caster's Karma.
                 */

                int chance = Caster.Karma switch
                {
                    < -5000 => 0,
                    < 0     => (int)Math.Sqrt(20000 + Caster.Karma) - 122,
                    < 5625  => (int)Math.Sqrt(Caster.Karma) + 25,
                    _       => 100
                };

                if (chance > Utility.Random(100))
                {
                    m.PlaySound(0xF6);
                    m.PlaySound(0x1F7);
                    m.FixedParticles(0x3709, 1, 30, 9963, 13, 3, EffectLayer.Head);

                    IEntity from = new Entity(Serial.Zero, new Point3D(m.X, m.Y, m.Z - 10), Caster.Map);
                    IEntity to = new Entity(Serial.Zero, new Point3D(m.X, m.Y, m.Z + 50), Caster.Map);
                    Effects.SendMovingParticles(
                        from,
                        to,
                        0x2255,
                        1,
                        0,
                        false,
                        false,
                        13,
                        3,
                        9501,
                        1,
                        0,
                        EffectLayer.Head,
                        0x100
                    );

                    var mod = m.GetStatMod("[Magic] Str Curse");
                    if (mod?.Offset < 0)
                    {
                        m.RemoveStatMod("[Magic] Str Curse");
                    }

                    mod = m.GetStatMod("[Magic] Dex Curse");
                    if (mod?.Offset < 0)
                    {
                        m.RemoveStatMod("[Magic] Dex Curse");
                    }

                    mod = m.GetStatMod("[Magic] Int Curse");
                    if (mod?.Offset < 0)
                    {
                        m.RemoveStatMod("[Magic] Int Curse");
                    }

                    m.Paralyzed = false;

                    EvilOmenSpell.EndEffect(m);
                    StrangleSpell.RemoveCurse(m);
                    CorpseSkinSpell.RemoveCurse(m);
                    CurseSpell.RemoveEffect(m);
                    MortalStrike.EndWound(m);
                    MindRotSpell.ClearMindRotScalar(m);
                    BloodOathSpell.RemoveCurse(m);
                    SpellPlagueSpell.RemoveEffect(m);

                    // TODO: Move these into their respective end effect methods
                    BuffInfo.RemoveBuff(m, BuffIcon.Clumsy);
                    BuffInfo.RemoveBuff(m, BuffIcon.FeebleMind);
                    BuffInfo.RemoveBuff(m, BuffIcon.Weaken);
                    BuffInfo.RemoveBuff(m, BuffIcon.Curse);
                    BuffInfo.RemoveBuff(m, BuffIcon.MassCurse);
                    BuffInfo.RemoveBuff(m, BuffIcon.MortalStrike);
                    BuffInfo.RemoveBuff(m, BuffIcon.Strangle);
                    BuffInfo.RemoveBuff(m, BuffIcon.EvilOmen);
                }
                else
                {
                    m.PlaySound(0x1DF);
                }
            }

            FinishSequence();
        }

        public override bool CheckCast()
        {
            if (DuelContext.CheckSuddenDeath(Caster))
            {
                Caster.SendMessage(0x22, "You cannot cast this spell when in sudden death.");
                return false;
            }

            return base.CheckCast();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Beneficial, Core.ML ? 10 : 12);
        }
    }
}
