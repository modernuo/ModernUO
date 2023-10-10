using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;
using Server.Network;

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
            QuestObjective obj = qs.FindObjective<FindCrystalCaveObjective>();

            if (obj?.Completed == false)
            {
                obj.Complete();
            }
        }
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        base.OnMovement(m, oldLocation);

        if (InRange(m.Location, 2) && !InRange(oldLocation, 2) && m is PlayerMobile pm)
        {
            var qs = pm.Quest;

            if (qs is DarkTidesQuest)
            {
                QuestObjective obj = qs.FindObjective<ReturnToCrystalCaveObjective>();

                if (obj?.Completed == false)
                {
                    obj.Complete();
                }
                else
                {
                    obj = qs.FindObjective<FindHorusAboutRewardObjective>();

                    if (obj?.Completed == false)
                    {
                        var cont = GetNewContainer();

                        cont.DropItem(new Gold(500));

                        BaseJewel jewel = new GoldBracelet();
                        if (Core.AOS)
                        {
                            BaseRunicTool.ApplyAttributesTo(jewel, 3, 20, 40);
                        }

                        cont.DropItem(jewel);

                        if (!pm.PlaceInBackpack(cont))
                        {
                            cont.Delete();
                            // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                            pm.SendLocalizedMessage(1046260);
                        }
                        else
                        {
                            obj.Complete();
                        }
                    }
                }
            }
        }
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);

        if (from.Alive)
        {
            if (from is PlayerMobile pm)
            {
                var qs = pm.Quest;

                if (qs is DarkTidesQuest)
                {
                    QuestObjective obj = qs.FindObjective<SpeakCavePasswordObjective>();
                    var enabled = obj?.Completed == false;

                    list.Add(new SpeakPasswordEntry(this, pm, enabled));
                }
            }
        }
    }

    public virtual void OnPasswordSpoken(PlayerMobile from)
    {
        var qs = from.Quest;

        if (qs is DarkTidesQuest)
        {
            QuestObjective obj = qs.FindObjective<SpeakCavePasswordObjective>();

            if (obj?.Completed == false)
            {
                obj.Complete();
                return;
            }
        }

        from.SendLocalizedMessage(1060185); // Horus ignores you.
    }

    private class SpeakPasswordEntry : ContextMenuEntry
    {
        private readonly PlayerMobile _from;
        private readonly Horus _horus;

        public SpeakPasswordEntry(Horus horus, PlayerMobile from, bool enabled) : base(6193, 3)
        {
            _horus = horus;
            _from = from;

            if (!enabled)
            {
                Flags |= CMEFlags.Disabled;
            }
        }

        public override void OnClick()
        {
            if (_from.Alive)
            {
                _horus.OnPasswordSpoken(_from);
            }
        }
    }
}
