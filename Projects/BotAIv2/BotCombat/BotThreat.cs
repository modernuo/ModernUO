using System;
using Server.Items;
using Server.Mobiles;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// One of this population, counted when adding up our side of a fight. Implemented by the bot.
///
/// A marker rather than a list, and deliberately so: the first version summed allies by walking the whole
/// registry — a hundred and fifty entries, per bot, per assessment. Asking the map what is nearby costs what
/// is nearby.
/// </summary>
public interface IBotAlly
{
    /// <summary>Whether this one could actually take part. A corpse and a bot at two hit points are not help.</summary>
    bool AbleToFight { get; }
}

/// <summary>What to do about being attacked on the way somewhere.</summary>
public enum BotStand
{
    /// <summary>Nothing hostile worth the name. Carry on.</summary>
    Nothing,

    /// <summary>
    /// Winnable, alone or with the company standing here. Put the road aside, deal with this, then carry on.
    /// </summary>
    Fight,

    /// <summary>
    /// Several times over. Keep walking the road and hit back on the move — the errand never changed.
    /// </summary>
    Outmatched
}

/// <summary>
/// How dangerous a thing is, how dangerous the situation is, and therefore whether a bot walking somewhere
/// should stop and deal with what just hit it.
///
/// <para>
/// <b>Power is endurance times output.</b> Judging by health alone rates an ogre and a lich as near-equals —
/// 108 against 111 — when one of them hits two and a half times harder and casts. The measured reference
/// values, from the first version: a bot 1116, an ogre 1080, a lich 6937.
/// </para>
///
/// <para>
/// <b>Maximum health, never current, and this is not a detail.</b> The question is "can we win this fight",
/// which does not become a different question because somebody has taken a few hits. Whether one bot should
/// personally back out is a separate question with its own answer. Mixing the two made groups talk
/// themselves out of fights they were winning, the moment they started winning them: the party took damage,
/// its "power" collapsed, and it disbanded mid-battle.
/// </para>
/// </summary>
public static class BotThreat
{
    /// <summary>
    /// How much stronger than us the opposition may be before walking on beats standing and fighting.
    ///
    /// One and a half, which is the first version's measured figure and not a guess. It is the meaning of
    /// "several times over": at or under it a bot commits, above it the road wins. Note what falls either
    /// side — a lone bot against a graveyard spectre is 2120 against 1116, so it walks on unless it has
    /// company; six bots against a lich is 6696 against 6937, ratio 1.04, so they commit.
    /// </summary>
    public static double Tolerance { get; set; } = 1.5;

    /// <summary>
    /// Weight given to hostiles other than the worst one.
    ///
    /// Adding every creature in sight at full strength badly overstates a fight: blows are traded with one
    /// thing at a time, and the rest arrive piecemeal, get held off, or never engage at all. Counting them
    /// fully meant a graveyard's ordinary residents — the skeletons and zombies that live there anyway —
    /// stacked on top of whatever had wandered in, and a band that could comfortably have taken the newcomer
    /// refused because it was also adding up the scenery.
    /// </summary>
    private const double SecondaryWeight = 0.4;

    /// <summary>The same weight, for a reckoning of a fight made away from this class. See <c>BotLairs.KeptFight</c>.</summary>
    public static double Secondary => SecondaryWeight;

    /// <summary>Rough fighting power: how long something lasts multiplied by how hard it hits.</summary>
    public static double Power(Mobile m)
    {
        if (m == null || m.Deleted)
        {
            return 0.0;
        }

        return Math.Max(1, m.HitsMax) * AverageDamage(m);
    }

    /// <summary>
    /// What this one can bring to a fight <em>now</em>: its strength taken down by the share of its health it still has.
    ///
    /// <para>
    /// <b><see cref="Power"/> is a threat, and a threat is measured at full health on purpose</b> — it answers "what is
    /// that thing", which is the right question about a stranger across a field and the wrong one about whether to set
    /// on it this minute. Faron Ashdown was pressed against Orin Ashdown the murderer at 23:38:46 on 17.09.2026 with
    /// "3409 of strength against its 2030", one and two thirds over the margin the law-abiding want, and died to it
    /// seventy-seven seconds later: both numbers were reckoned from hit points neither bot had. A floor of a tenth
    /// keeps a nearly-dead bot from reading as nothing at all, since it can still swing.
    /// </para>
    /// </summary>
    public static double Now(Mobile m)
    {
        if (m == null || m.Deleted || m.HitsMax <= 0)
        {
            return Power(m);
        }

        return Power(m) * Math.Clamp(m.Hits / (double)m.HitsMax, 0.1, 1.0);
    }

