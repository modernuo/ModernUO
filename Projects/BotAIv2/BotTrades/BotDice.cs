using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Dice at a tavern table (PLAN-life B5): two seated guests agree a small stake, throw, and the loser pays the winner
/// from its own purse — every step said aloud, one line a table-beat, so a player at the table watches the game.
///
/// <para>
/// <b>Honest money, no escrow.</b> Nothing moves until the throw is resolved: the stake is checked against both pockets
/// at the call, and charged at the payment (<see cref="BotMarket.Charge"/>, which never spends a want's reservation);
/// the loser's work never learns a loss (<see cref="BotYield.Aside"/>), and the winner is handed the coin
/// (<c>Backpack.DropItem</c>). A player gone from the table before the throw owes nothing; a loser short at the payment
/// voids the game — no credit, no coin from nowhere. A restart forgets a game whole: nothing was reserved, so nothing is
/// stranded.
/// </para>
///
/// <para>
/// <b>One game a table, on the table's own clock.</b> The talk's beat paces the lines (<c>BotTaverns.Talk</c> calls
/// <see cref="Play"/> where a line would be said); a game running takes the beat, a new one starts one time in
/// <see cref="GameChance"/> when two guests are seated. The cook keeps the kitchen and does not play.
/// </para>
/// </summary>
public static class BotDice
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDice));

    /// <summary>Whether dice are played at tavern tables at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>The chance a table's line is a new game instead, when two guests are seated. 0.08.</summary>
    public static double GameChance { get; set; } = 0.08;

    /// <summary>The stake, in gold, equal on both sides: between these, and never more than the poorer pocket holds.</summary>
    public static int StakeMin { get; set; } = 5;

    public static int StakeMax { get; set; } = 20;

    /// <summary>The chance a guest at the table says something about the throw. A half.</summary>
    public static double WatchChance { get; set; } = 0.5;

    // ---- counters ----------------------------------------------------------------------------------------------------------

    /// <summary>Games called ("Dice? Five gold a throw.").</summary>
    public static long GamesCalled { get; private set; }

    /// <summary>Calls taken — the second player sat in and could cover the stake.</summary>
    public static long GamesTaken { get; private set; }

    /// <summary>Calls refused because a pocket could not cover the stake.</summary>
    public static long RefusedPoor { get; private set; }

    /// <summary>Throws resolved and paid.</summary>
    public static long GamesPlayed { get; private set; }

    /// <summary>Throws even twice running: nobody pays.</summary>
    public static long Tied { get; private set; }

    /// <summary>Games dropped after the take: a player gone, or the loser short at the payment.</summary>
    public static long Voided { get; private set; }

    /// <summary>Gold that changed hands over dice. Loser-paid equals winner-taken by construction; this is the check.</summary>
    public static long GoldMoved { get; private set; }

    /// <summary>Guests who said something about a throw.</summary>
    public static long WatchersReacted { get; private set; }

    /// <summary>One game at one table: the two players, the stake, whether the call was taken, and the one re-throw allowed.</summary>
    public sealed class Game
    {
        public BotMobile First;

        public BotMobile Second;

        public int Stake;

        public bool Taken;

        public bool Rethrown;
    }

    /// <summary>
    /// The table's beat: a game running says its next line; else one time in <see cref="GameChance"/> a new game is called
    /// between two seated guests. Returns whether the beat went to dice (and no talk line is said).
    /// </summary>
    public static bool Play(BotTavern tavern, List<BotMobile> talkers)
    {
        if (!Running || tavern == null)
        {
            return false;
        }

        if (tavern.Dice is { } game)
        {
            Advance(tavern, game, talkers);

            return true;
        }

        if (talkers == null || Utility.RandomDouble() >= GameChance)
        {
            return false;
        }

        // The players are two guests: the cook keeps the kitchen.
        BotMobile first = null;
        BotMobile second = null;
        var guests = 0;

        for (var i = 0; i < talkers.Count; i++)
        {
            var bot = talkers[i];

            if (bot is not { Deleted: false, Alive: true } || ReferenceEquals(bot, tavern.Keeper))
            {
                continue;
            }

            guests++;

            if (first == null)
            {
                first = bot;
            }
            else if (second == null)
            {
                second = bot;
            }
            else if (Utility.Random(guests) is var pick && pick < 2)
            {
                if (pick == 0)
                {
                    first = bot;
                }
                else
                {
                    second = bot;
                }
            }
        }

        if (first == null || second == null)
        {
            return false;
        }

        var stake = Utility.RandomMinMax(Math.Max(1, StakeMin), Math.Max(StakeMin, StakeMax));
        var fill = new Dictionary<string, string> { ["stake"] = stake.ToString() };

        if (!BotCampTalk.Say(first, "tavern:dice:call", null, second, fill))
        {
            return false;
        }

        tavern.Dice = new Game { First = first, Second = second, Stake = stake };
        GamesCalled++;

        return true;
    }

    /// <summary>A game dropped by the table closing: counted when the take had already promised money.</summary>
    internal static void Drop(BotTavern tavern)
    {
        if (tavern?.Dice is { Taken: true })
        {
            Voided++;
        }

        if (tavern != null)
        {
            tavern.Dice = null;
        }
    }

    /// <summary>The next line of a game: the take (or the poor refusal), then the throw and the payment.</summary>
    private static void Advance(BotTavern tavern, Game game, List<BotMobile> talkers)
    {
        // Both players must still be seated at this table, alive: a player gone before the throw owes nothing.
        if (!Seated(talkers, game.First) || !Seated(talkers, game.Second))
        {
            if (game.Taken)
            {
                Voided++;
            }

            tavern.Dice = null;

            return;
        }

        if (!game.Taken)
        {
            if (Pocket(game.First) < game.Stake || Pocket(game.Second) < game.Stake)
            {
                BotCampTalk.Say(game.Second, "tavern:dice:poor", null, game.First, null);
                RefusedPoor++;
                tavern.Dice = null;

                return;
            }

            if (BotCampTalk.Say(game.Second, "tavern:dice:take", null, game.First, null))
            {
                game.Taken = true;
                GamesTaken++;
            }

            return;
        }

        var rollA = Utility.Dice(2, 6, 0);
        var rollB = Utility.Dice(2, 6, 0);

        if (rollA == rollB)
        {
            if (!game.Rethrown)
            {
                game.Rethrown = true;
                BotCampTalk.Say(game.First, "tavern:dice:tie", null, game.Second, null);

                return;
            }

            BotCampTalk.Say(game.First, "tavern:dice:tie", null, game.Second, null);
            Tied++;
            tavern.Dice = null;

            return;
        }

        var winner = rollA > rollB ? game.First : game.Second;
        var loser = ReferenceEquals(winner, game.First) ? game.Second : game.First;
        var roll = new Dictionary<string, string>
        {
            ["rollA"] = Math.Max(rollA, rollB).ToString(),
            ["rollB"] = Math.Min(rollA, rollB).ToString(),
            ["stake"] = game.Stake.ToString()
        };

        BotCampTalk.Say(winner, "tavern:dice:roll", null, loser, roll);

        if (!BotMarket.Charge(loser, game.Stake, out _))
        {
            BotCampTalk.Say(winner, "tavern:dice:void", null, loser, null);
            Voided++;
            tavern.Dice = null;

            return;
        }

        winner.Backpack?.DropItem(new Gold(game.Stake));

        // The coin is taken out of the loser by the game's own rule, not by any work it was doing: nothing learns from it.
        BotYield.Aside(loser, game.Stake);
        GoldMoved += game.Stake;
        GamesPlayed++;

        BotCampTalk.Say(loser, "tavern:dice:paid", null, winner, new Dictionary<string, string> { ["stake"] = game.Stake.ToString() });

        logger.Information(
            "Dice at {Table}: {Winner} {RollW} against {Loser} {RollL}; {Stake}gp changes hands",
            tavern.Name,
            winner.Name,
            Math.Max(rollA, rollB),
            loser.Name,
            Math.Min(rollA, rollB),
            game.Stake
        );

        // One guest at the table may say something about the throw.
        for (var i = 0; i < talkers.Count; i++)
        {
            var watcher = talkers[i];

            if (watcher is { Deleted: false, Alive: true } && !ReferenceEquals(watcher, winner) && !ReferenceEquals(watcher, loser)
                && Utility.RandomDouble() < WatchChance)
            {
                if (BotCampTalk.Say(watcher, "tavern:dice:watch", null, winner, null))
                {
                    WatchersReacted++;
                }

                break;
            }
        }

        tavern.Dice = null;
    }

    private static bool Seated(List<BotMobile> talkers, BotMobile bot)
    {
        if (talkers == null || bot is not { Deleted: false, Alive: true })
        {
            return false;
        }

        for (var i = 0; i < talkers.Count; i++)
        {
            if (ReferenceEquals(talkers[i], bot))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The coin in the bot's pocket, which is what a game at a table is played for.</summary>
    private static int Pocket(Mobile bot) => bot?.Backpack?.GetAmount(typeof(Gold)) ?? 0;

    public static string Describe() =>
        !Running
            ? "dice are off"
            : $"dice: {GamesCalled} games called, {GamesTaken} taken ({RefusedPoor} refused, pockets short), {GamesPlayed} played and paid ({Tied} tied, {Voided} voided), "
              + $"{GoldMoved}gp changed hands, {WatchersReacted} guests watched aloud";

    public static void Forget()
    {
        GamesCalled = 0;
        GamesTaken = 0;
        RefusedPoor = 0;
        GamesPlayed = 0;
        Tied = 0;
        Voided = 0;
        GoldMoved = 0;
        WatchersReacted = 0;
    }
}
