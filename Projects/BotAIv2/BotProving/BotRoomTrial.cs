using System;
using System.Collections.Generic;
using Server.Engines.Spawners;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// A guild's company of doubles against a dungeon's worst room, on the proving ground: whether the band clears it, what it
/// cost, and who fell.
///
/// <para>
/// <b>The second branch of Patrick's order of 26.09.2026, and the one the first hour asked for.</b> One bot against one
/// creature (<see cref="BotTrial"/>) says what a bot is worth against that creature; added up over a band it said three bands
/// could take Wrong, and all three came back "whoever was leading it fell". A dungeon is not its worst creature one at a time:
/// it is a room of them at once, several on one bot, spells from the back while the front swings. So this puts the band itself
/// — its best <see cref="BotProving.PartySize"/> fighters, doubled — against the room the dungeon's spawners make worst (the
/// spawner whose creatures add up to the most, up to <see cref="BotProving.MostFoes"/> of them), and reads whether it came
/// out. The delve then sends a guild past the easiest dungeon only where its company has cleared such a room lately.
/// </para>
///
/// <para>
/// Each double fights with the same hands as in a single fight (<see cref="BotDriver"/>), set on the creature nearest it that
/// still stands; the creatures choose for themselves, as they do underground. Nothing of the real bots is touched — a band
/// can be tried while its members are down a dungeon — and nothing leaves the ring.
/// </para>
/// </summary>
public sealed class BotRoomTrial
{
    public BotRoomTrial(string guild, List<BotMobile> party, BotDungeon.Deep deep, List<string> room, int ring, Map map, Point3D middle)
    {
        Guild = guild;
        Party = party;
        Deep = deep;
        Room = room;
        Ring = ring;
        Map = map;
        Middle = middle;
    }

    public string Guild { get; }

    public List<BotMobile> Party { get; }

    public BotDungeon.Deep Deep { get; }

    /// <summary>What the room holds, by the names its spawner gives them.</summary>
    public List<string> Room { get; }

    public int Ring { get; }

    public Map Map { get; }

    public Point3D Middle { get; }

    public long Began { get; private set; }

    public string Verdict { get; private set; }

    /// <summary>Doubles that fell.</summary>
    public int Deaths { get; private set; }

    /// <summary>Of the room's creatures, how many are down.</summary>
    public int Downed { get; private set; }

    /// <summary>The share of the room's whole health taken off it: one for a room cleared.</summary>
    public double Killed { get; private set; }

    /// <summary>The share of the company's whole health spent, net of healing, floored at nought.</summary>
    public double Spent { get; private set; }

    public double Seconds { get; private set; }

    public string Refused { get; private set; }

    public bool Cleared => Verdict == "cleared";

    private readonly List<BotDriver> _drivers = [];

    private readonly List<BaseCreature> _foes = [];

    private int _roomHits;

    private int _companyHits;

    /// <summary>Puts the company down on the west of the ring and the room on the east, and sets them on each other.</summary>
    public bool Begin()
    {
        if (Map == null || Map == Map.Internal || Party.Count == 0 || Room.Count == 0)
        {
            Refused = "nothing to set against anything";

            return false;
        }

        for (var i = 0; i < Room.Count; i++)
        {
            var type = AssemblyHandler.FindTypeByName(Room[i]);
            BaseCreature foe = null;

            try
            {
                foe = type == null ? null : Activator.CreateInstance(type) as BaseCreature;
            }
            catch (Exception)
            {
                foe = null;
            }

            if (foe == null)
            {
                continue;
            }

            foe.Home = Middle;
            foe.RangeHome = BotProving.Leash;
            foe.MoveToWorld(new Point3D(Middle.X + 4, Middle.Y - Room.Count / 2 + i, Middle.Z), Map);
            _foes.Add(foe);
            _roomHits += foe.HitsMax;
        }

        if (_foes.Count == 0)
        {
            Refused = "the room would not be built";

            return false;
        }

        var now = Core.TickCount;

        for (var i = 0; i < Party.Count; i++)
        {
            var me = BotStandIn.Copy(Party[i], Map, new Point3D(Middle.X - 4, Middle.Y - Party.Count / 2 + i, Middle.Z));

            if (me == null)
            {
                continue;
            }

            _drivers.Add(new BotDriver(me, now));
            _companyHits += me.HitsMax;
        }

        if (_drivers.Count == 0)
        {
            End();
            Refused = "no double could be made";

            return false;
        }

        Began = now;

        for (var i = 0; i < _foes.Count; i++)
        {
            var target = Nearest(_foes[i]);

            _foes[i].Warmode = true;
            _foes[i].Combatant = target;
        }

        return true;
    }

