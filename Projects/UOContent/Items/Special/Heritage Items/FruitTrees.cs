using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public abstract partial class BaseFruitTreeAddon : BaseAddon
{
    public BaseFruitTreeAddon()
    {
        Timer.StartTimer(TimeSpan.FromMinutes(5), Respawn);
    }

    public abstract override BaseAddonDeed Deed { get; }
    public abstract Item Fruit { get; }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Fruits
    {
        get => _fruits;
        set => _fruits = Math.Max(value, 0);
    }

    public override void OnComponentUsed(AddonComponent c, Mobile from)
    {
        if (from.InRange(c.Location, 2))
        {
            if (_fruits > 0)
            {
                var fruit = Fruit;

                if (fruit == null)
                {
                    return;
                }

                if (!from.PlaceInBackpack(fruit))
                {
                    fruit.Delete();
                    from.SendLocalizedMessage(501015); // There is no room in your backpack for the fruit.
                }
                else
                {
                    if (--Fruits == 0)
                    {
                        Timer.StartTimer(TimeSpan.FromMinutes(30), Respawn);
                    }

                    from.SendLocalizedMessage(501016); // You pick some fruit and put it in your backpack.
                }
            }
            else
            {
                from.SendLocalizedMessage(501017); // There is no more fruit on this tree
            }
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }

    private void Respawn()
    {
        _fruits = Utility.RandomMinMax(1, 4);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_fruits == 0)
        {
            Respawn();
        }
    }
}

[SerializationGenerator(0)]
public partial class AppleTreeAddon : BaseFruitTreeAddon
{
    [Constructible]
    public AppleTreeAddon()
    {
        AddComponent(new LocalizedAddonComponent(0xD98, 1076269), 0, 0, 0);
        AddComponent(new LocalizedAddonComponent(0x3124, 1076269), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new AppleTreeDeed();
    public override Item Fruit => new Apple();
}

[SerializationGenerator(0)]
public partial class AppleTreeDeed : BaseAddonDeed
{
    [Constructible]
    public AppleTreeDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new AppleTreeAddon();
    public override int LabelNumber => 1076269; // Apple Tree
}

[SerializationGenerator(0)]
public partial class PeachTreeAddon : BaseFruitTreeAddon
{
    [Constructible]
    public PeachTreeAddon()
    {
        AddComponent(new LocalizedAddonComponent(0xD9C, 1076270), 0, 0, 0);
        AddComponent(new LocalizedAddonComponent(0x3123, 1076270), 0, 0, 0);
    }

    public override BaseAddonDeed Deed => new PeachTreeDeed();
    public override Item Fruit => new Peach();
}

[SerializationGenerator(0)]
public partial class PeachTreeDeed : BaseAddonDeed
{
    [Constructible]
    public PeachTreeDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new PeachTreeAddon();
    public override int LabelNumber => 1076270; // Peach Tree
}
