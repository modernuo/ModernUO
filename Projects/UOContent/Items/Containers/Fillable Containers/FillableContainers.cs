using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xA97, 0xA99, 0xA98, 0xA9A, 0xA9B, 0xA9C)]
[SerializationGenerator(0)]
public partial class LibraryBookcase : FillableContainer
{
    [Constructible]
    public LibraryBookcase() : base(0xA97) => Weight = 1.0;

    public override bool IsLockable => false;
    public override int SpawnThreshold => 5;

    protected override int GetSpawnCount() => 5 - GetItemsCount();

    public override void AcquireContent()
    {
        if (ContentType != FillableContentType.None)
        {
            return;
        }

        ContentType = FillableContentType.Library;
    }
}

[Flippable(0xE3D, 0xE3C)]
[SerializationGenerator(0)]
public partial class FillableLargeCrate : FillableContainer
{
    [Constructible]
    public FillableLargeCrate() : base(0xE3D) => Weight = 1.0;
}

[Flippable(0x9A9, 0xE7E)]
[SerializationGenerator(0)]
public partial class FillableSmallCrate : FillableContainer
{
    [Constructible]
    public FillableSmallCrate() : base(0x9A9) => Weight = 1.0;
}

[Flippable(0x9AA, 0xE7D)]
[SerializationGenerator(0)]
public partial class FillableWoodenBox : FillableContainer
{
    [Constructible]
    public FillableWoodenBox() : base(0x9AA) => Weight = 4.0;
}

[Flippable(0x9A8, 0xE80)]
[SerializationGenerator(0)]
public partial class FillableMetalBox : FillableContainer
{
    [Constructible]
    public FillableMetalBox() : base(0x9A8)
    {
    }
}

[SerializationGenerator(0)]
public partial class FillableBarrel : FillableContainer
{
    [Constructible]
    public FillableBarrel() : base(0xE77)
    {
    }
}

[Flippable(0x9AB, 0xE7C)]
[SerializationGenerator(0, false)]
public partial class FillableMetalChest : FillableContainer
{
    [Constructible]
    public FillableMetalChest() : base(0x9AB)
    {
    }
}

[Flippable(0xE41, 0xE40)]
[SerializationGenerator(0, false)]
public partial class FillableMetalGoldenChest : FillableContainer
{
    [Constructible]
    public FillableMetalGoldenChest() : base(0xE41)
    {
    }
}

[Flippable(0xE43, 0xE42)]
[SerializationGenerator(0, false)]
public partial class FillableWoodenChest : FillableContainer
{
    [Constructible]
    public FillableWoodenChest() : base(0xE43)
    {
    }
}
