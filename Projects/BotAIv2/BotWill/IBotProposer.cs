namespace Server.BotAI.V2;

/// <summary>
/// The supply side of the auction: something that knows about one kind of work and can offer a bot a piece
/// of it.
///
/// <para>
/// <b>The brain does not hold a list of goals.</b> It holds a list of proposers, and they are registered by
/// the subsystems that own the work — mining by the mining folder, orders by the crafting folder, a muster
/// by the squad folder. Adding trade to this shard must not be an edit to the decision layer, and this
/// interface is the whole of why it is not.
/// </para>
///
/// <para>
/// <b>It is also how the slow tier gets a vote by construction.</b> The first version put a language model
/// behind the brain as an advisor, and the brain took 85 of the 135 plans it managed to review — the model's
/// suggestion lost to any errand the brain had of its own, and nothing recorded that it had lost, so the
/// model spent the night learning from noise and finished with 0 of 119 predictions borne out. A model that
/// proposes through this interface is offering in the same units as everybody else, wins or loses on the
/// same arithmetic, and has its actual takings written into the same ledger.
/// </para>
/// </summary>
public interface IBotProposer
{
    /// <summary>Short, stable name. Appears in the boot log and in the census.</summary>
    string Name { get; }

    /// <summary>
    /// Which rung this proposer answers. <see cref="BotStanding.Free"/> for ordinary wants; a rung above it
    /// for work that exists to get a bot out of trouble — mending, unloading, flight.
    ///
    /// <para>
    /// This is how the ladder is filled without the brain knowing what mending is. A rung with no proposer
    /// is honest rather than broken: the bot keeps what it is doing and the shortage is reported once, by
    /// name, in the same voice the module loader uses for a subsystem that should be running and is not.
    /// </para>
    /// </summary>
    BotStanding Rung { get; }

    /// <summary>
    /// The best piece of work this subsystem can offer this bot right now, or null for nothing.
    ///
    /// <para>
    /// <b>One offer, not a list</b>, and the proposer picks it. It is the only party that can compare two
    /// veins — richness, distance, whether somebody is already there — and a brain sorting forty candidate
    /// tiles per bot per decision is the first version's cost model wearing a new hat.
    /// </para>
    ///
    /// <para>
    /// Called on the bot's own beat and only when the bot is free to take something on, so it may be a real
    /// question of the world; it must not be an expensive one.
    /// </para>
    /// </summary>
    BotDeed Propose(IBotWilful bot);
}
