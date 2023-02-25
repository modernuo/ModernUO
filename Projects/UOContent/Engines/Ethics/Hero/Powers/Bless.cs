using System;
using Server.Spells;
using Server.Targeting;

namespace Server.Ethics.Hero
{
    public sealed class Bless : Power
    {
        public Bless() =>
            m_Definition = new PowerDefinition(
                15,
                "Bless",
                "Erstok Ontawl",
                ""
            );

        public override void BeginInvoke(Player from)
        {
            from.Mobile.BeginTarget(12, true, TargetFlags.None, Power_OnTarget, from);
            from.Mobile.SendMessage("Where do you wish to bless?");
        }

        private void Power_OnTarget(Mobile fromMobile, object obj, Player from)
        {
            if (obj is not IPoint3D p)
            {
                return;
            }

            if (!CheckInvoke(from))
            {
                return;
            }

            var powerFunctioned = false;

            SpellHelper.GetSurfaceTop(ref p);

            foreach (var mob in from.Mobile.GetMobilesInRange(6))
            {
                if (mob != from.Mobile && SpellHelper.ValidIndirectTarget(from.Mobile, mob))
                {
                    continue;
                }

                if (mob.GetStatMod("Holy Bless") != null)
                {
                    continue;
                }

                if (!from.Mobile.CanBeBeneficial(mob, false))
                {
                    continue;
                }

                from.Mobile.DoBeneficial(mob);

                mob.AddStatMod(new StatMod(StatType.All, "Holy Bless", 10, TimeSpan.FromMinutes(30.0)));

                mob.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
                mob.PlaySound(0x1EA);

                powerFunctioned = true;
            }

            if (powerFunctioned)
            {
                SpellHelper.Turn(from.Mobile, p);

                Effects.PlaySound(new Point3D(p), from.Mobile.Map, 0x299);

                from.Mobile.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "You consecrate the area.");

                FinishInvoke(from);
            }
            else
            {
                from.Mobile.FixedEffect(0x3735, 6, 30);
                from.Mobile.PlaySound(0x5C);
            }
        }
    }
}
