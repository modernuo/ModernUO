using Server.Spells.Sixth;

namespace Server.Regions;

public class MondainRegion : NoTravelSpellsAllowedRegion
{
    public MondainRegion(string name, Map map, Region parent, params Rectangle3D[] area)
        : base(name, map, parent, area)
    {
    }

    public MondainRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    public override bool OnBeginSpellCast(Mobile m, ISpell s)
    {
        if (m.Player && s is MarkSpell)
        {
            m.SendLocalizedMessage(501802); // Thy spell doth not appear to work...
            return false;
        }

        return base.OnBeginSpellCast(m, s);
    }
}
