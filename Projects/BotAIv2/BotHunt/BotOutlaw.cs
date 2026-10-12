using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// Who has murdered lately, who is in a cell for it, and the clock that lets both go.
///
/// <para>
/// <b>Patrick's order of 15.09.2026: a bot may set on another bot and rob it.</b> To rob it has to kill, and the killer
/// goes red; it is red for an hour, friendly to other reds and an enemy to everybody else; the Baron may raise a patrol
/// against it instead of hunting, and a red the Baron's patrol kills is caught and put in a cell at the bottom of the
/// map, one bot to a cell. The thought of robbing is a very rare one.
/// </para>
///
/// <para>
/// <b>Red is the engine's own word.</b> A killer's <c>Kills</c> is set to the engine's murderer threshold, so its name
/// turns red by the engine's notoriety, the guards of any town treat it as the engine treats reds, and the count here
/// only decides when the hour is up. Nothing is invented about what red means; what is added is the hour, the cell,
/// and the reading of "red" by the population's own rules — the robber's choice of victim, the Baron's patrol, and
/// the town the red keeps out of.
/// </para>
///
/// <para>
/// <b>The cells are the engine's jail.</b> The same ten points <c>JailSystem</c> uses, inside the jail region where
/// nothing may be harmed and nobody may travel out; a bot put there holds a sentence, which is a piece of work that
/// does nothing until the clock lets it go, and then it is lifted back to its guild's seat with a clean name.
/// </para>
/// </summary>
public static class BotOutlaw
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotOutlaw));

    /// <summary>Whether any of this happens. Off, and nobody robs, nobody is red, nobody is jailed.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How long a murderer stays red. An hour, by order.</summary>
    public static int RedMs { get; set; } = 3600000;

    /// <summary>How long a caught murderer sits in a cell. An hour.</summary>
    public static int JailMs { get; set; } = 3600000;

    /// <summary>How often the clock looks. Once a second.</summary>
    public static int EveryMs { get; set; } = 1000;

    /// <summary>The engine's murderer threshold, written onto a killer so its name goes red by the engine's own rule.</summary>
    public static int RedKills { get; set; } = 5;

    /// <summary>The ten cells, the engine's own jail's, one bot to a cell.</summary>
    public static readonly Point3D[] Cells =
    [
        new(5276, 1164, 0), new(5286, 1164, 0), new(5296, 1164, 0), new(5306, 1164, 0),
        new(5276, 1174, 0), new(5286, 1174, 0), new(5296, 1174, 0), new(5306, 1174, 0),
        new(5283, 1184, 0), new(5304, 1184, 0)
    ];

    /// <summary>Murders done.</summary>
    public static long Murders { get; private set; }

    /// <summary>Gold taken off victims' corpses.</summary>
    public static long Robbed { get; private set; }

    /// <summary>Murderers caught by a patrol and put in a cell.</summary>
    public static long Caught { get; private set; }

    /// <summary>Murderers let out of a cell.</summary>
    public static long Released { get; private set; }

    /// <summary>Murderers whose hour ran out uncaught.</summary>
    public static long Redeemed { get; private set; }

    /// <summary>Murderers killed by something other than a patrol, who stay red.</summary>
    public static long Unavenged { get; private set; }

    /// <summary>Whether a catch takes what the caught bot is carrying.</summary>
    public static bool Seizes { get; set; } = true;

    /// <summary>
    /// Coin taken out of a caught thief's bank on top of everything on the body. Patrick's number, 18.09.2026: "the
    /// bank is docked a thousand and the whole corpse is confiscated."
    /// </summary>
    public static int BankFine { get; set; } = 1000;

    /// <summary>Whether the catch empties the corpse. Off, and a catch takes only the bank and the bare body.</summary>
    public static bool TakesCorpse { get; set; } = true;

    /// <summary>Whether the catch breaks the bond, so no kit is handed back when the thief rises.</summary>
    public static bool TakesBond { get; set; } = true;

    /// <summary>
    /// Whether the catch leaves the thief its weapons: the ones in its hands and the ones bound to it. Patrick, 12.10.2026:
    /// "when they are caught everything is taken, but the weapon should be left."
    /// </summary>
    public static bool KeepsWeapons { get; set; } = true;

    /// <summary>Weapons a catch left on the thief. See <see cref="Arms"/>.</summary>
    public static long WeaponsKept { get; private set; }

    /// <summary>Rounds of bound ammunition a catch left on the thief. See <see cref="Arms"/>.</summary>
    public static long AmmunitionKept { get; private set; }

    /// <summary>Catches that took something, and what they came to.</summary>
    public static long Seized { get; private set; }

    public static long SeizedGold { get; private set; }

    public static long SeizedThings { get; private set; }

    /// <summary>Coin taken out of caught thieves' banks.</summary>
    public static long SeizedBank { get; private set; }

    /// <summary>Corpses emptied at the catch, before anybody could open them.</summary>
    public static long SeizedCorpse { get; private set; }

    /// <summary>Catches that broke a bond, so the kit was not handed back on resurrection.</summary>
    public static long Stripped { get; private set; }

    /// <summary>Murderers the Baron killed with his own hand, for which nobody was paid.</summary>
    public static long BaronsOwn { get; private set; }

    /// <summary>Prices of blood split between a company rather than paid to one bot.</summary>
    public static long Shared { get; private set; }

    /// <summary>How near the fallen a member of the killer's company must stand to have a share of the price.</summary>
    public static int BloodReach { get; set; } = 24;

    /// <summary>Errands a red was kept out of a guarded town by.</summary>
    public static long Kept { get; private set; }

    /// <summary>Catches that found every cell taken.</summary>
    public static long Overfull { get; private set; }

    private sealed class Record
    {
        public BotMobile Bot;

        /// <summary>Who it is, which outlives the body. See <see cref="Rebound"/>.</summary>
        public string Name;

        public long RedUntil;

        public long JailedUntil;

        public int Cell = -1;

        public string CaughtBy;

        /// <summary>Whether the Baron knows of this murderer: somebody saw it, or named it over a body. See <see cref="Tell"/>.</summary>
        public bool Known;

        public string KnownBy;

        /// <summary>Whether this one is wanted, red or not: grey to everybody until <see cref="WantedUntil"/>. See <see cref="Want"/>.</summary>
        public bool Wanted;

        public long WantedUntil;
    }

    private static readonly Dictionary<Serial, Record> _records = [];

    private static readonly List<Mobile> _hands = [];

    private static readonly List<KeyValuePair<Serial, Record>> _walking = [];

    /// <summary>
    /// Records moved onto a bot's new body after a boot.
    ///
    /// <para>
    /// <b>The population is raised afresh at every boot, and the cells were kept for bodies that no longer exist.</b>
    /// <c>BotPopulation.PurgeSaved</c> deletes every bot the world save brought back and the population is raised again,
    /// its learning carried over by name — so the records this store reads back point at bodies deleted a moment later,
    /// the clock found them deleted and dropped them, and every restart let every prisoner out and cleared every red.
    /// On 17.09.2026 the boot of 06:32 read back "0 red and 3 in the cells, their clocks carried on"; Faron Ashdown,
    /// caught at 06:14 for an hour, was gathering herbs by Britain two seconds later, and Cassia brewing. Rebound by name
    /// once the population stands, and the hour carries on as the line said it would.
    /// </para>
    /// </summary>
    public static long Rebound { get; private set; }

    /// <summary>The living bot of the population with this name, or null.</summary>
    private static BotMobile Named(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false } bot && bot.Name == name)
            {
                return bot;
            }
        }

        return null;
    }

    private static Timer _timer;

    public static void Start()
    {
        Stop();

        _timer = new Clock();
        _timer.Start();
    }

    public static void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    /// <summary>Whether this one is red and at large: lately a murderer, and not in a cell.</summary>
    /// <summary>Every record, for <see cref="BotOutlawStore"/>: the bot, and each clock as what is left of it.</summary>
    internal static void Save(IGenericWriter writer)
    {
        var now = Core.TickCount;
        var kept = 0;

        foreach (var (_, r) in _records)
        {
            if (r.Bot is { Deleted: false })
            {
                kept++;
            }
        }

        writer.WriteEncodedInt(kept);

        foreach (var (_, r) in _records)
        {
            if (r.Bot is not { Deleted: false })
            {
                continue;
            }

            writer.Write(r.Bot);
            writer.Write(Math.Max(0L, r.RedUntil - now));
            writer.Write(Math.Max(0L, r.JailedUntil - now));
            writer.WriteEncodedInt(r.Cell);
            writer.Write(r.CaughtBy ?? "");
            writer.Write(r.Wanted ? Math.Max(0L, r.WantedUntil - now) : 0L);
            writer.Write(r.Known);
            writer.Write(r.KnownBy ?? "");
        }
    }

    /// <summary>The records read back, each clock restarted with what was left of it. Returns how many were red and how many in a cell.</summary>
    internal static (int Reds, int Jailed) Load(IGenericReader reader, int shape)
    {
        var now = Core.TickCount;
        var count = reader.ReadEncodedInt();
        var reds = 0;
        var jailed = 0;

        for (var i = 0; i < count; i++)
        {
            var bot = reader.ReadEntity<BotMobile>();
            var redLeft = reader.ReadLong();
            var jailLeft = reader.ReadLong();
            var cell = reader.ReadEncodedInt();
            var caughtBy = reader.ReadString();

            // Shape 2 (build 114) carries the wanted clock and whether the Baron was told. Shape 1 carried neither, and a red
            // read back from it counts as told, as build 112 meant and did not do: its Load never set it, so every restart
            // left the Baron hunting nobody until somebody saw the red again.
            var wantedLeft = 0L;
            var known = true;
            string knownBy = null;

            if (shape >= 2)
            {
                wantedLeft = reader.ReadLong();
                known = reader.ReadBool();
                knownBy = reader.ReadString();
            }

            if (bot is not { Deleted: false })
            {
                continue;
            }

            var r = new Record
            {
                Bot = bot,
                Name = bot.Name,
                RedUntil = now + Math.Max(0L, redLeft),
                JailedUntil = now + Math.Max(0L, jailLeft),
                Cell = cell,
                CaughtBy = string.IsNullOrEmpty(caughtBy) ? null : caughtBy,
                Known = known,
                KnownBy = string.IsNullOrEmpty(knownBy) ? null : knownBy,
                Wanted = wantedLeft > 0,
                WantedUntil = now + Math.Max(0L, wantedLeft)
            };

            _records[bot.Serial] = r;

            if (cell >= 0)
            {
                jailed++;
            }
            else
            {
                reds++;
            }

            // The engine's own count, which the save carried anyway; put back so the two agree. Not for one only wanted.
            if ((cell >= 0 || redLeft > 0) && bot.Kills < RedKills)
            {
                bot.Kills = RedKills;
            }
        }

        return (reds, jailed);
    }

    public static bool IsRed(Mobile m)
    {
        if (m == null || !_records.TryGetValue(m.Serial, out var r))
        {
            return false;
        }

        return r.Cell < 0 && r.RedUntil - Core.TickCount > 0;
    }

    /// <summary>Whether this one is in a cell.</summary>
    public static bool Jailed(Mobile m) =>
        m != null && _records.TryGetValue(m.Serial, out var r) && r.Cell >= 0 && r.JailedUntil - Core.TickCount > 0;

    /// <summary>The cell this one is in, or nowhere.</summary>
    public static Point3D CellOf(Mobile m) =>
        m != null && _records.TryGetValue(m.Serial, out var r) && r.Cell >= 0 ? Cells[r.Cell] : Point3D.Zero;

    /// <summary>Minutes left in the cell, or nought.</summary>
    public static int LeftInCell(Mobile m) =>
        m != null && _records.TryGetValue(m.Serial, out var r) && r.Cell >= 0
            ? (int)Math.Max(0, (r.JailedUntil - Core.TickCount) / 60000)
            : 0;

    /// <summary>Reds at large right now.</summary>
    public static int RedCount()
    {
        var n = 0;

        foreach (var (_, r) in _records)
        {
            if (r.Cell < 0 && r.RedUntil - Core.TickCount > 0 && r.Bot is { Deleted: false })
            {
                n++;
            }
        }

        return n;
    }

    /// <summary>Murderers made known to the Baron by somebody who saw them or named them over a body.</summary>
    public static long Told { get; private set; }

    /// <summary>
    /// Makes this red known to the Baron, by whom. True the first time. A red read back after a restart is known: the
    /// record kept no witness, and forgetting every wanted murderer at a boot is the amnesty build 101 closed.
    /// </summary>
    public static bool Tell(BotMobile red, string by)
    {
        if (red == null || !_records.TryGetValue(red.Serial, out var r) || r.Known || r.Cell >= 0)
        {
            return false;
        }

        r.Known = true;
        r.KnownBy = by;
        Told++;

        return true;
    }

    /// <summary>
    /// How long a bot stays wanted once somebody has told on it. Half an hour. See <see cref="Want"/>.
    /// </summary>
    public static int WantedMs { get; set; } = 1800000;

    /// <summary>Times somebody was made wanted, or kept wanted longer.</summary>
    public static long WantedPosted { get; private set; }

    /// <summary>Whether this one is wanted and not in a cell.</summary>
    public static bool IsWanted(Mobile m) =>
        m != null && _records.TryGetValue(m.Serial, out var r) && r.Cell < 0 && r.Wanted && r.WantedUntil - Core.TickCount > 0;

    /// <summary>Whether this one is red or wanted: somebody the law-abiding, the Baron's patrol and the guards may set on.</summary>
    public static bool Outlaw(Mobile m) => IsRed(m) || IsWanted(m);

    /// <summary>
    /// Makes this bot wanted on somebody's word: known to the Baron, and a criminal in the engine's reckoning — grey, which
    /// anybody may set on and no healer may help — for <see cref="WantedMs"/>, the engine's two-minute grey renewed on this
    /// clock. Not red: a wanted bot has killed nobody, and its kills are not touched.
    ///
    /// <para>
    /// <b>Patrick's order of 17.09.2026: the price of The Shadow's trade is prison and pursuit.</b> A murderer pays it by the
    /// engine's own count of kills; a blackmailer killed nobody, and the engine has no crime for a threat. So the victim tells
    /// the Baron, and the engine's word for somebody everybody may lawfully set on — a criminal — is what the telling makes
    /// the blackmailer, for as long as the telling lasts.
    /// </para>
    /// </summary>
    public static bool Want(BotMobile bot, string by, string why)
    {
        if (!Running || bot is not { Deleted: false } || Jailed(bot))
        {
            return false;
        }

        var now = Core.TickCount;

        if (!_records.TryGetValue(bot.Serial, out var r))
        {
            _records[bot.Serial] = r = new Record { Bot = bot, Name = bot.Name, RedUntil = now };
        }

        var again = r.Wanted && r.WantedUntil - now > 0;

        r.Wanted = true;
        r.WantedUntil = now + WantedMs;
        r.Known = true;
        r.KnownBy ??= by;
        bot.Criminal = true;
        WantedPosted++;

        // <b>Said when it becomes true, not every time it is said again.</b> A raid is four or five bots searching the
        // same hideout, and each of them names what it finds: at 01:50:43 on 18.09.2026 Nessa Ashdown was reported
        // wanted five times in the same second, on the word of five raiders. The clock is still refreshed; only the
        // line is spared.
        if (!again)
        {
            logger.Information(
                "{Name} is wanted for {Why}, on the word of {By}: grey to everybody for {Minutes} minutes",
                bot.Name,
                why,
                by,
                WantedMs / 60000
            );
        }

        return true;
    }

    /// <summary>Adds the reds and the wanted at large right now to the list. See <see cref="BotLawful"/>.</summary>
    public static void Outlaws(List<BotMobile> into)
    {
        var now = Core.TickCount;

        foreach (var (_, r) in _records)
        {
            if (r.Cell < 0 && (r.RedUntil - now > 0 || r.Wanted && r.WantedUntil - now > 0) && r.Bot is { Deleted: false } bot)
            {
                into.Add(bot);
            }
        }
    }

    /// <summary>Whether the Baron knows of this murderer.</summary>
    public static bool Known(Mobile m) => m != null && _records.TryGetValue(m.Serial, out var r) && r.Known;

    /// <summary>Adds the reds at large right now to the list. See <see cref="BotLawful"/>.</summary>
    public static void Reds(List<BotMobile> into)
    {
        foreach (var (_, r) in _records)
        {
            if (r.Cell < 0 && r.RedUntil - Core.TickCount > 0 && r.Bot is { Deleted: false } bot)
            {
                into.Add(bot);
            }
        }
    }

    /// <summary>Bots in cells right now.</summary>
    public static int JailedCount()
    {
        var n = 0;

        foreach (var (_, r) in _records)
        {
            if (r.Cell >= 0 && r.Bot is { Deleted: false })
            {
                n++;
            }
        }

        return n;
    }

    /// <summary>
    /// The killer goes red. Called by the robbery when its victim falls; the loot is booked apart, because a killer
    /// that dies before it reaches the corpse is a murderer all the same.
    /// </summary>
    public static void Murdered(BotMobile killer, BotMobile victim)
    {
        if (!Running || killer is not { Deleted: false })
        {
            return;
        }

        if (!_records.TryGetValue(killer.Serial, out var r))
        {
            _records[killer.Serial] = r = new Record { Bot = killer, Name = killer.Name };
        }

        r.RedUntil = Core.TickCount + RedMs;

        if (killer.Kills < RedKills)
        {
            killer.Kills = RedKills;
        }

        Murders++;

        // The body is left for somebody to find. See BotInquest.
        BotInquest.Murder(killer, victim);

        logger.Information(
            "{Killer} the {Class} murdered {Victim} at ({X}, {Y}) and is red for {Minutes} minutes",
            killer.Name,
            killer.Class?.Name,
            victim?.Name,
            victim?.X ?? killer.X,
            victim?.Y ?? killer.Y,
            RedMs / 60000
        );
    }

    /// <summary>What a robbery took off the corpse.</summary>
    public static void Took(int coins)
    {
        if (coins > 0)
        {
            Robbed += coins;
        }
    }

    /// <summary>
    /// A bot died: if it was red and a patrol's hand killed it, it is caught. Called from the death hook, before the
    /// body is even cold, so the cell is chosen while the killer is still known.
    /// </summary>
    public static void Fell(BotMobile fallen, Mobile killer)
    {
        if (!Running || fallen == null || !Outlaw(fallen))
        {
            return;
        }

        // <b>A red that falls while a patrol stands against it is the patrol's catch, whoever landed the blow.</b> On
        // 16.09.2026 the Baron's posse fought Gerda Ashdown down — three "finished manhunt: … is down" lines — and the
        // engine's last killer was nobody a bot could be named for (poison, a summoned thing), so she "was killed by
        // something and is not caught by it", rose a minute later still red, walked into Britain and died to the guards.
        var patrol = Hunting(fallen);

        if (killer is not BotMobile by)
        {
            if (patrol != null)
            {
                logger.Information(
                    "{Name}, red, fell to {Killer} while {Patrol}'s patrol stood against it; the patrol has the catch",
                    fallen.Name,
                    killer?.Name ?? "something",
                    patrol.Name
                );

                Jail(fallen, patrol);

                return;
            }

            Unavenged++;

            logger.Information("{Name}, red, was killed by {Killer} and is not caught by it", fallen.Name, killer?.Name ?? "something");

            return;
        }

        Blood(fallen, by);

        var hunter = by.Class is BotBaron || by.Resolve?.Deed is BotBrawl { Kind: BotBrawl.Manhunt } || by.Resolve?.Deed is BotManhunt
                     || BotCrime.Hunts(by, fallen);

        if (!hunter && patrol != null)
        {
            logger.Information(
                "{Name}, red, fell to {Killer} while {Patrol}'s patrol stood against it; the patrol has the catch",
                fallen.Name,
                by.Name,
                patrol.Name
            );

            Jail(fallen, patrol);

            return;
        }

        if (!hunter)
        {
            Unavenged++;

            logger.Information("{Name}, red, was killed by {Killer}, who was not on a patrol; it stays red", fallen.Name, by.Name);

            return;
        }

        Jail(fallen, by);
    }

    /// <summary>
    /// The price of blood, paid at the kill: nothing for the Baron's own hand, the whole of it to a bot that killed
    /// alone, split between the company when it killed in one. See <c>BotUnderworld.Price</c> and <c>BotCity.Blood</c>.
    /// </summary>
    private static void Blood(BotMobile fallen, BotMobile by)
    {
        var price = BotUnderworld.Price(fallen);

        if (price <= 0)
        {
            return;
        }

        // <b>The Baron is the law and is not paid for doing it.</b> Patrick's order: "if the Baron killed it, nobody is
        // given anything" — not the Baron, and not the hands that stood with him either, because the kill was his.
        if (by.Class is BotBaron)
        {
            BaronsOwn++;

            logger.Information("{Name} was worth {Gold}gp, and the Baron took it himself: nobody is paid", fallen.Name, price);

            return;
        }

        _hands.Clear();

        var squad = BotSquads.Of(by);

        if (squad != null)
        {
            var company = squad.Members;

            for (var i = 0; i < company.Count; i++)
            {
                // Only those who were there: a member half the island away did not take this head.
                if (company[i]?.Self is { Deleted: false, Alive: true } member && member.Map == fallen.Map
                    && member.InRange(fallen.Location, BloodReach))
                {
                    _hands.Add(member);
                }
            }
        }

        if (_hands.Count == 0)
        {
            _hands.Add(by);
        }

        Shared += _hands.Count > 1 ? 1 : 0;

        if (BotCity.Blood(fallen, _hands, price) > 0)
        {
            BotUnderworld.Sold(fallen);
        }
    }

    /// <summary>
    /// What a catch takes off a thief: everything it has, and the promise of anything back.
    ///
    /// <para>
    /// <b>Patrick's rule of 18.09.2026:</b> "the bank is docked a thousand and the whole corpse is confiscated; being
    /// caught is the very worst thing that can happen to a bandit, because it literally loses everything and its
    /// progress stalls." The first cut of this, written the night before, took the purse and the loose goods at the
    /// cell door and spared the gear — and so took nothing at all, because <b>every catch on this shard is a kill</b>.
    /// The purse, the tools and the armour are in a corpse by the time this runs, and the hunter opens it a minute
    /// later. The corpse is taken here, in the death hook, while the killer is still standing over it.
    /// </para>
    ///
    /// <para>
    /// <b>And the bond with it, or none of the rest means anything.</b> Bound gear does not survive death by any engine
    /// rule on this shard — <c>LootType.Newbied</c> does nothing under AOS — it survives because
    /// <c>BotBinding.Restore</c> hands the bot another one when it rises. Emptying the corpse without breaking the bond
    /// would take a bandit's sword and hand it a new sword at the cell door.
    /// </para>
    /// </summary>
    private static void Seize(BotMobile bot)
    {
        if (!Seizes || bot is not { Deleted: false })
        {
            return;
        }

        // The weapons are lifted into the pack first and passed over by everything below. See Arms (12.10.2026).
        // A corpse only for a body that has just fallen: one that yields alive may still have an old corpse somewhere it
        // never went back to, and the cell door is no free errand to it (both reviews, 12.10.2026).
        var arms = Arms(bot, TakesCorpse && !bot.Alive ? bot.Remains : null);

        var took = 0;
        var coins = bot.Backpack is { } pack ? Empty(pack, arms, ref took) : 0;

        // The corpse is emptied and left where it lies rather than deleted: the engine is still inside its own death
        // handler and the lines below this one in BotMobile.OnDeath read the corpse for the killer. An empty corpse is
        // already understood everywhere — BotUndertaker drops a corpse with nothing in it.
        if (TakesCorpse && bot.Remains is { Deleted: false } corpse)
        {
            coins += Empty(corpse, arms, ref took);
            bot.Remains = null;
            SeizedCorpse++;
        }

        var kit = TakesBond ? BotBinding.Forfeit(bot, bot.Bond, KeepsWeapons ? arms : null) : 0;

        if (kit > 0)
        {
            Stripped++;
        }

        var fine = BankFine > 0 ? Math.Min(BankFine, Mobiles.Banker.GetBalance(bot)) : 0;

        if (fine > 0 && !Mobiles.Banker.Withdraw(bot, fine))
        {
            fine = 0;
        }

        BotCity.Tax(coins + fine);
        SeizedGold += coins;
        SeizedBank += fine;
        SeizedThings += took;

        if (coins <= 0 && fine <= 0 && took <= 0 && kit <= 0)
        {
            return;
        }

        Seized++;

        logger.Information(
            "{Name} was stripped at the cell door: {Gold}gp off the body, {Fine}gp out of its bank, {Things} things and {Kit} of its own kit forfeit; {Arms} weapons and quivers left to it",
            bot.Name,
            coins,
            fine,
            took,
            kit,
            arms.Count
        );
    }

    /// <summary>
    /// The weapons a catch leaves a thief, lifted out of the corpse and out of any bag into the top of its pack, where
    /// <c>BotMobile.Rearm</c> puts them on again when it rises.
    ///
    /// <para>
    /// <b>Its weapons, not every blade it carries.</b> What it had in its hands — on the body if it yielded alive, in the
    /// corpse's <c>EquipItems</c> if it was brought down — and every weapon bound to it, a bound knife or pickaxe included
    /// (Patrick: "the bound weapon must not be taken"). A spare sword picked up as loot is takings and goes with the rest.
    /// Worn weapons stay where they are: <see cref="Empty"/> never touches the body.
    /// </para>
    /// </summary>
    private static HashSet<Item> Arms(BotMobile bot, Corpse corpse)
    {
        HashSet<Item> arms = [];

        if (!KeepsWeapons || bot.Backpack is not { } pack)
        {
            return arms;
        }

        for (var i = 0; i < bot.Items.Count; i++)
        {
            if (bot.Items[i] is BaseWeapon { Deleted: false } held)
            {
                arms.Add(held);
            }
        }

        if (corpse is { Deleted: false, EquipItems: { } equipped })
        {
            for (var i = 0; i < equipped.Count; i++)
            {
                if (equipped[i] is BaseWeapon { Deleted: false } weapon && weapon.IsChildOf(corpse))
                {
                    arms.Add(weapon);
                }
            }
        }

        foreach (var weapon in pack.FindItemsByType<BaseWeapon>())
        {
            if (BotBinding.IsBound(weapon, bot.Bond))
            {
                arms.Add(weapon);
            }
        }

        if (corpse is { Deleted: false })
        {
            foreach (var weapon in corpse.FindItemsByType<BaseWeapon>())
            {
                if (BotBinding.IsBound(weapon, bot.Bond))
                {
                    arms.Add(weapon);
                }
            }
        }

        foreach (var weapon in arms)
        {
            if (weapon.Parent != bot && weapon.Parent != pack)
            {
                pack.DropItem(weapon);
            }
        }

        WeaponsKept += arms.Count;

        // And the quiver, by count: the thief keeps min(carried, granted) of each bound kind, as any death lets it (Patrick,
        // 12.10.2026: "keep the bound count"). TrimAmmunition lifts exactly that much into the top of the pack and leaves the
        // rest in the corpse for Empty to take; the death hook's own call of it afterwards finds nothing to trim.
        if (bot.Bond is { } bond && bond.Ammunition.Count > 0)
        {
            BotBinding.TrimAmmunition(bot, bond, corpse);

            for (var i = 0; i < pack.Items.Count; i++)
            {
                if (pack.Items[i] is { Deleted: false } stack && bond.Ammunition.ContainsKey(stack.GetType()))
                {
                    arms.Add(stack);
                    AmmunitionKept += stack.Amount;
                }
            }
        }

        return arms;
    }

    /// <summary>
    /// Empties a box into the treasury: the coin counted and handed back to the caller, everything else destroyed.
    ///
    /// <para>
    /// Gold is asked for by type rather than swept up by hand, because a purse inside a bag is still money; everything
    /// else is deleted at the top level, which takes a bag's contents with it — except what <see cref="Arms"/> left to it.
    /// </para>
    /// </summary>
    private static int Empty(Container box, HashSet<Item> spared, ref int took)
    {
        var coins = box.GetAmount(typeof(Gold));

        if (coins > 0 && !box.ConsumeTotal(typeof(Gold), coins))
        {
            coins = 0;
        }

        List<Item> forfeit = [];

        for (var i = 0; i < box.Items.Count; i++)
        {
            if (box.Items[i] is { Deleted: false } item && !spared.Contains(item))
            {
                forfeit.Add(item);
            }
        }

        for (var i = 0; i < forfeit.Count; i++)
        {
            took++;
            forfeit[i].Delete();
        }

        return coins;
    }

    /// <summary>The bot leading a patrol against this red now, or a posse member's, or null.</summary>
    private static BotMobile Hunting(BotMobile red)
    {
        var bots = BotPopulation.Bots;
        BotMobile posse = null;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is not { Deleted: false } || bot == red)
            {
                continue;
            }

            switch (bot.Resolve?.Deed)
            {
                case BotManhunt hunt when ReferenceEquals(hunt.Foe, red):
                    return bot;

                case BotBrawl { Kind: BotBrawl.Manhunt } brawl when ReferenceEquals(brawl.Foe, red):
                    posse ??= bot;

                    break;
            }
        }

        return posse;
    }

    /// <summary>
    /// Hands every bot a catch left bare its weapon back: the bandits caught before Patrick's rule of 12.10.2026 that a catch
    /// leaves the weapon. By hand through Argus's door ("rearm"); safe to ask again. See <c>BotBinding.Reissue</c>.
    /// </summary>
    public static string ReturnWeapons()
    {
        var bots = BotPopulation.Bots;
        List<string> names = [];
        var handed = 0;

        for (var i = 0; i < bots.Count; i++)
        {
            var given = BotBinding.Reissue(bots[i]);

            if (given > 0)
            {
                handed += given;
                names.Add(bots[i].Name);
            }
        }

        logger.Information("{Count} weapons handed back to {Bots} bots a catch had left bare", handed, names.Count);

        return handed == 0
            ? "nobody was missing the weapon it was born to."
            : $"{handed} weapons handed back to {names.Count} bots: {string.Join(", ", names)}. Rearm puts them in the hand within {BotMobile.DressEveryMs / 1000}s.";
    }

    /// <summary>
    /// Lets this one out at the clock's next look, its term cut short: a ransom paid (07.10.2026, crime17). True when it was in a
    /// cell. See <c>BotCrime.Ransom</c>.
    /// </summary>
    public static bool Free(Mobile m)
    {
        if (m == null || !_records.TryGetValue(m.Serial, out var r) || r.Cell < 0)
        {
            return false;
        }

        r.JailedUntil = Core.TickCount;

        return true;
    }

    /// <summary>Puts this one in the first free cell. The body is moved by the clock once it is alive.</summary>
    public static bool Jail(BotMobile bot, Mobile by)
    {
        if (!Running || bot is not { Deleted: false })
        {
            return false;
        }

        var cell = FreeCell();

        if (cell < 0)
        {
            Overfull++;

            logger.Warning("{Name} was caught by {By} but every cell is taken; it stays red", bot.Name, by?.Name);

            return false;
        }

        if (!_records.TryGetValue(bot.Serial, out var r))
        {
            _records[bot.Serial] = r = new Record { Bot = bot, Name = bot.Name };
        }

        r.Cell = cell;
        r.Wanted = false;
        // A term of the town's for a crime on its wanted list, doubling for a repeat; a murderer's hour otherwise (crime17).
        r.JailedUntil = Core.TickCount + BotCrime.Term(bot, JailMs);
        r.CaughtBy = by?.Name;
        Caught++;
        BotUnderworld.Jailed(bot);

        Seize(bot);

        // The city's price on this head, if there is one, to whoever caught it. See BotCity.Head.
        BotCity.Collect(bot, by);

        // And the town's bounty off its wanted list, out of the same purse, and the term begun on the town's books (crime17).
        BotCrime.Caught(bot, by);

        logger.Information(
            "{Name} was caught by {By} and is sentenced to cell {Cell} at ({X}, {Y}) for {Minutes} minutes",
            bot.Name,
            by?.Name,
            cell,
            Cells[cell].X,
            Cells[cell].Y,
            JailMs / 60000
        );

        return true;
    }

    private static int FreeCell()
    {
        for (var i = 0; i < Cells.Length; i++)
        {
            var taken = false;

            foreach (var (_, r) in _records)
            {
                if (r.Cell == i && r.Bot is { Deleted: false })
                {
                    taken = true;

                    break;
                }
            }

            if (!taken)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// The nearest red at large on this map within range, or null. For the Baron's patrol.
    /// </summary>
    public static BotMobile AtLarge(Map map, Point3D from, int range)
    {
        BotMobile best = null;
        var bestAway = double.MaxValue;

        foreach (var (_, r) in _records)
        {
            var bot = r.Bot;

            if (bot is not { Deleted: false, Alive: true } || bot.Map != map || r.Cell >= 0
                || r.RedUntil - Core.TickCount <= 0 && !(r.Wanted && r.WantedUntil - Core.TickCount > 0))
            {
                continue;
            }

            // <b>Only a murderer the Baron has been told of, and can see.</b> See Tell and BotInquest: Patrick's order of
            // 17.09.2026 — the Baron learns of a murder from whoever finds the body, and a hidden red is not found by a march.
            if (!r.Known || bot.Hidden)
            {
                continue;
            }

            var away = Math.Max(Math.Abs(bot.X - from.X), Math.Abs(bot.Y - from.Y));

            if (away > range || away >= bestAway)
            {
                continue;
            }

            best = bot;
            bestAway = away;
        }

        return best;
    }

    /// <summary>
    /// One of ours set on this bot, within range, or null: a red with this bot as its combatant, or a robber in the
    /// act — which is not red yet, because red comes with the kill. What a victim hits back at.
    /// </summary>
    public static BotMobile Assailant(Mobile body, int range)
    {
        var map = body?.Map;

        if (!Running || map == null || map == Map.Internal)
        {
            return null;
        }

        foreach (var other in map.GetMobilesInRange<BotMobile>(body.Location, range))
        {
            if (other != body && other.Alive && Assailing(other, body))
            {
                return other;
            }
        }

        return null;
    }

    /// <summary>Whether this one is set on that one: a red with it as combatant, or a robbery with it as the victim.</summary>
    public static bool Assailing(BotMobile other, Mobile victim) =>
        other?.Resolve?.Deed is BotRob rob && ReferenceEquals(rob.Foe, victim)
        || ReferenceEquals(other?.Combatant, victim) && Outlaw(other);

    /// <summary>Whether this red should be kept out of that place: guarded ground, where the engine's guards would kill it.</summary>
    public static bool Keeps(Mobile body, Map map, Point3D where)
    {
        if (!Running || map == null || where == Point3D.Zero || !Outlaw(body))
        {
            return false;
        }

        if (Region.Find(where, map)?.GetRegion<GuardedRegion>() is { } guarded && !guarded.IsDisabled())
        {
            Kept++;

            return true;
        }

        // <b>And the road, because the veto was on where the work is and a road is not asked.</b> Gerda Ashdown, red,
        // rose at her guild's seat at 11:20 on 16.09.2026, took a prowl towards (1350, 1422) — outside the guarded region
        // — and walked through Britain to get there, where the guards killed her; and again two minutes later. The
        // straight line from the bot to the work is sampled every few tiles; a sample inside a guarded town is a road
        // through it, near enough, and the work is refused.
        if (body.Map == map && Crosses(map, body.Location, where))
        {
            Routed++;

            return true;
        }

        return false;
    }

    /// <summary>How many tiles apart the samples along a red's road are taken.</summary>
    public static int Stride { get; set; } = 12;

    /// <summary>Work refused to a red for a road that would cross a guarded town.</summary>
    public static long Routed { get; private set; }

    /// <summary>
    /// Whether a red walks round the guarded towns rather than through them. Off, only the work and its straight road are
    /// asked, as before build 111.
    ///
    /// <para>
    /// <b>The road the walker draws is not the straight one.</b> Elspeth, red for Joss since 19:21:47 on 17.09.2026, took a
    /// prowl from about (1474, 1152) towards (966, 1590): the work outside every town, and the straight line between
    /// passing north of Britain's western ward, so <see cref="Keeps"/> let it. The road bent round the ground in between,
    /// went three tiles into the ward at (1160, 1541), and she was killed there at 19:30:38 with no spawner near; Patrick
    /// watched her walk in. So a red's every search keeps out of guarded ground (<see cref="Road"/>) unless it stands in
    /// a town already or is walking into one, and its every step from open ground into a guarded town is refused
    /// (<see cref="Steps"/>), whatever drew the step. A prisoner is not red, so a sentence walks as before.
    /// </para>
    /// </summary>
    public static bool WalksRound { get; set; } = true;

    /// <summary>A red's searches drawn round the guarded towns.</summary>
    public static long Detoured { get; private set; }

    /// <summary>A red's steps refused from open ground into a guarded town.</summary>
    public static long Stopped { get; private set; }

    private static readonly Dictionary<Serial, long> _stoppedSaid = [];

    /// <summary>Whether this place lies in a guarded town whose guards are about.</summary>
    public static bool Guarded(Map map, int x, int y, int z = 0) =>
        map != null && Region.Find(new Point3D(x, y, z), map)?.GetRegion<GuardedRegion>() is { } guarded && !guarded.IsDisabled();

    /// <summary>Whether this bot's roads are drawn round the towns, which is also why its refused roads prove nothing about a place.</summary>
    public static bool KeptRound(Mobile bot) => Running && WalksRound && Outlaw(bot);

    /// <summary>
    /// What a search for this bot's road keeps out of: the avoid it was given, and the guarded towns too when it is a red
    /// standing in open ground and walking to open ground. See <see cref="WalksRound"/>.
    /// </summary>
    public static BotAvoid Road(Mobile bot, Map map, Point3D target, BotAvoid avoid)
    {
        if (bot == null || map == null || !KeptRound(bot) || Guarded(map, bot.X, bot.Y, bot.Z) || Guarded(map, target.X, target.Y, target.Z))
        {
            return avoid;
        }

        Detoured++;

        return avoid.Towns(map);
    }

    /// <summary>Whether this bot may take this step: anything but a red's step from open ground into a guarded town. See <see cref="WalksRound"/>.</summary>
    public static bool Steps(Mobile bot, Direction d)
    {
        if (bot?.Map is not { } map || map == Map.Internal || !KeptRound(bot) || Guarded(map, bot.X, bot.Y, bot.Z))
        {
            return true;
        }

        int x = bot.X, y = bot.Y;
        global::Server.Movement.Movement.Offset(d, ref x, ref y);

        if (!Guarded(map, x, y, bot.Z))
        {
            return true;
        }

        Stopped++;

        var now = Core.TickCount;

        if (!_stoppedSaid.TryGetValue(bot.Serial, out var said) || now - said >= 60000)
        {
            _stoppedSaid[bot.Serial] = now;

            logger.Information(
                "{Name}, red, was refused a step into {Town} at ({X}, {Y})",
                bot.Name,
                Region.Find(new Point3D(x, y, bot.Z), map)?.GetRegion<GuardedRegion>()?.Name ?? "a guarded town",
                x,
                y
            );
        }

        return false;
    }

    private static bool Crosses(Map map, Point3D from, Point3D to)
    {
        var steps = Math.Max(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y)) / Math.Max(1, Stride);

        for (var i = 1; i < steps; i++)
        {
            var x = from.X + (to.X - from.X) * i / steps;
            var y = from.Y + (to.Y - from.Y) * i / steps;

            if (Region.Find(new Point3D(x, y, map.GetAverageZ(x, y)), map)?.GetRegion<GuardedRegion>() is { } guarded && !guarded.IsDisabled())
            {
                return true;
            }
        }

        return false;
    }

    private static void Tick()
    {
        if (!Running || _records.Count == 0)
        {
            return;
        }

        var now = Core.TickCount;
        List<Serial> gone = null;
        List<(Serial Was, Record Record)> rebound = null;

        // <b>Walked over a copy, because things that happen inside this loop write to the book it is reading.</b> The
        // shard died six seconds after the boot of 00:21:05 on 18.09.2026 with "Collection was modified; enumeration
        // operation may not execute" out of this line: releasing a bot from its cell settles whatever it was holding,
        // and a settled chase after a witness tells the Baron (BotFence.Tell), which makes a bot wanted, which adds a
        // record. Removals were already deferred to the end of the loop; this makes the reading safe against anything
        // the loop sets off, not only against the two cases somebody thought of.
        _walking.Clear();

        foreach (var pair in _records)
        {
            _walking.Add(pair);
        }

        for (var w = 0; w < _walking.Count; w++)
        {
            var (serial, r) = _walking[w];

            var bot = r.Bot;

            if (bot is not { Deleted: false })
            {
                // The body went with the boot's purge: the same bot stands again under its name. See Rebound.
                var again = Named(r.Name);

                if (again == null)
                {
                    // Not raised yet, so not gone yet.
                    if (BotPopulation.Count > 0)
                    {
                        (gone ??= []).Add(serial);
                    }

                    continue;
                }

                r.Bot = again;
                bot = again;
                (rebound ??= []).Add((serial, r));
                Rebound++;

                if (r.Cell < 0 && r.RedUntil - now > 0 && again.Kills < RedKills)
                {
                    again.Kills = RedKills;
                }
            }

            if (r.Cell >= 0)
            {
                if (now - r.JailedUntil >= 0)
                {
                    Release(bot, r);
                    (gone ??= []).Add(serial);
                }
                else if (bot.Alive && !InCell(bot, r))
                {
                    Place(bot, r);
                }

                continue;
            }

            if (r.Wanted && now - r.WantedUntil >= 0)
            {
                r.Wanted = false;

                logger.Information("{Name} is no longer wanted", bot.Name);
            }
            else if (r.Wanted && bot.Alive && !bot.Criminal)
            {
                // The engine's grey lasts two minutes; the wanted half hour is this record's. See Want.
                bot.Criminal = true;
            }

            if (now - r.RedUntil >= 0 && bot.Kills >= RedKills)
            {
                bot.Kills = 0;
                Redeemed++;

                logger.Information("{Name}'s hour is up; it is no longer red", bot.Name);
            }

            if (now - r.RedUntil >= 0 && !r.Wanted)
            {
                (gone ??= []).Add(serial);
            }
        }

        if (rebound != null)
        {
            for (var i = 0; i < rebound.Count; i++)
            {
                _records.Remove(rebound[i].Was);
                _records[rebound[i].Record.Bot.Serial] = rebound[i].Record;
            }
        }

        if (gone != null)
        {
            for (var i = 0; i < gone.Count; i++)
            {
                _records.Remove(gone[i]);
            }
        }
    }

    private static bool InCell(BotMobile bot, Record r) =>
        bot.Map == (BotPopulation.Home ?? Map.Felucca) && Utility.InRange(bot.Location, Cells[r.Cell], 3);

    private static void Place(BotMobile bot, Record r)
    {
        var map = BotPopulation.Home ?? Map.Felucca;
        var cell = Cells[r.Cell];

        bot.Combatant = null;
        bot.Warmode = false;
        bot.MoveToWorld(cell, map);
        bot.Journey?.Discard();

        if (bot.Resolve?.Deed is not BotSentence)
        {
            BotWill.Press(bot, new BotSentence(map, cell), $"caught by {r.CaughtBy ?? "a patrol"}");
        }
    }

    private static void Release(BotMobile bot, Record r)
    {
        bot.Kills = 0;
        Released++;

        var map = BotPopulation.Home ?? Map.Felucca;
        // Where it lives (29.09.2026). See BotPopulation.HomeOf.
        var home = BotPopulation.HomeOf(bot);

        if (bot.Alive)
        {
            bot.MoveToWorld(home, map);
            bot.Journey?.Discard();
        }

        logger.Information("{Name} is let out of cell {Cell} after {Minutes} minutes and put down at ({X}, {Y})", bot.Name, r.Cell, JailMs / 60000, home.X, home.Y);

        // The grey lifted, a criminal its guild would not ransom turned out of it, and the repentance clock started (crime17).
        BotCrime.Freed(bot);
    }

    public static string Describe() =>
        !Running
            ? "nobody robs, nobody is red and nobody is jailed"
            : $"{Murders} murders, {Robbed}gp robbed, {Caught} murderers caught by a patrol and {Released} let out, {Redeemed} went unpunished until the hour ran out, {Unavenged} were killed by something that was no patrol, {BaronsOwn} were the Baron's own kill and paid nobody, {Seized} catches stripped {SeizedGold}gp off the body, {SeizedBank}gp out of the bank and {SeizedThings} things into the treasury, {SeizedCorpse} corpses emptied where they lay and {Stripped} kits forfeit, {WeaponsKept} weapons and {AmmunitionKept} rounds left to the caught, {Shared} prices of blood split between a company; "
              + $"{RedCount()} red now and {JailedCount()} in cells of {Cells.Length}; {Told} made known to the Baron, {WantedPosted} made wanted; {Kept} errands kept a red out of a guarded town and {Routed} off a road through one, {Detoured} red roads drawn round the towns and {Stopped} red steps refused at a ward's edge; {Overfull} catches found every cell taken; {Rebound} records moved onto a bot's new body after a boot";

    public static void Forget()
    {
        _records.Clear();
        Murders = 0;
        Robbed = 0;
        Caught = 0;
        Released = 0;
        Redeemed = 0;
        Unavenged = 0;
        BaronsOwn = 0;
        Seized = 0;
        SeizedGold = 0;
        SeizedThings = 0;
        SeizedBank = 0;
        SeizedCorpse = 0;
        Stripped = 0;
        WeaponsKept = 0;
        AmmunitionKept = 0;
        Shared = 0;
        Kept = 0;
        Routed = 0;
        Told = 0;
        WantedPosted = 0;
        Detoured = 0;
        Stopped = 0;
        _stoppedSaid.Clear();
        Overfull = 0;
        Rebound = 0;
    }

    private sealed class Clock : Timer
    {
        public Clock() : base(TimeSpan.FromMilliseconds(Math.Max(250, EveryMs)), TimeSpan.FromMilliseconds(Math.Max(250, EveryMs)))
        {
        }

        protected override void OnTick()
        {
            // The same clock rolls the fighters for the thought of robbery: the thought is pressed rather than offered,
            // so nothing in the auction ever asks for it. See BotRobber.
            BotRobber.Beat(Core.TickCount);
            BotLawful.Beat(Core.TickCount);
            BotInquest.Beat(Core.TickCount);
            BotUnderworld.Beat(Core.TickCount);
            BotLair.Watch(Core.TickCount);
            BotFence.Beat(Core.TickCount);

            // The town's books and the underworld's trades: postings, ransoms, repentance, the recruit and the ambush (crime17).
            BotCrime.Beat(Core.TickCount);
            Tick();
        }
    }
}

