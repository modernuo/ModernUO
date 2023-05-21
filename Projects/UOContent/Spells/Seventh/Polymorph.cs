using System;
using System.Collections.Generic;
using Server.Factions;
using Server.Gumps;
using Server.Items;
using Server.Spells.Fifth;

namespace Server.Spells.Seventh
{
    public class PolymorphSpell : MagerySpell
    {
        private static readonly SpellInfo _info = new(
            "Polymorph",
            "Vas Ylem Rel",
            221,
            9002,
            Reagent.Bloodmoss,
            Reagent.SpidersSilk,
            Reagent.MandrakeRoot
        );

        private static readonly Dictionary<Mobile, TimerExecutionToken> _table = new();

        private readonly int m_NewBody;

        public PolymorphSpell(Mobile caster, Item scroll, int body = 0) : base(caster, scroll, _info) => m_NewBody = body;

        public override SpellCircle Circle => SpellCircle.Seventh;

        public override bool CheckCast()
        {
            var caster = Caster;

            /*if (caster.Mounted)
            {
              caster.SendLocalizedMessage( 1042561 ); //Please dismount first.
              return false;
            }
            else */
            if (Sigil.ExistsOn(caster))
            {
                caster.SendLocalizedMessage(1010521); // You cannot polymorph while you have a Town Sigil
                return false;
            }

            if (TransformationSpellHelper.UnderTransformation(caster))
            {
                caster.SendLocalizedMessage(1061633); // You cannot polymorph while in that form.
                return false;
            }

            if (DisguisePersistence.IsDisguised(caster))
            {
                caster.SendLocalizedMessage(502167); // You cannot polymorph while disguised.
                return false;
            }

            if (caster.BodyMod == 183 || caster.BodyMod == 184)
            {
                caster.SendLocalizedMessage(1042512); // You cannot polymorph while wearing body paint
                return false;
            }

            if (!caster.CanBeginAction<PolymorphSpell>())
            {
                if (Core.ML)
                {
                    EndPolymorph(caster);
                }
                else
                {
                    caster.SendLocalizedMessage(1005559); // This spell is already in effect.
                }

                return false;
            }

            if (m_NewBody == 0)
            {
                var gump = Core.SE ? (Gump)new NewPolymorphGump(caster, Scroll) : new PolymorphGump(caster, Scroll);

                caster.SendGump(gump);
                return false;
            }

            return true;
        }

        public override void OnCast()
        {
            var caster = Caster;

            /*if (caster.Mounted)
            {
              caster.SendLocalizedMessage(1042561); // Please dismount first.
              return;
            }*/

            if (Sigil.ExistsOn(caster))
            {
                caster.SendLocalizedMessage(1010521); // You cannot polymorph while you have a Town Sigil
                return;
            }

            if (!caster.CanBeginAction<PolymorphSpell>())
            {
                if (Core.ML)
                {
                    EndPolymorph(caster);
                }
                else
                {
                    caster.SendLocalizedMessage(1005559); // This spell is already in effect.
                }

                return;
            }

            if (TransformationSpellHelper.UnderTransformation(caster))
            {
                caster.SendLocalizedMessage(1061633); // You cannot polymorph while in that form.
                return;
            }

            if (DisguisePersistence.IsDisguised(caster))
            {
                caster.SendLocalizedMessage(502167); // You cannot polymorph while disguised.
                return;
            }

            if (caster.BodyMod == 183 || caster.BodyMod == 184)
            {
                caster.SendLocalizedMessage(1042512); // You cannot polymorph while wearing body paint
                return;
            }

            if (!caster.CanBeginAction<IncognitoSpell>() || caster.IsBodyMod)
            {
                DoFizzle();
                return;
            }

            if (CheckSequence())
            {
                if (!caster.BeginAction<PolymorphSpell>())
                {
                    caster.SendLocalizedMessage(1005559); // This spell is already in effect.
                    return;
                }

                if (m_NewBody != 0)
                {
                    if (!((Body)m_NewBody).IsHuman)
                    {
                        var mt = caster.Mount;

                        if (mt != null)
                        {
                            mt.Rider = null;
                        }
                    }

                    caster.BodyMod = m_NewBody;

                    caster.HueMod = m_NewBody is 400 or 401 ? caster.Race.RandomSkinHue() : 0;

                    BaseArmor.ValidateMobile(caster);
                    BaseClothing.ValidateMobile(caster);

                    if (!Core.ML)
                    {
                        StopTimer(caster);

                        var duration = Core.Expansion switch
                        {
                            Expansion.None => TimeSpan.FromSeconds(6 * caster.Skills.Magery.Value / 5.0),
                            _              => TimeSpan.FromSeconds(Math.Max(caster.Skills.Magery.Value, 120))
                        };

                        Timer.StartTimer(duration, () => EndPolymorph(caster), out var timerToken);
                        _table[caster] = timerToken;
                    }
                }
            }

            FinishSequence();
        }

        public static void StopTimer(Mobile m)
        {
            if (_table.Remove(m, out var timer))
            {
                timer.Cancel();
            }
        }

        public static void EndPolymorph(Mobile m)
        {
            if (m.CanBeginAction<PolymorphSpell>())
            {
                return;
            }

            m.BodyMod = 0;
            m.HueMod = -1;
            m.NameMod = null;
            m.EndAction<PolymorphSpell>();

            BaseArmor.ValidateMobile(m);
            BaseClothing.ValidateMobile(m);

            StopTimer(m);
        }
    }
}
