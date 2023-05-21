using ModernUO.Serialization;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class RewardPottedCactus : Item, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public RewardPottedCactus() : this(Utility.RandomMinMax(0x1E0F, 0x1E14))
    {
    }

    [Constructible]
    public RewardPottedCactus(int itemID) : base(itemID) => Weight = 5.0;

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
}

[SerializationGenerator(0)]
public partial class PottedCactusDeed : Item, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public PottedCactusDeed() : base(0x14F0)
    {
        LootType = LootType.Blessed;
        Weight = 1.0;
    }

    public override int LabelNumber => 1080407; // Potted Cactus Deed

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

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
        {
            list.Add(1076219); // 3rd Year Veteran Reward
        }
    }

    private class InternalGump : Gump
    {
        private readonly PottedCactusDeed _deed;

        public InternalGump(PottedCactusDeed cactus) : base(100, 200)
        {
            _deed = cactus;

            Closable = true;
            Disposable = true;
            Draggable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(0, 0, 425, 250, 0xA28);

            AddPage(1);
            AddLabel(45, 15, 0, "Choose a Potted Cactus:");

            AddItem(45, 75, 0x1E0F);
            AddButton(55, 50, 0x845, 0x846, 0x1E0F);

            AddItem(105, 75, 0x1E10);
            AddButton(115, 50, 0x845, 0x846, 0x1E10);

            AddItem(160, 75, 0x1E14);
            AddButton(175, 50, 0x845, 0x846, 0x1E14);

            AddItem(220, 75, 0x1E11);
            AddButton(235, 50, 0x845, 0x846, 0x1E11);

            AddItem(280, 75, 0x1E12);
            AddButton(295, 50, 0x845, 0x846, 0x1E12);

            AddItem(340, 75, 0x1E13);
            AddButton(355, 50, 0x845, 0x846, 0x1E13);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (_deed?.Deleted != false || info.ButtonID is < 0x1E0F or > 0x1E14)
            {
                return;
            }

            var cactus = new RewardPottedCactus(info.ButtonID)
            {
                IsRewardItem = _deed.IsRewardItem
            };

            if (!sender.Mobile.PlaceInBackpack(cactus))
            {
                cactus.Delete();
                sender.Mobile.SendLocalizedMessage(1078837); // Your backpack is full! Please make room and try again.
            }
            else
            {
                _deed.Delete();
            }
        }
    }
}
