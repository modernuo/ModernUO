namespace Server.Spells;

public interface ISpellTarget<in T> where T : IPoint3D
{
    ITargetingSpell<T> Spell { get; }
}
