using ModernUO.Serialization;

namespace Server.Engines.Mahjong;

[SerializationGenerator(0, false)]
public partial class MahjongWallBreakIndicator
{
    [DirtyTrackingEntity]
    private readonly MahjongGame _game;

    [SerializableField(0, setter: "private")]
    private Point2D _position;

    public MahjongWallBreakIndicator(MahjongGame game) => _game = game;

    public MahjongWallBreakIndicator(MahjongGame game, Point2D position)
    {
        _game = game;
        _position = position;
    }

    public MahjongPieceDim Dimensions => GetDimensions(_position);

    public static MahjongPieceDim GetDimensions(Point2D position) => new(position, 20, 20);

    public void Move(Point2D position)
    {
        var dim = GetDimensions(position);

        if (!dim.IsValid())
        {
            return;
        }

        _position = position;

        _game.Players.SendGeneralPacket(true, true);
    }
}
