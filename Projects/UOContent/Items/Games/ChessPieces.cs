using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PieceWhiteKing : BasePiece
{
    public PieceWhiteKing(BaseBoard board) : base(0x3587, board)
    {
    }

    public override string DefaultName => "white king";
}

[SerializationGenerator(0, false)]
public partial class PieceBlackKing : BasePiece
{
    public PieceBlackKing(BaseBoard board) : base(0x358E, board)
    {
    }

    public override string DefaultName => "black king";
}

[SerializationGenerator(0, false)]
public partial class PieceWhiteQueen : BasePiece
{
    public PieceWhiteQueen(BaseBoard board) : base(0x358A, board)
    {
    }

    public override string DefaultName => "white queen";
}

[SerializationGenerator(0, false)]
public partial class PieceBlackQueen : BasePiece
{
    public PieceBlackQueen(BaseBoard board) : base(0x3591, board)
    {
    }

    public override string DefaultName => "black queen";
}

[SerializationGenerator(0, false)]
public partial class PieceWhiteRook : BasePiece
{
    public PieceWhiteRook(BaseBoard board) : base(0x3586, board)
    {
    }

    public override string DefaultName => "white rook";
}

[SerializationGenerator(0, false)]
public partial class PieceBlackRook : BasePiece
{
    public PieceBlackRook(BaseBoard board) : base(0x358D, board)
    {
    }

    public override string DefaultName => "black rook";
}

[SerializationGenerator(0, false)]
public partial class PieceWhiteBishop : BasePiece
{
    public PieceWhiteBishop(BaseBoard board) : base(0x3585, board)
    {
    }

    public override string DefaultName => "white bishop";
}

[SerializationGenerator(0, false)]
public partial class PieceBlackBishop : BasePiece
{
    public PieceBlackBishop(BaseBoard board) : base(0x358C, board)
    {
    }

    public override string DefaultName => "black bishop";
}

[SerializationGenerator(0, false)]
public partial class PieceWhiteKnight : BasePiece
{
    public PieceWhiteKnight(BaseBoard board) : base(0x3588, board)
    {
    }

    public override string DefaultName => "white knight";
}

[SerializationGenerator(0, false)]
public partial class PieceBlackKnight : BasePiece
{
    public PieceBlackKnight(BaseBoard board) : base(0x358F, board)
    {
    }

    public override string DefaultName => "black knight";
}

[SerializationGenerator(0, false)]
public partial class PieceWhitePawn : BasePiece
{
    public PieceWhitePawn(BaseBoard board) : base(0x3589, board)
    {
    }

    public override string DefaultName => "white pawn";
}

[SerializationGenerator(0, false)]
public partial class PieceBlackPawn : BasePiece
{
    public PieceBlackPawn(BaseBoard board) : base(0x3590, board)
    {
    }

    public override string DefaultName => "black pawn";
}
