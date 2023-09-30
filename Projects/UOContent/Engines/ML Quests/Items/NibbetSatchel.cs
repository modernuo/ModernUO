using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class NibbetSatchel : Backpack
{
    [Constructible]
    public NibbetSatchel()
    {
        Hue = Utility.RandomBrightHue();
        DropItem(new TinkerTools());

        DropItem(
            Utility.Random(10) switch
            {
                0 => new Springs(3),
                1 => new Axle(3),
                2 => new Hinge(3),
                3 => new Key(),
                4 => new Scissors(),
                5 => new BarrelTap(),
                6 => new BarrelHoops(),
                7 => new Gears(3),
                8 => new Lockpick(3),
                _ => new ClockFrame(3) // 9
            }
        );
    }
}
