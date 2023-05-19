using System.Text.Json.Serialization;
using Server.Spells;
using Server.Spells.Sixth;

namespace Server.Regions;

public class GreenAcresRegion : BaseRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public GreenAcresRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public GreenAcresRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public GreenAcresRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    public override bool AllowHousing(Mobile from, Point3D p) =>
        from.AccessLevel != AccessLevel.Player && base.AllowHousing(from, p);

    public override bool CheckTravel(Mobile m, Point3D newLocation, TravelCheckType travelType, out TextDefinition message)
    {
        message = null; // Use default message
        return m.AccessLevel != AccessLevel.Player;
    }

    public override bool OnBeginSpellCast(Mobile m, ISpell s)
    {
        if (m.AccessLevel == AccessLevel.Player && s is MarkSpell)
        {
            m.SendLocalizedMessage(501802); // Thy spell doth not appear to work...
            return false;
        }

        return base.OnBeginSpellCast(m, s);
    }
}
