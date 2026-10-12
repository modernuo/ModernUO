using ModernUO.Serialization;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// The gloves a brawler fights in.
///
/// <para>
/// <b>What they can and cannot do, said plainly, because the difference is the whole design.</b> This shard
/// is Renaissance-era: nothing on an item adds damage, there are no properties to hang a bonus on, and a
/// glove that hit harder would be a glove from a different game. So these are armour and only armour — the
/// one thing the era does allow, and the one thing a brawler had none of. A class that holds no weapon by
/// definition is also a class nothing in the ordinary armour chain would ever arm: it orders no sword and
/// buys no shield, and its hands were bare in both senses.
/// </para>
///
/// <para>
/// <b>The damage comes from the build instead</b> — see <c>BotBrawler</c>, where Strength, Tactics, Anatomy
/// and the opening Wrestling all carry it. That is where a fist's damage actually comes from in this era, so
/// that is where the answer had to be. Putting a number here that the engine does not read would have looked
/// like a fix and been a decoration.
/// </para>
///
/// <para>
/// Leather rather than plate: the class fights unarmoured on purpose — it is the one build whose whole case
/// is standing in contact and drinking through it — and hanging metal on it would slow the hands it fights
/// with. What this is for is that the hands stop being the softest thing on the body.
/// </para>
/// </summary>
[SerializationGenerator(0, false)]
public partial class BotBrawlerGloves : LeatherGloves
{
    [Constructible]
    public BotBrawlerGloves() => Name = "a brawler's wraps";
}
