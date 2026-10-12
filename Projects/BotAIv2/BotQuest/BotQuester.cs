using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a bot the errand on the board that fits it best: a kill to a fighter who is no novice and dares the
/// ground, a delivery to a bot already carrying the goods, a scouting to anybody. Reckoned by the reward against
/// the walk and the work, so a rich errand far away and a poor one close by are weighed rather than ranked — by
/// <see cref="BotQuestDeed.Claim"/>, the very number the errand is then claimed at: until build 95 the proposer reckoned
/// a delivery or a look at one minute and the deed at two, and every errand was claimed at a flat sixty whatever it
/// paid.
///
/// <para>
/// A kill anywhere is offered with the nearest lair of the creature to the bot as its place (<see cref="BotLairs"/>),
/// and not at all when the island keeps none that a road reaches.
/// </para>
/// </summary>
public sealed class BotQuester : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotQuester));

    public string Name => "Quester";

    public BotStanding Rung => BotStanding.Free;

    public static bool Running { get; set; } = true;

    public static long Asked { get; private set; }

    /// <summary>Asked while the board was empty.</summary>
    public static long Empty { get; private set; }

    /// <summary>Asked while in a company.</summary>
    public static long Held { get; private set; }

    /// <summary>Askings refused because the bot was a thief of The Shadow.</summary>
    public static long Banded { get; private set; }

    /// <summary>Asked and fit for nothing on the board.</summary>
    public static long Unfit { get; private set; }

    /// <summary>
    /// Kills anywhere passed over because no lair of the creature a road reaches holds a living one now. Counted per errand
    /// weighed.
    /// </summary>
    public static long Lairless { get; private set; }

    public static long Offered { get; private set; }

    /// <summary>Errands passed over for having just been let go by the bot asking. See <see cref="BotQuests.RetakeMs"/>.</summary>
    public static long Retakes { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (!Running || !BotQuests.Running || map == null || map == Map.Internal || body is not { Alive: true })
        {
            return null;
        }

        Asked++;

        var board = BotQuests.All;

        if (board.Count == 0)
        {
            Empty++;

            return null;
        }

        if (bot is not IBotSquadMember { Squad: null })
        {
            Held++;

            return null;
        }

        // <b>The board is the city's, and a thief of The Shadow has no business at it.</b> Patrick's order of
        // 17.09.2026, night: "bandit bots may take part in championships, but the island's common doings are dangerous
        // to them." An errand ends at a paymaster in a guarded town, which is the one place a red cannot go; the fence
        // is not a thief and takes them as anybody does.
        if (BotUnderworld.Member(body) && !BotFence.Is(body))
        {
            Banded++;

            return null;
        }

        var skill = bot.Bond?.Weapon?.Skill;
        var fit = body.HitsMax > 0 && body.Hits >= body.HitsMax * BotHunter.FitAt;
        var novice = BotLadder.Novice(body);
        var pack = body.Backpack;
        BotQuest best = null;
        var bestScore = 0.0;
        var bestPlace = Point3D.Zero;
        var bestRoam = 0;

        for (var i = 0; i < board.Count; i++)
        {
            var quest = board[i];

            if (!quest.Open || quest.Map != map)
            {
                continue;
            }

            if (quest.LastTaker == body && Core.TickCount - quest.LetGoTick < BotQuests.RetakeMs)
            {
                Retakes++;

                continue;
            }

            var place = quest.Where;
            var roam = 0;

            switch (quest.Kind)
            {
                case BotQuestKind.Kill:
                    if (!skill.HasValue || !fit || novice)
                    {
                        continue;
                    }

                    if (place == Point3D.Zero)
                    {
                        place = BotLairs.Nearest(map, quest.Type, body.Location, null, out roam);

                        if (place == Point3D.Zero)
                        {
                            Lairless++;

                            continue;
                        }
                    }

                    if (!BotQuad.Dares(body, map, place))
                    {
                        continue;
                    }

                    break;

                case BotQuestKind.Gather:
                    if (pack == null || quest.Type == null || pack.GetAmount(quest.Type) < quest.Amount)
                    {
                        continue;
                    }

                    break;
            }

            var score = BotQuestDeed.Claim(quest, body.Location, place != Point3D.Zero ? place : body.Location);

            if (best == null || score > bestScore)
            {
                best = quest;
                bestScore = score;
                bestPlace = place;
                bestRoam = roam;
            }
        }

        if (best == null)
        {
            Unfit++;

            return null;
        }

        Offered++;

        if (Offered == 1)
        {
            logger.Information("{Name} has been offered the first errand off the board on this shard: {Errand}", body.Name, best.Tell());
        }

        return new BotQuestDeed(best, skill ?? SkillName.Wrestling, body.Location, bestPlace, bestRoam);
    }

    public static string Describe() =>
        !Running
            ? "nobody is offered errands"
            : $"{Asked} asked about the board: {Empty} found it empty, {Held} were in a company, {Banded} were of The Shadow, {Unfit} fit nothing on it, {Offered} were offered an errand, {Retakes} times one it had just let go was passed over; {Lairless} kills anywhere passed over with no lair a road reaches holding one alive; {BotQuestDeed.Describe()}";

    public static void Forget()
    {
        Asked = 0;
        Empty = 0;
        Held = 0;
        Banded = 0;
        Unfit = 0;
        Lairless = 0;
        Offered = 0;
        Retakes = 0;
    }
}
