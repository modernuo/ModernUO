namespace Server.Spells;

public interface IRangedSpell
{
    Mobile Caster { get; }

    // https://uo.com/wiki/ultima-online-wiki/technical/previous-publishes/1999-2/1999-06-14th-april/
    int TargetRange => Core.T2A ? 10 : 12;
    void FinishSequence();
}

public interface ITargetingSpell<in T> : IRangedSpell where T : IPoint3D
{
    void Target(T target);
}
