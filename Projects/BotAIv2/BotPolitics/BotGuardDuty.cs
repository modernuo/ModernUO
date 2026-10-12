using Server.Guilds;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a member of a guild that captains a town's guard a paid watch in that town: the captain's post as work.
///
/// <para>
/// <b>The town's guard for the town's wage (07.10.2026, Patrick's town council).</b> The watch the towns already set as a task
/// (<see cref="BotStandGuard"/>) is reckoned at <see cref="BotStandGuard.Prior"/> a minute and pays nothing; the captain's
/// members are offered the same five minutes inside the walls for <see cref="BotCouncil.Wage"/>gp, held out of the treasury
/// when the watch is taken and paid into the watchman's account when it is stood (<see cref="BotCouncil.Hold"/>,
/// <see cref="BotCouncil.Release"/>): a wage, not a handout, and never more than <see cref="BotCouncil.WageBudget"/> a town a
/// term. The claim is the watch's own worth plus the wage a minute, and it is a posted claim — the wage is held — so the
/// commons corrects it by how often it is earned, not towards an average.
/// </para>
/// </summary>
public sealed class BotGuardDuty : IBotProposer
{
    /// <summary>Watches taken, stood and paid, and ended without the wage (the purse could not hold it, or the watch was dropped).</summary>
    public static long Taken { get; private set; }

    public static long Stood { get; private set; }

    public static long Unpaid { get; private set; }

    public string Name => "GuardDuty";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotCouncil.Running || !BotCouncil.Captaincy || !BotCouncil.AnyCaptain || bot?.Self is not BotMobile who
            || who.Guild is not Guild guild || !who.Alive || who.Tired)
        {
            return null;
        }

        var map = who.Map;

        if (map == null || map == Map.Internal || map != BotPopulation.Home || bot is IBotSquadMember { Squad: not null }
            || BotDungeon.Under(who.Location))
        {
            return null;
        }

        var town = BotCouncil.CaptainOf(guild.Name, who.Location, BotCouncil.Reach);

        if (town == null || !BotCouncil.Room(town) || !BotGates.Joined(map, who.Location, town.Square))
        {
            return null;
        }

        return new Watch(map, town, guild.Name);
    }

    /// <summary>One paid watch inside a town's walls.</summary>
    public sealed class Watch : BotDeed
    {
        public const string Trade = "guard-duty";

        private readonly Map _map;

        private readonly BotTowns.Town _town;

        private readonly string _guild;

        private bool _held;

        private bool _paid;

        private bool _failed;

        private bool _in;

        private long _since;

        public Watch(Map map, BotTowns.Town town, string guild)
        {
            _map = map;
            _town = town;
            _guild = guild;
        }

        public override string Kind => Trade;

        public override Map Map => _map;

        public override Point3D Where => _town.Square;

        public override double Minutes => BotStandGuard.StandMs / 60000.0 + 2.0;

        private double WagePerMinute => BotCouncil.Wage / Minutes;

        public override double Expects => BotStandGuard.Prior + WagePerMinute;

        public override double Coin => Expects <= 0.0 ? 0.0 : WagePerMinute / Expects;

        public override bool Posted => true;

        public override bool Committed => _in;

        public override bool Still => _in;

        public override string Stage => _in ? $"on {_town.Name}'s guard" : $"on the way to {_town.Name}'s guard";

        public override void Taken(IBotWilful bot)
        {
            BotGuardDuty.Taken++;

            if (BotCouncil.Hold(_town))
            {
                _held = true;
            }
            else
            {
                _failed = true;
            }
        }

        public override void Drop(IBotWilful bot)
        {
            if (_held && !_paid)
            {
                _held = false;
                BotCouncil.Release(_town, false);
                BotGuardDuty.Unpaid++;
            }
        }

        public override BotDoing Advance(IBotWilful bot)
        {
            if (bot?.Self is not { } body)
            {
                return BotDoing.Failed("no body");
            }

            if (_failed || !_held)
            {
                return BotDoing.Failed($"{_town.Name}'s treasury could not hold a watch's wage");
            }

            if (!BotCouncil.Captains(_guild, _town))
            {
                return BotDoing.Done($"{_guild} no longer captains {_town.Name}'s guard");
            }

            if (!_town.Bounds.Contains(body.Location))
            {
                _in = false;

                return BotDoing.Walk(_map, _town.Square, BotArrival.Within(8), $"to stand {_town.Name}'s guard");
            }

            var now = Core.TickCount;

            if (!_in)
            {
                _in = true;
                _since = now;
                (body as BotMobile)?.Journey?.Finish();
            }

            if (now - _since < BotStandGuard.StandMs)
            {
                return BotDoing.Work($"on {_town.Name}'s guard");
            }

            if (!Banker.Deposit(body, BotCouncil.Wage))
            {
                return BotDoing.Failed($"{_town.Name}'s wage could not be put in the account");
            }

            _paid = true;
            _held = false;
            BotCouncil.Release(_town, true);
            Stood++;

            return BotDoing.Done($"stood {_town.Name}'s guard for {BotStandGuard.StandMs / 60000} minutes for the captain's guild, paid {BotCouncil.Wage}gp by the town");
        }
    }
}
