using System.Text.Json;
using Server.Json;
using Server.Spells;

namespace Server.Regions
{
  public class JailRegion : BaseRegion
  {
    public JailRegion(DynamicJson json, JsonSerializerOptions options) : base(json, options)
    {
    }

    public override bool AllowBeneficial(Mobile from, Mobile target)
    {
      if (from.AccessLevel == AccessLevel.Player)
      {
        from.SendMessage("You may not do that in jail.");
        return false;
      }

      return true;
    }

    public override bool AllowHarmful(Mobile from, Mobile target)
    {
      if (from.AccessLevel == AccessLevel.Player)
      {
        from.SendMessage("You may not do that in jail.");
        return false;
      }

      return true;
    }

    public override bool AllowHousing(Mobile from, Point3D p) => false;

    public override void AlterLightLevel(Mobile m, ref int global, ref int personal)
    {
      global = LightCycle.JailLevel;
    }

    public override bool CheckTravel(Mobile m, Point3D newLocation, TravelCheckType travelType)
    {
      if (m?.AccessLevel == AccessLevel.Player)
      {
        m.SendLocalizedMessage(1114345); // You'll need a better jailbreak plan than that!
        return false;
      }

      return base.CheckTravel(m, newLocation, travelType);
    }

    public override bool OnBeginSpellCast(Mobile from, ISpell s)
    {
      if (from.AccessLevel == AccessLevel.Player)
      {
        from.SendLocalizedMessage(502629); // You cannot cast spells here.
        return false;
      }

      return true;
    }

    public override bool OnSkillUse(Mobile from, int Skill)
    {
      if (from.AccessLevel == AccessLevel.Player)
      {
        from.SendMessage("You may not use skills in jail.");
        return false;
      }

      return true;
    }

    public override bool OnCombatantChange(Mobile from, Mobile Old, Mobile New) => from.AccessLevel > AccessLevel.Player;
  }
}