    /// <summary>Whether a wielder's weapon damage is taken at its skill's share. See the note in <see cref="AverageDamage"/>.</summary>
    public static bool SkillWeighs { get; set; } = true;

    /// <summary>The least share of its weapon's damage a wielder is read at.</summary>
    public static double SkillFloor { get; set; } = 0.25;

    private static double AverageDamage(Mobile m)
    {
        var melee = 1.0;

        if (m is BaseCreature creature && creature.DamageMax > 0)
        {
            melee = (creature.DamageMin + creature.DamageMax) / 2.0;

            // <b>And a creature's the same way (build 333).</b> Weighed for the wielders alone (330), the bots read weaker and
            // the creatures did not, and the one comparison both sides are measured in tilted: the delve's offers went from
            // 13 on 329 to none at all on 330 and 331 (the night analyst, `night-r3-companies.md`) — every band weaker than
            // the Orc Caves on paper. A creature lands its blows by its best fighting skill as a bot does by its weapon's.
            if (SkillWeighs)
            {
                var skill = Math.Max(
                    Math.Max(creature.Skills[SkillName.Wrestling].Value, creature.Skills[SkillName.Archery].Value),
                    Math.Max(
                        Math.Max(creature.Skills[SkillName.Swords].Value, creature.Skills[SkillName.Macing].Value),
                        creature.Skills[SkillName.Fencing].Value
                    )
                );

                melee *= Math.Clamp((skill + 20.0) / 120.0, SkillFloor, 1.0);
            }
        }
        else if (m.Weapon is BaseWeapon weapon)
        {
            melee = (weapon.MinDamage + weapon.MaxDamage) / 2.0;

            // <b>A weapon is worth what its wielder can land (29.09.2026, build 330).</b> The damage was the weapon's alone, so
            // a tamer with a bow and twenty points of Archery read as strong as a ranger: Sigurd set out alone after grizzly
            // bears (1554) at about 1500 "of strength", three times in half an hour on build 324, and died each time ("a
            // grizzly bear would not go down: 81% of it left and not a scratch in 45s"); twenty bots made seven deaths in ten
            // on 323–329. The era's hit chance grows with the attacker's skill ((skill + 20) against the defender's), so the
            // weapon's damage is taken at (skill + 20) / 120 of itself, never under SkillFloor: a master is read as before,
            // a novice as a third of it. Creatures keep their own figures; this is for bodies that swing a weapon.
            if (SkillWeighs)
            {
                var skill = m.Skills[weapon.Skill].Value;

                melee *= Math.Clamp((skill + 20.0) / 120.0, SkillFloor, 1.0);
            }
        }

        // A caster's real output is in its spells, and spells appear nowhere in the melee figures. Half its
        // magery is a crude stand-in, and it is the difference between correctly refusing a lich and walking
        // into one because its health bar looked ordinary.
        var magery = m.Skills[SkillName.Magery].Base;

        return Math.Max(1.0, melee + magery / 2.0);
    }

    /// <summary>
    /// Whether this creature is something a bot standing here would fight.
    ///
    /// The town test is on the <b>hunter</b>, not the quarry, because what matters is where the swing would
    /// happen — and it is a town rather than merely guarded ground. The wider test caught the graveyard too,
    /// which sits inside Britain's guarded region, and the population stopped fighting anywhere at all: a far
    /// worse fault than the one it was fixing.
    /// </summary>
    public static bool Hostile(Mobile bot, BaseCreature creature)
    {
        if (bot?.Region?.IsPartOf<TownRegion>() == true)
        {
            return false;
        }

        if (creature == null || creature.Deleted || !creature.Alive)
        {
            return false;
        }

        if (creature.Controlled || creature.Summoned || creature.IsDeadBondedPet)
        {
            return false;
        }

        if (creature is BaseVendor)
        {
            return false;
        }

        if (Notoriety.Compute(bot, creature) == Notoriety.Innocent)
        {
            return false;
        }

        return bot.CanBeHarmful(creature, false);
    }

