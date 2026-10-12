namespace Server.BotAI.V2;

/// <summary>
/// The establishments of the professions: a cook's tavern at an inn and a moneylender's desk at a shop by the bank — each a bot's
/// own shift, with its customers sent by the auction, its prices the market's, its coin hand to hand, and its numbers in the
/// <c>Bot shops:</c> line.
///
/// <para>
/// <b>Patrick, 07.10.2026 00:10, approved: "a tavern/inn kept by a bot (the cook sells food, travellers sit and eat and rest
/// THERE rather than in a field; rest with food restores faster) … a moneylender (loans to the poor against future spoils: an
/// honest way out of poverty, not a handout)."</b> The shops by the bank (<c>BotShopkeep</c>) were the first establishment; these
/// are the next. Nothing here mints coin: a meal's price goes from the guest's purse to the cook's, a loan's principal from the
/// lender's to the borrower's, and the lien brings it back with interest out of what the borrower earns — or does not, and the
/// lender has lost it.
/// </para>
/// </summary>
public static class BotTrades
{
    /// <summary>Every establishment at once. Off, no tavern opens, no guest is sent to one and no loan is made; loans standing are still collected.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>Cooks keep taverns at the inns, and guests and tired lodgers eat there.</summary>
    public static bool Taverns { get; set; } = true;

    /// <summary>Rich keepers of shops by the bank lend to poor bots with a trade.</summary>
    public static bool Lenders { get; set; } = true;

    /// <summary>Scribes keep libraries by their guilds' seats, and casters short of a scroll buy there (07.10.2026, life18b).</summary>
    public static bool Libraries { get; set; } = true;

    /// <summary>
    /// Traders keep buying posts by the dungeon mouths, and hunters sell their loot there (07.10.2026, life18b). Patrick: if the ragmen die
    /// often to the creatures, the role is taken out: this switch, live; their deaths are in the clause as "ragmen killed at the post".
    /// </summary>
    public static bool Ragmen { get; set; } = true;

    public static bool TavernsOn => Running && Taverns;

    public static bool LendersOn => Running && Lenders;

    public static bool LibrariesOn => Running && Libraries;

    public static bool RagmenOn => Running && Ragmen;

    /// <summary>The establishments' clause of the five-minute <c>Bot shops:</c> line.</summary>
    public static string Clause() =>
        $"; establishments: {(TavernsOn ? "" : "taverns OFF, ")}{BotTaverns.Describe()}; {(LendersOn ? "" : "lending OFF, ")}{BotLoans.Describe()}; {(BotInfirmaries.On ? "" : "infirmaries OFF, ")}{BotInfirmaries.Describe()}"
        + $"; {(LibrariesOn ? "" : "libraries OFF, ")}{BotLibraries.Describe()}; {(RagmenOn ? "" : "ragmen OFF, ")}{BotRagmen.Describe()}";

    public static void Forget()
    {
        BotTaverns.Forget();
        BotTaverner.Forget();
        BotSupperer.Forget();
        BotLoans.Forget();
        BotBorrower.Forget();
        BotInfirmaries.Forget();
        BotInfirmarer.Forget();
        BotPatienter.Forget();
        BotLibraries.Forget();
        BotLibrarian.Forget();
        BotReader.Forget();
        BotRagmen.Forget();
        BotRagman.Forget();
        BotRagSeller.Forget();
        BotRagHauler.Forget();
    }
}
