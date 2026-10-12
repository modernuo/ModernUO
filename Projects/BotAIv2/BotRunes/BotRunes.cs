using System;
using System.Collections.Generic;
using Server.Items;
using Server.Misc;
using Server.Spells;
using Server.Spells.Fourth;
using Server.Spells.Seventh;
using Server.Spells.Sixth;

namespace Server.BotAI.V2;

/// <summary>
/// Runes: a mage marks where it has stood, recalls there alone, and opens a gate there for its company.
///
/// <para>
/// <b>Patrick's order of 29.09.2026, point 7: "a mage with Magery 40+ marks a rune (Mark) where it has been, and then
/// Gate Travel opens a gate for the whole company; Recall — alone. A rune library in the guild hall and a pathfinder who
/// got there by gate or boat. Instant transfer of a company anywhere they have stood."</b> Until now the only way across
/// the island was on foot, and a delve's party walked four hundred tiles to the mouth of Despise and a thousand to
/// Shame's.
/// </para>
///
/// <para>
/// <b>Every cast is the engine's own spell object, cast by the engine's own sequence.</b> Mark is <see cref="MarkSpell"/>
/// with its target filled in by the bot (<c>Target.Invoke</c> on a blank <see cref="RecallRune"/>, as the heals are
/// aimed); Recall and Gate Travel are <see cref="RecallSpell"/> and <see cref="GateTravelSpell"/> built on a
/// <see cref="RunebookEntry"/>, which is the path the engine's own runebook gump takes. Skill, mana and reagents are asked
/// and spent by <c>Spell.CheckSequence</c>, a fizzle is the engine's, and the travel rules are the engine's
/// (<see cref="SpellHelper.CheckTravel(Map, Point3D, TravelCheckType, out TextDefinition)"/>): nobody marks inside a
/// Felucca dungeon, so a mouth's rune stands on the island side of the mouth, and the company walks the last few tiles
/// through the cave mouth as it always has.
/// </para>
///
/// <para>
/// <b>What "Magery 40+" means in this era.</b> <c>MagerySpell._requiredSkill</c> for UOR puts Recall from a book at 30 to
/// 70, Mark at 50 to 90 and Gate Travel at 60 to 100 (a scroll: two circles lower). So at 40 a mage recalls one time in
/// four, marks only from a scroll, and gates not at all; <see cref="LeastMagery"/> is Patrick's floor for being a rune-mage
/// at all, and the engine's own chance (<see cref="LeastChance"/>) decides each cast.
/// </para>
/// </summary>
public static class BotRunes
{
    /// <summary>The engine's spell ids (Spells/Initializer.cs).</summary>
    public const int Recall = 31;

    public const int Mark = 44;

    public const int Gate = 51;

    /// <summary>Whether runes are marked, recalled to and gated to at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>
    /// The Magery at which a bot is a rune-mage: offered marks, taught Recall and Mark next (<see cref="Wanted"/>), and
    /// given copies at the library. Forty, Patrick's number; what it can actually cast is the engine's chance.
    /// </summary>
    public static double LeastMagery { get; set; } = 40.0;

    /// <summary>The Magery at which a rune-mage wants Gate Travel in its book. Sixty: the book's floor for the seventh circle in UOR.</summary>
    public static double GateMagery { get; set; } = 60.0;

    /// <summary>
    /// Whether a rune-mage's book learns Recall, Mark and Gate Travel before the rest of its circles. True: without it
    /// the book fills lowest id first and reaches Recall after thirty other spells, Mark after forty-three.
    /// </summary>
    public static bool Learn { get; set; } = true;

    /// <summary>
    /// The least chance the engine's skill check must give a cast before it is tried. A fifth: below it a mage spends its
    /// reagents on fizzles, and the walk is the better way there.
    /// </summary>
    public static double LeastChance { get; set; } = 0.2;

    /// <summary>Casts tried before a recall, a mark or a gate is given up and the bot walks. Three.</summary>
    public static int Tries { get; set; } = 3;

    /// <summary>The pause between one failed cast and the next: the engine's recovery and a little mana. Four seconds.</summary>
    public static int RetryMs { get; set; } = 4000;

