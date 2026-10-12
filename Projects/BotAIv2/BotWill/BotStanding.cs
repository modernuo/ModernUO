namespace Server.BotAI.V2;

/// <summary>
/// Which rung of the survival ladder a bot is standing on. <b>Ordered worst first, and the order is the
/// whole of it.</b>
///
/// <para>
/// Survival is a ladder rather than a weighing, because a dying bot does not compare options. Everything
/// discretionary is weighed — see <see cref="BotAppraisal"/> — so that wants can compete in comparable
/// units; nothing above <see cref="Busy"/> competes with anything.
/// </para>
///
/// <para>
/// The order is the first version's, as it ended up after every correction, and one of those corrections
/// is worth stating because it was paid for: calling for help sat <em>above</em> running out of health.
/// A bot on its last few percent announced a company it could not itself take part in, the company found
/// nobody able and disbanded in the same tick, and the same bot posted it again — dozens in a row. Flight
/// outranks anything social.
/// </para>
///
/// <para>
/// <b>One deliberate departure from that order, and it is worth saying out loud.</b> In the first version
/// <em>something is hitting me</em> sat above <em>my health is running out</em>, because the first of those
/// was a rung with a goal of its own: it decided whether to fight or run. In this version that decision is
/// not on the ladder at all — it is <see cref="BotThreat.Decide"/>, called from <c>OnDamage</c>, and it
/// answers in the same instant. What is left of the rung is only "do not go shopping for work mid-fight",
/// and leaving that above health would mask flight for the whole duration of any fight, since a bot being
/// killed is a bot being hit continuously. That is the same defect the flight rule was written for, wearing
/// a new hat. So health outranks being hit here.
/// </para>
///
/// <para>
/// <b>A second departure, and this one was a deadlock.</b> The first version had a rung for <em>carrying
/// more than the engine will move</em>, high up, because nothing else in it would ever fix that: goals were
/// rechosen every tick, so a bot that had dug itself to a standstill had no ongoing piece of work to carry
/// the load anywhere. Here it does — taking the load somewhere <em>is</em> the next stage of the work — and
/// a rung that suspends the work in hand would suspend the only thing that ends the problem: bot stands
/// still, undertaking set aside, ten minutes later dropped, offered again, set aside again, for ever. So
/// being overloaded is a fact anyone can read — <see cref="BotLadder.Overloaded"/> — and not a rung. When
/// something worth offering exists for it, it will be offered through the auction like any other want, with
/// a high enough prior to win.
/// </para>
///
/// <para>
/// One documented rung is missing: <em>a parcel in hand</em>, which sat between the squad and the bot's own
/// errand. Nothing in this version can yet be carrying something for somebody else, and a rung no fact can
/// produce is a branch that never runs. It goes in when errands for others do.
/// </para>
/// </summary>
public enum BotStanding
{
    /// <summary>Not alive. Nothing is decided; whatever was being done ended when the bot did.</summary>
    Dead,

    /// <summary>Health is running out. Flight and mending, when there is a proposer for either.</summary>
    Failing,

    /// <summary>
    /// Something is hitting it. The reflex belongs to <see cref="BotThreat"/> and fires from
    /// <c>OnDamage</c>; this rung only says that the bot has no business shopping for work right now.
    /// </summary>
    Hunted,

    /// <summary>
    /// In a squad. The squad owns where the bot is — it rebases the bottom of the journey every beat — so a
    /// private errand to somewhere else would be overwritten within the second, which is exactly how the
    /// first version produced a bot whose status read <em>Trade</em> while it walked a graveyard.
    /// </summary>
    Bound,

    /// <summary>Holding an undertaking of its own. See <see cref="BotDeed"/>.</summary>
    Busy,

    /// <summary>Free to want something. The only rung on which the auction runs.</summary>
    Free
}