    /// <summary>
    /// <b>Hostile and minded to fight (29.09.2026, build 334).</b> <see cref="Hostile"/> asks whether a bot may strike a
    /// creature, and every deer, sheep, cow and rabbit answers yes; the threat round a hunter summed them all, and 12 of the
    /// 23 hunts that failed in the first thirteen minutes of 333 ended "too many of them around a timber wolf / a brown bear"
    /// in woods full of grazing animals. A creature that fights only when struck (<c>FightMode.None</c> or <c>Aggressor</c>)
    /// counts as a threat only once it is in a fight; the one being hunted is, as soon as the first blow lands.
    /// </summary>
    public static bool Menacing(Mobile bot, BaseCreature creature)
    {
        if (!Hostile(bot, creature))
        {
            return false;
        }

        return creature.FightMode is not (FightMode.None or FightMode.Aggressor) || creature.Combatant != null;
    }

    /// <summary>
    /// Combined power of everything hostile within range: the worst of it in full, the rest discounted.
    ///
    /// This counts creatures a bot cannot reach, unlike choosing a target. "I cannot land a blow on it" and
    /// "it cannot hurt me" are different statements — the wraith on the crypt roof that prompted the whole
    /// distinction casts down perfectly well — and a bot that judged the place safe on those grounds would
    /// stand in it and die.
    /// </summary>
    public static double ThreatPower(Mobile bot, int range)
    {
        var map = bot?.Map;

        if (map == null || map == Map.Internal)
        {
            return 0.0;
        }

        var total = 0.0;
        var worst = 0.0;

        foreach (var creature in map.GetMobilesInRange<BaseCreature>(bot.Location, range))
        {
            if (!Menacing(bot, creature))
            {
                continue;
            }

            var power = Power(creature);

            total += power;

            if (power > worst)
            {
                worst = power;
            }
        }

        if (worst <= 0.0)
        {
            return 0.0;
        }

        // The worst at full weight, everything else discounted.
        return worst + (total - worst) * SecondaryWeight;
    }

    /// <summary>
    /// The strongest hostile within range, which is <b>not</b> the nearest.
    ///
    /// The distinction cost the first version an evening. Its party formed against whatever was closest, and
    /// on a graveyard the closest thing is a skeleton — so six bots would declare a band against a skeleton,
    /// commit instantly because a skeleton is trivial, all go and kill it, while the lich that was actually
    /// killing them carried on casting. If a bot is going to put its road aside for a fight, it has to be the
    /// fight that matters.
    /// </summary>
    public static BaseCreature Strongest(Mobile bot, int range)
    {
        var map = bot?.Map;

        if (map == null || map == Map.Internal)
        {
            return null;
        }

        BaseCreature worst = null;
        var worstPower = 0.0;

        foreach (var creature in map.GetMobilesInRange<BaseCreature>(bot.Location, range))
        {
            if (!Menacing(bot, creature))
            {
                continue;
            }

            var power = Power(creature);

            if (power <= worstPower)
            {
                continue;
            }

            worst = creature;
            worstPower = power;
        }

        return worst;
    }

    /// <summary>
    /// Whatever hostile creature is presently set on this bot, or null.
    ///
    /// <para>
    /// <b>Asked of the creature rather than of the bot, and the direction is the whole point.</b>
    /// <c>bot.Combatant</c> says who the bot has decided to fight; this says who has decided to fight the
    /// bot, and those are different facts about different moments. A healer standing over a patient has no
    /// combatant of its own while a zombie eats it, and every rule about what a hurt bot may do — bandage,
    /// cast, stand still at all — turns on the second question, not the first.
    /// </para>
    /// </summary>
    public static BaseCreature Hunter(Mobile bot, int range)
    {
        var map = bot?.Map;

        if (map == null || map == Map.Internal)
        {
            return null;
        }

        BaseCreature worst = null;
        var worstPower = 0.0;

        foreach (var creature in map.GetMobilesInRange<BaseCreature>(bot.Location, range))
        {
            if (!Hostile(bot, creature) || creature.Combatant != bot)
            {
                continue;
            }

            var power = Power(creature);

            if (power <= worstPower)
            {
                continue;
            }

            worst = creature;
            worstPower = power;
        }

        return worst;
    }

