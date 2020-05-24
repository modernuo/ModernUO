using System.Text.Json;
using Server.Network;
using Server.Spells;
using Server.Spells.Ninjitsu;
using Server.Json;

namespace Server.Regions
{
  public class TwistedWealdDesertRegion : MondainRegion
  {
    public TwistedWealdDesertRegion(DynamicJson json, JsonSerializerOptions options) : base(json, options)
    {
    }

    public static void Initialize()
    {
      EventSink.Login += Desert_OnLogin;
    }

    public override void OnEnter(Mobile m)
    {
      NetState ns = m.NetState;
      if (ns != null && !TransformationSpellHelper.UnderTransformation(m, typeof(AnimalForm)) &&
          m.AccessLevel < AccessLevel.GameMaster)
        ns.Send(SpeedControl.WalkSpeed);
    }

    public override void OnExit(Mobile m)
    {
      NetState ns = m.NetState;
      if (ns != null && !TransformationSpellHelper.UnderTransformation(m, typeof(AnimalForm)))
        ns.Send(SpeedControl.Disable);
    }

    private static void Desert_OnLogin(Mobile m)
    {
      if (m.Region.IsPartOf<TwistedWealdDesertRegion>() && m.AccessLevel < AccessLevel.GameMaster)
        m.NetState.Send(SpeedControl.WalkSpeed);
    }
  }
}
