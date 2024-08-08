namespace Server.Spells;

public interface IRecallSpell : IRangedSpell
{
    void Effect(Point3D loc, Map map, bool checkMulti);
}
