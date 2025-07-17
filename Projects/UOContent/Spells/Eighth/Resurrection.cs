using Server.Engines.ConPVP;
using Server.Gumps;
using Server.Targeting;
using Server.Mobiles;

namespace Server.Spells.Eighth
{
    public class ResurrectionSpell : MagerySpell, ITargetingSpell<Mobile>
    {
        private static readonly SpellInfo _info = new(
            "Resurrection",
            "An Corp",
            245,
            9062,
            Reagent.Bloodmoss,
            Reagent.Garlic,
            Reagent.Ginseng
        );

        public ResurrectionSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Eighth;

        public int TargetRange => 1;

        public void Target(Mobile m)
        {
            if (m == Caster)
            {
                Caster.SendLocalizedMessage(501039); // Thou can not resurrect thyself.
            }
            else if (m is BaseCreature { IsDeadBondedPet: true })
            {
                Caster.SendMessage("Target can not be revived this way.");
            }
            else if (!m.Player)
            {
                Caster.SendLocalizedMessage(501043); // Target is not a being.
            }
            else if (m.Alive)
            {
                Caster.SendLocalizedMessage(501041); // Target is not dead.
            }
            else if (!Caster.InRange(m, TargetRange))
            {
                Caster.SendLocalizedMessage(501042); // Target is not close enough.
            }
            else if (m.Map?.CanFit(m.Location, 16, false, false) != true)
            {
                Caster.SendLocalizedMessage(501042); // Target can not be resurrected at that location.
                m.SendLocalizedMessage(502391);      // Thou can not be resurrected there!
            }
            else if (m.Region?.IsPartOf("Khaldun") == true)
            {
                // The veil of death in this area is too strong and resists thy efforts to restore life.
                Caster.SendLocalizedMessage(1010395);
            }
            else if (CheckBSequence(m, true))
            {
                SpellHelper.Turn(Caster, m);

                m.PlaySound(0x214);
                m.FixedEffect(0x376A, 10, 16);

                m.SendGump(new ResurrectGump(Caster));
            }
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
            Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Beneficial);
        }
    }
}
