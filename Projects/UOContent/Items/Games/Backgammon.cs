namespace Server.Items
{
    [Flippable(0xE1C, 0xFAD)]
    public class Backgammon : BaseBoard
    {
        [Constructible]
        public Backgammon() : base(0xE1C)
        {
        }

        public Backgammon(Serial serial) : base(serial)
        {
        }

        public override void CreatePieces()
        {
            for (var i = 0; i < 5; i++)
            {
                CreatePiece(new PieceWhiteChecker(this), 42, 17 * i + 6);
                CreatePiece(new PieceBlackChecker(this), 42, 17 * i + 119);

                CreatePiece(new PieceBlackChecker(this), 142, 17 * i + 6);
                CreatePiece(new PieceWhiteChecker(this), 142, 17 * i + 119);
            }

            for (var i = 0; i < 3; i++)
            {
                CreatePiece(new PieceBlackChecker(this), 108, 17 * i + 6);
                CreatePiece(new PieceWhiteChecker(this), 108, 17 * i + 153);
            }

            for (var i = 0; i < 2; i++)
            {
                CreatePiece(new PieceWhiteChecker(this), 223, 17 * i + 6);
                CreatePiece(new PieceBlackChecker(this), 223, 17 * i + 170);
            }
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
