using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// Bots answering each other's moods: what a bot in earshot does about a neighbour saying it is hurt, tired, grieving,
/// poor, lonely, bored or proud (01.10.2026, build 387; Patrick's point 4 of the night plan: "bots actively talk to each
/// other, read each other's mood from the phrases, and depending on the mood other bots offer company or help; not across
/// hostile guilds").
///
/// <para>
/// <b>Read from what was said, never from the speaker's insides.</b> The voice tags every mood line with one word
/// (<see cref="BotMoods"/>) and hands the tag, the line and whoever was in earshot to <see cref="Heard"/>. Nothing here
/// asks the speaker how it feels; the answer is to the tag. The deed that carries the help out does look at the world —
/// a bandage is wound only on a wound — because that is the deed's own business.
/// </para>
///
/// <para>
/// <b>Words at once, help through the auction.</b> Every answer is a line said a few seconds later. When the listener can
/// also do something — bind the wound, lay a fire, call the nearest quarry a hunt for two, fall in with the speaker's
/// company — the deed is written against the listener as an invitation and handed to the auction by <see cref="BotFellow"/>
/// at its next review, where it is weighed against whatever the listener holds. A seam that pays is kept; an idle bot
/// goes. The one call that is not an auction is a company's: a listener already in a company asks the speaker in through
/// the company's own door (<c>BotSquads.Enrol</c>), which takes only a bot whose work resumes and is the door every
/// company has used since 01.10.2026. Nothing is handed out: a poor bot is pointed at a hunt, never given coin.
/// </para>
///
/// <para>
/// <b>Cheap by construction.</b> A mood line is said once in fifteen minutes per bot, and the one spatial query it costs
/// is the voice's own, made to find an audience. Hearing is a loop over that short list; the proposer is a dictionary
/// read; the replies are a list walked once a second. Each listener answers at most once in <see cref="AnswerEveryMs"/>,
/// each pair once in <see cref="PairEveryMs"/>, and at most <see cref="MostListeners"/> answer one line, so the chat
/// cannot flood whatever the population does.
/// </para>
/// </summary>
public static class BotSocial
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSocial));

    /// <summary>Whether bots answer each other's moods at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How many of those in earshot answer one line: the nearest. Two.</summary>
    public static int MostListeners { get; set; } = 2;

    /// <summary>How long after the line its answer is said, and up to how much more. Three seconds, and up to two and a half.</summary>
    public static int ReplyMs { get; set; } = 3000;

    public static int ReplySpreadMs { get; set; } = 2500;

    /// <summary>How long one listener goes between answers. Two minutes.</summary>
    public static int AnswerEveryMs { get; set; } = 120000;

    /// <summary>How long before the same two bots have another such exchange. Twenty minutes.</summary>
    public static int PairEveryMs { get; set; } = 1200000;

    /// <summary>How long an offer waits for the listener's auction to take it before it lapses. A minute and a half.</summary>
    public static int InviteMs { get; set; } = 90000;

    /// <summary>How far the speaker may have walked off, in tiles, and the offer still be handed out. Twenty-four: twice earshot.</summary>
    public static int InviteReach { get; set; } = 24;

    /// <summary>The chance the one answered says thank you. A half.</summary>
    public static double ThanksChance { get; set; } = 0.5;

    public static bool OffersMending { get; set; } = true;

    public static bool OffersFires { get; set; } = true;

    public static bool OffersHunts { get; set; } = true;

    public static bool OffersCompany { get; set; } = true;

    /// <summary>What laying a fire for a tired neighbour adds to the fire's own claim a minute. Twelve, about what a line of company lifts (<c>BotCampTalk.CheerPerLine</c>).</summary>
    public static double FellowWorth { get; set; } = 12.0;

    /// <summary>Lines heard whose tag asks for something: the seven moods.</summary>
    public static long Asked { get; private set; }

    /// <summary>Lines heard whose tag asks nothing: content, middling, glum.</summary>
    public static long Unasked { get; private set; }

    public static long Hostile { get; private set; }

    public static long Shunned { get; private set; }

    public static long Unfit { get; private set; }

    public static long OnClock { get; private set; }

    public static long PairOnClock { get; private set; }

    public static long Unanswered { get; private set; }

    public static long Answers { get; private set; }

    public static long Words { get; private set; }

    public static long Pointed { get; private set; }

    public static long Called { get; private set; }

    public static long NotCalled { get; private set; }

    public static long Said { get; private set; }

    public static long Unsaid { get; private set; }

    public static long Thanks { get; private set; }

    public static long Proposed { get; private set; }

    public static long Lapsed { get; private set; }

    public static long Stale { get; private set; }

    public static long Whole { get; private set; }

    public static long Finished { get; private set; }

    public static long KeptOut { get; private set; }

    public static long Failed { get; private set; }

    public static long Dropped { get; private set; }

    public static long Died { get; private set; }

    private static readonly Dictionary<string, long> _heardBy = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, long> _offeredBy = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, long> _acceptedBy = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, long> _finishedBy = new(StringComparer.Ordinal);

    private sealed class Invite(BotMobile speaker, string mood, string kind, BotDeed deed, string occasion, Dictionary<string, string> fill, long tick)
    {
        public BotMobile Speaker { get; } = speaker;

        public string Mood { get; } = mood;

        public string Kind { get; } = kind;

        public BotDeed Deed { get; } = deed;

        /// <summary>The line that goes with the offer, said when the offer is taken up, not when it is made.</summary>
        public string Occasion { get; } = occasion;

        public Dictionary<string, string> Fill { get; } = fill;

        public long Tick { get; } = tick;
    }

    private sealed class Reply(BotMobile who, BotMobile to, string occasion, string fallback, Dictionary<string, string> fill, long due, bool thanks)
    {
        public BotMobile Who { get; } = who;

        public BotMobile To { get; } = to;

        public string Occasion { get; } = occasion;

        public string Fallback { get; } = fallback;

        public Dictionary<string, string> Fill { get; } = fill;

        public long Due { get; } = due;

        public bool Thanks { get; } = thanks;
    }

    /// <summary>The offer each listener undertook to make, by listener.</summary>
    private static readonly Dictionary<Serial, Invite> _invites = [];

    /// <summary>The deed last handed to each listener's auction, so the take can be recognised.</summary>
    private static readonly Dictionary<Serial, BotDeed> _offered = [];

    /// <summary>Offers taken and not yet ended, by kind.</summary>
    private static readonly Dictionary<BotDeed, string> _live = [];

    private static readonly Dictionary<Serial, long> _answered = [];

    private static readonly Dictionary<(Serial, Serial), long> _pairs = [];

    private static readonly List<Reply> _replies = [];

    private static readonly List<BotMobile> _picked = [];

    private static readonly List<Serial> _staleSerials = [];

    private static readonly List<(Serial, Serial)> _stalePairs = [];

    private static long _sweptTick;

    private static bool _swept;

    /// <summary>
    /// A mood line was said with somebody in earshot. From <see cref="BotVoice.MoodSaid"/>: the speaker, the tag the line carried,
    /// the line, and everybody within earshot, hostile or not.
    /// </summary>
    public static void Heard(BotMobile speaker, string tag, string line, List<BotMobile> audience)
    {
        if (!Running || speaker is not { Deleted: false, Alive: true } || audience == null || audience.Count == 0)
        {
            return;
        }

        if (!BotMoods.Asks(tag))
        {
            Unasked++;

            return;
        }

        var now = Core.TickCount;

        Asked++;
        Tally(_heardBy, tag);
        _picked.Clear();

        for (var i = 0; i < audience.Count; i++)
        {
            var near = audience[i];

            if (near is not { Deleted: false, Alive: true, Hidden: false } || near.Map != speaker.Map || ReferenceEquals(near, speaker))
            {
                continue;
            }

            // Never across hostile guilds: at war, or thought of at or below the war line either way.
            if (BotRegard.Hostile(speaker, near))
            {
                Hostile++;

                continue;
            }

            // Nor a murderer or a criminal, in either direction: the engine's own word, as the fires ask it.
            if (!BotCamp.Welcome(speaker, near))
            {
                Shunned++;

                continue;
            }

            if (near.Combatant != null || BotOutlaw.Jailed(near) || BotDuel.Duelling(near))
            {
                Unfit++;

                continue;
            }

            // Nor one that holds a grudge against the speaker (07.10.2026, traits17). Counted there. See BotTies.Spurns.
            if (BotTies.Spurns(near, speaker))
            {
                continue;
            }

            if (_answered.TryGetValue(near.Serial, out var last) && now - last < AnswerEveryMs)
            {
                OnClock++;

                continue;
            }

            if (_pairs.TryGetValue(Pair(speaker, near), out var pair) && now - pair < PairEveryMs)
            {
                PairOnClock++;

                continue;
            }

            Nearest(speaker, near);
        }

        if (_picked.Count == 0)
        {
            Unanswered++;

            return;
        }

        for (var i = 0; i < _picked.Count; i++)
        {
            Answer(_picked[i], speaker, tag, now);
        }

        _picked.Clear();
    }

    /// <summary>Keeps the <see cref="MostListeners"/> nearest, nearest first.</summary>
    private static void Nearest(BotMobile speaker, BotMobile near)
    {
        // A friend of the speaker's before anybody else, the nearest friend first (07.10.2026, traits17). See BotTies.SocialRank.
        var distance = BotTies.SocialRank(speaker, near, speaker.GetDistanceToSqrt(near));
        var at = _picked.Count;

        while (at > 0 && BotTies.SocialRank(speaker, _picked[at - 1], speaker.GetDistanceToSqrt(_picked[at - 1]), false) > distance)
        {
            at--;
        }

        if (at >= MostListeners)
        {
            return;
        }

        _picked.Insert(at, near);

        if (_picked.Count > MostListeners)
        {
            _picked.RemoveAt(_picked.Count - 1);
        }
    }

    private static void Answer(BotMobile listener, BotMobile speaker, string tag, long now)
    {
        _answered[listener.Serial] = now;
        _pairs[Pair(speaker, listener)] = now;
        Answers++;

        var fill = new Dictionary<string, string>();
        var (occasion, kind, deed) = Offer(listener, speaker, tag, fill);
        var due = now + Math.Max(500, ReplyMs) + Utility.Random(Math.Max(1, ReplySpreadMs));

        // <b>The word now; the offer's own line only when the offer is taken up.</b> "Hold still, I have bandages" said at the
        // invitation would promise what the listener's auction may yet refuse, and a promise nobody keeps is the one thing a
        // person at the log would read as a bot lying. So a deed is answered with the plain word for the mood here, and its line
        // is said from Started, as the bot sets off.
        _replies.Add(new Reply(listener, speaker, deed == null ? occasion : $"fellow:{tag}", $"fellow:{tag}", fill, due, thanks: true));

        if (deed != null)
        {
            _invites[listener.Serial] = new Invite(speaker, tag, kind, deed, occasion, fill, now);
            Tally(_offeredBy, kind);
            BotEvents.Post("fellow", listener, $"heard {speaker.Name} is {tag}; offers {kind}");

            return;
        }

        switch (kind)
        {
            case "words":
                Words++;

                break;
            case "fire-near":
                Pointed++;

                break;
        }
    }

    /// <summary>
    /// What this listener can offer this speaker for this mood: the occasion of the line, the kind, and the deed the proposer
    /// hands out — null for words alone. A listener in a company calls the speaker in here and now; everything else waits for
    /// the listener's auction.
    /// </summary>
    private static (string Occasion, string Kind, BotDeed Deed) Offer(BotMobile listener, BotMobile speaker, string tag, Dictionary<string, string> fill)
    {
        var map = speaker.Map;
        var takes = Takes(listener);

        switch (tag)
        {
            case BotMoods.Hurt:
                {
                    if (OffersMending && takes && Mends(listener, speaker, out var trains))
                    {
                        return ("offer:mend", "mend", new BotSalve(speaker, map, false, trains));
                    }

                    break;
                }
            case BotMoods.Tired:
                {
                    if (FireNear(speaker) is { } fire)
                    {
                        fill["x"] = fire.Centre.X.ToString();
                        fill["y"] = fire.Centre.Y.ToString();

                        return ("offer:fire:near", "fire-near", null);
                    }

                    if (OffersFires && takes && Kindles(listener, map))
                    {
                        return ("offer:fire", "fire", new BotKindle(map, listener.Location, BotCamp.Claim(listener, BotKindle.Prior) + FellowWorth, false));
                    }

                    break;
                }
            case BotMoods.Poor:
                {
                    if (OffersHunts && takes && Idle(speaker) && Bands(listener, speaker, out var quarry))
                    {
                        fill["quarry"] = quarry.Name ?? "something";

                        return ("offer:hunt", "hunt", new BotBand(quarry, BotThreat.Power(listener) + BotThreat.Power(speaker)));
                    }

                    break;
                }
            // <b>A grieving bot is offered company before a fire (01.10.2026, build 388).</b> On 387 grieving was the commonest mood
            // heard — 74 of 141 lines in 65 minutes, 30 of 46 in the first quarter hour after the restart — and it was answered
            // with words alone unless a fire happened to burn near: the offers stood at "fire 2 → 0 accepted". Patrick's point 4:
            // "depending on the mood, other bots offer company or help". Company is what a grieving bot is offered now, as a lonely
            // one is: a place in the listener's company, a walk to the speaker's, or a hunt together; the fire near stays the
            // fallback. (Fable's review of 388: the labels had been put on the hunt-only block above, which left this block
            // unreachable — the lonely and the bored lost their company offers and the grieving never gained one.)
            case BotMoods.Grieving:
            case BotMoods.Lonely:
            case BotMoods.Bored:
                {
                    if (OffersCompany && listener.Squad is { } mine && speaker.Squad == null)
                    {
                        if (Idle(speaker) && CallsIn(mine, listener, speaker, tag))
                        {
                            return ("offer:company", "company", null);
                        }
                    }
                    else if (OffersCompany && takes && speaker.Squad is { } theirs && Enlists(listener, theirs, out var where))
                    {
                        return ("offer:enlist", "enlist", new BotEnlist(theirs, map, where));
                    }
                    else if (OffersHunts && takes && speaker.Squad == null && Idle(speaker) && Bands(listener, speaker, out var quarry))
                    {
                        fill["quarry"] = quarry.Name ?? "something";

                        return ("offer:hunt", "hunt", new BotBand(quarry, BotThreat.Power(listener) + BotThreat.Power(speaker)));
                    }

                    if (tag == BotMoods.Lonely && OffersFires && takes && Kindles(listener, map))
                    {
                        return ("offer:fire", "fire", new BotKindle(map, listener.Location, BotCamp.Claim(listener, BotKindle.Prior) + FellowWorth, false));
                    }

                    if (tag == BotMoods.Grieving && FireNear(speaker) is { } fire)
                    {
                        fill["x"] = fire.Centre.X.ToString();
                        fill["y"] = fire.Centre.Y.ToString();

                        return ("offer:fire:near", "fire-near", null);
                    }

                    break;
                }
        }

        return ($"fellow:{tag}", "words", null);
    }

    /// <summary>Whether this listener may be handed a deed at all: free or on work the auction may outbid, not tired, not in a company, nothing paused beneath it.</summary>
    private static bool Takes(BotMobile listener)
    {
        if (listener.Tired || listener.Squad != null || listener.Fallen || BotDelveParty.Delving(listener))
        {
            return false;
        }

        var resolve = listener.Resolve;

        if (resolve == null || resolve.Standing is not (BotStanding.Free or BotStanding.Busy) || resolve.Paused != null)
        {
            return false;
        }

        var deed = resolve.Deed;

        return deed == null
               || !deed.Committed && deed.Foe == null
               && deed is not (BotChat or BotFireside or BotKindle or BotSalve or BotAccompany or BotAwaitMend or BotBand or BotEnlist);
    }

    /// <summary>Whether the speaker is not on paying work: nothing in hand, a guess (a prowl), or unpaid work. What may be called away.</summary>
    private static bool Idle(BotMobile speaker)
    {
        var resolve = speaker.Resolve;

        return resolve == null || resolve.Paused == null && (resolve.Deed is not { } deed || deed.Guess || deed.Unpaid);
    }

    private static bool Mends(BotMobile listener, BotMobile speaker, out SkillName trains)
    {
        trains = SkillName.Healing;

        if (!BotMend.Wants(speaker) || BotMend.OutOfReach(speaker) || BotMend.Abetting(listener, speaker) != null
            || BotThreat.Hunter(listener, BotDefender.Reach) != null)
        {
            return false;
        }

        var spell = BotMend.Spell(listener, speaker);

        if (spell < 0 && BotMend.Cloth(listener) <= 0)
        {
            return false;
        }

        trains = spell >= 0 ? SkillName.Magery : SkillName.Healing;

        return true;
    }

    /// <summary>The camper's own gates, asked of a listener laying a fire for somebody else: a lighter, on wild ground, with the means, not hurt, no fire near.</summary>
    private static bool Kindles(BotMobile listener, Map map)
    {
        if (!BotCamp.Running || map == null || map == Map.Internal || listener.Map != map || !BotCamp.Lighter(listener) || !BotCamp.Lights(listener))
        {
            return false;
        }

        if (listener.HitsMax <= 0 || listener.Hits < listener.HitsMax * BotHunter.FitAt)
        {
            return false;
        }

        if (BotCamp.LandOf(map, listener.Location) != BotCamp.Land.Wild || BotCamp.Count() >= BotCamp.MostFires || BotCamp.Crowded(map, listener.Location))
        {
            return false;
        }

        return BotKindle.Means(listener);
    }

    /// <summary>A hunt for two: the biggest thing within the hunter's reach of the listener that it could take, called as a band the speaker is nearest to.</summary>
    private static bool Bands(BotMobile listener, BotMobile speaker, out BaseCreature quarry)
    {
        quarry = null;

        if (listener.Squad != null || speaker.Squad != null || !listener.AbleToFight || !speaker.AbleToFight
            || BotTrading.Refuses(listener) || BotTrading.Refuses(speaker) || BotDungeon.Under(listener.Location))
        {
            return false;
        }

        quarry = BotQuarry.Best(listener, BotQuarry.Reach);

        // <b>Only on something that fights whoever comes near (Fable's review of 387).</b> Best ranks what is hostile first but
        // answers with whatever stands within thirty tiles, and round a town that is a cow; a band on a cow levies up to
        // BotSquad.Usual bots off their work for nothing, against Patrick's rule of 01.10.2026 (hunters hit monsters, not cows;
        // grazers last). A grazer at hand is words only — "coin comes to those who hunt" — and the speaker's own hunt applies
        // grazers-last by itself (BotHunter.Propose).
        if (quarry == null || !BotQuarry.Fights(quarry))
        {
            quarry = null;

            return false;
        }

        return true;
    }

    /// <summary>A listener in a company asks the speaker in through the company's own door.</summary>
    private static bool CallsIn(BotSquad mine, BotMobile listener, BotMobile speaker, string tag)
    {
        if (mine.Disbanded || mine.Count >= mine.Ceiling || mine.Leader?.Self is not Mobile lead || mine.Map != speaker.Map
            || BotRegard.Hostile(lead, speaker) || BotTrading.Refuses(speaker) || !speaker.AbleToFight)
        {
            NotCalled++;

            return false;
        }

        if (!BotSquads.Enrol(mine, speaker, speaker.Map, lead.Location, $"asked into {listener.Name}'s company, being {tag}"))
        {
            NotCalled++;

            return false;
        }

        Called++;
        Tally(_offeredBy, "company");
        Tally(_acceptedBy, "company");
        BotEvents.Post("fellow", listener, $"heard {speaker.Name} is {tag}; called it into company {mine.Id}");

        return true;
    }

    /// <summary>Whether the listener may fall in with the speaker's company: room, near enough to its leader, nobody hostile.</summary>
    private static bool Enlists(BotMobile listener, BotSquad theirs, out Point3D where)
    {
        where = Point3D.Zero;

        if (theirs.Disbanded || theirs.Count >= theirs.Ceiling || theirs.Leader?.Self is not Mobile lead || lead.Map != listener.Map
            || BotRegard.Hostile(lead, listener) || BotTrading.Refuses(listener) || !listener.AbleToFight
            || !lead.InRange(listener.Location, BotSquads.JoinReach))
        {
            return false;
        }

        where = lead.Location;

        return true;
    }

    /// <summary>A fire burning within the camp's joining reach of the speaker, or null. Six fires at most, so a loop.</summary>
    private static BotCamp.Fire FireNear(BotMobile speaker)
    {
        var fires = BotCamp.Fires;

        for (var i = 0; i < fires.Count; i++)
        {
            if (fires[i] is { Lit: true, Closed: false } fire && fire.Map == speaker.Map && speaker.InRange(fire.Centre, BotCamp.JoinReach))
            {
                return fire;
            }
        }

        return null;
    }

    /// <summary>The proposer's question: the offer this listener undertook, if it still stands. See <see cref="BotFellow"/>.</summary>
    public static BotDeed Propose(IBotWilful bot)
    {
        if (!Running || _invites.Count == 0 || bot?.Self is not BotMobile body || !_invites.TryGetValue(body.Serial, out var invite))
        {
            return null;
        }

        var now = Core.TickCount;

        if (now - invite.Tick >= InviteMs)
        {
            Drop(body.Serial);
            Lapsed++;

            return null;
        }

        var speaker = invite.Speaker;

        if (speaker is not { Deleted: false, Alive: true } || speaker.Map != body.Map || !body.InRange(speaker.Location, InviteReach))
        {
            Drop(body.Serial);
            Stale++;

            return null;
        }

        if (invite.Kind == "mend" && !BotMend.Wants(speaker))
        {
            Drop(body.Serial);
            Whole++;

            return null;
        }

        Proposed++;
        _offered[body.Serial] = invite.Deed;

        return invite.Deed;
    }

    private static void Drop(Serial listener)
    {
        _invites.Remove(listener);
        _offered.Remove(listener);
    }

    /// <summary>Work was taken on. If it is the offer this listener was handed, it is accepted. From <c>BotWill.Started</c>.</summary>
    public static void Started(Mobile who, BotDeed deed, string because)
    {
        if (who == null || deed == null || _offered.Count == 0 || !_offered.TryGetValue(who.Serial, out var offered) || !ReferenceEquals(offered, deed))
        {
            return;
        }

        var kind = _invites.TryGetValue(who.Serial, out var invite) ? invite.Kind : "?";

        Drop(who.Serial);
        Tally(_acceptedBy, kind);
        _live[deed] = kind;
        BotEvents.Post("fellow", who, $"took up {kind} for {invite?.Speaker?.Name ?? "somebody"}");

        // The offer's own line, now that it is being kept. See Answer.
        if (invite != null && who is BotMobile listener)
        {
            _replies.Add(new Reply(listener, invite.Speaker, invite.Occasion, $"fellow:{invite.Mood}", invite.Fill, Core.TickCount + 500, thanks: false));
        }
    }

    /// <summary>Work ended. If it was an accepted offer, how it ended is counted. From <c>BotWill.Ended</c>.</summary>
    public static void Ended(Mobile who, BotDeed deed, BotEnding ending, string why, double minutes, double perMinute, int coin)
    {
        if (deed == null || _live.Count == 0 || !_live.Remove(deed, out var kind))
        {
            return;
        }

        switch (ending)
        {
            case BotEnding.Done:
                {
                    Finished++;
                    Tally(_finishedBy, kind);

                    if (deed.KeptOut)
                    {
                        KeptOut++;
                    }

                    break;
                }
            case BotEnding.Failed:
                {
                    Failed++;

                    break;
                }
            case BotEnding.Died:
                {
                    Died++;

                    break;
                }
            default:
                {
                    Dropped++;

                    break;
                }
        }
    }

    /// <summary>The layer's second: answers due are said; clocks run out are forgotten.</summary>
    public static void Tick(long now)
    {
        BotMoods.Sweep(now);

        if (!Running)
        {
            return;
        }

        for (var i = _replies.Count - 1; i >= 0; i--)
        {
            var reply = _replies[i];

            if (now - reply.Due < 0)
            {
                continue;
            }

            _replies.RemoveAt(i);
            Speak(reply, now);
        }

        Sweep(now);
    }

    private static void Speak(Reply reply, long now)
    {
        var who = reply.Who;

        if (who is not { Deleted: false, Alive: true } || who.Map == null || who.Map == Map.Internal)
        {
            Unsaid++;

            return;
        }

        reply.Fill["other"] = reply.To?.Name ?? "friend";

        var line = BotVoice.Phrase(reply.Occasion, reply.Fallback, who, reply.Fill);

        if (line == null || !BotVoice.Say(who, "local", line, force: true))
        {
            Unsaid++;

            return;
        }

        Said++;

        if (reply.Occasion == "answer:thanks")
        {
            Thanks++;

            return;
        }

        if (reply.Thanks && reply.To is { Deleted: false, Alive: true } && Utility.RandomDouble() < ThanksChance)
        {
            _replies.Add(new Reply(reply.To, who, "answer:thanks", null, [], now + Math.Max(500, ReplyMs), thanks: false));
        }
    }

    /// <summary>Clocks long run out and offers long lapsed are forgotten once a minute.</summary>
    private static void Sweep(long now)
    {
        if (_swept && now - _sweptTick < 60000)
        {
            return;
        }

        _swept = true;
        _sweptTick = now;
        _staleSerials.Clear();
        _stalePairs.Clear();

        foreach (var (serial, when) in _answered)
        {
            if (now - when >= AnswerEveryMs)
            {
                _staleSerials.Add(serial);
            }
        }

        for (var i = 0; i < _staleSerials.Count; i++)
        {
            _answered.Remove(_staleSerials[i]);
        }

        _staleSerials.Clear();

        foreach (var (serial, invite) in _invites)
        {
            if (now - invite.Tick >= InviteMs)
            {
                _staleSerials.Add(serial);
            }
        }

        for (var i = 0; i < _staleSerials.Count; i++)
        {
            Drop(_staleSerials[i]);
            Lapsed++;
        }

        foreach (var (pair, when) in _pairs)
        {
            if (now - when >= PairEveryMs)
            {
                _stalePairs.Add(pair);
            }
        }

        for (var i = 0; i < _stalePairs.Count; i++)
        {
            _pairs.Remove(_stalePairs[i]);
        }
    }

    private static (Serial, Serial) Pair(Mobile a, Mobile b) =>
        a.Serial.Value <= b.Serial.Value ? (a.Serial, b.Serial) : (b.Serial, a.Serial);

    private static void Tally(Dictionary<string, long> by, string key)
    {
        by.TryGetValue(key, out var n);
        by[key] = n + 1;
    }

    private static string By(Dictionary<string, long> by)
    {
        if (by.Count == 0)
        {
            return "none";
        }

        using var sb = ValueStringBuilder.Create();
        var first = true;

        foreach (var (key, n) in by)
        {
            if (!first)
            {
                sb.Append(", ");
            }

            first = false;
            sb.Append(key);
            sb.Append(' ');
            sb.Append(n.ToString());
        }

        return sb.ToString();
    }

    public static string Describe() =>
        !Running
            ? "bots do not answer each other's moods"
            : $"{Asked} mood lines heard that asked for something ({By(_heardBy)}; {Unasked} content), {Answers} answers by the nearest {MostListeners} "
              + $"({Words} in words only, {Pointed} pointed at a fire, {Said} lines said, {Unsaid} not said, {Thanks} thanked); offers: {By(_offeredBy)} → "
              + $"{Proposed} put to the listener's auction, accepted: {By(_acceptedBy)}; ended: {Finished} finished ({By(_finishedBy)}; {KeptOut} kept out of the share), "
              + $"{Failed} failed, {Dropped} dropped, {Died} died, {Lapsed} lapsed untaken, {Stale} let go with the speaker gone, {Whole} mended by somebody else first; "
              + $"called into a company {Called} ({NotCalled} could not be); passed over: {Hostile} hostile, {Shunned} shunned (red or criminal), {Unfit} unfit, "
              + $"{OnClock} on the listener's clock, {PairOnClock} on the pair's, {Unanswered} lines nobody could answer";

    public static void Forget()
    {
        _invites.Clear();
        _offered.Clear();
        _live.Clear();
        _answered.Clear();
        _pairs.Clear();
        _replies.Clear();
        _picked.Clear();
        _heardBy.Clear();
        _offeredBy.Clear();
        _acceptedBy.Clear();
        _finishedBy.Clear();
        Asked = 0;
        Unasked = 0;
        Hostile = 0;
        Shunned = 0;
        Unfit = 0;
        OnClock = 0;
        PairOnClock = 0;
        Unanswered = 0;
        Answers = 0;
        Words = 0;
        Pointed = 0;
        Called = 0;
        NotCalled = 0;
        Said = 0;
        Unsaid = 0;
        Thanks = 0;
        Proposed = 0;
        Lapsed = 0;
        Stale = 0;
        Whole = 0;
        Finished = 0;
        KeptOut = 0;
        Failed = 0;
        Dropped = 0;
        Died = 0;
    }
}
