using ModernUO.Serialization;
using Server.Engines.VeteranRewards;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class ContestMiniHouse : MiniHouseAddon
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public ContestMiniHouse() : base(MiniHouseType.MalasMountainPass)
    {
    }

    [Constructible]
    public ContestMiniHouse(MiniHouseType type) : base(type)
    {
    }

    public override BaseAddonDeed Deed =>
        new ContestMiniHouseDeed(Type)
        {
            IsRewardItem = _isRewardItem
        };
}

[SerializationGenerator(0)]
public partial class ContestMiniHouseDeed : MiniHouseDeed, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public ContestMiniHouseDeed() : base(MiniHouseType.MalasMountainPass)
    {
    }

    [Constructible]
    public ContestMiniHouseDeed(MiniHouseType type) : base(type)
    {
    }

    public override BaseAddon Addon =>
        new ContestMiniHouse(Type)
        {
            IsRewardItem = _isRewardItem
        };

    public override void OnDoubleClick(Mobile from)
    {
        if (_isRewardItem && !RewardSystem.CheckIsUsableBy(from, this, new object[] { Type }))
        {
            return;
        }

        base.OnDoubleClick(from);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Core.ML && _isRewardItem)
        {
            list.Add(1076217); // 1st Year Veteran Reward
        }
    }
}
