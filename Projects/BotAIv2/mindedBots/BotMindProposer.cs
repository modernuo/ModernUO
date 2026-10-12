using Server.BotAI.V2;

namespace Server.BotAI.Mind;

/// <summary>
/// The one place a thought becomes an offer.
///
/// <para>
/// <b>It offers nothing of its own.</b> Everything this hands to the auction came out of one of the shard's
/// own proposers a moment earlier — the same hunt, the same dig — wrapped so that the mind's prediction is
/// the number being bid with and the outcome is measured against it. A thinking bot is therefore never doing
/// something the others cannot do; it is doing one of the same things for a different reason.
/// </para>
///
/// <para>
/// <b>And it offers to two bots out of fifteen.</b> Asked about anybody else it answers null in a few
/// instructions, which is what lets this whole assembly sit inside the shard's decision loop without being
/// felt by the population that has no use for it.
/// </para>
///
/// <para>
/// On the <c>Free</c> rung and nowhere else: a mind may not decide to bleed. Health, flight and the reflex
/// that fires when something starts hitting a bot all live below this rung, and they run whether or not
/// anything is thinking.
/// </para>
/// </summary>
public sealed class BotMindProposer : IBotProposer
{
    public string Name => "Mind";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot) => BotMinds.Offer(bot);
}
