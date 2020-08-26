namespace Server.Items
{
    public class FarmablePumpkin : FarmableCrop
    {
        [Constructible]
        public FarmablePumpkin()
            : base(GetCropID())
        {
        }

        public FarmablePumpkin(Serial serial)
            : base(serial)
        {
        }

        public static int GetCropID() => Utility.Random(3166, 3);

        public override Item GetCropObject()
        {
            var pumpkin = new Pumpkin();

            pumpkin.ItemID = Utility.Random(3178, 3);

            return pumpkin;
        }

        public override int GetPickedID() => Utility.Random(3166, 3);

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
