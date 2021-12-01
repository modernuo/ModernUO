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

        protected override bool CanTarget(Mobile from, StaticTarget staticTarget, ref Point3D loc, ref Map map) => false;
        protected override bool CanTarget(Mobile from, Mobile mobile, ref Point3D loc, ref Map map) => false;

        protected override void OnCantSeeTarget(Mobile from, object o)
        {
            from.SendLocalizedMessage(500237); // Target can not be seen.
        }

        protected override void OnTarget(Mobile from, object o)
        {
            _spell.Target(o as Item);
        }

        protected override void OnTargetFinish(Mobile from)
        {
            _spell?.FinishSequence();
        }
    }
}
