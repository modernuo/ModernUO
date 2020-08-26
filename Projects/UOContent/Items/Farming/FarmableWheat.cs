namespace Server.Items
{
    public class FarmableWheat : FarmableCrop
    {
        [Constructible]
        public FarmableWheat() : base(GetCropID())
        {
        }

        public FarmableWheat(Serial serial) : base(serial)
        {
        }

        public static int GetCropID() => Utility.Random(3157, 4);

        public override Item GetCropObject() => new WheatSheaf();

        public override int GetPickedID() => Utility.Random(3502, 2);

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
