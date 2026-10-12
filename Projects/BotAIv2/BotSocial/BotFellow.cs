namespace Server.BotAI.V2;

/// <summary>
/// The one place a thing heard becomes an offer: hands a bot the help it undertook to offer when it heard a neighbour's
/// mood — the bandage, the fire, the hunt, the enlistment — as ordinary work on the Free rung, to be weighed against
/// everything else the bot could do.
///
/// <para>
/// <b>Offered, never pressed.</b> The social layer writes an invitation against the listener when it hears; this reads it
/// back at the listener's own next review and returns the deed <see cref="BotSocial"/> built then. A bot at a seam that
/// pays sixty a minute is not taken off it for a bandage reckoned at thirty — the auction's margin holds — and a bot
/// with nothing on takes it at once. That is the whole of "it must not pull bots off paying work at a bad moment", and
/// it costs this proposer one dictionary read per review.
/// </para>
///
/// <para>
/// An invitation not taken up in <see cref="BotSocial.InviteMs"/> lapses; one whose subject has died, walked off the map
/// or out of <see cref="BotSocial.InviteReach"/>, or — for a bandage — has been mended meanwhile, is let go as stale.
/// Both are counted, because an offer that is made and never taken is the number that says whether the words mean
/// anything.
/// </para>
/// </summary>
public sealed class BotFellow : IBotProposer
{
    public string Name => "Fellow";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot) => BotSocial.Propose(bot);
}
