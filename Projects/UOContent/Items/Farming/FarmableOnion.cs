namespace Server.Items
{
    public class FarmableOnion : FarmableCrop
    {
        [Constructible]
        public FarmableOnion() : base(GetCropID())
        {
        }

        public FarmableOnion(Serial serial) : base(serial)
        {
        }

        public static int GetCropID() => 3183;

        public override Item GetCropObject()
        {
            var onion = new Onion();

            onion.ItemID = Utility.Random(3181, 2);

            return onion;
        }

        public override int GetPickedID() => 3254;

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
