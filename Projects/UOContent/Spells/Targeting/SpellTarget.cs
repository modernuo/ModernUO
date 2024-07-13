using Server.Targeting;

namespace Server.Spells;

public class SpellTarget<T> : Target, ISpellTarget<T> where T : class, IPoint3D
{
    private static readonly bool _canTargetStatic = typeof(T).IsAssignableFrom(typeof(StaticTarget));
    private static readonly bool _canTargetMobile = typeof(T).IsAssignableFrom(typeof(Mobile));
    private static readonly bool _canTargetItem = typeof(T).IsAssignableFrom(typeof(Item));

    protected readonly ITargetingSpell<T> _spell;

    public SpellTarget(ITargetingSpell<T> spell, TargetFlags flags = TargetFlags.None) : this(spell, false, flags)
    {
    }

    public SpellTarget(ITargetingSpell<T> spell, bool allowGround, TargetFlags flags = TargetFlags.None)
        : base(spell.TargetRange, allowGround, flags) => _spell = spell;

    public ITargetingSpell<T> Spell => _spell;

    protected override bool CanTarget(Mobile from, StaticTarget staticTarget, ref Point3D loc, ref Map map)
        => _canTargetStatic;

    protected override bool CanTarget(Mobile from, Mobile mobile, ref Point3D loc, ref Map map) => _canTargetMobile;

    protected override bool CanTarget(Mobile from, Item item, ref Point3D loc, ref Map map) => _canTargetItem;

    protected override void OnCantSeeTarget(Mobile from, object o)
    {
        from.SendLocalizedMessage(500237); // Target can not be seen.
    }

    protected override void OnTarget(Mobile from, object o) => _spell.Target(o as T);

    protected override void OnTargetFinish(Mobile from) => _spell?.FinishSequence();
}
