namespace Server.Items
{
    public class PieceWhiteChecker : BasePiece
    {
        public PieceWhiteChecker(BaseBoard board) : base(0x3584, board)
        {
        }

        public PieceWhiteChecker(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "white checker";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class PieceBlackChecker : BasePiece
    {
        public PieceBlackChecker(BaseBoard board) : base(0x358B, board)
        {
        }

        public PieceBlackChecker(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "black checker";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
