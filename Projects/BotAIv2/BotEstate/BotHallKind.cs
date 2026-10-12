using System;
using Server.Guilds;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// The sizes a guild's hall comes in, smallest first: what each is, how many members it holds, and what it costs.
///
/// <para>
/// <b>Patrick's order of 26.09.2026: "different houses for a guild, expansions".</b> Asked what that takes in, he chose all
/// three: the hall grows with the guild (a small house, then bigger), outposts on its far land, and the +10 places tied to
/// the size of the house — "a small hall will not hold more than 15". So a hall has a size, the size has room for so many
/// members (<see cref="Holds(Guild)"/> is a ceiling on <c>BotGuilds.Ceiling</c>), and a guild that has filled its hall moves
/// into the next size up (<see cref="BotEnlarger"/>).
/// </para>
///
/// <para>
/// <b>Only houses of one storey with a door on the ground,</b> because the hall's room — where its chest, benches and
/// counter stand — is found by flooding the ground floor from the middle (<c>BotFittings.Room</c>), and towers, villas and
/// cabins would put the chest on a roof. The engine's classic houses that qualify, by footprint: the small house (7×8),
/// the sandstone patio house (12×9), the large patio house (15×15) and the large marble house (15×15). The prices are the
/// shard's own, not the engine's deed prices (43,800 to 192,000), for the reason the first hall's price was: a guild's
/// purse is thousands, not hundreds of thousands.
/// </para>
/// </summary>
public sealed class BotHallKind
{
    private BotHallKind(int size, string name, Type type, int multiID)
    {
        Size = size;
        Name = name;
        Type = type;
        MultiID = multiID;
    }

    /// <summary>One for the smallest.</summary>
    public int Size { get; }

    public string Name { get; }

    public Type Type { get; }

    public int MultiID { get; }

    /// <summary>What it costs a guild. The smallest is the old hall price (<c>BotEstate.Price</c>), a dial of its own.</summary>
    public int Price =>
        Size switch
        {
            1 => BotEstate.Price,
            2 => PatioPrice,
            3 => LargePatioPrice,
            _ => MarblePrice
        };

    /// <summary>What the sandstone patio house costs a guild.</summary>
    public static int PatioPrice { get; set; } = 15000;

    /// <summary>What the large patio house costs.</summary>
    public static int LargePatioPrice { get; set; } = 35000;

    /// <summary>What the large marble house costs.</summary>
    public static int MarblePrice { get; set; } = 60000;

    /// <summary>The members a hall of each size holds, beyond the smallest's <c>BotGuilds.Most</c> (35): ten more a size, one widening each. 25/35/45 until 03.10.2026, when the ceiling went from fifteen to thirty-five.</summary>
    public static int PatioHolds { get; set; } = 45;

    public static int LargePatioHolds { get; set; } = 55;

    public static int MarbleHolds { get; set; } = 65;

    private static readonly BotHallKind[] _kinds =
    [
        new(1, "a small house", typeof(SmallOldHouse), 0x0064),
        new(2, "a sandstone patio house", typeof(SandStonePatio), 0x009C),
        new(3, "a large patio house", typeof(LargePatioHouse), 0x008C),
        new(4, "a large marble house", typeof(LargeMarbleHouse), 0x0096)
    ];

    /// <summary>The smallest: what a guild raises first.</summary>
    public static BotHallKind First => _kinds[0];

    /// <summary>This size, one to four.</summary>
    public static BotHallKind Sized(int size) => _kinds[Math.Clamp(size, 1, _kinds.Length) - 1];

    /// <summary>The size above this one, or null at the top.</summary>
    public BotHallKind Next => Size < _kinds.Length ? _kinds[Size] : null;

    /// <summary>How many members it holds now, the dials read. The smallest holds <c>BotGuilds.Most</c>.</summary>
    public int Room =>
        Size switch
        {
            1 => BotGuilds.Most,
            2 => PatioHolds,
            3 => LargePatioHolds,
            _ => MarbleHolds
        };

    /// <summary>What size this house is: by its type, and a small house of any of the six fronts is the smallest.</summary>
    public static BotHallKind Of(BaseHouse house)
    {
        if (house == null)
        {
            return First;
        }

        var type = house.GetType();

        for (var i = 0; i < _kinds.Length; i++)
        {
            if (_kinds[i].Type == type)
            {
                return _kinds[i];
            }
        }

        return First;
    }

    /// <summary>How many members this guild's hall has room for; a guild with no hall has the smallest's room.</summary>
    public static int Holds(Guild guild) => Of(guild == null ? null : BotEstate.Hall(guild)).Room;

    /// <summary>
    /// The multi a house of this size is built from, for the plot search and the engine's check. The smallest uses the
    /// front dialled on <c>BotPlot.MultiID</c>, as it always has.
    /// </summary>
    public int Multi => Size == 1 ? BotPlot.MultiID : MultiID;

    /// <summary>Builds one for this owner, not yet in the world. Only the small house takes a front.</summary>
    public BaseHouse Make(Mobile owner) =>
        Size == 1 ? new SmallOldHouse(owner, Multi) : Activator.CreateInstance(Type, owner) as BaseHouse;

    public override string ToString() => Name;
}