    /// <summary>Blank runes a rune-mage keeps in its pack for marking and for copies. Three.</summary>
    public static int Blanks { get; set; } = 3;

    /// <summary>How near a place its rune is marked, and how near a rune must be to a place to be a rune for it. Twelve.</summary>
    public static int Near { get; set; } = 12;

    /// <summary>The least walk worth a recall, as the crow flies. Two hundred tiles: under that the walk is a minute or two.</summary>
    public static int Far { get; set; } = 200;

    /// <summary>How much nearer the rune must put the bot, or the company, than it stands now. A hundred and fifty tiles.</summary>
    public static int LeastSaved { get; set; } = 150;

    /// <summary>How near a destination a rune may stand and still be the company's way there. Sixty: a march of half a minute.</summary>
    public static int GateNear { get; set; } = 60;

    /// <summary>Places a guild holds runes for: the engine's runebook holds sixteen, and so does a guild's library.</summary>
    public static int Shelf { get; set; } = 16;

    // ---- counters ----

    /// <summary>Runes marked, and by place name.</summary>
    public static long Marked { get; private set; }

    private static readonly Dictionary<string, int> _markedAt = new(StringComparer.Ordinal);

    public static long Recalls { get; internal set; }

    public static long TilesSaved { get; internal set; }

    public static long RecallFizzles { get; internal set; }

    public static long RecallFallbacks { get; internal set; }

    public static long RecallsAsked { get; internal set; }

    /// <summary>A far walk whose bot held a rune near where it was going.</summary>
    public static long RecallsHeld { get; internal set; }

    /// <summary>Held a rune and could not cast for it — the named reasons are in <see cref="_unready"/>, with the marker's own refusals prefixed "mark:".</summary>
    public static long RecallsUnready { get; internal set; }

    private static readonly Dictionary<string, int> _unready = new(StringComparer.Ordinal);

    public static void Unready(string why)
    {
        if (why == null)
        {
            return;
        }

        _unready.TryGetValue(why, out var had);
        _unready[why] = had + 1;
    }

    /// <summary>A mark, counted by place.</summary>
    public static void Counted(string place)
    {
        Marked++;

        if (place != null)
        {
            _markedAt.TryGetValue(place, out var had);
            _markedAt[place] = had + 1;
        }
    }

    // ---- who is a rune-mage, and what the engine will let it cast ----

    /// <summary>Whether this bot's Magery makes it a rune-mage. Cheap: asked by proposers on every free bot.</summary>
    public static bool Mage(Mobile body) =>
        Running && body is BotMobile { Deleted: false, Alive: true } && body.Skills[SkillName.Magery].Value >= LeastMagery;

    /// <summary>The scroll type of one of the three spells, or null.</summary>
    public static Type ScrollType(int spellId) =>
        spellId switch
        {
            Recall => typeof(RecallScroll),
            Mark   => typeof(MarkScroll),
            Gate   => typeof(GateTravelScroll),
            _      => null
        };

    /// <summary>Whether the bot owns the spell at all, in its book or as a scroll in its pack.</summary>
    public static bool Knows(Mobile body, int spellId) =>
        BotGrimoire.Holds(body, spellId) || ScrollType(spellId) is { } scroll && body?.Backpack?.FindItemByType(scroll) != null;

    /// <summary>The engine's spell object for one of the three, unaimed. Built to read its skill, mana and herbs, and to cast.</summary>
    public static Spell Make(int spellId, Mobile body, Item scroll, RunebookEntry entry = null) =>
        spellId switch
        {
            Recall => new RecallSpell(body, entry, null, scroll),
            Mark   => new MarkSpell(body, scroll),
            Gate   => new GateTravelSpell(body, entry, scroll),
            _      => null
        };

    /// <summary>The engine's chance of this cast landing, from its own <c>GetCastSkills</c>: nought below the floor, one at the ceiling.</summary>
    public static double Chance(Mobile body, Spell spell)
    {
        if (body == null || spell == null)
        {
            return 0.0;
        }

        spell.GetCastSkills(out var min, out var max);

        var skill = body.Skills[spell.CastSkill].Value;

        if (max <= min)
        {
            return skill >= min ? 1.0 : 0.0;
        }

        return Math.Clamp((skill - min) / (max - min), 0.0, 1.0);
    }

