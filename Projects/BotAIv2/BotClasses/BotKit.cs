using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// What the world hands a bot at birth, and on what terms.
///
/// Pure description: nothing here creates an item. The class says what a bot of its trade needs, and
/// a single granting step turns that into objects — which is the difference between this and the
/// first version, where the starting kit was a switch statement inside the bot itself and every
/// question about "what does a smith actually own" had to be answered by reading control flow.
///
/// <para>
/// <b>Everything granted is bound.</b> Bound means two things: the item weighs nothing, and death
/// does not take it. A bot's working tools are what let it start again after being killed, and a
/// hammer left in a corpse turns one bad fight into the end of a career. Weightlessness matters for a
/// reason the first version measured: three bots spent an entire session pinned in place because ore
/// plus tools crossed the engine's overload threshold, and past it every step costs stamina until
/// there is none and the step is refused outright.
/// </para>
///
/// <para>
/// <b>Stacks are bound differently from things.</b> A hammer is one object and the engine can keep it
/// through death by itself. A quiver cannot be handled that way: stacks merge, so a hundred bound
/// arrows and fifty bought ones become one stack of a hundred and fifty with a single loot flag, and
/// whichever flag wins is wrong. Ammunition is therefore bound by remembered <em>count</em>, and it
/// rides on the weapon that fires it — see <see cref="BotWeaponOption.AmmunitionCount"/>.
/// </para>
/// </summary>
public sealed class BotKit
{
    /// <summary>
    /// Melee weapons this class may roll, each paired with the skill that swings it. One is chosen at
    /// birth. Empty for classes that carry no blade at all — the mage, the healer and the brawler.
    /// </summary>
    public IReadOnlyList<BotWeaponOption> Melee { get; init; } = [];

    /// <summary>
    /// Ranged weapons this class may roll, on the same terms. Empty for everybody who does not shoot.
    ///
    /// A bow needs both hands, and the engine refuses a two-handed weapon while anything at all is in
    /// the other. That single rule cost the first version ten archers who spent their whole lives
    /// stabbing skeletons with the daggers they had been handed first, carrying the bows they had
    /// trained for. Whatever grants this kit therefore equips ranged before <see cref="Sidearm"/>,
    /// and the ordering is a property of the granting step rather than of the list.
    /// </summary>
    public IReadOnlyList<BotWeaponOption> Ranged { get; init; } = [];

    /// <summary>
    /// The blade an archer falls back on when something closes to arm's length. Null for everybody
    /// else. Goes in the pack rather than the hands, for the two-handed reason above.
    /// </summary>
    public BotWeaponOption? Sidearm { get; init; }

    /// <summary>
    /// The caster's staff, or null. Its colour and what it gives back are on the class rather than
    /// here: the hue tells a watching player which of the two casters this is, and the trickle is a
    /// talent that has to be comparable with the warrior-mage's, which comes from no item at all.
    /// </summary>
    public bool Staff { get; init; }

    /// <summary>
    /// Working tools — hammer, pickaxe, hatchet, sewing kit. Bound like everything else, and for the
    /// sharpest version of the reason: a smith without a hammer is a bot with an opinion about metal.
    /// </summary>
    public IReadOnlyList<Type> Tools { get; init; } = [];

    /// <summary>Bandages to start with. Not bound: they are a supply, and running out is the point.</summary>
    public int Bandages { get; init; } = 20;

    /// <summary>
    /// Reagents of each kind, or zero. Handed out once and never again by anybody: a caster short of
    /// reagents posts on the board and pays whoever brings them, which is how one trade's shortage
    /// becomes another bot's wage instead of a quiet end to the trade.
    /// </summary>
    public int Reagents { get; init; }

    /// <summary>
    /// Armour issued at birth and worn straight away.
    ///
    /// <para>
    /// <b>Empty for everybody, and it is meant to stay that way.</b> Armour on this shard is something a bot
    /// wants, orders and pays a crafter for — that whole chain exists and is the point of it. This list is
    /// for the one case the chain cannot answer: a class whose <em>fighting</em> depends on a piece of kit
    /// that nothing else about it would ever buy. A brawler holds no weapon by definition, so the only thing
    /// that can be issued to its hands is what goes on them.
    /// </para>
    /// </summary>
    public IReadOnlyList<Type> Armour { get; init; } = [];

    /// <summary>
    /// Spells written into the starting book, by circle index. An empty book is a prop, and a caster
    /// that has to find its first scroll is a caster in name only for its whole youth.
    /// </summary>
    public IReadOnlyList<int> Spells { get; init; } = [];
}
