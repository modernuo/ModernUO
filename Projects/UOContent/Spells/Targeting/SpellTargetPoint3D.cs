using Server.Targeting;

namespace Server.Spells
{
    public interface ISpellTargetingPoint3D : ISpell
    {
        void Target(IPoint3D p);
    }

    public class SpellTargetPoint3D : Target, ISpellTarget
    {
        private readonly bool _checkLOS;
        private ISpellTargetingPoint3D _spell;

        public SpellTargetPoint3D(
            ISpellTargetingPoint3D spell, TargetFlags flags = TargetFlags.None, int range = 12, bool checkLOS = false
        ) : base(range, true, flags)
        {
            _spell = spell;
            _checkLOS = checkLOS;
        }

        public ISpell Spell => _spell;

        protected override void OnTarget(Mobile from, object o)
        {
            if (o is IPoint3D p)
            {
                _spell.Target(p);
            }
        }

        protected override void OnTargetOutOfLOS(Mobile from, object o)
        {
            if (!_checkLOS)
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
