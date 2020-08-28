namespace Server.Items
{
    public class FarmableCarrot : FarmableCrop
    {
        [Constructible]
        public FarmableCarrot() : base(GetCropID())
        {
        }

        public FarmableCarrot(Serial serial) : base(serial)
        {
        }

        public static int GetCropID() => 3190;

        public override Item GetCropObject()
        {
            var carrot = new Carrot();

            carrot.ItemID = Utility.Random(3191, 2);

            return carrot;
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
