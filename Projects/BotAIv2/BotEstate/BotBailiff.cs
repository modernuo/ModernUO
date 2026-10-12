using System.Collections.Generic;
using Server.Guilds;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Whether anybody is standing on this guild's land who should not be, and who is going to say so.
///
/// <para>
/// <b>Rare by construction rather than by a dial.</b> Three things have to be true at once before this ever
/// offers anything: the bot is on its own guild's land, somebody of another guild is on it too and within
/// sight, and the guild already thinks poorly enough of theirs to bother. Without the third condition an
/// island of five guilds would spend its afternoon telling each other to move along, which is not a
/// population, it is a queue.
/// </para>
///
/// <para>
/// <b>And it is one bot at a time per trespasser, not per guild.</b> The other three officers claim their
/// guild because their errand is the guild's — one hall, one shopkeeper. This errand is about a person, and
/// two members telling the same stranger to move along is the shard shouting. See <c>BotOffice</c> for the
/// pattern and why the claim is short.
/// </para>
/// </summary>
public sealed class BotBailiff : IBotProposer
{
    /// <summary>How long a claim on a trespasser lasts after the bot holding it was last heard from.</summary>
    public static int ClaimMs { get; set; } = 60000;

    /// <summary>
    /// How poorly the guild must already think of theirs before anybody is told to move.
    ///
    /// <para>
    /// Below nought, which means the border drift or an earlier trespass has to have happened first. A guild
    /// with no opinion of its neighbours does not police its yard, so the first minutes after a hall goes up
    /// are quiet and the quarrel builds out of something rather than arriving with the building.
    /// </para>
    /// </summary>
    public static double Minding { get; set; } = -3.0;

    /// <summary>How far a member looks for somebody who should not be there.</summary>
    public static int Watch { get; set; } = 12;

    /// <summary>Bots looked at.</summary>
    public static long Asked { get; private set; }

    /// <summary>Evictions offered.</summary>
    public static long Offered { get; private set; }

    /// <summary>Times the bot was not on its own guild's land, which is most of them.</summary>
    public static long Elsewhere { get; private set; }

    /// <summary>Times nobody was trespassing within sight.</summary>
    public static long Quiet { get; private set; }

    /// <summary>Times somebody was, and the guild did not mind them enough to say anything.</summary>
    public static long Tolerated { get; private set; }

    /// <summary>Times somebody else was already dealing with them.</summary>
    public static long Claimed { get; private set; }

    /// <summary>
    /// How long a member that could not get near a trespasser is not sent after the same one again. Five minutes.
    ///
    /// <para>
    /// <b>Eviction is unpaid and a call from outside, so it learns nothing from failing, and this is the gate its failure
    /// needed.</b> Gwendra was sent after Corwin six times in twenty-nine seconds on 14.09.2026, each chase ending "could not
    /// get nearer to Corwin at (1262, 1458, 0)", until the breaker rested the work; the offer's claim lasts twelve seconds
    /// and is released when the errand ends, so nothing stood between the failure and the next offer. The same shape and
    /// the same answer as the healer that could not keep up with a fighter: see <c>BotAccompany.ShunMs</c>.
    /// </para>
    /// </summary>
    public static int ShunMs { get; set; } = 300000;

    /// <summary>Trespassers passed over because this member had lately failed to get near them.</summary>
    public static long Passed { get; private set; }

    private static readonly Dictionary<Serial, long> _claims = [];

    /// <summary>
    /// How long a trespasser that was told to leave and stayed is left alone before anybody tells it again.
    ///
    /// <para>
    /// <b>Refusing once was being charged eight times a minute.</b> The errand ends "was told to leave and stayed",
    /// releases its claim on the trespasser, and the next review — eight seconds on — offers the same trespasser to the
    /// same member, who is already standing beside it, says the sentence again, waits the grace and charges the guild's
    /// opinion again: Lorcan was minded eight times a minute for five minutes on 21.09.2026 (session 21:58), 371 times
    /// in all that session, at <c>BotRegard.Defiance</c> −12 apiece against a war at −100. One staker holding a square
    /// through its five-minute muster was a war in under a minute, and The Ash declared one on The Blade at 23:35 with
    /// no other cause on the ledger. The handover asked whether that was a loop or the design; it is the loop (C-class:
    /// the note about the trespasser was written into the opinion and read by nobody). Five minutes, the muster window:
    /// a trespasser told once is a fact until its errand there is over.
    /// </para>
    /// </summary>
    public static int ToldMs { get; set; } = 300000;

    /// <summary>Trespassers passed over because they had lately been told and the guild's opinion already knows it.</summary>
    public static long Warned { get; private set; }

    private static readonly Dictionary<Serial, long> _told = [];

    /// <summary>This trespasser was told to leave and stayed, and the opinion has been charged for it once.</summary>
    public static void Told(Mobile them)
    {
        if (them != null)
        {
            _told[them.Serial] = Core.TickCount + ToldMs;
        }
    }

    private static readonly Dictionary<(Serial Bot, Serial Them), long> _unreached = [];

