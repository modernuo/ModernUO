using Server.Targeting;

namespace Server.Spells
{
    public interface ISpellTargetingPoint3D : ISpell
    {
        void Target(IPoint3D p);
    }

    public class SpellTargetPoint3D : Target, ISpellTarget
    {
        private readonly bool _retryOnLos;
        private ISpellTargetingPoint3D _spell;

        public SpellTargetPoint3D(
            ISpellTargetingPoint3D spell, TargetFlags flags = TargetFlags.None, int range = 12, bool retryOnLOS = false
        ) : base(range, true, flags)
        {
            _spell = spell;
            _retryOnLos = retryOnLOS;
        }

        public ISpell Spell => _spell;

        protected override void OnTarget(Mobile from, object o)
        {
            _spell.Target(o as IPoint3D);
        }

        protected override void OnCantSeeTarget(Mobile from, object o)
        {
            from.SendLocalizedMessage(500237); // Target can not be seen.
        }

        protected override void OnTargetOutOfLOS(Mobile from, object o)
        {
            if (!_retryOnLos)
            {
                return;
            }

            from.SendLocalizedMessage(501943); // Target cannot be seen. Try again.
            from.Target = new SpellTargetPoint3D(_spell);
            from.Target.BeginTimeout(from, TimeoutTime - Core.TickCount);
            _spell = null; // Needed?
        }

        protected override void OnTargetFinish(Mobile from)
        {
            _spell?.FinishSequence();
        }
    }
}
