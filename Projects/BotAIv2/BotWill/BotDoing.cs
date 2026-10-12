namespace Server.BotAI.V2;

/// <summary>What an undertaking wants of the bot at this moment. Four answers and no fifth.</summary>
public enum BotDoingKind
{
    /// <summary>Nothing said. The value of an uninitialised <see cref="BotDoing"/>, never returned.</summary>
    None,

    /// <summary>Be somewhere else first.</summary>
    Walk,

    /// <summary>
    /// Work, here, standing still.
    ///
    /// A first-class answer rather than an absence of one, and the first version's sharpest lesson is behind
    /// it: trade at a counter, work at a forge and digging are all done standing, and a bot doing any of them
    /// was measured against the distance to somewhere it had no intention of going. After twenty-five seconds
    /// it was declared stuck, its errand was cancelled and it was barred from trading for five minutes — for
    /// trading. An undertaking that says <see cref="Work"/> is not judged for progress by anybody.
    /// </summary>
    Work,

    /// <summary>Finished, as its own definition of finished has it. The takings are counted.</summary>
    Done,

    /// <summary>Not going to happen. The takings are counted anyway, and the place is treated with caution.</summary>
    Failed
}

/// <summary>
/// One instruction from an undertaking to the decision layer.
///
/// <para>
/// A struct with named constructors rather than four methods on <see cref="BotDeed"/>, so that an
/// undertaking's whole conversation with the brain is one return value. What the brain does with each is
/// fixed and short: walk becomes the bottom of the journey, work becomes nothing at all, and the other two
/// end the undertaking.
/// </para>
/// </summary>
public readonly struct BotDoing
{
    private BotDoing(BotDoingKind kind, Map map, Point3D where, Mobile follow, BotArrival arrival, string note)
    {
        Kind = kind;
        Map = map;
        Where = where;
        Follow = follow;
        Arrival = arrival;
        Note = note;
    }

    public BotDoingKind Kind { get; }

    public Map Map { get; }

    /// <summary>Where, when the destination stands still.</summary>
    public Point3D Where { get; }

    /// <summary>What, when the destination walks. A reference, never a copied position.</summary>
    public Mobile Follow { get; }

    public BotArrival Arrival { get; }

    /// <summary>Why, in words. Reaches the journey and the log; never branched on.</summary>
    public string Note { get; }

    /// <summary>Go to a place. What most stages of most undertakings are.</summary>
    public static BotDoing Walk(Map map, Point3D where, BotArrival arrival, string note) =>
        new(BotDoingKind.Walk, map, where, null, arrival, note);

    /// <summary>Go to something that moves — a vein being worked by somebody, an escort, quarry.</summary>
    public static BotDoing Walk(Map map, Mobile follow, BotArrival arrival, string note) =>
        new(BotDoingKind.Walk, map, Point3D.Zero, follow, arrival, note);

    /// <summary>Stand here and work. See <see cref="BotDoingKind.Work"/>.</summary>
    public static BotDoing Work(string note) =>
        new(BotDoingKind.Work, null, Point3D.Zero, null, BotArrival.Beside, note);

    public static BotDoing Done(string note) =>
        new(BotDoingKind.Done, null, Point3D.Zero, null, BotArrival.Beside, note);

    public static BotDoing Failed(string note) =>
        new(BotDoingKind.Failed, null, Point3D.Zero, null, BotArrival.Beside, note);

    /// <summary>
    /// Whether this asks for the same place as <paramref name="other"/>.
    ///
    /// The brain keeps the last walk it passed on and compares against it, because rebasing the journey
    /// throws the plan away: an undertaking that says "walk to the forge" on every decision would otherwise
    /// buy a fresh path search every time it was asked, and a bot would never arrive anywhere it was more
    /// than a few tiles from.
    /// </summary>
    public bool Matches(BotDoing other) =>
        Kind == other.Kind
        && Map == other.Map
        && Where == other.Where
        && ReferenceEquals(Follow, other.Follow)
        && Arrival.Tiles == other.Arrival.Tiles;

    public override string ToString() =>
        Kind switch
        {
            BotDoingKind.Walk => Follow != null
                ? $"walk after {Follow.Name} ({Note})"
                : $"walk to {Where} ({Note})",
            BotDoingKind.Work => $"work here ({Note})",
            BotDoingKind.Done => $"done ({Note})",
            BotDoingKind.Failed => $"failed ({Note})",
            _ => "nothing"
        };
}
