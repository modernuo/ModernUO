using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Spells;

namespace Server.BotAI.V2;

/// <summary>
/// Mending: what a bot can heal with, who needs it, and the two ways of doing it.
///
/// <para>
/// <b>Spell before bandage, for anybody who can cast at all.</b> Not a preference — an ordering with three
/// reasons behind it. A spell lands in a couple of seconds where a bandage on yourself takes nine or ten; mana
/// comes back on its own where bandages cost money at a counter; and the herbs a heal spends are the ones a
/// caster is already walking to town for. So the cloth is what a caster falls back to when the pool is empty,
/// and what everybody else has instead.
/// </para>
///
/// <para>
/// <b>Every judgement about whether healing is possible belongs to the engine.</b> A bandage refuses an
/// undamaged patient by itself — "That being is not damaged!" — which is the anti-exploit this project would
/// otherwise have had to invent, because "bandage a healthy friend for ever" is the training dummy in a
/// different coat. A spell consumes its own reagents and mana in its own sequence. Nothing here simulates a
/// heal.
/// </para>
/// </summary>
public static class BotMend
{
    /// <summary>
    /// How long a patient nobody could walk to is left out of the picking.
    ///
    /// <para>
    /// Ten seconds, and it is not a verdict about the patient: it is a note that the road there was refused
    /// a moment ago, and roads change as both of them move.
    /// </para>
    /// </summary>
    public static int BeyondMs { get; set; } = 10000;

