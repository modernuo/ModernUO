using System.Text.Json;
using Server.Json;
using Server.Spells.Chivalry;
using Server.Spells.Fourth;
using Server.Spells.Seventh;
using Server.Spells.Sixth;

namespace Server.Regions
{
  public class GreenAcres : BaseRegion
  {
    public GreenAcres(DynamicJson json, JsonSerializerOptions options) : base(json, options)
    {
    }

    public override bool AllowHousing(Mobile from, Point3D p) => from.AccessLevel != AccessLevel.Player && base.AllowHousing(@from, p);

    public override bool OnBeginSpellCast(Mobile m, ISpell s)
    {
      if ((s is GateTravelSpell || s is RecallSpell || s is MarkSpell || s is SacredJourneySpell) &&
          m.AccessLevel == AccessLevel.Player)
      {
        m.SendMessage("You cannot cast that spell here.");
        return false;
      }

      return base.OnBeginSpellCast(m, s);
    }
  }
}
