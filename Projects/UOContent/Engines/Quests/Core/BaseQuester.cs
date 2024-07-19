using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests;

public class TalkEntry : ContextMenuEntry
{
    public TalkEntry(int talkNumber) : base(talkNumber)
    {
    }

    public override void OnClick(Mobile from, IEntity target)
    {
        if (from.CheckAlive() && from is PlayerMobile mobile && target is BaseQuester quester && quester.CanTalkTo(mobile))
        {
            quester.OnTalk(mobile, true);
        }
    }
}

[SerializationGenerator(0, false)]
public abstract partial class BaseQuester : BaseVendor
{
    protected List<SBInfo> _sbInfos = new();

    public BaseQuester(string title = null) : base(title)
    {
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override bool IsActiveVendor => false;
    public override bool IsInvulnerable => true;
    public override bool DisallowAllMoves => true;
    public override bool ClickTitle => false;
    public override bool CanTeach => false;

    public virtual int TalkNumber => 6146; // Talk

    public override void InitSBInfo()
    {
    }

    public abstract void OnTalk(PlayerMobile player, bool contextMenu);

    public virtual bool CanTalkTo(PlayerMobile to) => true;

    public virtual int GetAutoTalkRange(PlayerMobile m) => -1;

    public override bool CanBeDamaged() => false;

    protected Item SetHue(Item item, int hue)
    {
        item.Hue = hue;
        return item;
    }

    public override void AddCustomContextEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.AddCustomContextEntries(from, ref list);

        if (from.Alive && from is PlayerMobile mobile && TalkNumber > 0 && CanTalkTo(mobile))
        {
            list.Add(new TalkEntry(TalkNumber));
        }
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (m.Alive && m is PlayerMobile pm)
        {
            var range = GetAutoTalkRange(pm);

            if (pm.Alive && range >= 0 && InRange(m, range) && !InRange(oldLocation, range) && CanTalkTo(pm))
            {
                OnTalk(pm, false);
            }
        }
    }

    public void FocusTo(Mobile to)
    {
        QuestSystem.FocusTo(this, to);
    }

    public static Container GetNewContainer() => new Bag { Hue = QuestSystem.RandomBrightHue() };
}
