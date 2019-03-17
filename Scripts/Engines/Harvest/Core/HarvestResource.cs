using System;

namespace Server.Engines.Harvest
{
  public class HarvestResource
  {
    public HarvestResource(int reqSkill, int minSkill, int maxSkill, object message, params Type[] types)
    {
      ReqSkill = reqSkill;
      MinSkill = minSkill;
      MaxSkill = maxSkill;
      Types = types;
      SuccessMessage = message;
    }

    public Type[] Types{ get; set; }

    public int ReqSkill{ get; set; }

    public int MinSkill{ get; set; }

    public int MaxSkill{ get; set; }

    public object SuccessMessage{ get; }

    public void SendSuccessTo(Mobile m)
    {
      if (SuccessMessage is int messageInt)
        m.SendLocalizedMessage(messageInt);
      else
        m.SendMessage(SuccessMessage.ToString());
    }
  }
}
