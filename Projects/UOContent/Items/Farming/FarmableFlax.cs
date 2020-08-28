namespace Server.Items
{
    public class FarmableFlax : FarmableCrop
    {
        [Constructible]
        public FarmableFlax() : base(GetCropID())
        {
        }

        public FarmableFlax(Serial serial) : base(serial)
        {
        }

        public static int GetCropID() => Utility.Random(6809, 3);

        public override Item GetCropObject()
        {
            var flax = new Flax();

            flax.ItemID = Utility.Random(6812, 2);

            return flax;
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
