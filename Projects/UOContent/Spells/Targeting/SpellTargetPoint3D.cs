using Server.Targeting;

namespace Server.Spells;

public class SpellTargetPoint3D : SpellTarget<IPoint3D>
{
    private readonly bool _retryOnLos;

    public SpellTargetPoint3D(
        ITargetingSpell<IPoint3D> spell, TargetFlags flags = TargetFlags.None, bool retryOnLOS = false
    ) : base(spell, true, flags) => _retryOnLos = retryOnLOS;

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
    }
}