    private static readonly List<(Serial Bot, Serial Them)> _lapsed = [];

    public string Name => "bailiff";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotEstate.Running || !BotLand.Running || !BotRegard.Running)
        {
            return null;
        }

        if (bot?.Self is not BotMobile { Deleted: false } body || body.Map == null || body.Map == Map.Internal)
        {
            return null;
        }

        if (body.Guild is not Guild ours || BotEstate.Standing == 0)
        {
            return null;
        }

        // The Shadow keeps no house, no counter and no hired hands: its home is a fire and a chest far outside every
        // town, and a band with a signboard has stopped being secret. See BotUnderworld.Band.
        if (BotUnderworld.Band(ours))
        {
            return null;
        }

        Asked++;

        // On its own land, and that is asked of where the bot is standing rather than of where its hall is:
        // a member across the island is not keeping anybody's yard.
        if (BotLand.Holder(body.Map, body.Location) != ours.Name)
        {
            Elsewhere++;

            return null;
        }

        BotMobile worst = null;
        var lowest = Minding;
        var anybody = false;
        var passed = false;
        var looked = Core.TickCount;

        // <b>One sweep, and the two questions asked of it together.</b> The first cut asked a second time
        // whether anybody was there at all, which doubled the cost of the commonest answer — an area query
        // per bot per beat, for a rule that fires almost never.
        foreach (var near in body.GetMobilesInRange<BotMobile>(Watch))
        {
            if (near == body || near.Deleted || near.Guild is not Guild theirs || theirs == ours)
            {
                continue;
            }

            // On our land specifically, not merely near us: a member of ours standing at the edge of its own
            // yard can see well past it.
            if (BotLand.Holder(near.Map, near.Location) != ours.Name)
            {
                continue;
            }

            // Somebody this member lately chased and could not get near. Passed over rather than offered again, so the
            // next-worst trespasser can be the one it is sent after. See ShunMs.
            if (_unreached.TryGetValue((body.Serial, near.Serial), out var shunned) && looked - shunned < 0)
            {
                Passed++;
                passed = true;

                continue;
            }

            // Somebody already told and already minded. See ToldMs.
            if (_told.TryGetValue(near.Serial, out var told) && looked - told < 0)
            {
                Warned++;
                passed = true;

                continue;
            }

            anybody = true;

            var held = BotRegard.Of(ours.Name, theirs.Name);

            if (held > lowest)
            {
                continue;
            }

            worst = near;
            lowest = held;
        }

        if (worst == null)
        {
            // Told apart, because "nobody was there" and "somebody was there and we did not mind" are the
            // two halves of whether this rule is doing anything at all, and one nought for both would hide
            // a threshold set so low that nothing ever fires.
            if (anybody)
            {
                Tolerated++;
            }
            else if (!passed)
            {
                Quiet++;
            }

            return null;
        }

        var now = Core.TickCount;

        if (_claims.TryGetValue(worst.Serial, out var until) && now - until < 0)
        {
            Claimed++;

            return null;
        }

        Offered++;
        _claims[worst.Serial] = now + BotOffice.OfferedMs;

        return new BotEvict(worst, ours.Name);
    }

    /// <summary>The errand is alive and still on it.</summary>
    public static void Hold(Mobile them)
    {
        if (them != null)
        {
            _claims[them.Serial] = Core.TickCount + ClaimMs;
        }
    }

    /// <summary>The errand is over, however it went.</summary>
    public static void Release(Mobile them)
    {
        if (them != null)
        {
            _claims.Remove(them.Serial);
        }
    }

    /// <summary>This member chased that trespasser and could not get near: it is not sent after them again for <see cref="ShunMs"/>.</summary>
    public static void Unreached(Mobile bot, Mobile them)
    {
        if (bot == null || them == null)
        {
            return;
        }

        var now = Core.TickCount;

        // Kept small by dropping what has lapsed whenever it grows, rather than on a clock of its own.
        if (_unreached.Count >= 256)
        {
            foreach (var (key, until) in _unreached)
            {
                if (now - until >= 0)
                {
                    _lapsed.Add(key);
                }
            }

            for (var i = 0; i < _lapsed.Count; i++)
            {
                _unreached.Remove(_lapsed[i]);
            }

            _lapsed.Clear();
        }

        _unreached[(bot.Serial, them.Serial)] = now + ShunMs;
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been looked at for keeping a guild's yard"
            : $"the bailiff looked {Asked} times and sent {Offered}: {Elsewhere} were not on their own land, {Quiet} saw nobody on it, "
              + $"{Tolerated} saw somebody and did not mind them enough, {Claimed} found somebody already dealing with it, {Passed} passed over somebody they had lately failed to get near, {Warned} passed over somebody already told and minded; {BotEvict.Describe()}";

    public static void Forget()
    {
        Asked = 0;
        Offered = 0;
        Elsewhere = 0;
        Quiet = 0;
        Tolerated = 0;
        Claimed = 0;
        Passed = 0;
        Warned = 0;
        _claims.Clear();
        _unreached.Clear();
        _told.Clear();
        BotEvict.Forget();
    }
}
