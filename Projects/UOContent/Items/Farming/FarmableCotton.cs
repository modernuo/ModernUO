namespace Server.Items
{
    public class FarmableCotton : FarmableCrop
    {
        [Constructible]
        public FarmableCotton() : base(GetCropID())
        {
        }

        public FarmableCotton(Serial serial) : base(serial)
        {
        }

        public static int GetCropID() => Utility.Random(3153, 4);

        public override Item GetCropObject() => new Cotton();

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
