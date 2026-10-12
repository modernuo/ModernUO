using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// A murdered bot's body found: the finder searches for the killer with Detect Hidden, calls out, and tells the Baron.
///
/// <para>
/// <b>Patrick's order of 17.09.2026: a bot that finds a body tries to find the killer with its detecting skill and tells
/// the Baron.</b> Every murder leaves a scene here (<see cref="BotOutlaw.Murdered"/>). On every <see cref="SweepMs"/> the
/// nearest law-abiding bot within <see cref="FindWithin"/> of an unreported body finds it: it searches around itself with
/// Detect Hidden (<see cref="BotShadow.Search"/>) — with none of the skill that reaches no tile at all, and it is how the
/// skill is learned — and names the killer if the search brought it out of hiding or the killer stands in plain sight within
/// <see cref="BotLawful.Sight"/>. A named killer is made known to the Baron (<see cref="BotOutlaw.Tell"/>), who raises
/// patrols only against murderers he knows of and can see (<see cref="BotOutlaw.AtLarge"/>); an unnamed one is a murder by an
/// unknown hand, told and counted. A body nobody comes to in <see cref="SceneMs"/> is counted too.
/// </para>
///
/// <para>
/// It also presses a robber that has just gone through a body into lying low (<see cref="BotLieLow"/>), a sweep after the
/// robbery ends, because a deed cannot press its own bot while it is finishing.
/// </para>
/// </summary>
public static class BotInquest
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotInquest));

    public static bool Running { get; set; } = true;

    /// <summary>How often the bodies are looked at. Five seconds.</summary>
    public static int SweepMs { get; set; } = 5000;

    /// <summary>How long a body waits to be found before the murder is counted as nobody's business. Fifteen minutes.</summary>
    public static int SceneMs { get; set; } = 900000;

    /// <summary>How near a bot must come to a body to find it, in tiles.</summary>
    public static int FindWithin { get; set; } = 6;

    /// <summary>Murders that left a body here.</summary>
    public static long Scenes { get; private set; }

    /// <summary>Bodies found.</summary>
    public static long Found { get; private set; }

    /// <summary>Of those, the killer brought out of hiding by the finder's search.</summary>
    public static long Revealed { get; private set; }

    /// <summary>Of those, the killer seen in plain sight by the finder.</summary>
    public static long Seen { get; private set; }

    /// <summary>Of those, told as a murder by an unknown hand.</summary>
    public static long Unsolved { get; private set; }

    /// <summary>Bodies nobody came to in time.</summary>
    public static long Unfound { get; private set; }

    /// <summary>Robbers pressed into lying low.</summary>
    public static long LyingLow { get; private set; }

    private sealed class Scene
    {
        public BotMobile Killer;

        public string Victim;

        public Map Map;

        public Point3D Where;

        public long Tick;
    }

    private static readonly List<Scene> _scenes = [];

    private static readonly List<(BotMobile Robber, Map Map, Point3D Body)> _lieLow = [];

    private static long _sweptTick;

    /// <summary>A murder: the body lies where the victim fell. From <see cref="BotOutlaw.Murdered"/>.</summary>
    public static void Murder(BotMobile killer, BotMobile victim)
    {
        if (!Running || killer == null || victim?.Map is not { } map || map == Map.Internal)
        {
            return;
        }

        Scenes++;

        _scenes.Add(new Scene { Killer = killer, Victim = victim.Name, Map = map, Where = victim.Location, Tick = Core.TickCount });

        if (_scenes.Count > 32)
        {
            _scenes.RemoveAt(0);
        }
    }

    /// <summary>A robber that has just gone through a body, to be pressed into lying low on the next sweep. From <see cref="BotRob"/>.</summary>
    public static void LieLow(BotMobile robber, Map map, Point3D body)
    {
        if (Running && robber != null && map != null)
        {
            _lieLow.Add((robber, map, body));
        }
    }

    /// <summary>One look at the bodies and the robbers, from <c>BotOutlaw</c>'s clock.</summary>
    public static void Beat(long now)
    {
        if (!Running || now - _sweptTick < SweepMs)
        {
            return;
        }

        _sweptTick = now;

        for (var i = 0; i < _lieLow.Count; i++)
        {
            var (robber, map, body) = _lieLow[i];

            if (robber is not { Deleted: false, Alive: true } || robber.Map != map || BotOutlaw.Jailed(robber) || !BotShadow.CanHide(robber))
            {
                continue;
            }

            if (BotWill.Press(robber, new BotLieLow(map, body, robber.Location), "getting away from the body to lie low"))
            {
                LyingLow++;
            }
        }

        _lieLow.Clear();

        for (var i = _scenes.Count - 1; i >= 0; i--)
        {
            var scene = _scenes[i];

            if (now - scene.Tick >= SceneMs)
            {
                Unfound++;
                _scenes.RemoveAt(i);

                logger.Information(
                    "Nobody came to the body of {Victim} at ({X}, {Y}) in {Minutes} minutes; the murder goes untold",
                    scene.Victim,
                    scene.Where.X,
                    scene.Where.Y,
                    SceneMs / 60000
                );

                continue;
            }

            var finder = Finder(scene);

            if (finder == null)
            {
                continue;
            }

            _scenes.RemoveAt(i);
            Report(scene, finder, now);
        }
    }

    /// <summary>The nearest law-abiding bot within reach of the body, not the killer and not the dead one risen, or null.</summary>
    private static BotMobile Finder(Scene scene)
    {
        BotMobile best = null;
        var bestAway = double.MaxValue;

        foreach (var m in scene.Map.GetMobilesInRange<BotMobile>(scene.Where, FindWithin))
        {
            if (m == scene.Killer || m is not { Deleted: false, Alive: true, Fallen: false, Hidden: false } || m.Name == scene.Victim
                || BotOutlaw.Outlaw(m) || BotOutlaw.Jailed(m) || BotUnderworld.Member(m))
            {
                continue;
            }

            var away = m.GetDistanceToSqrt(scene.Where);

            if (away < bestAway)
            {
                best = m;
                bestAway = away;
            }
        }

        return best;
    }

    private static void Report(Scene scene, BotMobile finder, long now)
    {
        Found++;

        var killer = scene.Killer;
        var there = killer is { Deleted: false, Alive: true } && killer.Map == scene.Map;
        var wasHidden = there && killer.Hidden;
        var skill = finder.Skills.DetectHidden.Value;
        var searched = BotShadow.Search(finder);
        var revealed = searched && wasHidden && !killer.Hidden;
        var seen = !revealed && there && !killer.Hidden && finder.InRange(killer, BotLawful.Sight);

        string outcome;

        if (revealed)
        {
            Revealed++;
            outcome = $"brought {killer.Name} out of hiding at ({killer.X}, {killer.Y})";
        }
        else if (seen)
        {
            Seen++;
            outcome = $"saw {killer.Name} {(int)finder.GetDistanceToSqrt(killer)} tiles off";
        }
        else
        {
            Unsolved++;
            outcome = "found nobody";
        }

        if ((revealed || seen) && BotOutlaw.IsRed(killer))
        {
            BotOutlaw.Tell(killer, finder.Name);
        }

        if (revealed || seen)
        {
            BotVoice.Aloud(finder, $"Murder! {scene.Victim} lies dead here, and {killer.Name} did it!");
        }
        else
        {
            BotVoice.Aloud(finder, $"Murder! {scene.Victim} lies dead here!");
        }

        logger.Information(
            "{Finder} found the body of {Victim} at ({X}, {Y}) {Minutes:F1} minutes after the murder, {Searched} and {Outcome}, and told the Baron {Told}",
            finder.Name,
            scene.Victim,
            scene.Where.X,
            scene.Where.Y,
            (now - scene.Tick) / 60000.0,
            searched ? $"searched with Detect Hidden {skill:F1} ({(int)(skill / 10)} tiles)" : "could not search just then",
            outcome,
            revealed || seen ? $"that {killer.Name} did it" : "of a murder by an unknown hand"
        );
    }

    public static string Describe() =>
        !Running
            ? "nobody looks for a murderer"
            : $"{Scenes} bodies left by murder: {Found} found ({Revealed} killers brought out of hiding by the finder, {Seen} seen in plain sight, {Unsolved} unknown), {Unfound} nobody came to; {LyingLow} robbers sent to lie low; {BotLieLow.Describe()}";

    public static void Forget()
    {
        Scenes = 0;
        Found = 0;
        Revealed = 0;
        Seen = 0;
        Unsolved = 0;
        Unfound = 0;
        LyingLow = 0;
        _scenes.Clear();
        _lieLow.Clear();
        _sweptTick = 0;
        BotLieLow.Forget();
    }
}
