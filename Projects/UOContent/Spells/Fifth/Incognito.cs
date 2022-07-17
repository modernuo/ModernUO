using System;
using System.Collections.Generic;
using Server.Factions;
using Server.Items;
using Server.Mobiles;
using Server.Spells.Seventh;

namespace Server.Spells.Fifth
{
    public class IncognitoSpell : MagerySpell
    {
        private static readonly SpellInfo _info = new(
            "Incognito",
            "Kal In Ex",
            206,
            9002,
            Reagent.Bloodmoss,
            Reagent.Garlic,
            Reagent.Nightshade
        );

        private static readonly Dictionary<Mobile, TimerExecutionToken> _table = new();

        public IncognitoSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fifth;

        public override bool CheckCast()
        {
            if (Sigil.ExistsOn(Caster))
            {
                Caster.SendLocalizedMessage(1010445); // You cannot incognito if you have a sigil
                return false;
            }

            if (!Caster.CanBeginAction<IncognitoSpell>())
            {
                Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
                return false;
            }

            if (Caster.BodyMod == 183 || Caster.BodyMod == 184)
            {
                Caster.SendLocalizedMessage(1042402); // You cannot use incognito while wearing body paint
                return false;
            }

            return true;
        }

        public override void OnCast()
        {
            if (Sigil.ExistsOn(Caster))
            {
                Caster.SendLocalizedMessage(1010445); // You cannot incognito if you have a sigil
                return;
            }

            if (!Caster.CanBeginAction<IncognitoSpell>())
            {
                Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
                return;
            }

            if (Caster.BodyMod == 183 || Caster.BodyMod == 184)
            {
                Caster.SendLocalizedMessage(1042402); // You cannot use incognito while wearing body paint
                return;
            }

            if (DisguiseTimers.IsDisguised(Caster))
            {
                Caster.SendLocalizedMessage(1061631); // You can't do that while disguised.
                return;
            }

            if (!Caster.CanBeginAction<PolymorphSpell>() || Caster.IsBodyMod)
            {
                DoFizzle();
                return;
            }

            if (CheckSequence())
            {
                if (Caster.BeginAction<IncognitoSpell>())
                {
                    DisguiseTimers.StopTimer(Caster);

                    Caster.HueMod = Caster.Race.RandomSkinHue();
                    Caster.NameMod = Caster.Female ? NameList.RandomName("female") : NameList.RandomName("male");

                    var pm = Caster as PlayerMobile;

                    if (pm?.Race != null)
                    {
                        pm.SetHairMods(pm.Race.RandomHair(pm.Female), pm.Race.RandomFacialHair(pm.Female));
                        pm.HairHue = pm.Race.RandomHairHue();
                        pm.FacialHairHue = pm.Race.RandomHairHue();
                    }

                    Caster.FixedParticles(0x373A, 10, 15, 5036, EffectLayer.Head);
                    Caster.PlaySound(0x3BD);

                    BaseArmor.ValidateMobile(Caster);
                    BaseClothing.ValidateMobile(Caster);

                    StopTimer(Caster);

                    var duration = TimeSpan.FromSeconds(Math.Min(6 * Caster.Skills.Magery.Value / 5.0, 144));

                    Timer.StartTimer(duration, () => EndIncognito(Caster), out var timerToken);
                    _table[Caster] = timerToken;

                    BuffInfo.AddBuff(Caster, new BuffInfo(BuffIcon.Incognito, 1075819, duration, Caster));
                }
                else
                {
                    Caster.SendLocalizedMessage(1079022); // You're already incognitoed!
                }
            }

            FinishSequence();
        }

        public static void StopTimer(Mobile m)
        {
            if (_table.Remove(m, out var timerToken))
            {
                timerToken.Cancel();
            }

            BuffInfo.RemoveBuff(m, BuffIcon.Incognito);
        }

        public static void EndIncognito(Mobile m)
        {
            if (m.CanBeginAction<IncognitoSpell>())
            {
                return;
            }

            (m as PlayerMobile)?.SetHairMods(-1, -1);

            m.BodyMod = 0;
            m.HueMod = -1;
            m.NameMod = null;
            m.EndAction<IncognitoSpell>();
            BaseArmor.ValidateMobile(m);
            BaseClothing.ValidateMobile(m);

            StopTimer(m);
        }
    }
}
