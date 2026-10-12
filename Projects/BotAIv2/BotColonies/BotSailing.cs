using System.Collections.Generic;
using Server.Guilds;

namespace Server.BotAI.V2;

/// <summary>
/// One sailing of a guild's ship with a party aboard: the muster at the dock, the voyage, and the landing. An expedition out
/// to an island (<see cref="BotExpedition"/>) and a run home with the colony's goods (<see cref="BotColonyRun"/>) each hold one;
/// the members who sign on (<see cref="BotSignOn"/>) read it to know where to stand, whether they are aboard, and where to go
/// once ashore. The captain's own deed owns it and ends it.
/// </summary>
public sealed class BotSailing
{
    public enum Way
    {
        /// <summary>From the mainland out to an island: founding a colony or relieving one.</summary>
        Out,

        /// <summary>From the colony's island home, with the goods and the colonists whose shift is over.</summary>
        Home
    }

    public enum Tide
    {
        Mustering,
        Sailing,
        Landed,
        Over
    }

    public Guild Guild;

    public BotMobile Captain;

    public Way Heading;

    /// <summary>Whether this passage founds the colony: the party stands on the square while its claim is made.</summary>
    public bool Founding;

    public Map Map;

    public BotDock From;

    public BotDock To;

    /// <summary>Where the party walks once ashore: the colony's site going out, the guild's home coming back.</summary>
    public Point3D Goal;

    public BotColony Colony;

    /// <summary>How many the captain wants aboard besides itself, and the fewest it sails with.</summary>
    public int Wanted;

    public int Least;

    /// <summary>Members signed on, wherever they are.</summary>
    public readonly List<Mobile> Roster = [];

    /// <summary>The members handed to the voyage when it put to sea: the company the helm carries.</summary>
    public readonly List<Mobile> Aboard = [];

    public Tide State;

    public long Began;

    /// <summary>When the captain reached the berth and the muster's clock started; nought until then.</summary>
    public long AtDock;

    public string Why;

    /// <summary>Whether a member is at the captain's side, near enough for the helm to take it aboard.</summary>
    public bool AtHand(Mobile m) =>
        m is { Deleted: false, Alive: true } && Captain is { Deleted: false } && m.Map == Captain.Map
        && m.InRange(Captain.Location, System.Math.Max(2, BotHelm.CrewReach - 2));

    /// <summary>Signed members at the captain's side now.</summary>
    public int Present()
    {
        var count = 0;

        for (var i = 0; i < Roster.Count; i++)
        {
            if (Roster[i] != Captain && AtHand(Roster[i]))
            {
                count++;
            }
        }

        return count;
    }

    public bool Signed(Mobile m) => m != null && Roster.Contains(m);

    public override string ToString() =>
        $"{Guild?.Name}'s {(Heading == Way.Out ? Founding ? "expedition" : "relief" : "run home")} under {Captain?.Name} from {From?.Name} to {To?.Name}, {State}";
}
