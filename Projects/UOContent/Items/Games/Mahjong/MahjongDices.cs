using ModernUO.Serialization;

namespace Server.Engines.Mahjong;

[SerializationGenerator(0, false)]
public partial class MahjongDices
{
    [DirtyTrackingEntity]
    private readonly MahjongGame _game;

    [SerializableField(0, setter: "private")]
    private int _first;

    [SerializableField(1, setter: "private")]
    private int _second;

    public MahjongDices(MahjongGame game)
    {
        _game = game;
        _first = Utility.Random(1, 6);
        _second = Utility.Random(1, 6);
    }

    public void RollDices(Mobile from)
    {
        _first = Utility.Random(1, 6);
        _second = Utility.Random(1, 6);

        _game.Players.SendGeneralPacket(true, true);

        if (from != null)
        {
            // ~1_name~ rolls the dice and gets a ~2_number~ and a ~3_number~!
            _game.Players.SendLocalizedMessage(1062695, $"{from.Name}\t{_first}\t{_second}");
        }
    }
}
