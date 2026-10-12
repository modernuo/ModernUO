namespace Server.BotAI.V2;

/// <summary>
/// Metal, cloth and leather. Fights only because everybody has to.
///
/// <para>
/// <b>Mining is its highest target and Blacksmithing is its trade, and both statements are needed.</b>
/// Smelting ore into ingots is a Mining check, so a smith with a low Mining burns the ore it dug and
/// never reaches the anvil at all — the first version measured a smith at Mining 26 turning two ore out
/// of a hundred into ingots, and its <c>things made</c> counter stayed at zero for a whole session. So
/// the opening skill points, which go to the three highest targets in order, have to land on Mining
/// first. But then a champion of this class came out a grandmaster <em>miner</em> holding a journeyman's
/// hammer, because rank read the largest target as the trade. That is why <see cref="MainSkill"/> is
/// stated outright here instead of inferred.
/// </para>
///
/// <para>
/// <b>The free craft is aimed at a measured bottleneck, not at the skill.</b> A pack holds about twelve
/// ore, which is twenty ingots, which is one or two helmets, and then it is back to the mine — so a
/// smith's output is limited by trips underground rather than by anything at the forge. Once an hour one
/// attempt yields two items and charges materials for one, which is worth roughly one trip saved.
/// </para>
/// </summary>
public sealed class BotCrafter : BotClass
{
    public override string Name => "Crafter";

    public override BotRole Role => BotRole.Producer;

    /// <summary>Stated, not inferred. See the remarks: Mining is higher and Blacksmithing is the trade.</summary>
    public override SkillName? MainSkill => SkillName.Blacksmith;

    protected override void Defaults()
    {
        Str = 50;
        Dex = 30;
        Int = 20;

        Skills =
        [
            (SkillName.Mining, 100.0),
            (SkillName.Blacksmith, 100.0),
            (SkillName.Tailoring, 100.0),
            (SkillName.Tinkering, 100.0),

            // <b>Fletching, added 04.09.2026, and it is a supply line rather than a skill.</b> Arrows were
            // the one consumable on this island with no source: nobody could make one, the provisioner keeps
            // twenty, and thirteen shooters spend them. Fletching tops out at 40 for everything in the arrow
            // chain, so a hundred here is generous by a wide margin and costs the trade nothing.
            (SkillName.Fletching, 100.0),

            // And the axe that feeds it. Patrick's chain says the crafters go and cut the wood themselves
            // rather than wait on a carpenter's shelf, and a trade whose raw material can only be bought is
            // a trade that stops when the shop's stock does.
            (SkillName.Lumberjacking, 100.0),
            (SkillName.Tactics, 100.0),
            (SkillName.Healing, 100.0)
        ];

        // <b>"Crafters put the most into Blacksmithy, Fletching and the other producing trades, to give the market what it asks
        // for sooner" (Patrick, 01.10.2026, point 7). Every crafter leans, and leans hard: six in ten to the anvil, because the
        // anvil is what the island's coloured ore is waiting for and only six crafters are born with the world; two to the bow,
        // one to the needle, one to the tinker's bench, whose tools were the shard's worst glut on 384 (1,310 made unsold).</b>
        //
        // The smith is born at the first coloured metal's floor or above — dull copper, sixty-five, read off DefBlacksmithy; up to
        // copper at seventy-five — because that floor is the engine's on the Blacksmithy base (CraftItem line 647) and a smith born
        // at thirty, as every crafter was, needed thirty-five points at the anvil before the first coloured bar it held was any use
        // to it: on build 384 the island dug 622 coloured ingots and the live crafters stood at Blacksmithy 36 to 49. The tailor
        // likewise at the first coloured hide's floor (spined, sixty-five), which also opens the whole plain leather suit (legs at
        // 66.3, the tunic at 70.5). The bowyer to where crossbows open (sixty), the tinker to where every tool does (forty, and the
        // five-point margin the crafts keep).
        //
        // Paid for out of the weapon (to half its birth value), the other trades and Mining — which keeps forty, because smelting is
        // a Mining check and a smith that burns its ore never reaches the anvil (see the class note). Blacksmithy is the skill this
        // class is judged by and would otherwise never pay; a crafter that leans elsewhere lets it fall to ten.
        Leanings =
        [
            new BotLeaning("smith", SkillName.Blacksmith, 6, 65.0, 75.0, Tier: BotTier.Metal, Keeps: [(SkillName.Mining, 40.0)]),
            new BotLeaning(
                "tailor",
                SkillName.Tailoring,
                1,
                65.0,
                75.0,
                Tier: BotTier.Leather,
                Keeps: [(SkillName.Mining, 40.0), (SkillName.Blacksmith, 10.0)]
            ),
            new BotLeaning("bowyer", SkillName.Fletching, 2, 55.0, 65.0, Keeps: [(SkillName.Mining, 40.0), (SkillName.Blacksmith, 10.0)]),
            new BotLeaning("tinker", SkillName.Tinkering, 1, 45.0, 55.0, Keeps: [(SkillName.Mining, 40.0), (SkillName.Blacksmith, 10.0)])
        ];

        FreeCraftIntervalMs = 3600000;

        Kit = new BotKit
        {
            // Trained low: it carries a blade because everything on this island does, not because it
            // intends to use one. Every point above this would come out of the trade.
            Melee = BotArsenal.Melee(100.0),

            // The hammer is the trade. Without one a smith is a bot with an opinion about metal — it
            // cannot forge, cannot take commissions, and quietly spends its life hitting skeletons like
            // everybody else, which is exactly what the first version's smiths all did.
            //
            // Leatherwork needs no third tool: in this era the sewing kit makes leather armour as well
            // as cloth, so "tailoring and leather" is one skill and one implement.
            Tools =
            [
                typeof(Server.Items.SmithHammer),
                typeof(Server.Items.Pickaxe),
                typeof(Server.Items.SewingKit),
                typeof(Server.Items.FletcherTools),
                typeof(Server.Items.Hatchet),
                // The tinker's tools, which make every other tool in this list. No shop in Britain sells them
                // (the nearest tinker is in Minoc), so they are born with the crafter. See BotTinkering.
                typeof(Server.Items.TinkerTools)
            ]
        };
    }
}