/// <summary>
/// Sitting in a cell: a piece of work that does nothing until the clock lets the bot go. Pressed by
/// <see cref="BotOutlaw"/>, and the only thing a jailed bot holds.
/// </summary>
public sealed class BotSentence : BotDeed
{
    public const string Trade = "sentence";

    private readonly Map _map;

    private readonly Point3D _cell;

    public BotSentence(Map map, Point3D cell)
    {
        _map = map;
        _cell = cell;
    }

    public override string Kind => Trade;

    public override bool Committed => true;

    public override bool Steadfast => true;

    public override bool Summons => true;

    public override bool Braves => true;

    public override bool Unpaid => true;

    public override bool Still => true;

    public override Map Map => _map;

    public override Point3D Where => _cell;

    public override double Expects => 1.0;

    public override double Minutes => BotOutlaw.JailMs / 60000.0;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override int Made => 0;

    public override string Stage => "in a cell";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null)
        {
            return BotDoing.Failed("no body");
        }

        if (!BotOutlaw.Jailed(body))
        {
            return BotDoing.Done("let out");
        }

        return BotDoing.Work($"in a cell, {BotOutlaw.LeftInCell(body)} min left");
    }
}

/// <summary>
/// The Baron's patrol against a murderer: the Baron goes after the red, and presses the nearest few fit fighters into a
/// posse against the same one. A red the posse kills is caught — see <see cref="BotOutlaw.Fell"/>.
/// </summary>
public sealed class BotManhunt : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotManhunt));

    public const string Trade = "manhunt";

    /// <summary>How many fighters the Baron presses into the posse.</summary>
    public static int Posse { get; set; } = 4;

    /// <summary>How far around the Baron the posse is drawn from.</summary>
    public static int Reach { get; set; } = 60;

    /// <summary>Patrols raised.</summary>
    public static long Raised { get; private set; }

    /// <summary>Fighters pressed into posses.</summary>
    public static long Pressed { get; private set; }

    private readonly BotMobile _red;

    private readonly BotBrawl _fight;

    public BotManhunt(BotMobile red, SkillName trains)
    {
        _red = red;
        _fight = new BotBrawl(red, BotBrawl.Manhunt, trains, Over);
    }

    /// <summary>The rule every hand in the posse follows: over when the red is down, caught, or no longer red.</summary>
    public static string Over(IBotWilful bot, BotBrawl brawl)
    {
        var red = brawl?.Foe as BotMobile;

        if (red is not { Deleted: false, Alive: true })
        {
            return $"{red?.Name ?? "the murderer"} is down";
        }

        if (BotOutlaw.Jailed(red))
        {
            return $"{red.Name} is in a cell";
        }

        // A wanted thief that is no murderer, beaten down, gives itself up and goes to a cell alive (07.10.2026, crime17).
        if (BotCrime.Yield(bot, red))
        {
            return $"{red.Name} yielded";
        }

        return BotOutlaw.Outlaw(red) ? null : $"{red.Name} is no longer red or wanted";
    }

    public override string Kind => Trade;

    public override bool Committed => true;

    public override bool Steadfast => true;

    public override bool Braves => true;

    public override bool Unpaid => true;

    public override Mobile Foe => _red;

    public override Map Map => _fight.Map;

    public override Point3D Where => _fight.Where;

    public override double Expects => BotBrawl.Prior;

    public override double Minutes => BotBrawl.WorkMinutes;

    public override SkillName? Trains => _fight.Trains;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override int Made => 0;

    public override string Stage => $"patrol against {_red?.Name}: {_fight.Stage}";

    /// <summary>Raises the posse: the nearest fit fighters, pressed against the same red.</summary>
    public override void Taken(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (body == null || map == null || map == Map.Internal || _red == null)
        {
            return;
        }

        Raised++;

        var pressed = 0;
        List<BotMobile> near = [];

        foreach (var other in map.GetMobilesInRange<BotMobile>(body.Location, Reach))
        {
            if (other == body || other == _red || !other.Alive || other.Squad != null || other.Class is not { } klass
                || klass.Role is BotRole.Producer or BotRole.Medic || klass.Unpaid || BotOutlaw.Outlaw(other)
                || BotDuel.Duelling(other) || other.Resolve?.Deed is BotBrawl || BotUnderworld.Member(other))
            {
                continue;
            }

            near.Add(other);
        }

        near.Sort((x, y) => x.GetDistanceToSqrt(body).CompareTo(y.GetDistanceToSqrt(body)));

        for (var i = 0; i < near.Count && pressed < Posse; i++)
        {
            var hand = near[i];
            var trains = hand.Bond?.Weapon?.Skill ?? SkillName.Wrestling;

            if (BotWill.Press(hand, new BotBrawl(_red, BotBrawl.Manhunt, trains, Over), $"the Baron's patrol against {_red.Name}"))
            {
                pressed++;
            }
        }

        Pressed += pressed;

        logger.Information(
            "{Baron} raises a patrol of {Count} against {Red} the murderer, red at ({X}, {Y})",
            body.Name,
            pressed,
            _red.Name,
            _red.X,
            _red.Y
        );
    }

    public override BotDoing Advance(IBotWilful bot) => _fight.Advance(bot);

    public override bool Bend(IBotWilful bot) => _fight.Bend(bot);

    public override void Drop(IBotWilful bot) => _fight.Drop(bot);

    public static string Describe() => $"{Raised} patrols raised against murderers, {Pressed} fighters pressed into them; {BotLawful.Describe()}; {BotInquest.Describe()}";

    public static void Forget()
    {
        Raised = 0;
        Pressed = 0;
    }
}
