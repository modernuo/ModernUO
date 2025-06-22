using Server.Logging;
using Server.Targeting;

namespace Server.Spells;

public class SpellTarget<T> : Target, ISpellTarget<T> where T : class, IPoint3D
{
    private static readonly bool _canTargetStatic = typeof(T).IsAssignableFrom(typeof(StaticTarget));
    private static readonly bool _canTargetMobile = typeof(T).IsAssignableFrom(typeof(Mobile));
    private static readonly bool _canTargetItem = typeof(T).IsAssignableFrom(typeof(Item));

    protected static readonly ILogger logger = LogFactory.GetLogger(typeof(SpellTarget<T>));

    private readonly bool _retryOnLos;
    protected readonly ITargetingSpell<T> _spell;

    private T _chosenTarget;

    public SpellTarget(
        ITargetingSpell<T> spell,
        TargetFlags flags,
        bool retryOnLos = false
    ) : this(spell, false, flags, retryOnLos)
    {
    }

    public SpellTarget(
        ITargetingSpell<T> spell,
        bool allowGround = false,
        TargetFlags flags = TargetFlags.None,
        bool retryOnLos = false
    ) : base(spell.TargetRange, allowGround, flags)
    {
        _spell = spell;
        _retryOnLos = retryOnLos;
    }

    public ITargetingSpell<T> Spell => _spell;

    protected override bool CanTarget(Mobile from, StaticTarget staticTarget, ref Point3D loc, ref Map map)
        => base.CanTarget(from, staticTarget, ref loc, ref map) && _canTargetStatic;

    protected override bool CanTarget(Mobile from, Mobile mobile, ref Point3D loc, ref Map map) =>
        base.CanTarget(from, mobile, ref loc, ref map) && _canTargetMobile;

    protected override bool CanTarget(Mobile from, Item item, ref Point3D loc, ref Map map) =>
        base.CanTarget(from, item, ref loc, ref map) && _canTargetItem;

    protected override void OnCantSeeTarget(Mobile from, object o)
    {
        from.SendLocalizedMessage(500237); // Target can not be seen.
    }

    // When target is selected
    protected override void OnTarget(Mobile from, object o)
    {
        logger.Debug("Inside OnTarget method, target chosen");

        // Store the target and continue with casting
        _chosenTarget = o as T;
        if (_chosenTarget == null)
        {
            from.SendLocalizedMessage(501857); // This spell won't work on that!
            _spell?.FinishSequence();
            return;
        }

        // Successfully selected a target, continue with the spell
        if (_spell != null && _spell.State == SpellState.SelectingTarget)
        {
            _spell.ContinueCast();
        }
    }

    // Apply the spell effect to the chosen target
    public override void ApplySpellOnTarget()
    {
        var caster = _spell?.Caster;

        if (caster == null || _chosenTarget == null)
        {
            logger.Debug("Cannot apply spell: caster or target is null");
            _spell?.FinishSequence();
            return;
        }

        if (!caster.CanSee(_chosenTarget))
        {
            caster.SendLocalizedMessage(500237); // Target can not be seen.
            _spell?.FinishSequence();
            return;
        }

        if (!caster.InLOS(_chosenTarget))
        {
            caster.SendLocalizedMessage(500237); // Target can not be seen.
            _spell?.FinishSequence();
            return;
        }

        // Only apply if this target is still the active one
        if (caster.Player && caster.Target != this)
        {
            logger.Debug("Target changed, not applying spell effect");
            _spell?.FinishSequence();
            return;
        }

        // Apply the spell effect to the target
        _spell?.Target(_chosenTarget);
        _spell?.FinishSequence();
    }

    protected override void OnTargetOutOfLOS(Mobile from, object o)
    {
        from.SendLocalizedMessage(500237); // Target can not be seen.

        if (!_retryOnLos)
        {
            _spell?.FinishSequence();
            return;
        }

        // If retry on LOS is enabled, we could implement retry logic here
    }

    // Handle target cancellation
    protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
    {
        logger.Debug($"Target cancelled: {cancelType}");

        // Let the spell know it was cancelled
        if (_spell != null && _spell.State != SpellState.None)
        {
            _spell.FinishSequence();
        }
    }

    public override void CancelTimeout()
    {

    }

    public override void CancelSpellTargetTimeout()
    {
        m_TimeoutTimer?.Stop();
        m_TimeoutTimer = null;
    }



    // Cleanup in one place
    protected override void OnTargetFinish(Mobile from)
    {
        logger.Debug("OnTargetFinish called");

        // If we have a spell but no target was chosen, finish the spell sequence
        if (_chosenTarget == null && _spell != null && _spell.State == SpellState.SelectingTarget)
        {
            logger.Debug("No target chosen, finishing sequence");
            _spell.FinishSequence();
        }
    }
}
