using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Doom;

[SerializationGenerator(0, false)]
public partial class Victoria : BaseQuester
{
    private const int AltarRange = 24;

    private SummoningAltar _altar;

    [Constructible]
    public Victoria() : base("the Sorceress")
    {
    }

    public override int TalkNumber => 6159; // Ask about Chyloth
    public override string DefaultName => "Victoria";
    public override bool ClickTitle => true;
    public override bool IsActiveVendor => true;
    public override bool DisallowAllMoves => false;

    public SummoningAltar Altar
    {
        get
        {
            if (_altar?.Deleted != false || _altar.Map != Map ||
                !Utility.InRange(_altar.Location, Location, AltarRange))
            {
                foreach (var item in GetItemsInRange(AltarRange))
                {
                    if (item is SummoningAltar altar)
                    {
                        _altar = altar;
                        break;
                    }
                }
            }

            return _altar;
        }
    }

    public override void InitSBInfo()
    {
        _sbInfos.Add(new SBMage());
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Female = true;
        Hue = 0x8835;
        Body = 0x191;
    }

    public override void InitOutfit()
    {
        EquipItem(new GrandGrimoire());

        EquipItem(SetHue(new Sandals(), 0x455));
        EquipItem(SetHue(new SkullCap(), 0x455));
        EquipItem(SetHue(new PlainDress(), 0x455));

        HairItemID = 0x203C;
        HairHue = 0x482;
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (from is not PlayerMobile player)
        {
            return base.OnDragDrop(from, dropped);
        }

        var qs = player.Quest;

        if (qs is not TheSummoningQuest)
        {
            return base.OnDragDrop(from, dropped);
        }

        if (dropped is not DaemonBone bones)
        {
            return base.OnDragDrop(from, dropped);
        }

        QuestObjective obj = qs.FindObjective<CollectBonesObjective>();

        if (obj?.Completed == false)
        {
            var need = obj.MaxProgress - obj.CurProgress;

            if (bones.Amount < need)
            {
                obj.CurProgress += bones.Amount;
                bones.Delete();

                qs.ShowQuestLogUpdated();
            }
            else
            {
                obj.Complete();
                bones.Consume(need);

                if (!bones.Deleted)
                {
                    // You have already given me all the Daemon bones necessary to weave the spell.  Keep these for a later time.
                    SayTo(from, 1050038);
                }
            }
        }
        else
        {
            // TODO: Accurate?
            // You have already given me all the Daemon bones necessary to weave the spell.  Keep these for a later time.
            SayTo(from, 1050038);
        }

        return false;
    }

    public override bool CanTalkTo(PlayerMobile to) =>
        to.Quest == null && QuestSystem.CanOfferQuest(to, typeof(TheSummoningQuest));

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
        var qs = player.Quest;

        if (qs == null && QuestSystem.CanOfferQuest(player, typeof(TheSummoningQuest)))
        {
            Direction = GetDirectionTo(player);
            new TheSummoningQuest(this, player).SendOffer();
        }
    }
}
