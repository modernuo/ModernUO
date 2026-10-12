namespace Server.BotAI.V2;

/// <summary>
/// What a squad needs a bot to be. Implemented by the bot.
///
/// Four things and no more, and the shortness is the point: a squad decides where its people stand and
/// who gets what, and to do that it has to know where they are, what they are for, whether they can
/// fight, and how to send them somewhere. It has no business knowing anything else about them.
/// </summary>
public interface IBotSquadMember : IBotAlly
{
    /// <summary>
    /// The mobile this bot is. The same object, typed as the engine sees it.
    ///
    /// <b>Named <c>Self</c> rather than <c>Body</c>, and the compiler is why.</b> <c>Mobile.Body</c> already
    /// exists — it is the body graphic — so a bot implementing both would hide an engine property with one of
    /// a different type, and <c>bot.Body</c> would mean two different things depending on which type the
    /// reference had. That is a trap with no upside.
    /// </summary>
    Mobile Self { get; }

    /// <summary>What this one is for. The squad reads <see cref="BotClass.Role"/> and nothing else.</summary>
    BotClass Class { get; }

    /// <summary>
    /// Where it is going. The squad sends people places by putting errands on this, which is why holding
    /// formation needed no new mechanism: a station is an errand like any other.
    /// </summary>
    BotJourney Journey { get; }

    /// <summary>
    /// Which squad this one belongs to, or null.
    ///
    /// <b>Held by the bot, set by the registry</b>, rather than looked up in a table keyed by serial. The
    /// first version kept a bot's state in thirty-two such tables across as many files: every one needed its
    /// own reset, every one leaked when the population was torn down, and "what squad is this bot in" was a
    /// question you answered by reading another file. A deleted bot takes this with it.
    /// </summary>
    BotSquad Squad { get; set; }
}