    private Mobile Nearest(Mobile from)
    {
        Mobile best = null;
        var bestRange = double.MaxValue;

        for (var i = 0; i < _drivers.Count; i++)
        {
            var me = _drivers[i].Me;

            if (me is not { Deleted: false, Alive: true })
            {
                continue;
            }

            var range = from.GetDistanceToSqrt(me);

            if (range < bestRange)
            {
                bestRange = range;
                best = me;
            }
        }

        return best;
    }

    private BaseCreature NearestFoe(Mobile from)
    {
        BaseCreature best = null;
        var bestRange = double.MaxValue;

        for (var i = 0; i < _foes.Count; i++)
        {
            var foe = _foes[i];

            if (foe is not { Deleted: false, Alive: true })
            {
                continue;
            }

            var range = from.GetDistanceToSqrt(foe);

            if (range < bestRange)
            {
                bestRange = range;
                best = foe;
            }
        }

        return best;
    }

    /// <summary>One beat: every double answers the creature nearest it. True when it is over.</summary>
    public bool Drive()
    {
        if (Verdict != null)
        {
            return true;
        }

        var now = Core.TickCount;
        var standing = 0;
        var left = 0;

        for (var i = 0; i < _foes.Count; i++)
        {
            if (_foes[i] is { Deleted: false, Alive: true })
            {
                left++;
            }
        }

        for (var i = 0; i < _drivers.Count; i++)
        {
            if (_drivers[i].Me is { Deleted: false, Alive: true })
            {
                standing++;
            }
        }

        if (left == 0)
        {
            return Decide("cleared");
        }

        if (standing == 0)
        {
            return Decide("wiped out");
        }

        if (now - Began >= BotProving.RoomCapMs)
        {
            return Decide("called at the cap");
        }

        // <b>The company fights together: the leader's target first, the enemy nearest the leader where that cannot be struck
        // (30.09.2026, build 355; Patrick: "fight all together; their main target is the leader's target, and if it cannot be reached,
        // attack the one nearest the leader").</b> Each double took the creature nearest itself and kept it, so a company of five
        // spread its blows over the room: all six companies of the night against Wrong's four golem controllers were "wiped
        // out … 0 of 4 down", 13 to 49 % of the room's health taken. The leader is the first double standing; it keeps what it
        // fights, else takes what is nearest it; every other double strikes that when it can see it, else the creature nearest
        // the leader that it can see, else the nearest to itself.
        Mobile lead = null;

        for (var i = 0; i < _drivers.Count; i++)
        {
            if (_drivers[i].Me is { Deleted: false, Alive: true } first)
            {
                lead = first;

                break;
            }
        }

        var focus = lead == null
            ? null
            : lead.Combatant is BaseCreature { Deleted: false, Alive: true } leading && _foes.Contains(leading)
                ? leading
                : NearestFoe(lead);

        for (var i = 0; i < _drivers.Count; i++)
        {
            var me = _drivers[i].Me;

            if (me is not { Deleted: false, Alive: true })
            {
                continue;
            }

            BaseCreature target;

            if (!Doctrine)
            {
                target = me.Combatant is BaseCreature { Deleted: false, Alive: true } fighting && _foes.Contains(fighting)
                    ? fighting
                    : NearestFoe(me);
            }
            else if (focus != null && (ReferenceEquals(me, lead) || me.InLOS(focus)))
            {
                target = focus;
                OnLeaders++;
            }
            else
            {
                target = (lead == null ? null : NearestSeen(lead, me)) ?? NearestFoe(me);
                ByLeader++;
            }

            if (target != null)
            {
                _drivers[i].Answer(target, now);
            }
        }

        return false;
    }

    /// <summary>Whether the company strikes the leader's target (the default) or each double its own nearest. See the trial's beat.</summary>
    public static bool Doctrine { get; set; } = true;

    /// <summary>Beats a double struck the leader's target; beats it struck the enemy nearest the leader instead.</summary>
    public long OnLeaders { get; private set; }

    public long ByLeader { get; private set; }

    /// <summary>The creature nearest the leader that this double can see; null when it sees none.</summary>
    private BaseCreature NearestSeen(Mobile lead, Mobile me)
    {
        BaseCreature best = null;
        var bestRange = double.MaxValue;

        for (var i = 0; i < _foes.Count; i++)
        {
            var foe = _foes[i];

            if (foe is not { Deleted: false, Alive: true } || !me.InLOS(foe))
            {
                continue;
            }

            var range = lead.GetDistanceToSqrt(foe);

            if (range < bestRange)
            {
                bestRange = range;
                best = foe;
            }
        }

        return best;
    }

