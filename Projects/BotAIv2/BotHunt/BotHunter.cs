using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a fight to any bot healthy enough to want one.
///
/// <para>
/// <b>Nothing decides who is a fighter.</b> No class list, no role check: whoever can beat the thing standing
/// in the field is offered it, and a mage with wrestling at thirty will find that the arithmetic says no. That
/// is the same rule as the pickaxe and the pen — the ability decides, not the name — and it means a crafter
/// caught in a lean patch can go and hit something rather than sitting in a census as "nothing was worth
/// doing".
/// </para>
///
/// <para>
/// <b>Only within the population's own ground, and that is the whole of the answer to the first version's
/// worst night.</b> Four hundred and forty-three deaths, a hundred and four of them one bot resurrecting in
/// the same tile every thirty seconds, all of it in the far zones the population had walked to. A hunt that
/// cannot begin more than a screen and a half from where the bot is standing, in a world already bounded to
/// two hundred tiles around the spawn, cannot build that loop: the bot is never far from where it gets up.
/// </para>
///
/// <para>
/// The cost of this proposer is a real spatial sweep, every time a free bot asks. That is unavoidable and it
/// is the honest exception to "ask the world cheaply": a vein stays where it is and can be remembered, a shop
/// keeper stands still, but a monster walks and respawns, so a remembered one is a lie inside a minute.
/// </para>
/// </summary>
public sealed class BotHunter : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHunter));

    /// <summary>
    /// The share of its health a bot needs before it will go looking for a fight.
    ///
    /// Higher than the share at which it runs away, on purpose, and the gap is what stops a bot bouncing:
    /// flee at forty per cent, set out again at eighty. Without the gap a bot that just escaped is
    /// immediately offered the same fight by the same arithmetic.
    /// </summary>
    public static double FitAt { get; set; } = 0.8;

    private static bool _saidNoQuarry;

    public string Name => "Hunter";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        if (body.HitsMax <= 0 || body.Hits < body.HitsMax * FitAt)
        {
            return null;
        }

        // <b>A medic does not go looking.</b> See BotClass.DefendsOnly: its station is already at the back of
        // the formation, and this makes the same thing true of its instincts, so it does not walk forward
        // into a fight the company is winning and is then not there for the one it is losing. Hitting back
        // when something reaches it stays a reflex — BotMobile.OnDamage is not a decision and cannot be
        // talked out of — so this refuses the hunt, never the defence.
        if (body is BotMobile { Class.DefendsOnly: true })
        {
            Sworn++;

            return null;
        }

        // <b>Already in more of a fight than it can win.</b> Offering a quarry here offers a second front,
        // and it is not hypothetical: a bot that has just given a fight up for being outnumbered is standing
        // in the middle of those numbers with nothing in hand, and the nearest thing it can beat one-to-one
        // is one of them. Left alone it takes the same fight again, drops it again, and the pair of rules
        // sit there swapping the bot between them until something kills it. A prowl is scored at almost
        // nothing and is the right answer anyway: it is a walk to somewhere else.
        if (BotThreat.Decide(body, BotMobile.NoticeRange) == BotStand.Outmatched)
        {
            var elsewhere = Hunting(bot, body, map, out var outmatched, out var reach, out _);

            return Offer(bot, body, map, elsewhere, outmatched, reach);
        }

        // A poor hunter looks wider first, for a kind that pays (02.10.2026, build 389; Patrick's option 2). See BotQuarry.Paying.
        var quarry = BotQuarry.Paying(bot, body);

        // <b>A fighter within the walk outranks the grazing kind at hand (01.10.2026, build next; Patrick: hunters hit monsters,
        // not cows — the aggressive first, the harmless only when no aggressive one is within reach of the walk).</b> The choice
        // ranked what is hostile first, but only among what stood within thirty tiles, and round a town that is cows, sheep,
        // boars and great harts: on build 386 hunters were sent after 3231 creatures of the grazing kind against 476 that fight
        // and 94 at their throat, 38.6 % of the solo kills were livestock and deer at nought gold, and the gold came to 125 a
        // bot-hour. The zones know where the fighters live (BotZones; the fifth named ground, Zoned): when the quarry at hand is
        // of the grazing kind and a hostile zone within the walk holds a living fighter this hunter could take alone, the hunter
        // goes after that one instead — as its quarry, claimed and walked to like any other (BotSlay), not as a prowl, so the
        // auction weighs it as the hunt it is. A fighter the hunter could take whose ground asks a company (the quadrant's or
        // the peril map's word, with the bots standing by enough for it — BotQuad.Together) raises that company, as a prowl
        // for such ground always has, rather than taking the cow: the Opus dossier of 19:02 read 1,228,731 creature looks
        // passed over by Best as "on ground asking more strength than it brought", the monsters by the towns standing in
        // deadly squares while the cows stood in the fields. Only when neither is to be had does the grazer stay.
        if (quarry != null && GrazersLast && BotQuarry.HostileFirst
            && !ReferenceEquals(quarry.Combatant, body) && !BotQuarry.Fights(quarry))
        {
            var ground = Hunting(bot, body, map, out var needsCompany, out var zoneSeek, out var fighter);

            if (ground != Point3D.Zero && fighter is { Deleted: false, Alive: true }
                && BotQuarry.Ours(body, fighter) && !BotQuarry.Shunned(fighter) && !BotQuarry.Crowded(fighter))
            {
                if (needsCompany)
                {
                    GrazerCompanied++;

                    return Offer(bot, body, map, ground, true, zoneSeek);
                }

                GrazerPassed++;
                quarry = fighter;
            }
        }

        // <b>Asked before the walk instead of after it.</b> See BotThreat.Overrun: 202 hunts ended on "too
        // many of them around" over the night of 02-03.09.2026 and 184 of those fired inside the shortest
        // span the ledger records, so the crowd was standing there when the bot chose rather than gathering
        // while it walked. Only for quarry worth walking to — something already at arm's length costs no
        // journey, and the arrival test will settle it on the next beat anyway.
        //
        // <b>And for quarry at arm's length too (29.09.2026, build 331).</b> "The arrival test will settle it" settled it as a
        // failed hunt: of fifteen hunts that failed in the first ten minutes of build 330, eight were "too many of them around
        // a brown bear / a panther / a timber wolf" — a pack beside the bot, weighed only once the fight had begun. Asked
        // before the offer, a pack is a reason to look elsewhere, not a hunt taken and given up.
        if (quarry != null
            && BotThreat.Overrun(body, quarry, BotMobile.NoticeRange))
        {
            // Crowded rather than shunned, and BotQuarry says why at length: one bot being outnumbered is the
            // argument for a company, not against the creature. BotQuarry.Best skips it for lone hunters from
            // here, so the next pick is a different one.
            BotQuarry.Crowd(quarry);
            Overrun++;

            quarry = null;
        }

        if (quarry == null)
        {
            Missing(map);

            // Nothing here. Offer to go and look instead — worth almost nothing, so it wins only when the
            // auction has nothing else at all, which is exactly when a fighter should be out walking rather
            // than standing in a square.
            // <b>The squares a sweep left for the fight it found are walked first (06.10.2026, build next).</b> See BotProwl.Found.
            var again = Again(body, map);

            if (again != null)
            {
                return again;
            }

            var ground = Hunting(bot, body, map, out var company, out var seek, out _);

            return Offer(bot, body, map, ground, company, seek);
        }

        // Claimed the moment it is offered, not when the first blow lands.
        //
        // <b>This ordering is the whole of the difference between a fight and a crowd.</b> A claim made on
        // contact comes too late: every free bot in the field sees the same unclaimed skeleton in the same
        // beat, every one of them sets off for it, and only the two or three that can reach a tile beside it
        // ever swing — the rest orbit it for as long as it lives, which is what a pile of bots circling one
        // monster actually is. Claiming here means one bot goes, and the others are offered the next thing.
        BotQuarry.Claim(body, quarry);
        BotQuarry.Chosen(quarry, body);

        // The skill the roll actually handed this bot, not the one its class is named after. A bot with no
        // weapon is a bot with its hands, and hands train wrestling.
        var trains = bot.Bond?.Weapon?.Skill ?? SkillName.Wrestling;

        return new BotSlay(quarry, trains);
    }

    /// <summary>
    /// Somewhere within the population's ground that is worth walking to for a fight, or nothing.
    ///
    /// <para>
    /// Two conditions and no cleverness: a body can stand on it, and it is not a town — because a town is
    /// where fighting is forbidden outright, and offering a bot a walk to one would be offering it a walk to
    /// a place it cannot use. Sampled rather than searched: a handful of candidates costs a few surface
    /// probes, where anything exhaustive would cost a spatial sweep per idle bot per review.
    /// </para>
    ///
    /// <para>
    /// Whether the ground turns out to hold anything is not asked here and cannot be — that is what the walk
    /// is for. What the bot learns by arriving is filed by the ledger against that patch, so ground that
    /// never pays stops being chosen without anybody keeping a list of bad places.
    /// </para>
    /// </summary>
    /// <param name="company">
    /// Whether the ground that came back is ground this bot cannot take alone. True means the strength gate
    /// refused it and the bots standing nearby would clear it together — so whatever walks there is obliged
    /// to raise a company first. See BotQuad.Together.
    /// </param>
    /// <param name="sighted">
    /// The living fighter of a hostile zone whose place the ground that came back is (01.10.2026, build next), or null: the
    /// creature <see cref="Zoned"/> named, when it fights whoever comes near and its place won the sample. See Propose.
    /// </param>
    private static Point3D Hunting(IBotWilful bot, Mobile body, Map map, out bool company, out int seek, out BaseCreature sighted)
    {
        company = false;
        seek = 0;
        sighted = null;

        var ledger = bot?.Resolve?.Ledger;
        // Round where this bot lives (29.09.2026): the darts of a hunter living in Trinsic land round Trinsic.
        var home = BotPopulation.HomeOf(body);
        // Half the ground the population may want things on, and not because a prowl is less entitled to it.
        // The far edge of the roam is where the bad terrain is — the bots carried home by the rescue were all
        // picked up out there — and a walk that ends in being rescued is worse than a shorter walk.
        //
        // <b>And never further than a path is paid to look, which is the half that was missing.</b> Two
        // numbers sat on one shelf and had never met: this one says how far a dart may be thrown, and
        // BotPath's ceiling says how far a road is ever searched for. On 08.09.2026 they read five hundred
        // and two hundred and forty. In the ten minutes measured, 432 of the shard's 495 refused roads were
        // prowls, their destinations averaged 392 tiles from home, and 324 of them lay beyond three hundred
        // and fifty — a band no search on this shard is funded to cross. Those errands did not sometimes
        // fail; they could not succeed, and every one of them paid for a full search before finding out.
        //
        // Nothing else needed changing to see it: Roam is a dial, and halving it live moved the whole of the
        // shard's refused-road count within minutes.
        //
        // <b>It cannot be fixed by remembering places.</b> BotRefused files a square eight tiles across and
        // the far edge of a five-hundred-tile box is thousands of them, so a dart lands on a fresh square
        // every time and the record it should have consulted is always empty. A note about a place is no
        // answer to a sample taken over an area.
        // Half the bot's own leash, not half the population's roam: a newcomer's darts land near home. See BotPopulation.Leash.
        var roam = Math.Clamp(BotPopulation.Leash(body) / 2, BotQuarry.Reach, Walkable);

        var best = Point3D.Zero;
        var bestPaid = -1.0;
        var bestKnown = -1.0;
        var bestWanted = false;
        var bestLive = false;

        // How much of this sample the quietness rule threw away. Counted so that "nowhere to walk" can be
        // told apart from "nowhere quiet enough to be worth walking to" — see Stranded.
        var refused = 0;

        // Whether the ground that wins will need a company raised for it. Held beside the winner rather than
        // recomputed, because the answer belongs to the square that was chosen and not to the last one looked at.
        var needsCompany = false;
        var bestNeedsCompany = false;

        // The least dull of what it threw away, in case it throws away all of it.
        var quietest = Point3D.Zero;
        var quietestSafety = 0.0;

        // The shard's own answers go in first, so that they are always on the list and are beaten only by
        // ground this bot has actually been paid on. See Noisy and Paying.
        //
        // <b>Two of them, and the second was missing.</b> Noisy is where blood is being spilt, which is the
        // right thing to name when nobody has been paid anywhere yet. Paying is where hunting has actually
        // come to something, which is a different question and the better one once the island has been
        // worked — and with the roam raised to five hundred on 27.08.2026, eight random darts into a box a
        // quarter of a million tiles across stopped finding either. Eighteen bots of thirty-four were
        // prowling at once, which is this shard's own way of saying that half the population had nothing to
        // do and was walking to prove it.
        var noisy = Noisy(body, map, roam);
        var paying = Paying(body, map, roam);

        // <b>And the map's own answer, which it had never been asked for.</b> Noisy is where blood is being
        // spilt now and forgets in twenty minutes; Paying is where this trade has come to something. Neither
        // is the quadrant record, which is the one thing on this shard that remembers for good which ground
        // has hurt people — and it was consulted only to veto a square somebody else had thought of. The beat
        // printed the consequence every window without a second number to compare it to: 15125 grounds passed
        // over as too quiet against 0 picked for having hurt somebody.
        //
        // At the whole roam rather than half of it, and that is the other half of the complaint. The halving
        // above is an argument about darts thrown into terrain nobody has seen — a walk that ends in being
        // rescued is worse than a shorter walk. It is not an argument about a square the population has stood
        // in and bled in: that is a known place, and on 02.09.2026 the known dangerous ones sat between
        // Britain and Minoc, about four hundred and ninety tiles from home, with the darts reaching 250.
        var feared = Feared(body, map);

        // <b>And the guild's board, before any of them.</b> A material that comes off a carcass, and the nearest square
        // where such carcasses were seen walking — see BotQuad.Yielding, filled in by every bot's look at its own
        // square. Patrick's order of 15.09.2026: if a resource comes off a creature, the hunters hunt exactly that
        // creature; until now they only carved it when they happened upon it.
        var material = BotCharter.Carved(body);
        var yielding = material == null ? Point3D.Zero : BotQuad.Yielding(map, body.Location, roam, material);
        var bestBoarded = false;

        // <b>An island dweller hunts over the water too (29.09.2026, build 332; Patrick: "Jhelom is fine, but the bots in it
        // sit and do nothing").</b> The ten of The Anchor live on the Jhelom islands; their darts land round the house, in
        // the sea or on islets no road joins, and 66 % of their prowls on 329–331 ended "nothing here" while the mainland
        // lay two thousand tiles off by the crow — past Walkable from anywhere. Every other dart of a bot on a land other
        // than the population's home is thrown round the far side of the nearest moongate that leads there, and measured
        // from that landing: the walk goes through the moongate (BotGates) and each leg of it is short.
        var mainland = BotGates.Ready ? BotGates.LandOf(map, BotPopulation.Where) : -1;
        var overWater = mainland >= 0 ? BotGates.NearestMoongate(map, body.Location, mainland, Walkable) : null;
        var landing = overWater?.To ?? Point3D.Zero;

        // <b>And the zones' answer: where the quarry actually lives, and how much of it a hunter can take (30.09.2026, build
        // 375).</b> See Zoned.
        var zoned = Zoned(body, map, home, roam, out var zoneSeek, out var zoneCreature);

        for (var tries = 0; tries <= Samples + 4; tries++)
        {
            Point3D where;

            // Per candidate, or the first square that wanted a company would mark every square after it.
            needsCompany = false;

            if (tries == 0)
            {
                if (noisy == Point3D.Zero)
                {
                    continue;
                }

                where = noisy;
            }
            else if (tries == 1)
            {
                if (paying == Point3D.Zero)
                {
                    continue;
                }

                where = paying;
            }
            else if (tries == 2)
            {
                if (feared == Point3D.Zero)
                {
                    continue;
                }

                where = feared;
            }
            else if (tries == 3)
            {
                if (yielding == Point3D.Zero)
                {
                    continue;
                }

                where = yielding;
            }
            else if (tries == 4)
            {
                if (zoned == Point3D.Zero)
                {
                    continue;
                }

                where = zoned;
            }
            else
            {
                var centre = landing != Point3D.Zero && (tries & 1) == 0 ? landing : home;
                var x = centre.X + Utility.RandomMinMax(-roam, roam);
                var y = centre.Y + Utility.RandomMinMax(-roam, roam);

                if (!BotStep.Settle(map, x, y, out var z))
                {
                    continue;
                }

                where = new Point3D(x, y, z);
            }

            // <b>The zone's living creature is known quarry, not a guess (01.10.2026, build next).</b> Patrick, 01.10 ~00:50: the
            // zones are practical — the bots know where to hunt, what is nearer, where there are more mobs. Build 381 named a
            // living creature's place for the fifth ground, and the darts' filters then threw it away like any dart — too quiet,
            // lately empty, untrodden, under the bot's nose — or let a random dart outrank it: on build 384 (00:42–10:20) 109384
            // zones named, 8956 chosen, and 2148 of 4767 prowls ended "nothing here". A creature standing there now is the proof
            // those filters stand in for, so they are not asked of it (counted, ZoneSpared), and it ranks next after the board's.
            var live = tries == 4;

            // Far enough to be somewhere else. A candidate under the bot's nose is the ground it is already
            // standing on, and walking to it proves nothing. Except a living creature seen from inside a town: the hunter there
            // sees nothing to hunt beyond the gate (BotThreat.Hostile asks the hunter's region), and the creature is a walk away.
            var near = live && body.Region?.IsPartOf<TownRegion>() == true ? BotProwl.ArriveWithin : BotQuarry.Reach;

            if (Utility.InRange(body.Location, where, near))
            {
                continue;
            }

            // A dart beyond the bot's own leash is not thrown (BotPopulation.Leash, 29.09.2026): on build 292 Gunnloda of
            // The Needle, standing at Cove, was sent "looking for a fight near (2144, 928)" — the plague bog.
            if (!BotPopulation.Within(map, where, body))
            {
                continue;
            }

            // Nor one thrown onto a land no gate joins to this one. See BotPopulation.Reachable.
            if (!BotPopulation.Reachable(map, body.Location, where))
            {
                continue;
            }

            // <b>And near enough that a road to it would ever be searched for — measured from the bot, which
            // is the correction that makes the rule work at all.</b> The candidates are reckoned around the
            // population's home and not around the asker, on purpose, so that a company setting out sets out
            // to the same place. That is right, and it means clamping the throw says nothing about the length
            // of the walk: a bot that has itself wandered four hundred tiles out is handed a destination
            // beside the house, and the road it needs is four hundred tiles long whatever the throw was.
            //
            // The search is funded by distance — MsPerTile a tile up to CeilingMs — so beyond Walkable no
            // road is ever found, from anywhere, for anybody. Asked here, of this bot and this square, rather
            // than of the box the darts were thrown into. See Walkable.
            // Over the water the search is funded leg by leg: to the moongate, and from its landing. See the note above.
            var overland = landing != Point3D.Zero && BotGates.LandOf(map, where) == mainland;

            if (overland ? !Utility.InRange(landing, where, Walkable) : !Utility.InRange(body.Location, where, Walkable))
            {
                Distant++;

                continue;
            }

            if (overland)
            {
                OverWater++;
            }

            // <b>Ground no gate joins to the hunter is not ground to offer it (30.09.2026, build 355).</b> The auction refuses such
            // work outright (BotAppraisal: "hunt is on ground no gate joins to this bot's"), and a sampler that did not ask handed
            // it the one candidate it then threw away: Liesl the Warrior, 4gp to her name, stood five minutes at (657, 848) by
            // Yew on 354 "holding nothing because: 71 proposers asked, 2 offered work, and it was refused: hunt is on ground no
            // gate joins to this bot's" — a poor bot that should have been hunting (Patrick, 30.09: poor bots hunt).
            if (BotGates.Ready && !BotGates.Joined(map, body.Location, where))
            {
                Unjoined++;

                continue;
            }

            // <b>And near enough by road, which is not the same thing.</b> The straight line above is what a search is
            // funded by; the road is what the walk has to cover, and ground behind a river whose way round runs off south
            // for six hundred tiles passes the first and is hopeless by the second: on 15.09.2026 the ground north-west of
            // the banks west of Britain took one arriving prowl in 44 sessions. Counted whatever the dial says, so a session
            // before it is turned says how many darts it would touch. See BotRoads.
            if (BotRoads.Detour(map, body.Location, where) > Detour || BotRoads.Behind(map, body.Location, where) > Detour)
            {
                Roundabout++;

                if (ReadsRoads)
                {
                    continue;
                }
            }

            // <b>And ground somebody has actually stood on, which is the only cheap promise that a road to it
            // exists.</b> Everything this shard knows about bad ground is a record of failures — the reach
            // ledger's pockets, the quadrant baulks, BotRefused's squares — and all three are the wrong shape
            // for a dart. A refusal describes one square; the island has hundreds of thousands of them, so a
            // random throw lands on a fresh one every time and the record it should have consulted is always
            // empty. Measured on 08.09.2026: with the throw already clamped to what a search is funded to
            // cross, 249 of 350 failures in five minutes were still prowls, their destinations averaging 177
            // tiles away — well inside the budget. Remembering where nobody could get to is no answer to a
            // sample taken over an area.
            //
            // <b>The positive record is the one that scales, and it was already written down.</b> BotQuad
            // marks a square trodden when a bot has stood in it, keeps it across restarts, and this shard
            // read 4096 of them back at boot — some three and a half million tiles. Its own Frontier says the
            // reasoning in as many words, about exploration: a candidate beside ground somebody has walked
            // can be got to, and a square picked off a blank map cannot promise that. Frontier was the only
            // thing reading it.
            //
            // Its own square or one beside it, so the hunt may still push a ring outwards rather than pace
            // what it already knows. Off by a dial, because a rule that narrows where the population may go
            // has to be answerable on a running shard.
            if (TroddenOnly && !Walked(map, where))
            {
                if (live)
                {
                    ZoneSpared++;
                }
                else
                {
                    Untrodden++;

                    continue;
                }
            }

            if (Region.Find(where, map)?.IsPartOf<TownRegion>() == true)
            {
                continue;
            }

            // <b>And here, where every candidate passes, rather than only in the two named pickers.</b>
            // Paying and Noisy each ask this and the darts did not, so the one path that produces most of the
            // prowls on this shard was the one path that could not learn: 1009 prowls in forty-four minutes on
            // 08.09.2026, 676 of them over inside fifteen seconds, GOODS nought and GOLD -2840. A dart lands
            // on a rooftop, the road is refused, the square is written down where everybody can read it, and
            // the next dart lands on it again because nobody here was reading.
            //
            // Free, and it has to be: this is inside the sampling loop. One dictionary lookup, and arriving
            // anywhere clears the square again.
            // <b>And the quadrant's own rest, which the two named pickers read and the darts did not.</b> A prowl that stops
            // closing baulks its destination's quadrant (BotProwl → BotQuad.Baulk), not BotRefused, so the darts never saw
            // it — and with darts kept to trodden ground (TroddenOnly) they land on the same few squares beyond a river
            // again and again. Build 201, 07:26–07:56 on 24.09.2026: twelve prowls "got no nearer", every one stopped on
            // the bank at (1088–1092, 1617–1644) or south of it, their thirteen destinations in seven quadrants west of
            // the river, four of them in (960–990, 1650–1680) alone. The dart that asks here is thrown once per quadrant
            // per rest, and the rest grows by ten minutes with every refusal (BotQuad.BaulkMs).
            if (BotRefused.Refusing(map, where) || BotQuad.Baulking(map, where) || NoWay(map, where))
            {
                Darted++;

                continue;
            }

            // Somewhere already proved impossible from here, asked for nothing.
            //
            // <b>A dictionary lookup, and it must stay one.</b> The first attempt at this ran a real path
            // search per candidate — four of them, per idle bot, per review — on the reasoning that a search
            // costs two milliseconds and this is the cheapest moment there is. It is not: the whole population
            // shares sixty milliseconds of searching a second, and fifteen bots vetting prowl points spent all
            // of it. Every other search on the shard then shrank to the floor of a quarter millisecond, came
            // back with nothing walkable, and the bots stopped being able to reach anything at all — a hundred
            // and fifty failures and eighteen bots "rescued" from a spot three tiles from home. The proposer
            // contract says this in as many words: the question may be real, it may not be expensive.
            //
            // What is free is what somebody has already proved. A pocket of ground walked to its edges answers
            // in one comparison, which is exactly the case worth excluding — across water, behind a wall.
            if (BotReach.Ask(map, body.Location, where, BotArrival.Within(BotQuarry.Reach)) == BotReachVerdict.Sealed)
            {
                continue;
            }

            // <b>And not beyond a place where walks heading this way keep stopping.</b> The pocket above is proved from the
            // far side and a river open at both ends is no pocket, so darts went on landing past the same few banks west
            // and south of Britain. See BotBarrier: a list of stopping places, one comparison each.
            if (BotBarrier.Beyond(map, body.Location, where))
            {
                continue;
            }

            if (!live && LatelyEmpty(body, where))
            {
                Revisits++;

                continue;
            }

            // <b>And whether whoever is going is strong enough for it, which is a wall and not a price.</b>
            // Patrick's order of 03.09.2026: below the threshold a bot does not go, whatever the ground is
            // worth. A bot in a company brings the company's strength, so the same square that refuses one
            // brawler accepts the four of them — which is the whole point of the rule and the reason it is
            // asked here, where the ground is chosen, rather than at the moment of stepping into it.
            // <b>And ground where bots have been dying lately is company ground, whatever one bot dares.</b> The auction's
            // veto on such ground (BotPeril.Lethal) lets through the work that goes where the fighting is, because that
            // is the work that deals with it — and a lone prowl is on that list without being the remedy. At 02:37 on
            // build 45 the noisiest square on the island was a plague beast's field at (1854, 1062), where a company was
            // killing its spawn; Quenna, Edda Ashdown and Yarrow took lone prowls to it inside five seconds and Isolde a
            // minute later, and between 02:40:37 and 02:44:43 five of them died there, Yarrow twice. The square asked
            // little strength of the quadrant record, which had not yet seen the deaths it was about to hold.
            // From 26.09.2026 the fight there makes it company ground, and no longer a count of the dead: see
            // BotPeril.Overwhelm, Patrick's answer that bots go round only ground several times their strength.
            var dares = BotQuad.Dares(body, map, where);
            var lethal = BotPeril.Overwhelms(map, where, body.Location, BotQuad.Strength(body), out _, count: false);

            // <b>The zone's own reckoning of the fight there (build 375).</b> A deadly zone is lethal ground whatever the
            // quadrant says; a hostile one whose fight — the strongest in full and the rest that can close at once — outweighs
            // the hunter (or its company) is ground it does not dare alone. Both then go the way of any ground too strong: a
            // company, or passed over. See BotZones.
            if (ReadsZones)
            {
                var level = BotZones.LevelAt(map, where.X, where.Y, out _, out var threat);

                if (level == BotZoneLevel.Deadly && !lethal)
                {
                    lethal = true;
                    ZoneDeadly++;
                }
                else if (level == BotZoneLevel.Hostile && dares && threat > BotQuad.Strength(body) * ZoneDare)
                {
                    dares = false;
                    ZoneTooStrong++;
                }
            }

            if (lethal && dares)
            {
                Deadly++;
            }

            // <b>And the road there, for a hunter going alone (01.10.2026, build 388).</b> The ground was weighed and the road
            // never was: 124 of 139 prowl deaths on 386 fell more than a hundred tiles from the prowl's target. A road whose
            // record of deaths or whose patrols ask more than the hunter alone brings makes the ground company ground, as a
            // fight too strong on the ground itself does. See BotDanger.Road.
            if (dares && !lethal && BotDanger.Road(body, map, body.Location, where, BotQuad.Strength(body), out _, out _, out _))
            {
                dares = false;
                RoadTooStrong++;
            }

            if (lethal || !dares)
            {
                // <b>Or bring people, which is the other half of the order.</b> Ground this bot cannot take
                // alone is still ground it may take with the bots standing beside it, and refusing it here
                // would leave the population permanently unable to work anywhere worth working. Nothing is
                // formed at this point — a proposer weighs and an undertaking acts — so the ground is
                // accepted and the company is raised by the prowl itself on its first beat.
                // <b>Counted where the company will be raised, which is not where this bot is standing.</b> BotProwl
                // walks to the edge of the town first — Patrick's order of 03.09.2026 — and raises the company out of
                // whoever stands there, and this counted whoever stood beside the bot. By 23:33 on build 41 that was
                // 16 companies raised and 24 errands given up for want of strength, most of them at one square,
                // (1425, 2055), the worst on the island. Outside a town Gate answers nothing, and both count here.
                // <b>Not over the water (29.09.2026, build 333).</b> A company is raised out of whoever stands at the edge of the
                // asker's town, and for an island dweller that is the island: the over-water darts of 332 took The Anchor to
                // ground by Trinsic asking 6500–12500 alone or in scratch companies of two to four, and the guild — a tenth of
                // the population — made 16 of the first 27 deaths of the session (`night-r4-jhelom.md`). Across the water only
                // ground the bot can take by itself is walked to.
                if (overland)
                {
                    Overmatched++;
                    AbroadTooHard++;

                    continue;
                }

                var gate = BotPopulation.Gate(map, body.Location, where, counted: false);

                if (!BotQuad.Together(body, map, where, BotMuster.Reach, gate))
                {
                    Overmatched++;

                    continue;
                }

                // <b>Somebody is already raising one for this square, so this bot is not.</b> Refused here
                // rather than by the errand, and that distinction is the whole of it: a proposer that skips
                // a candidate costs nothing, while an errand that fails on its first beat is taken again on
                // the next — 1728 of those inside five minutes when this same test lived one layer down.
                if (BotProwl.Raising(map, where))
                {
                    Claimed++;

                    continue;
                }

                needsCompany = true;
            }

            // Of the places that pass, the one this bot has actually been paid at.
            //
            // <b>A prowl to a random point is a walk that usually finds nothing, and it showed.</b> Forty-six
            // of sixty-four finished undertakings in one stretch were prowls — bots spending their evening
            // walking to empty fields — against four hunts. The ledger already knows where fighting paid this
            // bot, because it files every finished hunt under the patch of ground it happened on; it simply
            // was not being asked. Nothing new is measured here and no new memory is kept.
            //
            // Asked with a prior of nothing, so unknown ground scores zero and known-bad ground scores what it
            // is worth. Early on everything ties at zero and the pick is the first sampled — which is the
            // exploration this needs — and the moment one patch pays, that patch wins.
            var paid = ledger?.Expect(BotSlay.Trade, map, where, 0.0) ?? 0.0;

            // <b>And where the shard as a whole knows there is fighting, for the bot that knows nothing.</b>
            // The ledger above is private, and a private memory cannot start itself: a scribe that has never
            // once been paid for a hunt scores every candidate at nought, takes the first sample, walks a
            // minute to an empty field and does it again for the rest of the evening. That is not a
            // hypothetical — Lysa did exactly that four times running on 25.08.2026 while two companies were
            // killing spectres and trolls two hundred tiles away, which she had no way to know.
            //
            // BotPeril is that fact, kept shard-wide and decaying, and it is already what a captain reads to
            // decide where to take a company. Used only to break the tie the comment above admits to — "early
            // on everything ties at zero and the pick is the first sampled" — so a bot with real experience
            // of its own still prefers its own ground, and a bot with none walks towards the noise instead of
            // at random.
            var known = BotPeril.Reading(map, where);

            // <b>Ground the population has walked through fifty times without incident, refused outright.</b>
            // By order, and it is the one rule here that throws a candidate away rather than ranking it: a
            // square that has earned its way above BotQuad.TooQuiet has nothing living in it worth killing,
            // and a hunter standing in it is a hunter earning nothing. Peril cannot express this — it forgets
            // — so "quiet because it was cleared an hour ago" and "quiet because there was never anything
            // here" read the same to it. See BotQuad on why the two maps are separate.
            var safety = BotQuad.Safety(map, where);

            if (safety > BotQuad.TooQuiet && live)
            {
                ZoneSpared++;
            }
            else if (safety > BotQuad.TooQuiet)
            {
                Quiet++;
                refused++;

                // <b>Kept as the answer of last resort, because a filter with nothing behind it is a ban.</b>
                // This candidate has already passed every other test — it is somewhere else, out of town and
                // reachable — and is being thrown away only for being dull. That is right while anything
                // better exists and is a refusal to walk when nothing does: the counter put in on 02.09.2026
                // to settle the argument came back with 1585 hunters in a single window left with nowhere to
                // go at all, which is the population's own way of saying that half of it was standing still
                // because every square it thought of was safe. The quietest ground on the island is still a
                // walk to somewhere else, which is what a prowl is for and what this shard calls "scored at
                // almost nothing and the right answer anyway".
                if (quietest == Point3D.Zero || safety < quietestSafety)
                {
                    quietest = where;
                    quietestSafety = safety;
                }

                continue;
            }

            // <b>And ground that has hurt somebody outranks ground that merely paid.</b> Also by order, and
            // it is a precedence rather than a bonus: any square at or below BotQuad.Wanted beats every
            // square above it, whatever the ledger says about takings. Inside each of the two groups the old
            // ordering stands untouched — what this bot was paid, then where the shard hears fighting — so a
            // hunter with real experience still prefers its own ground among equals.
            var wanted = safety <= BotQuad.Wanted;

            // <b>And ground that has hurt somebody is not for a novice, whatever its rank.</b> The order that such
            // ground outranks ground that merely paid sent the least able bots to the most dangerous squares first,
            // and the deaths of 14:33–15:19 on 16.09.2026 were theirs. See BotLadder.Novice.
            if (wanted && BotLadder.Novice(body))
            {
                Green++;
                refused++;

                continue;
            }

            // The board outranks everything: a square where the creature the guild asks for is known to walk beats
            // ground that has hurt somebody and ground that has paid. See BotCharter.Carved.
            var boarded = material != null && BotQuad.YieldsAt(map, where, material);

            var better = best == Point3D.Zero
                || (boarded && !bestBoarded)
                || (boarded == bestBoarded
                    && ((ZoneFirst && live && !bestLive)
                        || ((!ZoneFirst || live == bestLive)
                            && ((wanted && !bestWanted)
                                || (wanted == bestWanted && (paid > bestPaid || (paid >= bestPaid && known > bestKnown)))))));

            if (better)
            {
                best = where;
                bestPaid = paid;
                bestKnown = known;
                bestWanted = wanted;
                bestBoarded = boarded;
                bestLive = live;
                bestNeedsCompany = needsCompany;
                company = needsCompany;

                if (wanted)
                {
                    Sought++;
                }

                if (boarded)
                {
                    Boarded++;
                }
            }
        }

        // <b>The one number that says whether the quietness rule is a preference or a veto.</b> On 02.09.2026
        // six warriors stood at 1420,1685 in Britain holding 304 to 446 gold while the record said "31
        // proposers asked, not one of them had anything to offer", and the beat said 15125 hunting grounds
        // had been passed over as too quiet against 0 picked. Those two facts are consistent with a rule
        // working as designed and equally consistent with a filter that leaves a hunter with nothing at all,
        // and nothing on the shard could tell them apart. This can: it counts only the times every last
        // candidate was thrown away for quietness and the hunter came back empty.
        if (best == Point3D.Zero && refused > 0)
        {
            Stranded++;

            // Still counted as stranded above, because it is: the count is what says how often the whole
            // sample was dull, and that stays worth knowing after the bot has somewhere to walk again.
            return quietest;
        }

        if (zoned != Point3D.Zero && best == zoned)
        {
            ZoneChosen++;
            seek = zoneSeek;

            if (zoneCreature != null && BotQuarry.Fights(zoneCreature))
            {
                sighted = zoneCreature;
            }
        }

        return best;
    }

    /// <summary>
    /// A prowl over the ground <see cref="Hunting"/> chose, as a sweep of the squares round it. See <see cref="Sweep"/>.
    /// </summary>
    private static BotProwl Offer(IBotWilful bot, Mobile body, Map map, Point3D ground, bool company, int seek)
    {
        if (ground == Point3D.Zero)
        {
            return null;
        }

        // <b>The first square asks too (07.10.2026, work18).</b> The ring of a sweep asked the reach verdict; the first square is
        // whatever the hunter's pickers chose, and on build 416 most of the prowls that failed "no way through" failed on their first
        // square. A square a sweep could not walk to within the hour, or ground the reach map calls an island or a pocket, is no
        // prowl. Counted in the island line (BotProwl.DescribeSweeps).
        if (NoWay(map, ground))
        {
            BotProwl.FirstNoWay++;

            return null;
        }

        List<Point3D> legs = null;

        if (BotProwl.Sweeps)
        {
            legs = Sweep(bot, body, map, ground, company);

            BotProwl.SweepsOffered++;
            BotProwl.SquaresOffered += legs.Count;

            if (!company && legs.Count < BotProwl.SweepLeast)
            {
                BotProwl.ShortSweeps++;
            }
        }

        return new BotProwl(map, ground, company, legs) { Seek = seek };
    }

    /// <summary>Squares round the last one of a sweep, weighed. Reused.</summary>
    private static readonly List<(Point3D At, double Score)> _ring = [];

    /// <summary>How many of a square's best-scored neighbours are tried for a place to stand before the sweep stops growing.</summary>
    public static int SweepTries { get; set; } = 3;

    /// <summary>
    /// <b>The squares a sweep walks: the ground chosen, then the best square beside the last one, up to
    /// <see cref="BotProwl.SweepMost"/> (06.10.2026, build next).</b> Patrick, 06.10: a prowl is several quadrants to clear.
    ///
    /// <para>
    /// The first square is the one every rule of the hunter already chose (<see cref="Hunting"/>: the board, the zones' living
    /// fighters, ground that hurt somebody, ground that paid). Each next square is one of the eight round the last, never the
    /// one the bot stands in nor one it swept empty within <see cref="EmptyRestMs"/> (both counted, <see cref="BotProwl.Underfoot"/>),
    /// and only ground this bot takes alone: trodden, out of town, not closed, rested or refused, not in a deadly zone nor one
    /// whose fight there outweighs it (<see cref="ZoneDare"/>), not ground that hurt somebody for a novice, ground the quadrant
    /// record lets it dare and where the fighting does not overwhelm it (<see cref="BotPeril.Overwhelms"/>). Ranked aggressive
    /// kinds first — a hostile zone, then its aggro fringe, then the hostile creatures counted there at the last look — then
    /// what sweeps of the square paid lately (<see cref="BotSweepRecord.Worth"/>) and this bot's own record of hunts there,
    /// ground too quiet to hold anything last. A company is raised for one square and is given that square only.
    /// </para>
    /// </summary>
    private static List<Point3D> Sweep(IBotWilful bot, Mobile body, Map map, Point3D first, bool company)
    {
        var legs = new List<Point3D>(Math.Max(1, BotProwl.SweepMost)) { first };

        if (company || BotProwl.SweepMost <= 1)
        {
            return legs;
        }

        var ledger = bot?.Resolve?.Ledger;
        var strength = BotQuad.Strength(body);
        var own = BotQuad.Key(map, body.Location);
        var last = BotQuad.Key(map, first);
        var now = Core.TickCount;
        var novice = BotLadder.Novice(body);

        while (legs.Count < BotProwl.SweepMost)
        {
            _ring.Clear();

            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    var qx = last.X + dx;
                    var qy = last.Y + dy;

                    if (own.Map == last.Map && own.X == qx && own.Y == qy)
                    {
                        BotProwl.Underfoot++;

                        continue;
                    }

                    if (Listed(map, legs, qx, qy))
                    {
                        continue;
                    }

                    var mx = qx * BotQuad.Side + BotQuad.Side / 2;
                    var my = qy * BotQuad.Side + BotQuad.Side / 2;
                    var probe = new Point3D(mx, my, 0);
                    var quad = BotQuad.Known(map, probe);

                    if (quad == null || !quad.Trodden || quad.Townbound || quad.Deep)
                    {
                        continue;
                    }

                    if (BotQuad.Baulking(map, probe) || BotRefused.Refusing(map, probe) || BotBarred.Holds(map, probe)
                        || !BotPopulation.Within(map, probe, body) || NoWay(map, probe))
                    {
                        continue;
                    }

                    if (LatelyEmpty(body, probe))
                    {
                        BotProwl.Underfoot++;

                        continue;
                    }

                    var level = BotZoneLevel.Clear;
                    var threat = 0f;

                    if (ReadsZones)
                    {
                        level = BotZones.LevelAt(map, mx, my, out _, out threat);
                    }

                    if (level == BotZoneLevel.Deadly || level is BotZoneLevel.Hostile or BotZoneLevel.Edge && threat > strength * ZoneDare)
                    {
                        continue;
                    }

                    var safety = BotQuad.Safety(map, probe);

                    if (novice && safety <= BotQuad.Wanted)
                    {
                        continue;
                    }

                    var mobs = quad.Mobs > 0 && now - quad.Sighted < BotQuad.SightMs ? Math.Min(quad.Mobs, 5) : 0;

                    var score = (level == BotZoneLevel.Hostile ? 20.0 : level == BotZoneLevel.Edge ? 10.0 : 0.0)
                                + mobs * 3.0
                                + BotSweepRecord.Worth(map, probe)
                                + (ledger?.Expect(BotSlay.Trade, map, probe, 0.0) ?? 0.0) / 10.0
                                - (safety > BotQuad.TooQuiet ? 5.0 : 0.0);

                    _ring.Add((probe, score));
                }
            }

            if (_ring.Count == 0)
            {
                break;
            }

            _ring.Sort(static (a, b) => b.Score.CompareTo(a.Score));

            var next = Point3D.Zero;

            for (var i = 0; i < _ring.Count && i < SweepTries; i++)
            {
                if (!StandIn(map, _ring[i].At, out var stand))
                {
                    continue;
                }

                if (Region.Find(stand, map)?.IsPartOf<TownRegion>() == true
                    || !BotQuad.Dares(body, map, stand)
                    || BotPeril.Overwhelms(map, stand, body.Location, strength, out _, count: false)
                    || BotReach.Ask(map, body.Location, stand, BotArrival.Within(BotProwl.ArriveWithin)) == BotReachVerdict.Sealed
                    || NoWay(map, stand))
                {
                    continue;
                }

                next = stand;

                break;
            }

            if (next == Point3D.Zero)
            {
                break;
            }

            legs.Add(next);
            last = BotQuad.Key(map, next);
        }

        _ring.Clear();

        return legs;
    }

    /// <summary>
    /// Whether a sweep must not be sent to this square (07.10.2026, work18): a sweep's walk to its waypoint was refused or given up
    /// within the hour (<see cref="BotSweepRecord.NoWay"/>), or the reach map calls the place an island or a pocket — somewhere to
    /// stand that no walk from home reaches. Water and rock are not refused here: a prowl arrives within eight tiles of its point,
    /// and the open ground round a pond or a boulder is reached. Two lookups, no search. Counted in BotProwl.
    /// </summary>
    public static bool NoWay(Map map, Point3D where)
    {
        if (map == null || where == Point3D.Zero)
        {
            return false;
        }

        if (BotSweepRecord.NoWay(map, where))
        {
            BotProwl.SquaresNoWay++;

            return true;
        }

        if (BotReachMap.Class(map, where.X, where.Y, where.Z) is BotReachClass.Isle or BotReachClass.Pocket)
        {
            BotProwl.SquaresNoGround++;

            return true;
        }

        return false;
    }

    private static bool Listed(Map map, List<Point3D> legs, int qx, int qy)
    {
        for (var i = 0; i < legs.Count; i++)
        {
            var key = BotQuad.Key(map, legs[i]);

            if (key.X == qx && key.Y == qy)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A tile a body can stand on near the middle of the square whose middle this is: the middle, or a step of eight off it.</summary>
    private static bool StandIn(Map map, Point3D middle, out Point3D stand)
    {
        for (var i = 0; i < 5; i++)
        {
            var x = middle.X + (i == 1 ? 8 : i == 2 ? -8 : 0);
            var y = middle.Y + (i == 3 ? 8 : i == 4 ? -8 : 0);

            if (BotStep.Settle(map, x, y, out var z))
            {
                stand = new Point3D(x, y, z);

                return true;
            }
        }

        stand = Point3D.Zero;

        return false;
    }

    /// <summary>
    /// The squares this bot's last sweep left for a fight it found, offered again as a sweep of their own, less any that are
    /// closed, rested, refused or no longer ground it takes alone. See <see cref="BotSweepRecord.Left"/>.
    /// </summary>
    private static BotProwl Again(Mobile body, Map map)
    {
        if (!BotProwl.Sweeps)
        {
            return null;
        }

        var left = BotSweepRecord.Left(body, map);

        if (left == null)
        {
            return null;
        }

        var strength = BotQuad.Strength(body);
        var legs = new List<Point3D>(left.Length);

        for (var i = 0; i < left.Length; i++)
        {
            var at = left[i];

            if (BotBarred.Holds(map, at) || BotQuad.Baulking(map, at) || BotRefused.Refusing(map, at) || !BotQuad.Dares(body, map, at)
                || BotPeril.Overwhelms(map, at, body.Location, strength, out _, count: false))
            {
                continue;
            }

            legs.Add(at);
        }

        if (legs.Count == 0)
        {
            BotSweepRecord.Taken(body);

            return null;
        }

        BotProwl.ResumeOffered++;

        return new BotProwl(map, legs[0], false, legs, resumed: true);
    }

    /// <summary>
    /// The ground the population has actually been paid for fighting on, if this bot could get there.
    ///
    /// <para>
    /// The same shape as <see cref="Noisy"/> and offered on the same terms — a candidate, never an answer,
    /// so a bot with real experience of its own still prefers its own ground. What it adds is the case Noisy
    /// cannot cover: a wood that pays well and has stopped hurting anybody reads as nothing on the peril map,
    /// because the peril map is a record of harm and decays in twenty minutes. Being good at somewhere is
    /// exactly what makes it disappear from the only shard-wide list there was.
    /// </para>
    /// </summary>
    private static Point3D Paying(Mobile body, Map map, int roam)
    {
        var rich = BotCommons.Richest(BotSlay.Trade, map, body.Location, roam);

        if (rich == Point3D.Zero || !BotStep.Settle(map, rich.X, rich.Y, out var z))
        {
            return Point3D.Zero;
        }

        var where = new Point3D(rich.X, rich.Y, z);

        // The same three tests every sampled candidate has to pass: somewhere else, not resting after
        // refusing somebody, and reachable.
        //
        // <b>The middle one was written down and never read.</b> BotProwl.Bend files a baulk on ground it
        // could not get through, and until 07.09.2026 the only list that consulted those marks was the one
        // companies are sent to. This picker asked nothing, so the square that had just refused four bots was
        // handed to a fifth - 618 prowls failed on "no way through" in fifty minutes, and every one of them
        // paid for a full path search first.
        if (Utility.InRange(body.Location, where, BotQuarry.Reach))
        {
            return Point3D.Zero;
        }

        if (BotQuad.Baulking(map, where) || BotRefused.Refusing(map, where) || NoWay(map, where))
        {
            Rested++;

            return Point3D.Zero;
        }

        return BotReach.Ask(map, body.Location, where, BotArrival.Within(BotQuarry.Reach)) == BotReachVerdict.Sealed
            ? Point3D.Zero
            : where;
    }

    /// <summary>
    /// The worst ground the quadrant map knows of, if this bot could get to it.
    ///
    /// <para>
    /// A candidate on the same terms as the other two named ones — never an answer, so a hunter with real
    /// experience of its own still prefers its own ground. Reckoned from the population's home rather than
    /// from the bot's feet, exactly as the random darts are, so that a company setting out is setting out to
    /// the same place.
    /// </para>
    /// </summary>
    private static Point3D Feared(Mobile body, Map map)
    {
        // From where this bot lives (29.09.2026), so the hunters of one town set out to the same place.
        var middle = BotQuad.WorstNear(map, BotPopulation.HomeOf(body), FearedReach);

        if (middle == Point2D.Zero || !BotStep.Settle(map, middle.X, middle.Y, out var z))
        {
            return Point3D.Zero;
        }

        var where = new Point3D(middle.X, middle.Y, z);

        // The same two tests every candidate has to pass: somewhere else, and reachable.
        if (Utility.InRange(body.Location, where, BotQuarry.Reach))
        {
            return Point3D.Zero;
        }

        return BotReach.Ask(map, body.Location, where, BotArrival.Within(BotQuarry.Reach)) == BotReachVerdict.Sealed
            ? Point3D.Zero
            : where;
    }

    /// <summary>
    /// The square the shard has been bleeding in lately, if it is somewhere this bot could go.
    ///
    /// <para>
    /// Offered as a candidate rather than as an answer. Random sampling can look eight times and never land
    /// in the one square that matters — a graveyard is a couple of hundred tiles across at most and the roam
    /// is six hundred — so the place most likely to have a fight in it has to be put on the list by name or
    /// it will usually not be on the list at all.
    /// </para>
    /// </summary>
    private static Point3D Noisy(Mobile body, Map map, int roam)
    {
        var worst = BotPeril.Worst(map, body.Location, roam, out _);

        if (worst == Point3D.Zero || !BotStep.Settle(map, worst.X, worst.Y, out var z))
        {
            return Point3D.Zero;
        }

        var where = new Point3D(worst.X, worst.Y, z);

        // The same tests as the noisy picker above, including the baulk, and for the same reason.
        if (Utility.InRange(body.Location, where, BotQuarry.Reach)
            || Region.Find(where, map)?.IsPartOf<TownRegion>() == true
            || BotReach.Ask(map, body.Location, where, BotArrival.Within(BotQuarry.Reach)) == BotReachVerdict.Sealed)
        {
            return Point3D.Zero;
        }

        if (BotQuad.Baulking(map, where) || BotRefused.Refusing(map, where) || NoWay(map, where))
        {
            Rested++;

            return Point3D.Zero;
        }

        return where;
    }

    /// <summary>
    /// How many places to try before giving up for this beat. Each one is a surface probe and a lookup, both
    /// cheap; finding nowhere is not a failure, and the bot asks again in a few seconds.
    /// </summary>
    private const int Samples = 8;

    /// <summary>Close enough that there is no walk to save by weighing the odds first. See the check in Propose.</summary>
    public static int Near { get; set; } = 5;

    /// <summary>Quarry passed over because the crowd around it was already hopeless. See <c>BotThreat.Overrun</c>.</summary>
    public static long Overrun { get; private set; }

    /// <summary>
    /// Candidate hunting grounds passed over because that square is resting after refusing somebody.
    ///
    /// Its own tally rather than folded into the others: this is the shard declining to spend a path search
    /// on ground it has already been turned away from, and if it ever grows to swallow the whole map that is
    /// a different fault from the one it was written for.
    /// </summary>
    public static long Rested { get; private set; }

    /// <summary>
    /// Whether a dart may only land on ground the population has stood on, or beside it.
    ///
    /// A dial, not a constant: it narrows where the population is willing to go, and anything that does that
    /// has to be answerable without a rebuild. Set it false and the darts go back to the blank map.
    /// </summary>
    public static bool TroddenOnly { get; set; } = true;

    /// <summary>Sampled grounds passed over because nobody has ever stood in that square or beside it.</summary>
    public static long Untrodden { get; private set; }

    /// <summary>
    /// Whether this square, or one of the eight around it, is ground a bot has actually stood in.
    ///
    /// The ring is what keeps the hunt able to grow: the middle alone would pace what the population already
    /// knows for ever, and one square out is a walk of at most <see cref="BotQuad.Side"/> tiles past the last
    /// place somebody proved a road to.
    /// </summary>
    private static bool Walked(Map map, Point3D where)
    {
        if (BotQuad.Trodden(map, where))
        {
            return true;
        }

        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if ((dx != 0 || dy != 0)
                    && BotQuad.Trodden(map, new Point3D(where.X + dx * BotQuad.Side, where.Y + dy * BotQuad.Side, where.Z)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Hunting grounds passed over for lying further from the asker than any road is searched for.
    ///
    /// Its own bucket, and it wants watching against <see cref="Stranded"/>: the day this reads high and
    /// Stranded reads high with it is the day the population has walked so far from home that nothing near
    /// it is on offer any more, and the answer to that is to send it home, not to widen this.
    /// </summary>
    public static long Distant { get; private set; }

    /// <summary>
    /// Sampled hunting grounds passed over for having refused somebody. Its own bucket rather than
    /// <see cref="Rested"/>'s: the two named pickers ask the same question about squares somebody chose on
    /// purpose, and pouring the darts into that number would hide which of the four candidate paths the
    /// saving came from.
    /// </summary>
    public static long Darted { get; private set; }

    /// <summary>
    /// How long a ground a bot walked to and found empty stays out of that bot's next choices of where to look.
    ///
    /// <para>
    /// <b>Back to the same empty field, again and again.</b> On build 100 (01:30–03:16, 17.09.2026) 1124 of 1859 prowls
    /// ended "nothing here", and 427 of all of them were walks to within ten tiles of a ground the same bot had found empty
    /// in the quarter of an hour before — Perri Ashdown 37 times, Bryn Ashdown 21. Alden Ashdown walked to (2080, 928) at
    /// 03:01, 03:03, 03:05 and 03:10, and the watchers wrote it up three times as stuck in a loop. The named grounds — the
    /// noisiest, the best paying, the most feared, the richest in carcasses — are the same ground every time they are asked,
    /// and a prowl that finds nothing ends Done, which teaches the choosing nothing. Per bot, because the ground is empty for
    /// the one that has just walked it; another bot a road away has not looked.
    /// </para>
    /// </summary>
    public static int EmptyRestMs { get; set; } = 900000;

    /// <summary>How near a candidate must lie to a ground found empty to be that ground.</summary>
    public static int EmptyNear { get; set; } = 16;

    /// <summary>Candidates passed over for lying where the asking bot had lately found nothing. See <see cref="EmptyRestMs"/>.</summary>
    public static long Revisits { get; private set; }

    /// <summary>Grounds each bot found empty lately, the last few of them.</summary>
    private static readonly Dictionary<Serial, List<(Point3D At, long Tick)>> _empty = [];

    /// <summary>Eight: a sweep files up to four squares found empty, and the two before it are still worth remembering.</summary>
    private const int EmptyKept = 8;

    /// <summary>A prowl reached its ground and found nothing there. See <see cref="EmptyRestMs"/>.</summary>
    public static void FoundEmpty(Mobile body, Point3D at)
    {
        if (body == null || EmptyRestMs <= 0)
        {
            return;
        }

        if (!_empty.TryGetValue(body.Serial, out var grounds))
        {
            grounds = [];
            _empty[body.Serial] = grounds;
        }

        if (grounds.Count >= EmptyKept)
        {
            grounds.RemoveAt(0);
        }

        grounds.Add((at, Core.TickCount));
    }

    /// <summary>Whether this bot found the ground about this place empty within <see cref="EmptyRestMs"/>.</summary>
    private static bool LatelyEmpty(Mobile body, Point3D where)
    {
        if (EmptyRestMs <= 0 || !_empty.TryGetValue(body.Serial, out var grounds))
        {
            return false;
        }

        var now = Core.TickCount;

        for (var i = 0; i < grounds.Count; i++)
        {
            if (now - grounds[i].Tick < EmptyRestMs && Utility.InRange(grounds[i].At, where, EmptyNear))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether a candidate is passed over when its road runs more than <see cref="Detour"/> tiles past the straight line
    /// from the asker. See <see cref="BotRoads"/>.
    ///
    /// <para>
    /// <b>On since build 64, after the numbers were read.</b> Build 63 drew the road map and only counted in
    /// <see cref="Roundabout"/>, and was then switched through the door on 15.09.2026 — on at 19:46, off at 20:09, on at
    /// 20:31. Its first two windows, off then on: prowls that stopped closing 32 against 8, darts failed in the ground
    /// north-west of the banks 7 against none, hunts finished 6.4 a minute against 10.1, walking about 27 ms a look against
    /// 10.5; DECISIONS holds all four windows. A dial, like every rule that narrows where the population may go.
    /// </para>
    /// </summary>
    public static bool ReadsRoads { get; set; } = true;

    /// <summary>
    /// How far past the straight line a road may run before its end is not worth a dart. Three hundred until the map's own
    /// numbers say otherwise: the pathfinder's road from home into the south-west, where walks do arrive, was 611 tiles for
    /// 453 in a straight line, and the ground north-west of the banks west of Britain is some 1,100 for 440.
    /// </summary>
    public static int Detour { get; set; } = 300;

    /// <summary>
    /// Candidates whose road runs more than <see cref="Detour"/> past the straight line from the asker — passed over when
    /// <see cref="ReadsRoads"/> is on, and only counted when it is off.
    /// </summary>
    public static long Roundabout { get; private set; }

    /// <summary>
    /// How far from home a square the map calls dangerous may be and still be worth setting out for.
    ///
    /// <para>
    /// <b>Its own number, because the roam is an argument about somewhere else.</b> The roam bounds ground the
    /// population may want things on, and the hunt halves it again for random darts, on the grounds that a
    /// walk into unseen terrain can end in being carried home. Neither argument is about a square the
    /// population has already stood in and bled in.
    /// </para>
    ///
    /// <para>
    /// Eight hundred, and the figure comes from the island itself. On 02.09.2026 the worst square the map knew
    /// was (2025, 975) at -0.60 on 28 blows and one death — the swamps between Britain and Minoc, which is
    /// where Patrick said the danger was — and home is (1440, 1470), five hundred and eighty-five tiles off.
    /// The roam was five hundred and the darts reached two hundred and fifty, so the one square everybody
    /// should have been walking to was outside both, and the beat recorded it plainly: 12 squares worth going
    /// to, 0 ever picked, and 1585 hunters in a single window left with nowhere to walk at all.
    /// </para>
    /// </summary>
    public static int FearedReach { get; set; } = 800;

    /// <summary>
    /// How far a hunting ground may be from home before no road to it is ever searched for.
    ///
    /// <para>
    /// Not a preference: it is <see cref="BotPath.CeilingMs"/> divided by <see cref="BotPath.MsPerTile"/>,
    /// which is the shard's own statement of how much distance one search is funded to cross. Reading it
    /// rather than writing a second number down is the point — a copy of it here would drift from the
    /// original the first time either moved, and the whole of this defect was two numbers that had never
    /// been compared.
    /// </para>
    ///
    /// <para>
    /// <b>It is a price, not a wall, and the first version of this note said the opposite.</b> The evidence
    /// for "no road exists past here" — 85% of refused roads lying beyond the line — was gathered while
    /// every bot on the shard was being refused every step by an exhausted mount, so those failures belonged
    /// to the engine and not to the search. Tested properly the same afternoon, on a healthy shard, by
    /// raising this to 900 with a dial and watching for fifteen minutes:
    /// </para>
    ///
    /// <para>
    /// refused roads stayed flat at one or two a minute — so distance does <em>not</em> make roads vanish —
    /// while the searching bill went from 2191 searches at 3.36ms each with 8% partial, to 6325 at 13.36ms
    /// with <b>51% partial</b>: eleven times the game loop for the same work. So the clamp stays, and it
    /// stays as what it is: the distance past which a search stops being worth what it costs. Errands that
    /// are rare enough to pay that bill — scouting is one Baron a few times an hour — should not use it, and
    /// BotScout says so at length.
    /// </para>
    /// </summary>
    /// <summary>Hunting grounds passed over for lying on ground no gate joins to the hunter. See Hunting.</summary>
    public static long Unjoined { get; private set; }

    /// <summary>
    /// <b>The hunt reads the zones (30.09.2026, build 375, Patrick's order of that evening: the zones' precision "drives the safe
    /// paths and the quality of the bots' hunting").</b> The darts land at random round home and the shard's four named
    /// grounds (noisy, paying, feared, yielding) say where blood was spilt or pay was had — none says where the quarry
    /// lives now or how many of it will come at once. <see cref="BotZones"/> does: each zone knows its living creatures, the
    /// fight at its worst place and a tile to stand on. The fifth named ground is the zone within the roam that holds
    /// quarry and whose fight the hunter can take (its <see cref="BotQuad.Strength"/> — a company's when it has one — at or
    /// above the zone's threat × <see cref="ZoneDare"/>), the worthiest first: the most quarry, the fight nearest the
    /// hunter's strength, the nearest. Passive-only grounds (deer, rabbits) count, at half their number.
    /// </summary>
    public static bool ReadsZones { get; set; } = true;

    /// <summary>A hostile zone is ground the hunter dares alone when its threat is at most this share of its strength.</summary>
    public static double ZoneDare { get; set; } = 1.0;

    /// <summary>Zones offered as the fifth named ground, grounds a deadly zone made lethal, grounds a zone's fight made too strong.</summary>
    public static long ZoneOffered { get; private set; }

    public static long ZoneDeadly { get; private set; }

    /// <summary>Hunts whose ground was the zone named, having beaten the other named grounds and the darts.</summary>
    public static long ZoneChosen { get; private set; }

    public static long ZoneTooStrong { get; private set; }

    /// <summary>Grounds whose road asked more than the hunter alone brought, kept for a company. See <see cref="BotDanger.Road"/>.</summary>
    public static long RoadTooStrong { get; private set; }

    /// <summary>
    /// The zone within the roam that holds quarry the hunter can take, as the place of a living creature of it the hunter could
    /// take; <paramref name="seek"/> is how far round that place the prowl looks for it on arrival. See <see cref="ReadsZones"/>.
    ///
    /// <para>
    /// <b>A creature's place, not the zone's standing tile (01.10.2026).</b> Build 375 handed the hunter <c>BotZone.Stand</c> — a
    /// spawner's own tile — while the zone's creatures walk up to their spawner's range from it, and the prowl on arrival looks
    /// only thirty tiles round: on 378 prowls ending "nothing here" rose to 61 of 158 (39 %, ~30 % before 375), and of those sent
    /// into hostile zones 20 of 33 (61 %), into clear ones 27 of 71 (38 %), against 14 of 50 with no zone (the coordinator's
    /// measurement of 23:06). The best <see cref="ZoneLooks"/> zones by the old score are looked into in turn; the first with a
    /// living creature the hunter could take within the zone's reach of its standing tile (<see cref="ZoneReach"/>) gives that
    /// creature's place, the nearest such to the hunter; a zone with none is passed over and counted.
    /// </para>
    /// </summary>
    private static Point3D Zoned(Mobile body, Map map, Point3D home, int roam, out int seek, out BaseCreature creature)
    {
        seek = 0;
        creature = null;

        if (!ReadsZones || BotZones.State is not { } state || state.Zones.Length == 0)
        {
            return Point3D.Zero;
        }

        var strength = BotQuad.Strength(body);

        // <b>A hostile zone too strong for one hunter is company ground, not no ground (01.10.2026, build next; the Sonnet analyst
        // of 386).</b> Of 188 hostile zones at 18:50 only 29 suited a lone bot (88 aggressive creatures), while the companies took
        // 97 gp a bot-hour against a lone hunt's 12. Such a zone was dropped here before the ground's own tests could ask for a
        // company; now it is weighed against what the bots standing by would bring together (BotThreat.OurPower, as the band's
        // call weighs its quarry), and the ground's tests in Hunting — the zone's fight outweighing the hunter alone, the
        // quadrant's muscle, the peril map — turn it into a company prowl or pass it over, as they do any ground.
        var together = ZoneCompanies ? Math.Max(strength, BotThreat.OurPower(body, BotMuster.Reach)) : strength;

        _looks.Clear();

        for (var i = 0; i < state.Zones.Length; i++)
        {
            var zone = state.Zones[i];

            if (zone.Water || zone.Live <= 0 || zone.Stand == Point3D.Zero)
            {
                continue;
            }

            if (!Utility.InRange(home, zone.Stand, roam))
            {
                continue;
            }

            double quarry;
            double worth;

            if (zone.Level >= BotZoneLevel.Hostile)
            {
                // <b>A deadly zone is company ground, not no ground (02.10.2026, build 389).</b> Deadly is ×6 the median bot at the
                // zone's worst place; a company of six or eight brings that. Patrick: "the zone is information both for caution and
                // for fighting and hunting — companies take zones too strong for one bot." The zone is weighed against what the
                // bots standing by bring together, as a hostile one is; the ground's own tests in Hunting (a deadly zone is lethal
                // ground, BotPeril, the road) then raise the company or pass it over. Deadly zones were skipped outright before.
                if (zone.Threat > together * ZoneDare)
                {
                    continue;
                }

                // <b>And its fight at what its dearest kind has cost us (02.10.2026, build next).</b> A zone of wisps and reapers
                // weighed at their strength alone sent the companies that died to them: 112 of 264 deaths on 390–392 were enlisted,
                // a median ten times weaker than their killer. See BotQuarry.Price.
                // A zone kept for a company (its fight past the hunter alone) at its kinds' company prices (03.10.2026): what companies
                // as strong have beaten in their own fights. See BotCompanyRecord.
                var forCompany = zone.Threat > strength * ZoneDare;

                if (zone.Threat * BotQuarry.ZonePrice(zone.Kinds, forCompany) > together * ZoneDare)
                {
                    BotQuarry.ZoneRefused();

                    continue;
                }

                if (forCompany && zone.Threat * BotQuarry.ZonePrice(zone.Kinds) > together * ZoneDare)
                {
                    BotCompanyRecord.Zoned();
                }

                if (zone.Threat > strength * ZoneDare)
                {
                    ZoneForCompany++;
                }

                quarry = Math.Min(zone.Aggressive, 20);
                worth = strength > 0.0 ? 0.5 + zone.Threat / strength : 1.0;
            }
            else
            {
                quarry = Math.Min(zone.Live, 20) * 0.5;
                worth = 0.5;
            }

            var away = Math.Max(Math.Abs(zone.Stand.X - body.X), Math.Abs(zone.Stand.Y - body.Y));
            // A poor hunter's zone is weighed by what its kinds pay (02.10.2026, build 389; Patrick's option 2). See BotQuarry.ZonePays.
            var score = quarry * worth * BotQuarry.ZonePays(zone.Kinds, body) / (1.0 + away / 150.0);

            if (score <= 0.0)
            {
                continue;
            }

            // The best few, best first: a short insertion into a list of ZoneLooks.
            var at = _looks.Count;

            while (at > 0 && _looks[at - 1].Score < score)
            {
                at--;
            }

            if (at < ZoneLooks)
            {
                _looks.Insert(at, (zone, score));

                if (_looks.Count > ZoneLooks)
                {
                    _looks.RemoveAt(_looks.Count - 1);
                }
            }
        }

        for (var i = 0; i < _looks.Count; i++)
        {
            var zone = _looks[i].Zone;
            var reach = Math.Clamp(Math.Max(zone.DeclaredRange, zone.ObservedRange), ZoneReachLeast, ZoneReach);

            // A creature the company could take, in a zone kept for one; the hunter's own measure elsewhere.
            var company = zone.Level >= BotZoneLevel.Hostile && zone.Threat > strength * ZoneDare;
            var live = Living(body, map, zone, reach, company ? together : strength, company);

            if (live != null)
            {
                ZoneOffered++;
                ZoneLive++;
                seek = reach;
                creature = live;
                _looks.Clear();

                return live.Location;
            }

            ZoneUnpeopled++;
        }

        _looks.Clear();

        return Point3D.Zero;
    }

    /// <summary>
    /// The living creature of this zone nearest the hunter that it could take: within <paramref name="reach"/> of the zone's
    /// standing tile, one it may strike and not an innocent, nobody else's, not given up on, and no stronger than
    /// <paramref name="strength"/> × <c>BotQuarry.Daring</c>; in a hostile zone one that fights, in a clear one any. Asked of the
    /// creature rather than of <c>BotThreat.Hostile</c>, whose town test is on the hunter — and a hunter choosing its ground is
    /// very often standing in a town.
    /// </summary>
    private static BaseCreature Living(Mobile body, Map map, BotZone zone, int reach, double strength, bool company = false)
    {
        BaseCreature best = null;
        var bestAway = int.MaxValue;
        var fights = zone.Level >= BotZoneLevel.Hostile;

        foreach (var creature in map.GetMobilesInRange<BaseCreature>(zone.Stand, reach))
        {
            if (creature is not { Deleted: false, Alive: true } || creature.Controlled || creature.Summoned || creature.IsDeadBondedPet
                || creature.Blessed || creature is BaseVendor || creature.CanSwim && !BotStep.Settle(map, creature.X, creature.Y, out _))
            {
                continue;
            }

            if (fights && creature.FightMode is FightMode.None or FightMode.Aggressor)
            {
                continue;
            }

            var away = Math.Max(Math.Abs(creature.X - body.X), Math.Abs(creature.Y - body.Y));

            if (away >= bestAway || BotThreat.Power(creature) > strength * BotQuarry.Daring)
            {
                continue;
            }

            if (Notoriety.Compute(body, creature) == Notoriety.Innocent || !BotQuarry.Ours(body, creature) || BotQuarry.Shunned(creature))
            {
                continue;
            }

            // At what its kind has cost us (02.10.2026, build next). See BotQuarry.Price. Asked last, so the count is of creatures
            // the price alone turned away (review of build 394). For a company's zone, at the kind's company price (03.10.2026).
            if ((company ? BotCompanyRecord.Priced(creature) : BotQuarry.Priced(creature)) > strength * BotQuarry.Daring)
            {
                BotQuarry.ZoneRefused();

                continue;
            }

            best = creature;
            bestAway = away;
        }

        return best;
    }

    /// <summary>
    /// What a bot holds, in gold, below which it counts as poor for the hunt's instrument: the analyst's line of 01.10.2026
    /// (59.5 % of the bots under it on build 384). Only counted; nothing is offered or refused on it.
    /// </summary>
    public static int PoorBelow { get; set; } = 100;

    /// <summary>Whether the zone's living creature outranks every ground but the board's. A switch. See the note in Hunting.</summary>
    public static bool ZoneFirst { get; set; } = true;

    /// <summary>
    /// Whether a quarry of the grazing kind at hand is passed over for a hostile zone's living fighter within the walk. A switch
    /// (01.10.2026, build next). See the note in Propose.
    /// </summary>
    public static bool GrazersLast { get; set; } = true;

    /// <summary>Quarry of the grazing kind passed over for a zone's fighter the hunter went after instead. See <see cref="GrazersLast"/>.</summary>
    public static long GrazerPassed { get; private set; }

    /// <summary>Whether a hostile zone too strong for one hunter is weighed for the bots standing by together. A switch. See <see cref="Zoned"/>.</summary>
    public static bool ZoneCompanies { get; set; } = true;

    /// <summary>Hostile zones kept as ground for a company, being too strong for the hunter alone. See <see cref="ZoneCompanies"/>.</summary>
    public static long ZoneForCompany { get; private set; }

    /// <summary>Of the same, passed over for a company raised against a zone's fighter whose ground asked one. See <see cref="GrazersLast"/>.</summary>
    public static long GrazerCompanied { get; private set; }

    /// <summary>Times a zone's living creature was let past a dart's filter (too quiet, lately empty, untrodden). See Hunting.</summary>
    public static long ZoneSpared { get; private set; }

    /// <summary>How many of the best zones are looked into for a living creature before the zones give nothing. Three.</summary>
    public static int ZoneLooks { get; set; } = 3;

    /// <summary>
    /// The most and the least a zone's reach is taken as, round its standing tile, for the creature and for the prowl's look on
    /// arrival: the spawners' declared or observed walking range, clamped. Forty at most keeps it to one spatial query of a few
    /// sectors per zone looked into.
    /// </summary>
    public static int ZoneReach { get; set; } = 40;

    public static int ZoneReachLeast { get; set; } = 8;

    /// <summary>Zones that gave a living creature's place; zones among the best that had none the hunter could take.</summary>
    public static long ZoneLive { get; private set; }

    public static long ZoneUnpeopled { get; private set; }

    /// <summary>The zones being looked into, reused: one game thread.</summary>
    private static readonly List<(BotZone Zone, double Score)> _looks = [];

    /// <summary>Hunting grounds weighed on the far side of a moongate for a bot living on another land. See Hunting.</summary>
    public static long OverWater { get; private set; }

    /// <summary>Of those, grounds passed over for asking more than the bot alone brings. See Hunting.</summary>
    public static long AbroadTooHard { get; private set; }

    public static int Walkable
    {
        get => _walkable > 0 ? _walkable : Default;
        set => _walkable = value;
    }

    /// <summary>
    /// How far a dart may be thrown from the asker, when nothing has been dialled.
    ///
    /// <para>
    /// <b>Five hundred, and the derived 240 turned out to be a cage.</b> The derivation — CeilingMs over
    /// MsPerTile — is where the search budget stops growing, and using it here penned the whole population
    /// into a circle of that radius around wherever it was standing, which is to say around home. Measured
    /// 08.09.2026: 16151 candidates thrown away in twenty-five minutes as too far, and every target the
    /// population chose that hour fell inside x 1200-1680, y 1230-1710 — the circle, drawn on the map by the
    /// bots themselves. Patrick asked why they never go south to the marsh; that is the answer.
    /// </para>
    ///
    /// <para>
    /// Five hundred is <c>Roam / 2</c>, which is what the dart sampler was written to throw at in the first
    /// place, so this stops being a second opinion about distance and goes back to being one. It is not free
    /// — at 900 the searching bill went from 3.36ms and 8% partial to 13.4ms and 51% — so it stays a dial,
    /// and the two numbers to watch when moving it are the average search cost and the partial share in
    /// "Getting about".
    /// </para>
    /// </summary>
    public static int Default { get; set; } = 500;

    private static int _walkable;

    private static void Missing(Map map)
    {
        if (_saidNoQuarry)
        {
            return;
        }

        _saidNoQuarry = true;

        // Once, by name. A population with nothing to fight and no gold coming in looks exactly like a
        // population that does not feel like fighting, and the difference is the whole economy.
        logger.Error(
            "Nothing within {Reach} tiles of the bots on {Map} is worth fighting, so no gold will enter the world",
            BotQuarry.Reach,
            map
        );
    }

    /// <summary>Lets the complaint be made again after a world reload.</summary>
    /// <summary>Answers that went to a class which only ever fights what reaches it first.</summary>
    public static long Sworn { get; private set; }

    /// <summary>Candidates thrown away for being ground the population has found nothing in.</summary>
    public static long Quiet { get; private set; }

    /// <summary>Times a hunter was left with nowhere to walk because every sampled ground was too quiet.</summary>
    public static long Stranded { get; private set; }

    /// <summary>Grounds passed over because whoever asked was not strong enough for them.</summary>
    public static long Overmatched { get; private set; }

    /// <summary>Grounds one bot would have dared alone, kept to companies because bots had been dying in or beside them lately.</summary>
    public static long Deadly { get; private set; }

    /// <summary>Grounds that had hurt somebody, passed over because the asker was a novice.</summary>
    public static long Green { get; private set; }

    /// <summary>Grounds passed over because another bot is already raising a company for them.</summary>
    public static long Claimed { get; private set; }

    /// <summary>Times a hunting ground was picked because it is ground that has hurt somebody.</summary>
    public static long Sought { get; private set; }

    /// <summary>Times a hunting ground was picked because the creature the guild's board asks for walks there.</summary>
    public static long Boarded { get; private set; }

    public static string Describe() =>
        $"{Sworn} answers went to classes that only defend; {Quiet} hunting grounds passed over as too quiet (above {BotQuad.TooQuiet:F2}), {Sought} picked for having hurt somebody (at or below {BotQuad.Wanted:F2}), {Boarded} picked for a creature the guild's board asks for, {Stranded} hunters left with nowhere to walk at all because every ground they looked at was too quiet, {Overmatched} grounds passed over for asking more strength than whoever looked had, {Deadly} grounds where bots had lately died kept to companies, {Green} grounds that had hurt somebody passed over for a novice (main skill under {BotLadder.NoviceSkill:F0}), {Claimed} for somebody already raising a company for them, {Overrun} quarry passed over for the crowd already round it, {Rested} named grounds passed over as resting after refusing somebody and {Darted} sampled ones, {Revisits} for lying where the asker had lately found nothing, {Distant} further from the asker than {Walkable} tiles, which is as far as a road is ever searched for, {Unjoined} on ground no gate joins to the hunter, {OverWater} weighed over the water beyond a moongate for a hunter living on another land ({AbroadTooHard} of them passed over for asking more than it alone brings), {Roundabout} whose road from home runs more than {Detour} tiles past the straight line from the asker ({(ReadsRoads ? "passed over" : "only counted")}),{Untrodden} on squares nobody has ever stood in or beside, {BotProwl.Baulked} prowls that stopped getting nearer ({BotProwl.RoadKept} beats kept going along a road that was not closing as the crow flies), {BotProwl.Redarted} of them sent on to ground they could walk to on their own side and {BotProwl.Unredarted} given up for finding none ({BotBarrier.Stops} of their stopping places remembered and {BotBarrier.Behind} grounds passed over for lying beyond them), {BotProwl.Turned} refused a road on the way and sent on to ground on their own side and {BotProwl.Unturned} given up for finding none,{BotProwl.Raised} companies raised for ground one bot could not take, {BotProwl.Regathered} that came up short and went to ground they could take instead, {BotProwl.Unraised} given up for raising none they could use anywhere ({BotProwl.ShortRested} of their grounds rested {BotProwl.ShortRestMs / 60000} minutes for it); {BotSlay.Undaring} of {BotSlay.Begun} hunts begun at a quarry on ground asking more strength than the hunter brought (counted, not refused); the zones: {ZoneOffered} zones named as ground the hunter could take ({ZoneLive} at a living creature's place, {ZoneUnpeopled} of the best {ZoneLooks} passed over for holding none the hunter could take within their reach) and {ZoneChosen} of them chosen, {ZoneDeadly} grounds made lethal for lying in a deadly zone and {ZoneTooStrong} too strong for a zone's fight outweighing the hunter ({ZoneForCompany} hostile zones kept as ground for a company, too strong for the hunter alone{(ZoneCompanies ? "" : ", off")}); {RoadTooStrong} grounds kept for a company because the road there asked more than the hunter alone brought; {BotProwl.ZoneFollowed} zone prowls walked on after quarry in the zone's patrol and {BotProwl.ZoneEmpty} found it empty after all; {ZoneSpared} times a zone's living creature was let past a dart's filter ({(ZoneFirst ? "and ranked next after the board's" : "ranked as any dart")}); {GrazerPassed} quarry of the grazing kind at hand passed over for a zone's fighter within the walk and {GrazerCompanied} for a company raised against one whose ground asked it ({(GrazersLast ? "grazers last" : "off")});{BotProwl.Widened} prowls walked on after quarry seen within {BotProwl.LookWider} tiles of their ground on arrival; {BotProwl.CameApart} prowls for company ground given up when the company came apart on the way; {BotProwl.ClosedOn} prowls given up when their ground was closed while they walked there; {BotProwl.CompanyTookOn} fights a prowl's raised company took on for what its leader found (up to {BotProwl.CompanyFights} a prowl); the hunt itself: {BotSlay.Slipped} quarry let go for running {BotSlay.Leash} tiles from where it was found, {BotSlay.Dragged} for drawing the hunter {BotSlay.Leash * BotSlay.FarLeash} past its walk out, {BotSlay.Unleashed} hunts kept past the old leash for fighting the bot or having been fought, {BotSlay.Rebased} leashes measured again after a jump of more than {BotSlay.JumpTiles} tiles, {BotSlay.StaleResumes} hunts not taken up again for a quarry far away by then; {BotQuarry.InTown} quarry passed over for standing in a town, {BotQuarry.OnGuarded} hostile quarry for standing on guarded ground (and {BotHaven.QuarryTaken} taken by the guards all the same); {BotSlay.PoorBegun} of {BotSlay.Begun} hunts begun and {BotProwl.PoorTaken} prowls taken by bots holding under {PoorBelow}gp free of what their wants on the board hold; {BotSlay.Thrifted} hunts left when the casts had spent what the quarry pays (a caster casts at least {BotSlay.CastFloor} times{(BotSlay.CastsByGold ? "" : ", off")}); {BotProwl.DescribeSweeps()}";

    public static void Forget()
    {
        _saidNoQuarry = false;
        BotBarrier.Forget();
        Sworn = 0;
        Rested = 0;
        Darted = 0;
        Distant = 0;
        Untrodden = 0;
        Quiet = 0;
        Stranded = 0;
        Overmatched = 0;
        Deadly = 0;
        Green = 0;
        Claimed = 0;
        Overrun = 0;
        Sought = 0;
        Boarded = 0;
        Revisits = 0;
        GrazerPassed = 0;
        GrazerCompanied = 0;
        ZoneForCompany = 0;
        RoadTooStrong = 0;
        _empty.Clear();
        BotSweepRecord.Forget();
    }
}