    private static readonly Dictionary<Serial, long> _beyond = [];

    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMend));

    /// <summary>Whether a healer leaves alone anybody it would be a crime to help. Off, the reasons are still counted. See <see cref="Abetting"/>.</summary>
    public static bool ShunsOutlaws { get; set; } = true;

    /// <summary>How often the log names one bot passed over for being a criminal by the engine's reckoning alone. Two minutes, the engine's own grey.</summary>
    public static int CriminalSayMs { get; set; } = 120000;

    /// <summary>Looks at which somebody was left alone, or a stint or a mending ended, because it was at a robbery. Looks, not bots.</summary>
    public static long Robbing { get; private set; }

    /// <summary>The same, because it was red or in a cell.</summary>
    public static long Red { get; private set; }

    /// <summary>The same, because the engine counts helping it a crime and it was neither at a robbery nor red.</summary>
    public static long Criminal { get; private set; }

    private static readonly Dictionary<Serial, long> _criminalSaid = [];

    /// <summary>
    /// Why helping this one would be a crime — "at a robbery", "red" or "a criminal" — or null when it would not be, or
    /// when <see cref="ShunsOutlaws"/> is off. Never of oneself.
    ///
    /// <para>
    /// <b>The healers took the robber's side.</b> At 19:20 on 17.09.2026 Elspeth set on Joss for the 207gp in his pack;
    /// Gwendra and Jorunn went to stand by her, because she was one of ours and in a fight, and the second Joss fell and
    /// she went red Kestrel took on mending her. Over the logs of 16 and 17.09 healers stood by a robber mid-robbery six
    /// times and by its victim four, and stood by or mended a red eleven — three of them bandaged Gerda Ashdown twenty
    /// minutes after her murder. The engine counts a beneficial act on a criminal or a murderer as a crime
    /// (<c>Mobile.IsBeneficialCriminal</c>), so each of those bandages made its healer a criminal; the choice had asked only
    /// "ours" and "fighting" or "hurt".
    /// </para>
    ///
    /// <para>
    /// The robbery is asked of the deed as well as of the engine, because a robber walking up to its victim has not struck
    /// and the engine does not know it for a criminal yet; the red is asked of <see cref="BotOutlaw"/>, as the patrols ask.
    /// Anybody else the engine greys is named in the log, since nobody has yet counted who that is.
    /// </para>
    /// </summary>
    public static string Abetting(Mobile healer, Mobile other)
    {
        if (healer == null || other == null || other == healer)
        {
            return null;
        }

        string why;

        if (other is BotMobile { Resolve.Deed: BotRob })
        {
            Robbing++;
            why = "at a robbery";
        }
        else if (BotOutlaw.Outlaw(other) || BotOutlaw.Jailed(other))
        {
            Red++;
            why = "red";
        }
        else if (healer.IsBeneficialCriminal(other))
        {
            Criminal++;
            why = "a criminal";

            var now = Core.TickCount;

            if (!_criminalSaid.TryGetValue(other.Serial, out var said) || now - said >= CriminalSayMs)
            {
                _criminalSaid[other.Serial] = now;

                logger.Information(
                    "{Healer} leaves {Other} alone: the engine counts helping it a crime ({Notoriety}), though it is neither at a robbery nor red",
                    healer.Name,
                    other.Name,
                    other.Murderer ? "a murderer" : "a criminal"
                );
            }
        }
        else
        {
            return null;
        }

        return ShunsOutlaws ? why : null;
    }

    public static string Describe() =>
        $"{Robbing + Red + Criminal} looks at which a healer {(ShunsOutlaws ? "left alone" : "only counted")} somebody it would have been a crime to help ({Robbing} at a robbery, {Red} red, {Criminal} a criminal by the engine alone), {BotAccompany.Turned} stints and {BotSalve.Turned} mendings of another ended when the one helped turned; {BotAwaitMend.Describe()}";

    public static void Forget()
    {
        Robbing = 0;
        Red = 0;
        Criminal = 0;
        _criminalSaid.Clear();
    }

    /// <summary>
    /// The way to this patient was refused. Written where every healer reads it, not in one bot's memory.
    ///
    /// <para>
    /// <b>A healer with no memory of the last attempt walks the same refused road again on the next beat.</b>
    /// Over the night of 02-03.09.2026, 209 of the 230 refused roads on the shard were a mend — Ulla went to
    /// Joss six times inside one minute and five times in the minute before, Edda to Rowan five times in a
    /// minute, and every one of those ended in "no way through" from the same place as the last. The patient
    /// is chosen by who is worst hurt, and being unreachable does not make anybody less hurt, so the same
    /// answer came back every time.
    /// </para>
    ///
    /// <para>
    /// Shared rather than private, for the reason this project has settled twice already: what one bot proves
    /// about a road is true for the next one along. Short, because both ends of this walk are moving.
    /// </para>
    /// </summary>
    public static void Beyond(Mobile patient)
    {
        if (patient != null)
        {
            _beyond[patient.Serial] = Core.TickCount + BeyondMs;
        }
    }

    /// <summary>Whether a healer should leave this one alone for the moment. See <see cref="Beyond"/>.</summary>
    public static bool OutOfReach(Mobile patient)
    {
        if (patient == null || !_beyond.TryGetValue(patient.Serial, out var until))
        {
            return false;
        }

        if (Core.TickCount - until < 0)
        {
            return true;
        }

        _beyond.Remove(patient.Serial);

        return false;
    }

    /// <summary>
    /// Mana a circle costs, read off the same ladder the scribe pays to write one.
    ///
    /// <b>Inferred rather than read, and it is the one guess in this folder.</b> The engine keeps the cost
    /// inside the spell where nothing outside can ask it, so this is the era's own circle ladder — the same
    /// numbers <c>DefInscription</c> charges to scribe each circle. If it is wrong, the cost is a cast that
    /// fizzles and a bot that reaches for cloth instead, which is the failure this was going to have anyway.
    /// </summary>
    private static readonly int[] ManaByCircle = [4, 6, 9, 11, 14, 20, 40, 50];

    /// <summary>How hurt something has to be before it is worth mending. Below this, leave it alone.</summary>
    public static double Hurt { get; set; } = 0.7;

    /// <summary>
    /// How near a bandage has to be wound. Read off the engine rather than chosen: one tile on a renaissance
    /// shard, two under AOS rules.
    ///
    /// It used to be a two of my own, which on this era's rules is a tile too far — a healer that walked to
    /// where it thought cloth reached would stand there failing to bandage anybody.
    /// </summary>
    public static int Touch => Bandage.Range;

    /// <summary>
    /// How near a heal has to be cast.
    ///
    /// <b>Eight, well inside the engine's own ten to twelve, and this is the single most useful number in this
    /// folder.</b> A heal reaches across a screen; walking to a tile away from the patient put the healer inside
    /// melee range of whatever was hitting the patient — and a caster that is being hit cannot cast at all, so
    /// the healer was walking into the one condition that stops it working. Standing off is not a tactic here,
    /// it is the difference between a heal and a fizzle.
    /// </summary>
    public static int Cast { get; set; } = 8;

    /// <summary>
    /// How near something hostile has to be before mending is off the table altogether.
    ///
    /// <para>
    /// Wider than a blow reaches, on purpose: the rule is about standing still for several seconds, and what
    /// matters is not whether the thing can hit the bot now but whether it will before the bandage is
    /// finished. Anything inside a few paces will.
    /// </para>
    /// </summary>
    public static int Peril { get; set; } = 6;

    /// <summary>
    /// How long after a blow a bot still counts as being under fire.
    ///
    /// Three seconds: long enough to cover the gap between two swings of anything, short enough that a bot does
    /// not still think it is in a fight it walked away from.
    /// </summary>
    public static int UnderFireMs { get; set; } = 3000;

    /// <summary>
    /// The share of health below which a bot swallows a bottle rather than trying to mend properly.
    ///
    /// <b>A potion is the only mending in the game that works while something is hitting you.</b> A cast is
    /// destroyed by a blow outright; a bandage survives one but slips, losing success with every hit. A bottle
    /// is instant and cannot be interrupted at all. So it is what a bot reaches for when it is actually about to
    /// die — and only then, because there are two of them and a counter is a walk away.
    /// </summary>
    public static double Gulp { get; set; } = 0.4;

    /// <summary>How much of its health a bot is mended to before the job is called done.</summary>
    public static double Mended { get; set; } = 0.95;

    /// <summary>The share of health this one has left, or one when it is not hurt at all.</summary>
    public static double Share(Mobile m) =>
        m == null || m.HitsMax <= 0 ? 1.0 : Math.Clamp(m.Hits / (double)m.HitsMax, 0.0, 1.0);

    /// <summary>Whether this one is hurt enough to be worth mending, and can be mended at all.</summary>
    /// <summary>
    /// Whether this one needs looking after.
    ///
    /// <para>
    /// <b>Poison counts, and leaving it out meant the cure bottles were never once opened.</b> The mending
    /// work knows perfectly well what to do about poison — <see cref="Draught"/> reaches for a cure before it
    /// reaches for anything else, and says why — but nothing ever got that far: the only door into mending
    /// was this test, and this test asked about health alone. A bot at full health with a green tint walked
    /// about carrying the antidote for it until the poison ticked it down far enough to look like an ordinary
    /// wound, and then drank the cure as a side effect of bleeding.
    /// </para>
    ///
    /// <para>
    /// The same shape as every other defect this population has produced: two facts about one thing, and only
    /// one of them asked at the door.
    /// </para>
    /// </summary>
    public static bool Wants(Mobile m) =>
        m is { Deleted: false, Alive: true } && m.Map != null && m.Map != Map.Internal
        && (Share(m) < Hurt || m.Poisoned);

    /// <summary>Whether this one has been mended as far as this is going to take it.</summary>
    public static bool Whole(Mobile m) => m == null || Share(m) >= Mended;

    /// <summary>Bandages in the pack.</summary>
    public static int Cloth(Mobile bot) => bot?.Backpack?.GetAmount(typeof(Bandage)) ?? 0;

    /// <summary>Whether a bandage is already being wound. Winding a second one restarts the first.</summary>
    public static bool Winding(Mobile bot) => bot != null && BandageContext.GetContext(bot) != null;

    /// <summary>
    /// Whether something has hit this bot recently enough that casting is pointless.
    ///
    /// <para>
    /// <b>This one fact decides which way round spell and cloth go.</b> <c>Spell.OnCasterHurt</c> disturbs a
    /// cast whenever the caster is a player — and every bot here is a <c>PlayerMobile</c>, so a healer under
    /// fire burns mana and herbs producing nothing. A bandage is not interrupted: the engine calls
    /// <c>BandageContext.Slip</c> instead, which costs two per cent of the success chance per blow. So cloth
    /// works under fire and a spell does not, and the ordering follows from the mechanics rather than from a
    /// preference.
    /// </para>
    /// </summary>
    public static bool UnderFire(IBotWilful bot)
    {
        var resolve = bot?.Resolve;

        return resolve is { Struck: true } && Core.TickCount - resolve.HurtTick < UnderFireMs;
    }

    /// <summary>
    /// Whether the patient is in a fight: it has a living combatant, its company is fighting, it was hit within
    /// <see cref="UnderFireMs"/>, or something hostile stands within <see cref="Peril"/> of it.
    ///
    /// <para>
    /// <b>Patrick's order of 15.09.2026: in a fight a healer heals by spell, because a spell is fast; out of one, by
    /// cloth.</b> Until now the choice read the healer alone — cloth when the healer itself was under fire, a spell
    /// otherwise — so a healer standing safely eight tiles off cast its mana into a scratch on a bot at a forge, and
    /// walked to touch a bot that was being killed only when the fight had reached the healer too. The right question
    /// is whose fight it is: a patient in one needs the two-second cast from a distance, and a patient out of one can
    /// wait ten seconds for a bandage that costs no mana, which keeps the pool for the next fight.
    /// </para>
    /// </summary>
    public static bool Embattled(Mobile patient)
    {
        if (patient is not { Deleted: false, Alive: true } || patient.Map == null || patient.Map == Map.Internal)
        {
            return false;
        }

        if (patient.Combatant is { Deleted: false, Alive: true })
        {
            return true;
        }

        if (patient is BotMobile bot
            && (bot.Squad is { Stance: BotSquadStance.Fighting }
                || (bot.Resolve is { Struck: true } && Core.TickCount - bot.Resolve.HurtTick < UnderFireMs)))
        {
            return true;
        }

        return BotThreat.Anything(patient, Peril);
    }

    /// <summary>
    /// A bottle worth swallowing right now, or null.
    ///
    /// <para>
    /// Only on itself, because nothing in the engine pours a potion down somebody else's throat — which is why
    /// this is a bot's own last resort rather than a healer's tool.
    /// </para>
    ///
    /// <para>
    /// Poison first: it outlasts several rounds of anything else, and a cure bottle ends it in one. Then the
    /// wound. And the engine holds both guards itself — a heal potion refuses a patient at full health and keeps
    /// its own cooldown — so nothing here has to remember either.
    /// </para>
    /// </summary>
    public static BasePotion Draught(Mobile bot)
    {
        var pack = bot?.Backpack;

        if (pack == null)
        {
            return null;
        }

        if (bot.Poisoned)
        {
            var cure = Bottle(pack, BotPotionKind.Cure);

            if (cure != null)
            {
                return cure;
            }
        }

        return Share(bot) < Gulp ? Bottle(pack, BotPotionKind.Heal) : null;
    }

    /// <summary>Swallows it. Returns whether the engine allowed it.</summary>
    public static bool Swallow(Mobile bot, BasePotion potion)
    {
        if (bot == null || potion == null || potion.Deleted || !potion.CanDrink(bot))
        {
            return false;
        }

        potion.Drink(bot);

        BotReflex.Drank(bot);

        return true;
    }

    /// <summary>How many of that family are in the pack, and one of them.</summary>
    public static int Bottles(Mobile bot, BotPotionKind kind)
    {
        var type = BotArsenal.Potion(kind);

        return type == null ? 0 : bot?.Backpack?.GetAmount(type) ?? 0;
    }

    /// <remarks>
    /// Public because a bottle is reached for from two places now, and only one of them is a decision: see
    /// <c>BotMobile.Gasp</c> for the reflex that fires when a bot is about to die whatever it had planned.
    /// </remarks>
    public static BasePotion Bottle(Container pack, BotPotionKind kind)
    {
        var type = BotArsenal.Potion(kind);

        if (type == null)
        {
            return null;
        }

        var found = pack.FindItemByType(type);

        return found as BasePotion;
    }

    /// <summary>
    /// The strongest heal this bot could actually cast on that patient right now, or -1.
    ///
    /// <para>
    /// Strongest first and cure before either: poison outruns cloth, which is the whole reason the healer is
    /// born knowing <c>Cure</c>. After that it is greater heal if the book has it and the pool can pay, then
    /// the plain one. A caster whose book holds nothing but magic arrow gets -1 and reaches for cloth, which is
    /// correct rather than a shortfall.
    /// </para>
    /// </summary>
    public static int Spell(Mobile bot, Mobile patient)
    {
        if (bot == null || patient == null || BotGrimoire.Book(bot) == null)
        {
            return -1;
        }

        if (patient.Poisoned && Ready(bot, BotArsenal.SpellCure))
        {
            return BotArsenal.SpellCure;
        }

        if (Ready(bot, BotArsenal.SpellGreaterHeal))
        {
            return BotArsenal.SpellGreaterHeal;
        }

        return Ready(bot, BotArsenal.SpellHeal) ? BotArsenal.SpellHeal : -1;
    }

    /// <summary>Whether the book holds it, the pool can pay for it, and the herbs for it are in the pack.</summary>
    public static bool Ready(Mobile bot, int spell)
    {
        if (!BotGrimoire.Holds(bot, spell))
        {
            return false;
        }

        var circle = BotGrimoire.Circle(spell);

        if (circle < 1 || circle > ManaByCircle.Length || bot.Mana < ManaByCircle[circle - 1])
        {
            return false;
        }

        var made = SpellRegistry.NewSpell(spell, bot, null);
        var herbs = made?.Reagents;

        if (made == null)
        {
            return false;
        }

        var pack = bot.Backpack;

        for (var i = 0; herbs != null && i < herbs.Length; i++)
        {
            if ((pack?.GetAmount(herbs[i]) ?? 0) < 1)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Whether the book holds the spell and the herbs for it are in the pack, whatever the pool holds right now.
    ///
    /// <para>
    /// <b>For deciding where to be rather than whether to cast.</b> <see cref="Ready"/> answers "can this be cast this
    /// second", and mana comes back on its own — so a healer asked "have you anything to heal with" at the bottom of its
    /// pool would say no, walk away from the fighter it was standing by, and have its mana back before it arrived
    /// anywhere. Herbs do not come back on their own; that is the question worth asking. Builds the spell to read its
    /// reagents, so it is asked on a clock by its callers, never every beat.
    /// </para>
    /// </summary>
    public static bool Herbs(Mobile bot, int spell)
    {
        if (bot == null || !BotGrimoire.Holds(bot, spell))
        {
            return false;
        }

        var made = SpellRegistry.NewSpell(spell, bot, null);

        if (made == null)
        {
            return false;
        }

        var herbs = made.Reagents;
        var pack = bot.Backpack;

        for (var i = 0; herbs != null && i < herbs.Length; i++)
        {
            if ((pack?.GetAmount(herbs[i]) ?? 0) < 1)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Begins a heal. Returns whether the cast started; the patient is reached on a later beat.
    ///
    /// <para>
    /// Two beats rather than one, because that is how the engine casts: <c>Cast</c> starts a delay, the delay
    /// ends in <c>OnCast</c>, and <c>OnCast</c> puts a target on the caster which somebody then has to fill in.
    /// A bot has no client to click with, so <see cref="Aim"/> is the click.
    /// </para>
    /// </summary>
    public static bool Begin(Mobile bot, int spell)
    {
        if (bot == null || bot.Spell != null)
        {
            return false;
        }

        var made = SpellRegistry.NewSpell(spell, bot, null);

        return made != null && made.Cast();
    }

    /// <summary>
    /// Points a finished cast at the patient. Returns whether there was one to point.
    ///
    /// The mana and the reagents are spent here rather than at the cast, because that is where the engine spends
    /// them — in the sequence check behind the target.
    /// </summary>
    public static bool Aim(Mobile bot, Mobile patient)
    {
        var aiming = bot?.Target;

        if (aiming == null || patient == null)
        {
            return false;
        }

        aiming.Invoke(bot, patient);

        // The stopwatch: a heal begun is what stops a patient's clock. See BotReflex.
        BotReflex.Mending(bot, patient);

        return true;
    }

    /// <summary>
    /// Winds a bandage on. Returns whether one was used.
    ///
    /// The cloth is consumed only when the engine accepted the patient, which is the order the engine's own
    /// item uses — and it is what makes "that being is not damaged" cost nothing.
    /// </summary>
    public static bool Wind(Mobile bot, Mobile patient)
    {
        var pack = bot?.Backpack;
        var cloth = pack?.FindItemByType<Bandage>();

        if (cloth == null || patient == null || Winding(bot))
        {
            return false;
        }

        if (BandageContext.BeginHeal(bot, patient) == null)
        {
            return false;
        }

        cloth.Consume();

        BotReflex.Mending(bot, patient);

        return true;
    }
}
