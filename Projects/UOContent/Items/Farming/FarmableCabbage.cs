namespace Server.Items
{
    public class FarmableCabbage : FarmableCrop
    {
        [Constructible]
        public FarmableCabbage() : base(GetCropID())
        {
        }

        public FarmableCabbage(Serial serial) : base(serial)
        {
        }

        public static int GetCropID() => 3254;

        public override Item GetCropObject()
        {
            var cabbage = new Cabbage();

            cabbage.ItemID = Utility.Random(3195, 2);

            return cabbage;
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
