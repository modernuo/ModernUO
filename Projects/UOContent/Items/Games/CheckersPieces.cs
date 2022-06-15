using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PieceWhiteChecker : BasePiece
{
    public PieceWhiteChecker(BaseBoard board) : base(0x3584, board)
    {
    }

    public override string DefaultName => "white checker";
}

[SerializationGenerator(0, false)]
public partial class PieceBlackChecker : BasePiece
{
    public PieceBlackChecker(BaseBoard board) : base(0x358B, board)
    {
    }

    public override string DefaultName => "black checker";
}