    /// <summary>
    /// Whether the bot can start this cast now, and if not, why — asked before the engine is, so that every refusal is
    /// named in the summary rather than suffered as a fizzle nobody counts. A scroll in the pack is preferred: it wants no
    /// reagents and asks two circles less of the skill.
    /// </summary>
    public static bool Ready(Mobile body, int spellId, out SpellScroll scroll, out double chance, out string why)
    {
        scroll = null;
        chance = 0.0;
        why = null;

        var pack = body?.Backpack;

        if (body is not { Deleted: false, Alive: true } || pack == null)
        {
            why = "no body";

            return false;
        }

        if (body.Spell != null)
        {
            why = "already casting";

            return false;
        }

        if (body.Paralyzed || body.Frozen)
        {
            why = "held fast";

            return false;
        }

        // The travel spells' own refusals, asked of the engine's own predicates.
        if (spellId != Mark && body.Criminal)
        {
            why = "a criminal";

            return false;
        }

        if (spellId != Mark && SpellHelper.CheckCombat(body))
        {
            why = "in the heat of battle";

            return false;
        }

        if (spellId == Recall && StaminaSystem.IsOverloaded(body))
        {
            why = "overloaded";

            return false;
        }

        var kind = ScrollType(spellId);

        scroll = kind == null ? null : pack.FindItemByType(kind) as SpellScroll;

        if (scroll == null && !BotGrimoire.Holds(body, spellId))
        {
            why = "neither book nor scroll";

            return false;
        }

        var spell = Make(spellId, body, scroll);

        if (spell == null)
        {
            why = "no such spell";

            return false;
        }

        chance = Chance(body, spell);

        if (chance < LeastChance)
        {
            why = "too little Magery";

            return false;
        }

        if (body.Mana < spell.GetMana())
        {
            why = "too little mana";

            return false;
        }

        if (Core.TickCount - body.NextSpellTime < 0)
        {
            why = "not recovered";

            return false;
        }

        if (scroll != null)
        {
            return true;
        }

        var herbs = spell.Reagents;

        for (var i = 0; herbs != null && i < herbs.Length; i++)
        {
            if (pack.GetAmount(herbs[i]) < 1)
            {
                why = "out of reagents";

                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The next travel spell a rune-mage's book lacks, or -1: Recall and Mark from <see cref="LeastMagery"/>, Gate Travel
    /// from <see cref="GateMagery"/>. Asked by <c>BotGrimoire.Missing</c> after the class's own first spells.
    /// </summary>
    public static int Wanted(Mobile body, Spellbook book)
    {
        if (!Running || !Learn || book == null || body == null)
        {
            return -1;
        }

        var magery = body.Skills[SkillName.Magery].Value;

        if (magery < LeastMagery)
        {
            return -1;
        }

        if (!book.HasSpell(Recall))
        {
            return Recall;
        }

        if (!book.HasSpell(Mark))
        {
            return Mark;
        }

        return magery >= GateMagery && !book.HasSpell(Gate) ? Gate : -1;
    }

    // ---- what a bot carries ----

    /// <summary>Marks a thing as the bot's own the way <c>BotBinding</c> does — kept through death, weightless, never sold — without making it an entitlement a rising would hand back.</summary>
    public static void Bind(BotMobile bot, Item item)
    {
        if (bot == null || item == null)
        {
            return;
        }

        if (!Core.AOS && item.LootType == LootType.Regular)
        {
            item.LootType = LootType.Newbied;
        }

        item.Weight = 0.0;
        bot.Bond?.Items.Add(item.Serial);
    }

    /// <summary>One blank rune in the pack, or null.</summary>
    public static RecallRune Blank(Mobile body)
    {
        var pack = body?.Backpack;

        if (pack == null)
        {
            return null;
        }

        foreach (var rune in pack.FindItemsByType<RecallRune>())
        {
            if (rune is { Deleted: false, Marked: false })
            {
                return rune;
            }
        }

        return null;
    }

    /// <summary>How many blank runes are in the pack.</summary>
    public static int BlankCount(Mobile body)
    {
        var pack = body?.Backpack;
        var many = 0;

        if (pack == null)
        {
            return 0;
        }

        foreach (var rune in pack.FindItemsByType<RecallRune>())
        {
            if (rune is { Deleted: false, Marked: false })
            {
                many++;
            }
        }

        return many;
    }

    /// <summary>The name of a rune-mage's own book of runes.</summary>
    public static string SatchelName(Mobile body) => $"{body?.Name}'s runes";

    /// <summary>
    /// The rune-mage's own book of runes, made and bound the first time it has a rune to keep. Not bought: no shopkeeper
    /// in this era sells a runebook (<c>SBScribe</c>'s line is commented out), and no scribe bot writes one yet.
    /// </summary>
    public static Runebook Satchel(BotMobile bot, bool make)
    {
        var pack = bot?.Backpack;

        if (pack == null)
        {
            return null;
        }

        var name = SatchelName(bot);

        foreach (var book in pack.FindItemsByType<Runebook>())
        {
            if (book is { Deleted: false } && string.Equals(book.Description, name, StringComparison.Ordinal))
            {
                return book;
            }
        }

        if (!make)
        {
            return null;
        }

        var made = new Runebook(0) { Description = name };

        pack.DropItem(made);
        Bind(bot, made);
        Satchels++;

        return made;
    }

    /// <summary>Books of runes made for rune-mages.</summary>
    public static long Satchels { get; private set; }

    /// <summary>Whether this book already holds a rune within <see cref="Near"/> of that place.</summary>
    public static bool Has(Runebook book, Map map, Point3D at)
    {
        var entries = book?.Entries;

        for (var i = 0; entries != null && i < entries.Count; i++)
        {
            var entry = entries[i];

            if (entry.Map == map && Utility.InRange(entry.Location, at, Near))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The rune this bot holds nearest to <paramref name="where"/> and within <paramref name="within"/> of it: in its own
    /// book, in the guild's library when it is the one carrying it, or loose in its pack. The entry is the engine's, ready
    /// to cast from.
    /// </summary>
    public static bool Holds(Mobile body, Map map, Point3D where, int within, out RunebookEntry entry)
    {
        entry = null;

        var pack = body?.Backpack;

        if (pack == null || map == null)
        {
            return false;
        }

        var best = int.MaxValue;

        foreach (var book in pack.FindItemsByType<Runebook>())
        {
            var entries = book?.Entries;

            for (var i = 0; entries != null && i < entries.Count; i++)
            {
                var one = entries[i];
                var away = Apart(one.Location, where);

                if (one.Map == map && away <= within && away < best)
                {
                    best = away;
                    entry = one;
                }
            }
        }

        foreach (var rune in pack.FindItemsByType<RecallRune>())
        {
            if (rune is not { Deleted: false, Marked: true } || rune.TargetMap != map)
            {
                continue;
            }

            var away = Apart(rune.Target, where);

            if (away <= within && away < best)
            {
                best = away;
                entry = new RunebookEntry(null, rune.Target, rune.TargetMap, rune.Description);
            }
        }

        return entry != null;
    }

    public static int Apart(Point3D a, Point3D b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    // ---- where a rune may be marked ----

    /// <summary>
    /// Whether a rune marked here could be recalled and gated to: a body fits, the engine's travel rules allow a mark,
    /// a recall and a gate, no house or boat stands on it, and no teleporter is underfoot or beside it (a gate opened on
    /// one would hand the company to the teleporter).
    /// </summary>
    public static bool Landable(Map map, Point3D p)
    {
        // The engine's CanSpawnMobile without its mobiles: a bot standing on the tile is not a wall, and the cast asks again.
        if (map == null || map == Map.Internal || !Region.Find(p, map).AllowSpawn() || !map.CanFit(p.X, p.Y, p.Z, 16, false, false))
        {
            return false;
        }

        if (!SpellHelper.CheckTravel(map, p, TravelCheckType.Mark, out _)
            || !SpellHelper.CheckTravel(map, p, TravelCheckType.RecallTo, out _)
            || !SpellHelper.CheckTravel(map, p, TravelCheckType.GateTo, out _))
        {
            return false;
        }

        if (SpellHelper.CheckMulti(p, map))
        {
            return false;
        }

        foreach (var _ in map.GetItemsInRange<Teleporter>(p, 1))
        {
            return false;
        }

        // Nor beside a public moongate (build 317 raised them all): a recall landing on the gate's tile is put through it.
        foreach (var _ in map.GetItemsInRange<PublicMoongate>(p, 1))
        {
            return false;
        }

        return true;
    }

    /// <summary>The nearest landable tile to an anchor, ring by ring out to <paramref name="sweep"/>, on the anchor's own land.</summary>
    public static bool Spot(Map map, Point3D anchor, int sweep, out Point3D spot)
    {
        spot = Point3D.Zero;

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        var land = BotGates.LandOf(map, anchor);

        for (var r = 0; r <= sweep; r++)
        {
            for (var dx = -r; dx <= r; dx++)
            {
                for (var dy = -r; dy <= r; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                    {
                        continue;
                    }

                    var x = anchor.X + dx;
                    var y = anchor.Y + dy;

                    if (!BotStep.Settle(map, x, y, out var z))
                    {
                        continue;
                    }

                    var at = new Point3D(x, y, z);

                    if (!Landable(map, at))
                    {
                        continue;
                    }

                    // On the anchor's land, or a spot across a river is "near" a town the rune cannot reach.
                    if (land >= 0 && BotGates.LandOf(map, at) is var there && there >= 0 && there != land)
                    {
                        continue;
                    }

                    spot = at;

                    return true;
                }
            }
        }

        return false;
    }

    // ---- the summary ----

    /// <summary>
    /// One line: the headline first — marked by place, recalls, gates, bodies through — then every gate behind it, so that a
    /// nought in the headline can be followed to the refusal that made it.
    /// </summary>
    public static string Describe()
    {
        if (!Running)
        {
            return "runes are off";
        }

        using var line = Server.Text.ValueStringBuilder.Create(1024);

        line.Append($"{Marked} marked");

        if (_markedAt.Count > 0)
        {
            line.Append(" (");

            var first = true;

            foreach (var (place, many) in _markedAt)
            {
                line.Append(first ? "" : ", ");
                line.Append($"{place} {many}");
                first = false;
            }

            line.Append(')');
        }

        line.Append(
            $", {Recalls} recalls saving {TilesSaved} tiles, {BotPassage.Opened} gates opened, {BotPassage.Through} through them; {BotRuneShelf.Describe()}; {BotRuneLibrary.Describe()}; {BotMarker.Describe()}; {BotRuneBuyer.Describe()}; "
        );
        line.Append(
            $"recalls: {RecallsAsked} far walks by rune-mages asked, {RecallsHeld} held a rune near where they went, {RecallsUnready} times one could not cast for it"
        );

        if (_unready.Count > 0)
        {
            line.Append("; refusals by reason (");

            var first = true;

            foreach (var (why, many) in _unready)
            {
                line.Append(first ? "" : ", ");
                line.Append($"{why} {many}");
                first = false;
            }

            line.Append(')');
        }

        line.Append($", {RecallFizzles} casts failed, {RecallFallbacks} fell back to walking; gates: {BotPassage.Describe()}; {Satchels} rune-mages' books made");

        return line.ToString();
    }

    public static void Forget()
    {
        Marked = 0;
        _markedAt.Clear();
        Recalls = 0;
        TilesSaved = 0;
        RecallFizzles = 0;
        RecallFallbacks = 0;
        RecallsAsked = 0;
        RecallsHeld = 0;
        RecallsUnready = 0;
        _unready.Clear();
        Satchels = 0;
        BotMarker.Forget();
        BotRuneBuyer.Forget();
        BotRuneLibrary.Forget();
        BotPassage.Forget();
    }
}
