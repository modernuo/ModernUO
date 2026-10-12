using ModernUO.Serialization;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// The sign a guild hangs on the wall beside the door of its town house, reading the guild's name and tag.
///
/// <para>
/// <b>The engine's own shop sign, and a name on it.</b> <see cref="Sign"/> is what the engine's sign generator hangs over
/// every shop (<c>SignParser</c> reading <c>Data/signs.cfg</c>), and a single click shows an item's name; so this is that
/// sign with the guild's name set, and the one thing added is whose it is — so the sign can be found and taken down again
/// when the guild leaves or dies out, by <see cref="BotGuildHouses"/>, without trusting the text on it. A plain wooden
/// sign in any other town; the brass one in Britain, where the house was bought.
/// </para>
///
/// <para>
/// Where it hangs follows the engine's own data rather than a guess: of the 41 shop signs <c>signs.cfg</c> puts in Britain
/// within three tiles of a doorway, the commonest place for a door in a wall running east–west is a west-facing sign one
/// tile west of the door and one tile out (13), and for a door in a wall running north–south a north-facing sign one tile
/// out and one tile north (9); and every sign whose doorway's outside could be told hangs on the outside. Measured on
/// 29.09.2026, offline, against the doorways the engine's door generator would fill.
/// </para>
/// </summary>
[SerializationGenerator(0, false)]
public partial class BotGuildSign : Sign
{
    /// <summary>The guild whose sign this is, by name.</summary>
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _guild;

    [Constructible]
    public BotGuildSign(SignType type, SignFacing facing, string guild) : base(type, facing) => _guild = guild;
}
