using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Necro;

[SerializationGenerator(0, false)]
public partial class Horus : BaseQuester
{
    [Constructible]
    public Horus() : base("the Guardian")
    {
    }

    public override string DefaultName => "Horus";

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = 0x83F3;
        Body = 0x190;
    }

    public override void InitOutfit()
    {
        AddItem(SetHue(new PlateLegs(), 0x849));
        AddItem(SetHue(new PlateChest(), 0x849));
        AddItem(SetHue(new PlateArms(), 0x849));
        AddItem(SetHue(new PlateGloves(), 0x849));
        AddItem(SetHue(new PlateGorget(), 0x849));

        AddItem(SetHue(new Bardiche(), 0x482));

        AddItem(SetHue(new Boots(), 0x001));
        AddItem(SetHue(new Cloak(), 0x482));

        Utility.AssignRandomHair(this, false);
        Utility.AssignRandomFacialHair(this, false);
    }

    public override int GetAutoTalkRange(PlayerMobile m) => 3;

    public override bool CanTalkTo(PlayerMobile to)
    {
        var qs = to.Quest;

        return qs is DarkTidesQuest && qs.IsObjectiveInProgress(typeof(FindCrystalCaveObjective));
    }

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
        var qs = player.Quest;

        if (qs is DarkTidesQuest)
        {
            var obj = qs.FindObjective<FindCrystalCaveObjective>();

            if (obj?.Completed == false)
            {
                obj.Complete();
            }
        }
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        base.OnMovement(m, oldLocation);

        if (!InRange(m.Location, 2) || InRange(oldLocation, 2) || m is not PlayerMobile pm)
        {
            return;
        }

        var qs = pm.Quest;

        if (qs is not DarkTidesQuest)
        {
            return;
        }

        if (qs.FindObjective<ReturnToCrystalCaveObjective>() is { Completed: false } obj1)
        {
            obj1.Complete();
            return;
        }

        if (qs.FindObjective<FindHorusAboutRewardObjective>() is { Completed: false } obj2)
        {
            var cont = GetNewContainer();

            cont.DropItem(new Gold(500));

            BaseJewel jewel = new GoldBracelet();
            if (Core.AOS)
            {
                BaseRunicTool.ApplyAttributesTo(jewel, 3, 20, 40);
            }

            cont.DropItem(jewel);

            if (pm.PlaceInBackpack(cont))
            {
                obj2.Complete();
            }
            else
            {
                cont.Delete();
                // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                pm.SendLocalizedMessage(1046260);
            }
        }
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (!from.Alive || from is not PlayerMobile pm)
        {
            return;
        }

        var qs = pm.Quest;

        if (qs is DarkTidesQuest)
        {
            var obj = qs.FindObjective<SpeakCavePasswordObjective>();
            var enabled = obj?.Completed == false;

            list.Add(new SpeakPasswordEntry(enabled));
        }
    }

    public virtual void OnPasswordSpoken(PlayerMobile from)
    {
        var obj = (from.Quest as DarkTidesQuest)?.FindObjective<SpeakCavePasswordObjective>();

        if (obj?.Completed == false)
        {
            obj.Complete();
            return;
        }

        from.SendLocalizedMessage(1060185); // Horus ignores you.
    }

    private class SpeakPasswordEntry : ContextMenuEntry
    {
        public SpeakPasswordEntry(bool enabled) : base(6193, 3) => Enabled = enabled;

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from.Alive && from is PlayerMobile pm && target is Horus horus)
            {
                horus.OnPasswordSpoken(pm);
            }
        }
    }
}
