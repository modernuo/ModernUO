using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x3D98, 0x3D94)]
[SerializationGenerator(0)]
public partial class WallTorchComponent : AddonComponent
{
    public WallTorchComponent() : base(0x3D98)
    {
    }

    public override int LabelNumber => 1076282; // Wall Torch

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(Location, 2))
        {
            ItemID = ItemID switch
            {
                0x3D98 => 0x3D9B,
                0x3D9B => 0x3D98,
                0x3D94 => 0x3D97,
                0x3D97 => 0x3D94,
                _      => ItemID
            };

            Effects.PlaySound(Location, Map, 0x3BE);
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }
}

[SerializationGenerator(0)]
public partial class WallTorchAddon : BaseAddon
{
    public WallTorchAddon()
    {
        AddComponent(new WallTorchComponent(), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new WallTorchDeed();
}

[SerializationGenerator(0)]
public partial class WallTorchDeed : BaseAddonDeed
{
    [Constructible]
    public WallTorchDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new WallTorchAddon();
    public override int LabelNumber => 1076282; // Wall Torch
}
