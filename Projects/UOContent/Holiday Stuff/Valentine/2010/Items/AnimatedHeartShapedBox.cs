namespace Server.Items
{
    [Flippable(0x49CC, 0x49D0)]
    public class AnimatedHeartShapedBox : HeartShapedBox
    {
        [Constructible]
        public AnimatedHeartShapedBox() => ItemID = 0x49CC;

        public AnimatedHeartShapedBox(Serial serial)
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
