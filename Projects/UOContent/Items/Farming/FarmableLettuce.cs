namespace Server.Items
{
    public class FarmableLettuce : FarmableCrop
    {
        [Constructible]
        public FarmableLettuce() : base(GetCropID())
        {
        }

        public FarmableLettuce(Serial serial) : base(serial)
        {
        }

        public static int GetCropID() => 3254;

        public override Item GetCropObject()
        {
            var lettuce = new Lettuce();

            lettuce.ItemID = Utility.Random(3184, 2);

            return lettuce;
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
