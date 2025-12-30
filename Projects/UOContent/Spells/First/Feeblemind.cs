using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.First
{
    public class FeeblemindSpell : MagerySpell, ITargetingSpell<Mobile>
    {
        private static readonly SpellInfo _info = new(
            "Feeblemind",
            "Rel Wis",
            212,
            9031,
            Reagent.Ginseng,
            Reagent.Nightshade
        );

        public FeeblemindSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.First;

        public void Target(Mobile m)
        {
            if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                SpellHelper.CheckReflect((int)Circle, Caster, ref m);

                // TODO: StoneForm immunity

                var length = SpellHelper.GetDuration(Caster, m);
                SpellHelper.AddStatCurse(Caster, m, StatType.Int, length, false);

                m.Spell?.OnCasterHurt();

                m.Paralyzed = false;

                m.FixedParticles(0x3779, 10, 15, 5004, EffectLayer.Head);
                m.PlaySound(0x1E4);

                var percentage = (int)(SpellHelper.GetOffsetScalar(Caster, m, true) * 100);

                (m as PlayerMobile)?.AddBuff(new BuffInfo(BuffIcon.FeebleMind, 1075833, length, percentage.ToString()));

                HarmfulSpell(m);
            }
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
        }
    }
}
