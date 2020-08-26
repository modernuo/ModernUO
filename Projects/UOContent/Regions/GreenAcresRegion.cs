using System.Text.Json;
using Server.Json;
using Server.Spells;
using Server.Spells.Sixth;

namespace Server.Regions
{
  public class GreenAcresRegion : BaseRegion
  {
    public GreenAcresRegion(DynamicJson json, JsonSerializerOptions options) : base(json, options)
    {
    }

    public override bool AllowHousing(Mobile from, Point3D p) =>
      from.AccessLevel != AccessLevel.Player && base.AllowHousing(from, p);

    public override bool CheckTravel(Mobile m, Point3D newLocation, TravelCheckType travelType) =>
      m.AccessLevel != AccessLevel.Player;

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
}
