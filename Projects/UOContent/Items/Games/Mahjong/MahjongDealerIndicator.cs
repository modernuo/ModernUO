using ModernUO.Serialization;

namespace Server.Engines.Mahjong;

[SerializationGenerator(1, false)]
public partial class MahjongDealerIndicator
{
    [DirtyTrackingEntity]
    private readonly MahjongGame _game;

    [SerializableField(0, setter: "private")]
    private Point2D _position;

    [SerializableField(1, setter: "private")]
    private MahjongPieceDirection _direction;

    [SerializableField(2, setter: "private")]
    private MahjongWind _wind;

    public MahjongDealerIndicator(MahjongGame game)
    {
        _game = game;
    }

    public MahjongDealerIndicator(MahjongGame game, Point2D position, MahjongPieceDirection direction, MahjongWind wind)
    {
        _game = game;
        _position = position;
        _direction = direction;
        _wind = wind;
    }

    public MahjongPieceDim Dimensions => GetDimensions(_position, _direction);

    public static MahjongPieceDim GetDimensions(Point2D position, MahjongPieceDirection direction) =>
        direction is MahjongPieceDirection.Up or MahjongPieceDirection.Down
            ? new MahjongPieceDim(position, 40, 20)
            : new MahjongPieceDim(position, 20, 40);

    public void Move(Point2D position, MahjongPieceDirection direction, MahjongWind wind)
    {
        var dim = GetDimensions(position, direction);

        if (!dim.IsValid())
        {
            return;
        }

        _position = position;
        _direction = direction;
        _wind = wind;

        _game.Players.SendGeneralPacket(true, true);
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _position = reader.ReadPoint2D();
        _direction = (MahjongPieceDirection)reader.ReadInt();
        _wind = (MahjongWind)reader.ReadInt();
    }
}
