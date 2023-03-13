using ModernUO.Serialization;
using Server.Engines.VeteranRewards;

namespace Server.Items;

[Furniture]
[SerializationGenerator(0)]
public partial class CommodityDeedBox : BaseContainer, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public CommodityDeedBox() : base(0x9AA)
    {
        Hue = 0x47;
        Weight = 4.0;
    }

    public override int LabelNumber => 1080523; // Commodity Deed Box
    public override int DefaultGumpID => 0x43;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
        {
            list.Add(1076217); // 1st Year Veteran Reward
        }
    }

    public static CommodityDeedBox Find(Item deed)
    {
        var parent = deed;

        while (parent != null && parent is not CommodityDeedBox)
        {
            parent = parent.Parent as Item;
        }

        return parent as CommodityDeedBox;
    }
}
