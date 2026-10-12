namespace Server.BotAI.V2;

/// <summary>
/// What the watcher has announced, in a shape this assembly can read.
///
/// <para>
/// <b>A seam, for the same reason <see cref="BotAppraisal.Revelry"/> is one.</b> Argus lives in the minds
/// assembly, which references this one and must never be referenced back — so the dashboard cannot ask him
/// anything. He posts here instead, and the window reads a notice rather than a subsystem.
/// </para>
///
/// <para>
/// Everything on it is a copy taken at the moment it was asked for. A notice that held live references would
/// be a window showing a revel that ended while somebody was reading it.
/// </para>
/// </summary>
public sealed class BotNotice
{
    /// <summary>Whether something is on right now.</summary>
    public bool Running { get; init; }

    /// <summary>The trade being rewarded, as the ledger spells it.</summary>
    public string Kind { get; init; }

    /// <summary>What the winner gets.</summary>
    public int Prize { get; init; }

    /// <summary>How much dearer the named work is while it runs.</summary>
    public double Bonus { get; init; }

    /// <summary>What the watcher called out to the island when it began.</summary>
    public string Said { get; init; }

    /// <summary>Its own reasoning for declaring it.</summary>
    public string Why { get; init; }

    /// <summary>Milliseconds until it is judged and paid.</summary>
    public long EndsIn { get; init; }

    /// <summary>Milliseconds until the watcher may think about the next one.</summary>
    public long NextIn { get; init; }

    /// <summary>How many bots have done the named work since it began.</summary>
    public int Entered { get; init; }

    /// <summary>Who is winning, and by how much.</summary>
    public string Leading { get; init; }

    public int LeadingDid { get; init; }

    /// <summary>Where the thing itself is — a camp, or wherever it was declared.</summary>
    public Map Map { get; init; }

    public Point3D Where { get; init; }

    /// <summary>Whether a camp was raised for it, and is presumably still standing.</summary>
    public bool Camp { get; init; }

    /// <summary>Where the watcher is, so an administrator can go and stand beside him.</summary>
    public Map WatcherMap { get; init; }

    public Point3D Watcher { get; init; }

    public string WatcherName { get; init; }

    public long Declared { get; init; }

    public long Won { get; init; }

    public long Ignored { get; init; }

    public long Paid { get; init; }

    public int Treasury { get; init; }

    /// <summary>What the crown has left this moment. It rises with the tax and falls with the prizes.</summary>
    public int Purse { get; init; }

    /// <summary>Coin the crown has taken in tax this session.</summary>
    public long Collected { get; init; }

    /// <summary>Revels won by a whole guild between them rather than by one bot.</summary>
    public long Bands { get; init; }

    /// <summary>The ones before this, newest first, one line each.</summary>
    public string[] Past { get; init; }

    /// <summary>Which wave of a hunt is standing, one-based, or nought between them.</summary>
    public int Wave { get; init; }

    /// <summary>How many of that wave are still on their feet.</summary>
    public int Standing { get; init; }

    /// <summary>The whole tally of what has been called up for revels, in one clause.</summary>
    public string Waves { get; init; }

    /// <summary>What each trade's revels have come to: held, taken up, paid. The watcher's own results.</summary>
    public string Ledger { get; init; }
}

/// <summary>
/// Where the watcher pins its notices, and the dashboard reads them.
///
/// <para>
/// Null until the minds assembly is running, which is the honest state of a shard with no watcher on it:
/// the tab then says so rather than showing an empty table that could equally mean "nothing declared".
/// </para>
/// </summary>
public static class BotCrier
{
    /// <summary>Filled in by the minds assembly at start-up, and cleared when it stops.</summary>
    public static System.Func<BotNotice> Posted { get; set; }

    /// <summary>The notice as it stands this moment, or null if nobody is watching.</summary>
    public static BotNotice Read() => Posted?.Invoke();

    /// <summary>
    /// Where the watchers stand, filled from the debugger's side for the same reason <see cref="Posted"/>
    /// is: the dashboard may not reference the minds. Null when nobody is watching.
    /// </summary>
    public static System.Func<System.Collections.Generic.IReadOnlyList<(string Name, Map Map, Point3D At)>> Watchers { get; set; }

    public static System.Collections.Generic.IReadOnlyList<(string Name, Map Map, Point3D At)> Squad() =>
        Watchers?.Invoke() ?? System.Array.Empty<(string, Map, Point3D)>();
}
