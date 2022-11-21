using System;
using System.Xml;
using Server;
using Server.Logging;
using Server.Regions;
using Server.Mobiles;
using Server.Utilities;

namespace Server.Engines.Quests;

public class QuestOfferRegion : BaseRegion
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(QuestOfferRegion));

    public QuestOfferRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }

    public QuestOfferRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public Type Quest { get; set; }

    public override void OnEnter(Mobile m)
    {
        base.OnEnter(m);

        if (Quest == null)
        {
            return;
        }

        if (m is PlayerMobile player && player.Quest == null && QuestSystem.CanOfferQuest(m, Quest))
        {
            try
            {
                QuestSystem qs = Quest.CreateInstance<QuestSystem>(player);
                qs.SendOffer();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error creating quest {Quest}", Quest);
            }
        }
    }
}
