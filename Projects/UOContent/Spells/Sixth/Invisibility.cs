using System;
using System.Collections.Generic;
using Server.Engines.ConPVP;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Sixth
{
    public class InvisibilitySpell : MagerySpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo _info = new(
            "Invisibility",
            "An Lor Xen",
            206,
            9002,
            Reagent.Bloodmoss,
            Reagent.Nightshade
        );

        private static readonly Dictionary<Mobile, TimerExecutionToken> _table = new();

        public InvisibilitySpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Sixth;

        public void Target(Mobile m)
        {
            if (m is BaseVendor or PlayerVendor || m.AccessLevel > Caster.AccessLevel)
            {
                Caster.SendLocalizedMessage(501857); // This spell won't work on that!
            }
            else if (CheckBSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                Effects.SendLocationParticles(
                    EffectItem.Create(new Point3D(m.X, m.Y, m.Z + 16), Caster.Map, EffectItem.DefaultDuration),
                    0x376A,
                    10,
                    15,
                    5045
                );
                m.PlaySound(0x3C4);

                m.Hidden = true;
                m.Combatant = null;
                m.Warmode = false;

                StopTimer(m);

                var duration = TimeSpan.FromSeconds(1.2 * Caster.Skills.Magery.Value);

                BuffInfo.RemoveBuff(m, BuffIcon.HidingAndOrStealth);
                BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.Invisibility, 1075825, duration, m)); // Invisibility/Invisible

                Timer.StartTimer(duration,
                    () =>
                    {
                        m.RevealingAction();
                        StopTimer(m);
                    },
                    out var timerToken
                );

                _table[m] = timerToken;
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

        public static bool HasTimer(Mobile m) => _table.ContainsKey(m);

        public static void StopTimer(Mobile m)
        {
            if (_table.Remove(m, out var timerToken))
            {
                timerToken.Cancel();
            }
        }
    }
}
