using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xE1C, 0xFAD)]
[SerializationGenerator(0, false)]
public partial class Backgammon : BaseBoard
{
    [Constructible]
    public Backgammon() : base(0xE1C)
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
}
