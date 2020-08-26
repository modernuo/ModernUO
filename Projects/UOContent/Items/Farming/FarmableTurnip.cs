namespace Server.Items
{
    public class FarmableTurnip : FarmableCrop
    {
        [Constructible]
        public FarmableTurnip() : base(GetCropID())
        {
        }

        public FarmableTurnip(Serial serial) : base(serial)
        {
        }

        public static int GetCropID() => Utility.Random(3169, 3);

        public override Item GetCropObject()
        {
            var turnip = new Turnip();

            turnip.ItemID = Utility.Random(3385, 2);

            return turnip;
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
