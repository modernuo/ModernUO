using ModernUO.Serialization;

namespace Server.Engines.Mahjong;

[SerializationGenerator(1, false)]
public partial class MahjongTile
{
    [DirtyTrackingEntity]
    private readonly MahjongGame _game;

    [SerializableField(0, setter: "private")]
    private int _number;

    [SerializableField(1, setter: "private")]
    private MahjongTileType _value;

    [SerializableField(2, setter: "private")]
    private Point2D _position;

    [SerializableField(3, setter: "private")]
    private int _stackLevel;

    [SerializableField(4, setter: "private")]
    private MahjongPieceDirection _direction;

    [SerializableField(5, setter: "private")]
    private bool _flipped;

    public MahjongTile(MahjongGame game) => _game = game;

    public MahjongTile(
        MahjongGame game, int number, MahjongTileType value, Point2D position, int stackLevel,
        MahjongPieceDirection direction, bool flipped
    )
    {
        _game = game;
        _number = number;
        _value = value;
        _position = position;
        _stackLevel = stackLevel;
        _direction = direction;
        _flipped = flipped;
    }

    public MahjongGame Game => _game;

    private void Deserialize(IGenericReader reader, int version)
    {
        _number = reader.ReadInt();
        _value = (MahjongTileType)reader.ReadInt();
        _position = reader.ReadPoint2D();
        _stackLevel = reader.ReadInt();
        _direction = (MahjongPieceDirection)reader.ReadInt();
        _flipped = reader.ReadBool();
    }

    public MahjongPieceDim Dimensions => GetDimensions(_position, _direction);

    public bool IsMovable => _game.GetStackLevel(Dimensions) <= _stackLevel;

    public static MahjongPieceDim GetDimensions(Point2D position, MahjongPieceDirection direction) =>
        direction is MahjongPieceDirection.Up or MahjongPieceDirection.Down
            ? new MahjongPieceDim(position, 20, 30)
            : new MahjongPieceDim(position, 30, 20);

    public void Move(Point2D position, MahjongPieceDirection direction, bool flip, int validHandArea)
    {
        var dim = GetDimensions(position, direction);
        var curHandArea = Dimensions.GetHandArea();
        var newHandArea = dim.GetHandArea();

        if (!IsMovable || !dim.IsValid() || validHandArea >= 0 &&
            (curHandArea >= 0 && curHandArea != validHandArea || newHandArea >= 0 && newHandArea != validHandArea))
        {
            return;
        }

        Position = position;
        Direction = direction;
        StackLevel = -1; // Avoid self interference
        StackLevel = _game.GetStackLevel(dim) + 1;
        Flipped = flip;

        _game.Players.SendTilePacket(this, true, true);
    }
}
