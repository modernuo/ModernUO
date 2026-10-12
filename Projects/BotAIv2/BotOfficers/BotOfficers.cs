using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Server.Guilds;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>The three posts a guild's head gives out: the store, the stand by the bank, and the road.</summary>
public enum BotOfficerPost
{
    Quartermaster,
    Herald,
    Scout
}

/// <summary>
/// A guild's officers: who holds which post, how they are chosen, and what the guild hears of them.
///
/// <para>
/// <b>Patrick's approval of 07.10.2026 (night 17, "a livelier world"): three officers appointed by the head, each with real
/// actions and its own log signature.</b> The quartermaster keeps the guild's store at the hall (<see cref="BotGuildStore"/>,
/// <see cref="BotQuartermaster"/>), the herald keeps a recruiting stand by the bank, reads the guild's news and challenges a rival
/// to a bout (<see cref="BotHeraldStand"/>, <see cref="BotGuildBout"/>), and the scout walks the dangerous ground by a plan, reaches
/// rumoured chests first and shadows another guild's company (<see cref="BotGuildScout"/>).
/// </para>
///
/// <para>
/// <b>The head appoints, by a rule anybody can read.</b> Every <see cref="TermMs"/> of running time (six hours) the head reviews
/// each post and gives it to the member who fits it best — a quartermaster from the makers, a herald from whoever has the most
/// renown, a scout from the shooters and the strong — a veteran first among equals (<see cref="BotTraits.Veteran"/>). A post
/// whose holder died for good, left, or became the head is filled at once. The holder keeps it unless the best fits a fifth
/// better (<see cref="Margin"/>), so a post does not change hands on noise. Nobody holds two posts and the head holds none.
/// </para>
///
/// <para>
/// <b>Kept across restarts</b> by <see cref="BotOfficersStore"/>: the holders by name with the age of their appointment.
/// </para>
/// </summary>
public static class BotOfficers
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotOfficers));

    /// <summary>Whether guilds have officers at all. Off: nobody is appointed and no officer's work is offered.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>Each post, its own switch.</summary>
    public static bool Quartermasters { get; set; } = true;

    public static bool Heralds { get; set; } = true;

    public static bool Scouts { get; set; } = true;

    /// <summary>How often the head reviews a post. Six hours, by Patrick's order.</summary>
    public static int TermMs { get; set; } = 21600000;

    /// <summary>How often a post nobody fitted is looked at again.</summary>
    public static int EmptyRetryMs { get; set; } = 1800000;

    /// <summary>How much better the best must fit than the holder before a review gives the post away.</summary>
    public static double Margin { get; set; } = 1.2;

    /// <summary>The fewest members a guild must have before it has officers: below it the guild is its head and a friend.</summary>
    public static int LeastMembers { get; set; } = 4;

    /// <summary>What a veteran's fit is multiplied by.</summary>
    public static double VeteranLean { get; set; } = 1.4;

    /// <summary>How long a member counts as a newcomer after the officers first see it in the guild. Two hours.</summary>
    public static int NewcomerMs { get; set; } = 7200000;

    /// <summary>How long a piece of guild news is worth reading aloud. Two hours.</summary>
    public static int NewsMs { get; set; } = 7200000;

    /// <summary>The most pieces of news a guild keeps waiting for its herald.</summary>
    public static int MostNews { get; set; } = 8;

    // ---- counters ----------------------------------------------------------------------------------------------------------

    public static long Appointed { get; private set; }

    public static long ByGone { get; private set; }

    public static long ByTerm { get; private set; }

    public static long ByHand { get; private set; }

    public static long Kept { get; private set; }

    public static long NoneFit { get; private set; }

    public static long NewsPosted { get; private set; }

    // ---- the posts ---------------------------------------------------------------------------------------------------------

    private sealed class Post
    {
        public string Holder;

        public long Since;

        public long Reviewed;

        public string Why;
    }

    private static readonly Dictionary<(string Guild, BotOfficerPost Office), Post> _posts = [];

    private static readonly Dictionary<string, List<(string Text, long Tick)>> _news = new(StringComparer.Ordinal);

    private static readonly Dictionary<Serial, (string Guild, long Tick)> _firstSeen = [];

    private static readonly Dictionary<string, BotMobile> _byName = new(StringComparer.Ordinal);

    private static bool _seenOnce;

    private static long _lastLook;

    /// <summary>Every post there is, in the order they are spoken of.</summary>
    public static readonly BotOfficerPost[] Offices = [BotOfficerPost.Quartermaster, BotOfficerPost.Herald, BotOfficerPost.Scout];

    public static string Word(BotOfficerPost office) =>
        office switch
        {
            BotOfficerPost.Quartermaster => "quartermaster",
            BotOfficerPost.Herald => "herald",
            _ => "scout"
        };

    public static bool Parse(string word, out BotOfficerPost office)
    {
        office = BotOfficerPost.Quartermaster;

        switch ((word ?? "").Trim().ToLowerInvariant())
        {
            case "quartermaster":
            case "qm":
            case "store":
                office = BotOfficerPost.Quartermaster;

                return true;
            case "herald":
                office = BotOfficerPost.Herald;

                return true;
            case "scout":
                office = BotOfficerPost.Scout;

                return true;
            default:
                return false;
        }
    }

    /// <summary>Whether this post is switched on.</summary>
    public static bool On(BotOfficerPost office) =>
        Running && office switch
        {
            BotOfficerPost.Quartermaster => Quartermasters,
            BotOfficerPost.Herald => Heralds,
            _ => Scouts
        };

    /// <summary>Whether a guild may have officers at all: not The Shadow, not disbanded.</summary>
    public static bool Keeps(Guild guild) => guild is { Disbanded: false, Name: not null } && !BotUnderworld.Band(guild);

    /// <summary>The bot holding this post in this guild, if it is one of ours and still in the guild; else null.</summary>
    public static BotMobile Of(Guild guild, BotOfficerPost office)
    {
        if (!On(office) || guild?.Name == null || !_posts.TryGetValue((guild.Name, office), out var post) || post.Holder == null)
        {
            return null;
        }

        return _byName.TryGetValue(post.Holder, out var bot) && bot is { Deleted: false } && ReferenceEquals(bot.Guild, guild) ? bot : null;
    }

    /// <summary>Whether this bot holds this post in its own guild now.</summary>
    public static bool Holds(BotMobile bot, BotOfficerPost office) =>
        bot?.Guild is Guild guild && ReferenceEquals(Of(guild, office), bot);

    /// <summary>The post this bot holds, if any.</summary>
    public static BotOfficerPost? OfficeOf(BotMobile bot)
    {
        if (bot?.Guild is not Guild guild || bot.Name == null)
        {
            return null;
        }

        for (var i = 0; i < Offices.Length; i++)
        {
            if (_posts.TryGetValue((guild.Name, Offices[i]), out var post) && post.Holder == bot.Name)
            {
                return Offices[i];
            }
        }

        return null;
    }

    /// <summary>Whether the officers first saw this member in its guild less than <see cref="NewcomerMs"/> ago (not counting the boot's first look).</summary>
    public static bool Newcomer(BotMobile bot) =>
        bot?.Guild is Guild guild && _firstSeen.TryGetValue(bot.Serial, out var seen) && seen.Guild == guild.Name && seen.Tick != long.MinValue
        && Core.TickCount - seen.Tick < NewcomerMs;

    /// <summary>A bot by name among the living population, as of the last look. Null when unknown.</summary>
    public static BotMobile Named(string name) => name != null && _byName.TryGetValue(name, out var bot) && bot is { Deleted: false } ? bot : null;

    // ---- news ----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// A piece of news for the guild's herald to read aloud in town. Open to any folder: wars, elections, births and relics are
    /// news from wherever they happen. Kept at most <see cref="MostNews"/> pieces and <see cref="NewsMs"/> old.
    /// </summary>
    public static void News(string guild, string text)
    {
        if (!Running || string.IsNullOrEmpty(guild) || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (!_news.TryGetValue(guild, out var list))
        {
            _news[guild] = list = [];
        }

        list.Add((text, Core.TickCount));
        NewsPosted++;

        while (list.Count > Math.Max(1, MostNews))
        {
            list.RemoveAt(0);
        }
    }

    /// <summary>Pieces of news waiting for this guild's herald.</summary>
    public static int Pending(string guild) => guild != null && _news.TryGetValue(guild, out var list) ? list.Count : 0;

    /// <summary>The oldest fresh piece of news, taken off the list; null when there is none.</summary>
    public static string NextNews(string guild)
    {
        if (guild == null || !_news.TryGetValue(guild, out var list))
        {
            return null;
        }

        var now = Core.TickCount;

        while (list.Count > 0)
        {
            var (text, tick) = list[0];
            list.RemoveAt(0);

            if (now - tick < NewsMs)
            {
                return text;
            }
        }

        return null;
    }

    // ---- appointment ----------------------------------------------------------------------------------------------------

    /// <summary>The officers' look, once a minute from the module's clock: names, newcomers, and every post due a review.</summary>
    public static void Look(long now)
    {
        if (now - _lastLook < 60000)
        {
            return;
        }

        _lastLook = now;
        _byName.Clear();

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is not { Deleted: false, Name: not null } bot)
            {
                continue;
            }

            _byName[bot.Name] = bot;

            if (bot.Guild is Guild g && g.Name != null && (!_firstSeen.TryGetValue(bot.Serial, out var seen) || seen.Guild != g.Name))
            {
                // The boot's first look sees every member for the first time; none of them is a newcomer.
                _firstSeen[bot.Serial] = (g.Name, _seenOnce ? now : long.MinValue);
            }
        }

        _seenOnce = true;

        if (!Running)
        {
            return;
        }

        foreach (var guild in BotGuilds.Standing)
        {
            if (!Keeps(guild) || guild.Members.Count < LeastMembers)
            {
                continue;
            }

            for (var i = 0; i < Offices.Length; i++)
            {
                if (On(Offices[i]))
                {
                    Review(guild, Offices[i], now);
                }
            }
        }
    }

    private static void Review(Guild guild, BotOfficerPost office, long now)
    {
        _posts.TryGetValue((guild.Name, office), out var post);

        var holder = post?.Holder == null ? null : _byName.GetValueOrDefault(post.Holder);
        string cause = null;

        if (post?.Holder == null)
        {
            // An empty post nobody fitted is looked at again on its own clock, not every minute.
            if (post != null && now - post.Reviewed < EmptyRetryMs)
            {
                return;
            }

            cause = "the post stood empty";
        }
        else if (holder == null)
        {
            // Resting bots are not in the list but still exist: their post waits for them.
            if (BotRest.Records.ContainsKey(post.Holder))
            {
                return;
            }

            cause = $"{post.Holder} is gone from the island";
        }
        else if (!ReferenceEquals(holder.Guild, guild))
        {
            cause = $"{holder.Name} has left {guild.Name}";
        }
        else if (BotGuilds.IsHead(holder))
        {
            cause = $"{holder.Name} leads the guild now";
        }

        if (cause == null && now - post.Reviewed < TermMs)
        {
            return;
        }

        var head = BotGuilds.Head(guild);
        var best = Best(guild, office, cause == null ? holder : null, out var bestFit);

        if (cause == null)
        {
            post.Reviewed = now;

            var holderFit = Fit(holder, office);

            if (best == null || ReferenceEquals(best, holder) || bestFit < holderFit * Margin)
            {
                Kept++;

                logger.Information(
                    "{Head} kept {Holder} the {Class} {Guild}'s {Office} at the review after {Hours:F1}h: fit {Fit:F1}{Rival}",
                    head?.Name ?? "nobody",
                    holder.Name,
                    holder.Class?.Name,
                    guild.Name,
                    Word(office),
                    (now - post.Since) / 3600000.0,
                    holderFit,
                    best == null || ReferenceEquals(best, holder) ? "" : $" against {best.Name}'s {bestFit:F1}, under ×{Margin:F1}"
                );

                return;
            }

            cause = $"the term's review: {best.Name} fits {bestFit:F1} against {holder.Name}'s {holderFit:F1}";
            ByTerm++;
        }
        else if (best != null)
        {
            ByGone++;
        }

        if (best == null)
        {
            NoneFit++;
            _posts[(guild.Name, office)] = new Post { Holder = null, Since = now, Reviewed = now, Why = "nobody fit" };

            return;
        }

        Set(guild, office, best, head, cause, now, bestFit);
    }

    private static void Set(Guild guild, BotOfficerPost office, BotMobile bot, BotMobile head, string cause, long now, double fit)
    {
        // Nobody holds two posts: whatever this bot held before is left empty, and filled at the next look.
        for (var i = 0; i < Offices.Length; i++)
        {
            if (Offices[i] != office && _posts.TryGetValue((guild.Name, Offices[i]), out var other) && other.Holder == bot.Name)
            {
                other.Holder = null;
            }
        }

        _posts[(guild.Name, office)] = new Post { Holder = bot.Name, Since = now, Reviewed = now, Why = cause };
        Appointed++;

        var veteran = BotTraits.Veteran(bot);

        logger.Information(
            "{Head} appointed {Bot} the {Class} {Guild}'s {Office}: {Why}; fit {Fit:F1}{Veteran}",
            head?.Name ?? "the guild",
            bot.Name,
            bot.Class?.Name,
            guild.Name,
            Word(office),
            cause,
            fit,
            veteran ? ", a veteran" : ""
        );

        var fill = new Dictionary<string, string> { ["bot"] = bot.Name, ["office"] = Word(office) };

        if (head != null)
        {
            BotVoice.Say(
                head,
                "guild",
                BotVoice.Phrase($"officers:appoint:{Word(office)}", "officers:appoint", head, fill) ?? $"{bot.Name} is our {Word(office)} now.",
                force: true
            );
        }

        BotEvents.Post("officers", bot, $"appointed {guild.Name}'s {Word(office)}: {cause}");
        News(guild.Name, $"{bot.Name} is {guild.Name}'s {Word(office)} now.");
    }

    /// <summary>The best-fitting member for a post, never the head and never one holding another post (except <paramref name="holder"/>).</summary>
    private static BotMobile Best(Guild guild, BotOfficerPost office, BotMobile holder, out double fit)
    {
        BotMobile best = null;
        fit = 0.0;

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is not BotMobile { Deleted: false } m || BotGuilds.IsHead(m))
            {
                continue;
            }

            if (!ReferenceEquals(m, holder) && OfficeOf(m) is { } held && held != office)
            {
                continue;
            }

            var f = Fit(m, office);

            if (f > fit)
            {
                fit = f;
                best = m;
            }
        }

        return best;
    }

    /// <summary>
    /// How well this member fits a post, nought for not at all. A quartermaster from the makers, the gatherers and the healers;
    /// a herald from anybody, by renown; a scout from the shooters, the tamers and the blades, by strength. A veteran first.
    /// </summary>
    public static double Fit(BotMobile bot, BotOfficerPost office)
    {
        if (bot?.Class is not { } klass)
        {
            return 0.0;
        }

        var name = klass.Name;
        var renown = 1.0 + Math.Max(0, bot.Fame) / 5000.0;

        double fit = office switch
        {
            BotOfficerPost.Quartermaster => name switch
            {
                "Crafter" => 3.0,
                "Gatherer" => 2.5,
                "Healer" => 2.0,
                _ => 1.0
            } * renown,
            BotOfficerPost.Herald => name switch
            {
                "Healer" or "Mage" => 1.5,
                "Crafter" or "Gatherer" => 1.2,
                _ => 1.0
            } * renown * renown,
            _ => name switch
            {
                "Archer" => 3.0,
                "WarriorArcher" => 2.5,
                "Tamer" => 2.0,
                "Warrior" or "Brawler" => 1.5,
                "Mage" or "WarriorMage" => 1.2,
                _ => 0.0
            } * Math.Max(0.5, BotQuad.Strength(bot) / 50.0)
        };

        return fit <= 0.0 ? 0.0 : BotTraits.Veteran(bot) ? fit * VeteranLean : fit;
    }

    /// <summary>
    /// Argus's <c>appoint &lt;guild&gt; &lt;quartermaster|herald|scout&gt; &lt;bot&gt;</c>: the post given by hand, whatever the fit.
    /// </summary>
    public static string Appoint(string tail)
    {
        var words = (tail ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var at = -1;
        var office = BotOfficerPost.Quartermaster;

        for (var i = 0; i < words.Length; i++)
        {
            if (Parse(words[i], out office))
            {
                at = i;

                break;
            }
        }

        if (at <= 0 || at >= words.Length - 1)
        {
            return "appoint wants a guild, a post and a member: appoint The Wolf scout Nessa.";
        }

        var guildName = string.Join(' ', words, 0, at);
        var botName = string.Join(' ', words, at + 1, words.Length - at - 1);
        var guild = BotGuilds.Find(guildName);

        if (guild == null)
        {
            return $"there is no guild called \"{guildName}\".";
        }

        if (!Keeps(guild))
        {
            return $"{guild.Name} keeps no officers.";
        }

        BotMobile bot = null;

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is BotMobile { Deleted: false } m && string.Equals(m.Name, botName, StringComparison.OrdinalIgnoreCase))
            {
                bot = m;

                break;
            }
        }

        if (bot == null)
        {
            return $"{guild.Name} has no member called \"{botName}\".";
        }

        if (BotGuilds.IsHead(bot))
        {
            return $"{bot.Name} leads {guild.Name}; the head holds no post.";
        }

        _byName[bot.Name] = bot;
        ByHand++;
        Set(guild, office, bot, BotGuilds.Head(guild), "by hand at Argus's door", Core.TickCount, Fit(bot, office));

        return $"{bot.Name} the {bot.Class?.Name} is {guild.Name}'s {Word(office)} now. {Tell(guild.Name)}";
    }

    // ---- telling ----------------------------------------------------------------------------------------------------------

    /// <summary>Argus's <c>officers [&lt;guild&gt;]</c>: who holds what, and their counts.</summary>
    public static string Tell(string tail)
    {
        var name = (tail ?? "").Trim();

        if (name.Length > 0)
        {
            var guild = BotGuilds.Find(name);

            return guild == null ? $"there is no guild called \"{name}\"." : TellOne(guild);
        }

        var say = new StringBuilder();

        foreach (var guild in BotGuilds.Standing)
        {
            if (Keeps(guild))
            {
                say.Append(TellOne(guild)).Append(' ');
            }
        }

        say.Append(Clause());

        return say.ToString();
    }

    private static string TellOne(Guild guild)
    {
        var say = new StringBuilder();
        var now = Core.TickCount;

        say.Append(guild.Name).Append(": ");

        for (var i = 0; i < Offices.Length; i++)
        {
            var office = Offices[i];

            say.Append(Word(office)).Append(' ');

            if (!On(office))
            {
                say.Append("(off)");
            }
            else if (_posts.TryGetValue((guild.Name, office), out var post) && post.Holder != null)
            {
                var bot = _byName.GetValueOrDefault(post.Holder);

                say.Append(post.Holder)
                    .Append(bot?.Class != null ? $" the {bot.Class.Name}" : "")
                    .Append($" for {(now - post.Since) / 60000}m")
                    .Append(bot == null ? " (resting or gone)" : !bot.Alive ? " (dead)" : "");
            }
            else
            {
                say.Append("none");
            }

            say.Append(i < Offices.Length - 1 ? ", " : "; ");
        }

        say.Append(BotGuildStore.Tell(guild.Name)).Append("; ");
        say.Append(BotHeraldStand.Tell(guild.Name)).Append("; ");
        say.Append(BotGuildScout.Tell(guild.Name)).Append('.');

        return say.ToString();
    }

    /// <summary>The <c>officers:</c> clause at the end of the <c>Guilds:</c> line.</summary>
    public static string Clause()
    {
        if (!Running)
        {
            return "; officers: OFF";
        }

        var held = 0;

        foreach (var (key, post) in _posts)
        {
            if (post.Holder != null && On(key.Office))
            {
                held++;
            }
        }

        return $"; officers: {held} posts held (quartermasters {(Quartermasters ? "on" : "OFF")}, heralds {(Heralds ? "on" : "OFF")}, scouts {(Scouts ? "on" : "OFF")}), "
               + $"{Appointed} appointed ({ByGone} to an empty post or one whose holder left, {ByTerm} at a {TermMs / 3600000}h review, {ByHand} by hand), {Kept} kept at review, {NoneFit} posts nobody fit, {NewsPosted} pieces of news; "
               + $"store: {BotGuildStore.Describe()}; quartermasters: {BotQuartermaster.Describe()}; heralds: {BotHeraldStand.Describe()}; bouts: {BotGuildBout.Describe()}; scouts: {BotGuildScout.Describe()}";
    }

    /// <summary>The guild's officers on the page, inside its object in <c>/api/state</c>'s <c>guilds</c>. From <c>BotWebSnapshot</c>.</summary>
    public static void Write(Utf8JsonWriter w, Guild guild)
    {
        if (guild?.Name == null)
        {
            return;
        }

        var now = Core.TickCount;

        w.WritePropertyName("officers");
        w.WriteStartObject();

        for (var i = 0; i < Offices.Length; i++)
        {
            var office = Offices[i];

            if (On(office) && _posts.TryGetValue((guild.Name, office), out var post) && post.Holder != null)
            {
                w.WritePropertyName(Word(office));
                w.WriteStartObject();
                w.WriteString("name", post.Holder);
                w.WriteNumber("minutes", (now - post.Since) / 60000);
                w.WriteString("why", post.Why ?? "");
                w.WriteEndObject();
            }
            else
            {
                w.WriteNull(Word(office));
            }
        }

        BotGuildStore.Write(w, guild.Name);
        BotHeraldStand.Write(w, guild.Name);
        BotGuildScout.Write(w, guild.Name);
        w.WriteEndObject();
    }

    // ---- store ------------------------------------------------------------------------------------------------------------

    internal static void Save(IGenericWriter writer)
    {
        var now = Core.TickCount;
        var count = 0;

        foreach (var post in _posts.Values)
        {
            if (post.Holder != null)
            {
                count++;
            }
        }

        writer.WriteEncodedInt(count);

        foreach (var ((guild, office), post) in _posts)
        {
            if (post.Holder == null)
            {
                continue;
            }

            writer.Write(guild);
            writer.WriteEncodedInt((int)office);
            writer.Write(post.Holder);
            writer.Write(Math.Max(0L, now - post.Since));
            writer.Write(Math.Max(0L, now - post.Reviewed));
            writer.Write(post.Why ?? "");
        }
    }

    internal static int Load(IGenericReader reader)
    {
        _posts.Clear();

        var now = Core.TickCount;
        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var guild = reader.ReadString();
            var office = (BotOfficerPost)reader.ReadEncodedInt();
            var holder = reader.ReadString();
            var since = reader.ReadLong();
            var reviewed = reader.ReadLong();
            var why = reader.ReadString();

            if (!string.IsNullOrEmpty(guild) && !string.IsNullOrEmpty(holder) && Enum.IsDefined(office))
            {
                _posts[(guild, office)] = new Post { Holder = holder, Since = now - since, Reviewed = now - reviewed, Why = why };
            }
        }

        return _posts.Count;
    }

    /// <summary>A world reload: the counters and the session's memory, not the posts (the store reads them back).</summary>
    public static void Forget()
    {
        Appointed = 0;
        ByGone = 0;
        ByTerm = 0;
        ByHand = 0;
        Kept = 0;
        NoneFit = 0;
        NewsPosted = 0;
        _news.Clear();
        _firstSeen.Clear();
        _byName.Clear();
        _seenOnce = false;
        _lastLook = 0;
    }

    /// <summary>
    /// <b>Patrick's reset (07.10.2026, reset26): every post vacated and every guild's store emptied, so the save writes the officers
    /// empty.</b> The reset of 20:56 that night left <c>Saves/BotOfficers</c> whole: the posts are kept by the holder's name and the
    /// guild's, so a newcomer born under an old name into a guild founded again under an old name would have woken up its
    /// quartermaster. This empties exactly what <see cref="BotOfficersStore"/> writes — the posts here and the stores and kit debts
    /// in <see cref="BotGuildStore.Wipe"/> — and forgets what the module's own reload forgets: the quartermasters, the kit draws,
    /// the herald's stand, the bouts and the scouts. No coin is held by a post. Returns the posts vacated.
    /// </summary>
    public static int Wipe()
    {
        var posts = 0;

        foreach (var post in _posts.Values)
        {
            if (post.Holder != null)
            {
                posts++;
            }
        }

        _posts.Clear();
        Forget();
        BotGuildStore.Wipe();
        BotQuartermaster.Forget();
        BotKitDraw.Forget();
        BotHeraldStand.Forget();
        BotGuildBout.Forget();
        BotGuildScout.Forget();

        return posts;
    }

    /// <summary>The words, written into the voice's bank for every occasion <c>bot-voice.json</c> does not already name.</summary>
    public static Dictionary<string, string[]> Phrases() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["officers:appoint"] = ["{bot} is our {office} now.", "I name {bot} our {office}."],
            ["officers:appoint:quartermaster"] = ["{bot} keeps our store now. Bring your surplus to the hall.", "{bot} is our quartermaster. Mind the store, {bot}."],
            ["officers:appoint:herald"] = ["{bot} speaks for us in town now.", "{bot} is our herald. Bring us people, {bot}."],
            ["officers:appoint:scout"] = ["{bot} scouts for us now. Keep your eyes open, {bot}.", "{bot} is our scout. Tell us what is out there."],
            ["officers:store-open"] = ["The store is open at the hall: {stock}. Come for your kit.", "Store's open. {stock}."],
            ["officers:kit"] = ["Here's your kit, {other}: {what}. {owed}gp on your account with the guild.", "{other}, take this: {what}. Pay the guild back from your spoils: {owed}gp."],
            ["officers:repaid"] = ["My debt to the guild's store is paid.", "Square with the store now."],
            ["officers:want"] = ["The guild needs {amount} {item}, {price}gp each. Bring them to me.", "{guild} buys {amount} {item} at {price}gp."],
            ["officers:invite"] = ["{other}! {guild} has room for you. Join us.", "You there, {other}. {guild} looks after its own. Join us?"],
            ["officers:joined"] = ["Welcome to {guild}, {other}!", "{other} is one of us now."],
            ["officers:declined"] = ["Another time, then, {other}.", "The offer stands, {other}."],
            ["officers:challenge"] = ["{guild} challenges {rival} to a bout: {purse}gp a side!", "Hear this: {guild} calls {rival} out. {champion} will fight for us."],
            ["officers:bout-won"] = ["{winner} has won the bout for {guild}!", "The bout is ours: {winner} beat {loser}."],
            ["officers:scouted"] = ["{place}: {what}.", "I've been to {place}. {what}."],
            ["officers:shadow"] = ["{rival} gathers {count} at {town}.", "{rival} has {count} in a company near {town}. Watch yourselves."],
            ["officers:warn"] = ["{rival} is coming with {count}. To the hall!", "{rival}'s company is near {town}, {count} strong. Muster!"],
            ["took:keep-store"] = ["To the hall, the store wants keeping.", "Store duty."],
            ["finished:keep-store"] = ["Store's shut for now.", "The store's in order."],
            ["took:herald-stand"] = ["To the bank, to speak for the guild.", "Time to bring us some new faces."],
            ["finished:herald-stand"] = ["That's enough talking for now.", "The stand is done."],
            ["took:scout"] = ["Off to look around.", "Somebody has to go and see."],
            ["finished:scout"] = ["Back with news.", "I've seen what I came to see."]
        };
}
