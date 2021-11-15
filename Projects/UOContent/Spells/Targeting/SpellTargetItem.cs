using Server.Targeting;

namespace Server.Spells
{
    public interface ISpellTargetingItem : ISpell
    {
        void Target(Item item);
    }

    public class SpellTargetItem : Target, ISpellTarget
    {
        private readonly ISpellTargetingItem _spell;

        public SpellTargetItem(ISpellTargetingItem spell, TargetFlags flags = TargetFlags.None, int range = 12)
            : base(range, false, flags) => _spell = spell;

        public ISpell Spell => _spell;

        protected override void OnTarget(Mobile from, object o)
        {
            if (o is Item item)
            {
                _spell.Target(item);
            }
        }

        protected override void OnTargetFinish(Mobile from)
        {
            _spell?.FinishSequence();
        }
    }
}
