using System;
using System.Xml;
using Server.Mobiles;
using Server.Regions;
using Server.Utilities;

namespace Server.Engines.Quests
{
  public class QuestOfferRegion : BaseRegion
  {
    private Type m_Quest;

    public QuestOfferRegion(XmlElement xml, Map map, Region parent) : base(xml, map, parent)
    {
      ReadType(xml["quest"], "type", ref m_Quest);
    }

    public Type Quest => m_Quest;

    public override void OnEnter(Mobile m)
    {
      base.OnEnter(m);

      if (m_Quest == null)
        return;

      if (m is PlayerMobile player && player.Quest == null && QuestSystem.CanOfferQuest(m, m_Quest))
        try
        {
          QuestSystem qs = (QuestSystem)ActivatorUtil.CreateInstance(m_Quest, player);
          qs.SendOffer();
        }
        catch (Exception ex)
        {
          Console.WriteLine("Error creating quest {0}: {1}", m_Quest, ex);
        }
    }
  }
}
