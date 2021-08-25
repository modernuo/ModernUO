namespace Server.Items
{
    public class NibbetSatchel : Backpack
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

        public NibbetSatchel(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
