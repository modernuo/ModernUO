namespace Server.Spells;

public interface IRangedSpell
{
    Mobile Caster { get; }

    int TargetRange => Core.ML ? 10 : 12;
    void FinishSequence();
}

public interface ITargetingSpell<in T> : IRangedSpell where T : IPoint3D
{
    void Target(T target);
}
