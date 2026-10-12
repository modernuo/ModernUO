namespace Server.BotAI.V2;

/// <summary>
/// The island's merchant: buys where a thing is to be had, carries it to whoever said aloud that it wants it, and keeps
/// a book of what it has promised.
///
/// <para>
/// <b>Patrick's order of 30.09.2026, point 5:</b> "traders, a new faction: they like buying in other towns, trading at
/// the market, offering to resell goods and services; the result of their work must be healthy demand for products and
/// supplying it in time; once well off they may sail to other towns to buy materials for other bots." His answer the
/// same evening: <b>neutral in wars</b> — not attacked in guild wars and not taking part in them (<see cref="Neutral"/>).
/// Plan v2 of 01.10.2026, point 3: they feel the market and follow the needs, and they write their orders into a book
/// and strike them out when delivered (<c>BotOrderBook</c>).
/// </para>
///
/// <para>
/// <b>A producer by role, and that one word carries most of the neutrality.</b> The levy, the delve, the harrow, the
/// tollman, the robber and the skulker all pass over <see cref="BotRole.Producer"/>, so a trader is born out of every one
/// of them; the guilds' muster passes over it by name (<c>BotGuilds.Barred</c>), and with no guild nothing that picks an
/// enemy by guild — the feud, the rally, the quarrel, a war company's attackers — can ever pick it. The two doors a role
/// does not shut, a company's and a toll's, are shut in <c>BotTrading.Neutral</c>. It may still keep shop by Britain's
/// bank (<c>BotShopkeeper.Merchant</c> reads the role), which is where what it could not hand over is sold.
/// </para>
///
/// <para>
/// <b>Armed for the road and no further.</b> A trader walks from town to town with goods in its pack, so it carries a
/// blade it can use — the weapon roll and Tactics take the opening points — and binds its own wounds. Its trade is
/// <see cref="SkillName.ArmsLore"/>, the eye for what a thing is worth: ItemID is everybody's at a hundred
/// (<c>BotMobile.Common</c>), so the appraiser's mark had to be the other lore. It is trained by the haul itself, a check
/// on every lot bought and every lot handed over (<c>BotHaul</c>), so a trader's rank says how much it has traded.
/// </para>
/// </summary>
public sealed class BotTrader : BotClass
{
    public override string Name => "Trader";

    public override BotRole Role => BotRole.Producer;

    public override SkillName? MainSkill => SkillName.ArmsLore;

    /// <summary>Stands outside every war. See <see cref="BotClass.Neutral"/> and <c>BotTrading.Neutral</c>.</summary>
    public override bool Neutral => true;

    protected override void Defaults()
    {
        Str = 45;
        Dex = 40;
        Int = 15;

        // Declared in the order the opening points should go: the road first (the rolled weapon takes its own fifty in
        // BotMobile.LearnWeapon), then the trade, then the bandage.
        Skills =
        [
            (SkillName.Tactics, 100.0),
            (SkillName.ArmsLore, 100.0),
            (SkillName.Healing, 100.0),
            (SkillName.Anatomy, 100.0)
        ];

        Kit = new BotKit
        {
            Melee = BotArsenal.Melee(100.0)
        };
    }
}
