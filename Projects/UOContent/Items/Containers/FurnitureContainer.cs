using System;
using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[Flippable(0x2815, 0x2816)]
[SerializationGenerator(0, false)]
public partial class TallCabinet : BaseContainer
{
    [Constructible]
    public TallCabinet() : base(0x2815) => Weight = 1.0;
}

[Furniture]
[Flippable(0x2817, 0x2818)]
[SerializationGenerator(0, false)]
public partial class ShortCabinet : BaseContainer
{
    [Constructible]
    public ShortCabinet() : base(0x2817) => Weight = 1.0;
}

[Furniture]
[Flippable(0x2857, 0x2858)]
[SerializationGenerator(0, false)]
public partial class RedArmoire : BaseContainer
{
    [Constructible]
    public RedArmoire() : base(0x2857) => Weight = 1.0;
}

[Furniture]
[Flippable(0x285D, 0x285E)]
[SerializationGenerator(0, false)]
public partial class CherryArmoire : BaseContainer
{
    [Constructible]
    public CherryArmoire() : base(0x285D) => Weight = 1.0;
}

[Furniture]
[Flippable(0x285B, 0x285C)]
[SerializationGenerator(0, false)]
public partial class MapleArmoire : BaseContainer
{
    [Constructible]
    public MapleArmoire() : base(0x285B) => Weight = 1.0;
}

[Furniture]
[Flippable(0x2859, 0x285A)]
[SerializationGenerator(0, false)]
public partial class ElegantArmoire : BaseContainer
{
    [Constructible]
    public ElegantArmoire() : base(0x2859) => Weight = 1.0;
}

[Furniture]
[SerializationGenerator(0)]
[Flippable(0x2D07, 0x2D08)]
public partial class FancyElvenArmoire : BaseContainer
{
    [Constructible]
    public FancyElvenArmoire() : base(0x2D07) => Weight = 1.0;
    public override int DefaultGumpID => 0x4E;
    public override int DefaultDropSound => 0x42;
}

[Furniture]
[SerializationGenerator(0)]
[Flippable(0x2D05, 0x2D06)]
public partial class SimpleElvenArmoire : BaseContainer
{
    [Constructible]
    public SimpleElvenArmoire() : base(0x2D05) => Weight = 1.0;
    public override int DefaultGumpID => 0x4F;
    public override int DefaultDropSound => 0x42;
}

[Furniture]
[Flippable(0xa97, 0xa99, 0xa98, 0xa9a, 0xa9b, 0xa9c)]
[SerializationGenerator(0, false)]
public partial class FullBookcase : BaseContainer
{
    [Constructible]
    public FullBookcase() : base(0xA97) => Weight = 1.0;
}

[Furniture]
[Flippable(0xa9d, 0xa9e)]
[SerializationGenerator(0, false)]
public partial class EmptyBookcase : BaseContainer
{
    [Constructible]
    public EmptyBookcase() : base(0xA9D)
    {
    }
}

[Furniture]
[Flippable(0xa2c, 0xa34)]
[SerializationGenerator(0, false)]
public partial class Drawer : BaseContainer
{
    [Constructible]
    public Drawer() : base(0xA2C) => Weight = 1.0;
}

[Furniture]
[Flippable(0xa30, 0xa38)]
[SerializationGenerator(0, false)]
public partial class FancyDrawer : BaseContainer
{
    [Constructible]
    public FancyDrawer() : base(0xA30) => Weight = 1.0;
}

[Furniture]
[Flippable(0xa4f, 0xa53)]
[SerializationGenerator(0, false)]
public partial class Armoire : BaseContainer
{
    [Constructible]
    public Armoire() : base(0xA4F) => Weight = 1.0;

    public override void DisplayTo(Mobile m)
    {
        if (DynamicFurniture.Open(this, m))
        {
            base.DisplayTo(m);
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        DynamicFurniture.Close(this);
    }
}

[Furniture]
[Flippable(0xa4d, 0xa51)]
[SerializationGenerator(0, false)]
public partial class FancyArmoire : BaseContainer
{
    [Constructible]
    public FancyArmoire() : base(0xA4D) => Weight = 1.0;

    public override void DisplayTo(Mobile m)
    {
        if (DynamicFurniture.Open(this, m))
        {
            base.DisplayTo(m);
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        DynamicFurniture.Close(this);
    }
}

public static class DynamicFurniture
{
    private static readonly Dictionary<Container, Timer> _table = new();

    public static bool Open(Container c, Mobile m)
    {
        if (_table.ContainsKey(c))
        {
            c.SendRemovePacket();
            Close(c);
            c.Delta(ItemDelta.Update);
            c.ProcessDelta();
            return false;
        }

        if (c is Armoire or FancyArmoire)
        {
            Timer t = new FurnitureTimer(c, m);
            t.Start();
            _table[c] = t;

            c.ItemID = c.ItemID switch
            {
                0xA4D => 0xA4C,
                0xA4F => 0xA4E,
                0xA51 => 0xA50,
                0xA53 => 0xA52,
                _     => c.ItemID
            };
        }

        return true;
    }

    public static void Close(Container c)
    {
        if (_table.Remove(c, out var t))
        {
            t.Stop();
        }

        if (c is Armoire or FancyArmoire)
        {
            c.ItemID = c.ItemID switch
            {
                0xA4C => 0xA4D,
                0xA4E => 0xA4F,
                0xA50 => 0xA51,
                0xA52 => 0xA53,
                _     => c.ItemID
            };
        }
    }
}

public class FurnitureTimer : Timer
{
    private readonly Container _container;
    private readonly Mobile _mobile;

    public FurnitureTimer(Container c, Mobile m) : base(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5))
    {

        _container = c;
        _mobile = m;
    }

    protected override void OnTick()
    {
        if (_mobile.Map != _container.Map || !_mobile.InRange(_container.GetWorldLocation(), 3))
        {
            DynamicFurniture.Close(_container);
        }
    }
}
