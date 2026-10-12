using ModernUO.Serialization;

namespace Server.BotAI.V2;

/// <summary>
/// A champion's steed: a mount of a kind no stable sells, won in the championship.
///
/// <para>
/// <b>Patrick's order of 25.09.2026: "prizes must be unique … or a unique mount".</b> It is a <see cref="BotSteed"/> in
/// every other way — carried in the pack as a statuette and called up by the engine's own ethereal spell — so the
/// stable needs no new rule; <see cref="BotStable.Of"/> takes it before a bought horse. Its look (a kirin, a unicorn, a
/// swamp dragon, an ostard or a ridgeback), its gold colour and its name are set when it is given and kept by the
/// engine's own fields. See <c>BotTourney.Steed</c>.
/// </para>
/// </summary>
[SerializationGenerator(0, false)]
public partial class BotChampionSteed : BotSteed
{
    [Constructible]
    public BotChampionSteed() => Name = "a champion's steed";
}
