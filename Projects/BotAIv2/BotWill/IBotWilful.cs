namespace Server.BotAI.V2;

/// <summary>
/// What deciding needs a bot to be. Implemented by the bot, like the squad's contract, and deliberately
/// built on top of it rather than beside it.
///
/// <para>
/// <b>Why it extends <see cref="IBotSquadMember"/> instead of repeating it.</b> Three of the four things
/// deciding needs — the body, the class, the journey — are the same three a squad needs, and the fourth,
/// squad membership, is something deciding must be able to read: a bot whose place is the squad's business
/// must not be given somewhere else to be. Reading another subsystem's data is allowed here; asking it to
/// decide something is not. Nothing in this folder calls into <c>BotSquad</c>.
/// </para>
/// </summary>
public interface IBotWilful : IBotSquadMember
{
    /// <summary>
    /// Everything this bot has resolved, felt and learned.
    ///
    /// <b>Held by the bot</b>, like <see cref="BotBond"/> and <see cref="BotJourney"/>, and for the reason
    /// those are: in the first version a bot's state lived in thirty-two dictionaries keyed by serial,
    /// across as many files. Every one needed its own reset, every one leaked when the population was torn
    /// down, and "what does this bot want" was a question you answered by reading another file.
    /// </summary>
    BotResolve Resolve { get; }

    /// <summary>
    /// What the world handed this bot at birth, and which of the weapons it was offered it actually rolled.
    ///
    /// <para>
    /// Read here rather than guessed, because the roll is a fact nothing else records. A class offers six
    /// blades and the bot got one of them, together with the skill that swings it — and when that blade
    /// finally breaks in somebody's ribs, "buy another of what I had" is answerable only from this. Deriving
    /// it from the class would hand a warrior whichever weapon the list happens to start with and quietly
    /// undo the one fact <see cref="BotWeaponOption"/> exists to keep together.
    /// </para>
    /// </summary>
    BotBond Bond { get; }
}