    /// <summary>Whether anything hostile is standing near enough to interfere with standing still.</summary>
    public static bool Anything(Mobile bot, int range)
    {
        var map = bot?.Map;

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        foreach (var creature in map.GetMobilesInRange<BaseCreature>(bot.Location, range))
        {
            if (Hostile(bot, creature))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Our side: this bot plus every able one of its own standing near enough to join in.
    ///
    /// Counted from what is nearby rather than from a roster, and only those that could actually swing. A bot
    /// on its last two hit points is not reinforcement — the first version formed bands whose founder could
    /// not fight, found nobody able, and disbanded in the same tick, over and over.
    /// </summary>
    public static double OurPower(Mobile bot, int range)
    {
        var map = bot?.Map;

        if (map == null || map == Map.Internal)
        {
            return 0.0;
        }

        var total = Power(bot);

        foreach (var mobile in map.GetMobilesInRange<Mobile>(bot.Location, range))
        {
            if (mobile == bot || mobile is not IBotAlly { AbleToFight: true })
            {
                continue;
            }

            total += Power(mobile);
        }

        return total;
    }

    /// <summary>
    /// Our side against one thing: this bot plus every able one of its own near enough — <b>less those held by a foe of their own</b>
    /// (13.10.2026, w5 review A). The reckoning above counts a company member fighting its own enemy beside a crier as on the crier's
    /// side, so the crier's need read as met by bots that would never turn, and a company asked to spare them found nothing to add
    /// (<c>BotSquad.Spare</c>: "too little to spare"). For the summons' need only; a bot's own odds (<see cref="Decide"/>) keep the
    /// reckoning above, which is what the whole population's fight-or-flee has been measured on.
    /// </summary>
    public static double OurPower(Mobile bot, int range, Mobile foe)
    {
        var map = bot?.Map;

        if (map == null || map == Map.Internal)
        {
            return 0.0;
        }

        var total = Power(bot);

        foreach (var mobile in map.GetMobilesInRange<Mobile>(bot.Location, range))
        {
            if (mobile == bot || mobile is not IBotAlly { AbleToFight: true } || Held(mobile, foe))
            {
                continue;
            }

            total += Power(mobile);
        }

        return total;
    }

    /// <summary>Whether an ally is held by a living foe of its own that is not <paramref name="foe"/>: it is no reinforcement against this one.</summary>
    public static bool Held(Mobile ally, Mobile foe) =>
        ally?.Combatant is { Deleted: false, Alive: true } own && own != foe && own != ally;

    /// <summary>
    /// The same reckoning of the opposition, taken where the fight would be rather than where the bot is.
    ///
    /// <para>
    /// <b>Because the odds were being learnt by walking into them.</b> A quarry is weighed alone — its own
    /// power against ours — and the crowd standing round it was found only on arrival, by
    /// <see cref="Decide"/>, which then called the fight off. Over the night of 02-03.09.2026 that came to
    /// 202 endings on "too many of them around", and 184 of them fired inside the shortest span the ledger
    /// can record: the crowd was not gathering while the bot walked, it was standing there when the bot
    /// chose. What is paid for that is a whole walk, every time, by every bot in turn.
    /// </para>
    ///
    /// <para>
    /// Ours is still counted around the bot, and that is a forecast rather than a promise: the allies near it
    /// now are the ones likely to be near it there. The yardstick is deliberately the same <see cref="Tolerance"/>
    /// the arrival test uses — a pre-check that disagreed with the test it is meant to spare would refuse
    /// fights the bot would have won, or send it to ones it would drop on arrival, which is the loop again
    /// with an extra step.
    /// </para>
    /// </summary>
    public static bool Overrun(Mobile bot, IPoint3D where, int range)
    {
        var map = bot?.Map;

        if (map == null || map == Map.Internal || where == null)
        {
            return false;
        }

        var total = 0.0;
        var worst = 0.0;

        foreach (var creature in map.GetMobilesInRange<BaseCreature>(new Point3D(where), range))
        {
            if (!Menacing(bot, creature))
            {
                continue;
            }

            var power = Power(creature);

            total += power;

            if (power > worst)
            {
                worst = power;
            }
        }

        if (worst <= 0.0)
        {
            return false;
        }

        var threat = worst + (total - worst) * SecondaryWeight;
        var ours = OurPower(bot, range);

        return ours > 0.0 && threat / ours > Tolerance;
    }

    /// <summary>
    /// What stands round a place, reckoned as <see cref="ThreatPower"/> reckons what stands round a bot: the worst of it in
    /// full, the rest discounted. For whoever must add up a side before anybody walks there. See <c>BotCry.Summon</c>.
    /// </summary>
    public static double ThreatAt(Mobile bot, IPoint3D where, int range)
    {
        var map = bot?.Map;

        if (map == null || map == Map.Internal || where == null)
        {
            return 0.0;
        }

        var total = 0.0;
        var worst = 0.0;

        foreach (var creature in map.GetMobilesInRange<BaseCreature>(new Point3D(where), range))
        {
            if (!Menacing(bot, creature))
            {
                continue;
            }

            var power = Power(creature);

            total += power;

            if (power > worst)
            {
                worst = power;
            }
        }

        return worst <= 0.0 ? 0.0 : worst + (total - worst) * SecondaryWeight;
    }

    /// <summary>
    /// The same odds asked for a fight somebody else is already in: the opposition round <paramref name="where"/>
    /// against <paramref name="beside"/>'s side there plus this bot, which is what will be standing together
    /// once the bot arrives. For a rescue, where <see cref="Overrun"/>'s reckoning of "ours" — the allies
    /// round the rescuer now — is the wrong crowd: the crowd that matters is the one round the friend.
    /// </summary>
    public static bool OverrunThere(Mobile bot, Mobile beside, IPoint3D where, int range)
    {
        var map = bot?.Map;

        if (map == null || map == Map.Internal || where == null || beside == null)
        {
            return false;
        }

        var total = 0.0;
        var worst = 0.0;

        foreach (var creature in map.GetMobilesInRange<BaseCreature>(new Point3D(where), range))
        {
            if (!Menacing(bot, creature))
            {
                continue;
            }

            var power = Power(creature);

            total += power;

            if (power > worst)
            {
                worst = power;
            }
        }

        if (worst <= 0.0)
        {
            return false;
        }

        var threat = worst + (total - worst) * SecondaryWeight;
        var ours = Power(bot) + OurPower(beside, range);

        return ours > 0.0 && threat / ours > Tolerance;
    }

    /// <summary>
    /// How much stronger the opposition is than us. Above <see cref="Tolerance"/> the road wins.
    /// </summary>
    public static double Danger(Mobile bot, int range)
    {
        var ours = OurPower(bot, range);

        return ours <= 0.0 ? 0.0 : ThreatPower(bot, range) / ours;
    }

    /// <summary>
    /// The whole decision, in one call: stand and deal with it, or walk on and hit back on the move.
    ///
    /// <para>
    /// <b>Binary and immediate, with no assembling and no hesitating.</b> The first version had a soft
    /// utility here and it produced the worst behaviour on the shard: six bots stood politely in a circle
    /// waiting for a rally that was already complete while a lich killed them one at a time. A caster strikes
    /// from eight tiles and never closes, so not one rung of the survival ladder ever fired. Standing still is
    /// not an option at any number.
    /// </para>
    /// </summary>
    public static BotStand Decide(Mobile bot, int range)
    {
        if (bot == null || !bot.Alive)
        {
            return BotStand.Nothing;
        }

        var threat = ThreatPower(bot, range);

        if (threat <= 0.0)
        {
            return BotStand.Nothing;
        }

        var ours = OurPower(bot, range);

        return ours > 0.0 && threat / ours <= Tolerance ? BotStand.Fight : BotStand.Outmatched;
    }
}
