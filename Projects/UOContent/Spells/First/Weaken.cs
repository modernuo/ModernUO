using Server.Targeting;

namespace Server.Spells.First
{
    public class WeakenSpell : MagerySpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo _info = new(
            "Weaken",
            "Des Mani",
            212,
            9031,
            Reagent.Garlic,
            Reagent.Nightshade
        );

        public WeakenSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.First;

        public void Target(Mobile m)
        {
            if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                SpellHelper.CheckReflect((int)Circle, Caster, ref m);

                var length = SpellHelper.GetDuration(Caster, m);
                SpellHelper.AddStatCurse(Caster, m, StatType.Str, length, false);

                m.Spell?.OnCasterHurt();

                m.Paralyzed = false;

                m.FixedParticles(0x3779, 10, 15, 5009, EffectLayer.Waist);
                m.PlaySound(0x1E6);

                var percentage = (int)(SpellHelper.GetOffsetScalar(Caster, m, true) * 100);

                BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.Weaken, 1075837, length, m, percentage.ToString()));

                HarmfulSpell(m);
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
        }
    }
}
