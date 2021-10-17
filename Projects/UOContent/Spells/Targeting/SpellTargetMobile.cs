using Server.Targeting;

namespace Server.Spells
{
    public interface ISpellTargetingMobile : ISpell
    {
        void Target(Mobile from);
    }

    public class SpellTargetMobile : Target, ISpellTarget
    {
        private readonly ISpellTargetingMobile m_Spell;

        public SpellTargetMobile(ISpellTargetingMobile spell, TargetFlags flags, int range = 12) :
            base(range, false, flags) => m_Spell = spell;

        public ISpell Spell => m_Spell;

        protected override void OnCantSeeTarget(Mobile from, object o)
        {
            from.SendLocalizedMessage(500237); // Target can not be seen.
        }

        protected override void OnTarget(Mobile from, object o)
        {
            if (o is Mobile m)
            {
                m_Spell.Target(m);
            }
        }

        protected override void OnTargetFinish(Mobile from)
        {
            m_Spell?.FinishSequence();
        }
    }
}