    private bool Decide(string verdict)
    {
        Verdict = verdict;
        Seconds = (Core.TickCount - Began) / 1000.0;

        var roomLeft = 0;

        for (var i = 0; i < _foes.Count; i++)
        {
            if (_foes[i] is { Deleted: false, Alive: true } foe)
            {
                roomLeft += foe.Hits;
            }
            else
            {
                Downed++;
            }
        }

        Killed = _roomHits <= 0 ? 0.0 : Math.Clamp(1.0 - roomLeft / (double)_roomHits, 0.0, 1.0);

        var taken = 0;
        var healed = 0;

        for (var i = 0; i < _drivers.Count; i++)
        {
            var me = _drivers[i].Me;

            if (me == null)
            {
                continue;
            }

            taken += me.Taken;
            healed += me.Healed;

            if (me.Deleted || !me.Alive)
            {
                Deaths++;
            }
        }

        Spent = _companyHits <= 0 ? 0.0 : Math.Max(0.0, (taken - healed) / (double)_companyHits);

        return true;
    }

    /// <summary>Everything off the ring: doubles, creatures, corpses.</summary>
    public void End()
    {
        for (var i = 0; i < _drivers.Count; i++)
        {
            if (_drivers[i].Me is { Deleted: false } me)
            {
                me.Combatant = null;
                me.Delete();
            }
        }

        for (var i = 0; i < _foes.Count; i++)
        {
            if (_foes[i] is { Deleted: false } foe)
            {
                foe.Combatant = null;
                foe.Delete();
            }
        }

        if (Map != null && Map != Map.Internal)
        {
            BotProving.Sweep(Map, Middle, BotProving.Leash + BotProving.Apart + 6);
        }
    }

    public string Say()
    {
        var names = new List<string>();

        for (var i = 0; i < Party.Count; i++)
        {
            names.Add(Party[i]?.Name);
        }

        var kinds = new Dictionary<string, int>();

        for (var i = 0; i < Room.Count; i++)
        {
            kinds[Room[i]] = kinds.GetValueOrDefault(Room[i]) + 1;
        }

        var room = new List<string>();

        foreach (var (kind, count) in kinds)
        {
            room.Add($"{count} {kind}");
        }

        return $"{Guild}'s company ({string.Join(", ", names)}) against the worst room of {Deep.Name} ({string.Join(", ", room)}): "
            + $"{Verdict} in {Seconds:F0}s, {Downed} of {_foes.Count} down, {Killed:P0} of the room's health taken, "
            + $"{Deaths} of {_drivers.Count} fell, {Spent:P0} of the company's health spent; "
            + (Doctrine ? $"on the leader's target {OnLeaders} beats, on the one nearest the leader {ByLeader}" : "each on its own nearest");
    }

    /// <summary>
    /// The room the dungeon makes worst: the spawner inside its box whose creatures add up to the most on the old scale, as
    /// many of each as it keeps, strongest first, up to <paramref name="most"/>. Empty when there is none.
    /// </summary>
    public static List<string> Worst(Map map, BotDungeon.Deep deep, int most)
    {
        List<string> best = [];
        var bestMight = 0.0;

        if (map == null || deep == null)
        {
            return best;
        }

        foreach (var spawner in map.GetItemsInBounds<BaseSpawner>(deep.Bounds))
        {
            if (spawner is not { Deleted: false } || spawner.Entries == null)
            {
                continue;
            }

            List<(string Name, double Might, int Count)> entries = [];

            for (var i = 0; i < spawner.Entries.Count; i++)
            {
                var entry = spawner.Entries[i];
                var might = BotDungeon.Might(entry?.SpawnedName);

                if (might > 0.0)
                {
                    entries.Add((entry.SpawnedName, might, Math.Max(1, entry.SpawnedMaxCount)));
                }
            }

            entries.Sort(static (a, b) => b.Might.CompareTo(a.Might));

            List<string> room = [];
            var total = 0.0;
            var cap = Math.Min(most, Math.Max(1, spawner.Count));

            for (var i = 0; i < entries.Count && room.Count < cap; i++)
            {
                for (var n = 0; n < entries[i].Count && room.Count < cap; n++)
                {
                    room.Add(entries[i].Name);
                    total += entries[i].Might;
                }
            }

            if (total > bestMight)
            {
                bestMight = total;
                best = room;
            }
        }

        return best;
    }
}
