using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CheckerBoard : BaseBoard
{
    [Constructible]
    public CheckerBoard() : base(0xFA6)
    {
    }

    public override int LabelNumber => 1016449; // a checker board

    public override void CreatePieces()
    {
        for (var i = 0; i < 4; i++)
        {
            CreatePiece(new PieceWhiteChecker(this), 50 * i + 45, 25);
            CreatePiece(new PieceWhiteChecker(this), 50 * i + 70, 50);
            CreatePiece(new PieceWhiteChecker(this), 50 * i + 45, 75);
            CreatePiece(new PieceBlackChecker(this), 50 * i + 70, 150);
            CreatePiece(new PieceBlackChecker(this), 50 * i + 45, 175);
            CreatePiece(new PieceBlackChecker(this), 50 * i + 70, 200);
        }
    }
}
