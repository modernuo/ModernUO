using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Sixth
{
    public class DispelSpell : MagerySpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo m_Info = new(
            "Dispel",
            "An Ort",
            218,
            9002,
            Reagent.Garlic,
            Reagent.MandrakeRoot,
            Reagent.SulfurousAsh
        );

        public DispelSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Sixth;

        public void Target(Mobile m)
        {
            if (m == null)
            {
                return;
            }

            if (!Caster.CanSee(m))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (!(m is BaseCreature bc && bc.IsDispellable))
            {
                Caster.SendLocalizedMessage(1005049); // That cannot be dispelled.
            }
            else if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                var dispelChance =
                    (50.0 + 100 * (Caster.Skills.Magery.Value - bc.DispelDifficulty) / (bc.DispelFocus * 2)) / 100;

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
                }
                else
                {
                    m.FixedEffect(0x3779, 10, 20);
                    Caster.SendLocalizedMessage(1010084); // The creature resisted the attempt to dispel it!
                }
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
        }
    }
}
