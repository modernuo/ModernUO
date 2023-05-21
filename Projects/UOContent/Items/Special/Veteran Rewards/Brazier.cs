using ModernUO.Serialization;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class RewardBrazier : Item, IRewardItem
{
    private static readonly int[] _art = { 0x19AA, 0x19BB };

    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [SerializableField(1, getter: "private", setter: "private")]
    private Item _fire;

    [Constructible]
    public RewardBrazier() : this(_art.RandomElement())
    {
    }

    [Constructible]
    public RewardBrazier(int itemID) : base(itemID)
    {
        LootType = LootType.Blessed;
        Weight = 10.0;
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public override void OnDelete()
    {
        TurnOff();

        base.OnDelete();
    }

    public void TurnOff()
    {
        if (_fire != null)
        {
            _fire.Delete();
            Fire = null;
        }
    }

    public void TurnOn()
    {
        Fire ??= new Item();

        _fire.ItemID = 0x19AB;
        _fire.Movable = false;
        _fire.MoveToWorld(new Point3D(X, Y, Z + ItemData.Height + 2), Map);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        if (!IsLockedDown)
        {
            from.SendLocalizedMessage(502692); // This must be in a house and be locked down to work.
            return;
        }

        var house = BaseHouse.FindHouseAt(from);

        if (house?.IsCoOwner(from) != true)
        {
            from.SendLocalizedMessage(502436); // That is not accessible.
            return;
        }

        if (_fire != null)
        {
            TurnOff();
        }
        else
        {
            TurnOn();
        }
    }

    public override void OnLocationChange(Point3D old)
    {
        _fire?.MoveToWorld(new Point3D(X, Y, Z + ItemData.Height), Map);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
        {
            list.Add(1076222); // 6th Year Veteran Reward
        }
    }
}

[SerializationGenerator(0)]
public partial class RewardBrazierDeed : Item, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public RewardBrazierDeed() : base(0x14F0)
    {
        LootType = LootType.Blessed;
        Weight = 1.0;
    }

    public override int LabelNumber => 1080527; // Brazier Deed

    public override void OnDoubleClick(Mobile from)
    {
        if (_isRewardItem && !RewardSystem.CheckIsUsableBy(from, this))
        {
            return;
        }

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042038); // You must have the object in your backpack to use it.
            return;
        }

        from.CloseGump<InternalGump>();
        from.SendGump(new InternalGump(this));
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
        {
            list.Add(1076222); // 6th Year Veteran Reward
        }
    }

    private class InternalGump : Gump
    {
        private readonly RewardBrazierDeed _brazier;

        public InternalGump(RewardBrazierDeed brazier) : base(100, 200)
        {
            _brazier = brazier;

            Closable = true;
            Disposable = true;
            Draggable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(0, 0, 200, 200, 2600);

            AddPage(1);
            AddLabel(45, 15, 0, "Choose a Brazier:");

            AddItem(40, 75, 0x19AA);
            AddButton(55, 50, 0x845, 0x846, 0x19AA);

            AddItem(100, 75, 0x19BB);
            AddButton(115, 50, 0x845, 0x846, 0x19BB);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (_brazier?.Deleted != false)
            {
                return;
            }

            var m = sender.Mobile;

            if (info.ButtonID != 0x19AA && info.ButtonID != 0x19BB)
            {
                return;
            }

            var brazier = new RewardBrazier(info.ButtonID) { IsRewardItem = _brazier.IsRewardItem };

            if (!m.PlaceInBackpack(brazier))
            {
                brazier.Delete();
                m.SendLocalizedMessage(1078837); // Your backpack is full! Please make room and try again.
            }
            else
            {
                _brazier.Delete();
            }
        }
    }
}
