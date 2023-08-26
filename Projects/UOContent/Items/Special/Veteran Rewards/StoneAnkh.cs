using ModernUO.Serialization;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class StoneAnkhComponent : AddonComponent
{
    public StoneAnkhComponent(int itemID) : base(itemID) => Weight = 1.0;

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Addon is StoneAnkh { IsRewardItem: true })
        {
            list.Add(1076221); // 5th Year Veteran Reward
        }
    }
}

[SerializationGenerator(0)]
public partial class StoneAnkh : BaseAddon, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public StoneAnkh(bool east = true)
    {
        if (east)
        {
            AddComponent(new StoneAnkhComponent(0x2), 0, 0, 0);
            AddComponent(new StoneAnkhComponent(0x3), 0, -1, 0);
        }
        else
        {
            AddComponent(new StoneAnkhComponent(0x5), 0, 0, 0);
            AddComponent(new StoneAnkhComponent(0x4), -1, 0, 0);
        }
    }

    public override BaseAddonDeed Deed =>
        new StoneAnkhDeed
        {
            IsRewardItem = _isRewardItem
        };

    public override void OnChop(Mobile from)
    {
        from.SendLocalizedMessage(500489); // You can't use an axe on that.
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Core.ML && _isRewardItem)
        {
            list.Add(1076221); // 5th Year Veteran Reward
        }
    }

    public override void OnComponentUsed(AddonComponent c, Mobile from)
    {
        if (from.InRange(Location, 2))
        {
            var house = BaseHouse.FindHouseAt(this);

            if (house?.IsOwner(from) == true)
            {
                from.CloseGump<RewardDemolitionGump>();
                from.SendGump(new RewardDemolitionGump(this, 1049783)); // Do you wish to re-deed this decoration?
            }
            else
            {
                // You can only re-deed this decoration if you are the house owner or originally placed the decoration.
                from.SendLocalizedMessage(1049784);
            }
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }
}

[SerializationGenerator(0)]
public partial class StoneAnkhDeed : BaseAddonDeed, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    private bool _east;

    [Constructible]
    public StoneAnkhDeed() => LootType = LootType.Blessed;

    public override int LabelNumber => 1049773; // deed for a stone ankh

    public override BaseAddon Addon =>
        new StoneAnkh(_east)
        {
            IsRewardItem = _isRewardItem
        };

    public override void OnDoubleClick(Mobile from)
    {
        if (_isRewardItem && !RewardSystem.CheckIsUsableBy(from, this))
        {
            return;
        }

        if (IsChildOf(from.Backpack))
        {
            from.CloseGump<InternalGump>();
            from.SendGump(new InternalGump(this));
        }
        else
        {
            from.SendLocalizedMessage(1042038); // You must have the object in your backpack to use it.
        }
    }

    private void SendTarget(Mobile m)
    {
        base.OnDoubleClick(m);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
        {
            list.Add(1076221); // 5th Year Veteran Reward
        }
    }

    private class InternalGump : Gump
    {
        private readonly StoneAnkhDeed _deed;

        public InternalGump(StoneAnkhDeed deed) : base(150, 50)
        {
            _deed = deed;

            Closable = true;
            Disposable = true;
            Draggable = true;
            Resizable = false;

            AddPage(0);

            AddBackground(0, 0, 300, 150, 0xA28);

            AddItem(90, 30, 0x4);
            AddItem(112, 30, 0x5);
            AddButton(50, 35, 0x867, 0x869, (int)Buttons.South); // South

            AddItem(170, 30, 0x2);
            AddItem(192, 30, 0x3);
            AddButton(145, 35, 0x867, 0x869, (int)Buttons.East); // East
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (_deed?.Deleted != false || info.ButtonID == (int)Buttons.Cancel)
            {
                return;
            }

            _deed._east = info.ButtonID == (int)Buttons.East;
            _deed.SendTarget(sender.Mobile);
        }

        private enum Buttons
        {
            Cancel,
            South,
            East
        }
    }
}
