using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Holds the modules, works out the order, starts them and says what happened.
///
/// <para>
/// Registration is a flat list and the order of that list means nothing — the sequence comes from what
/// each module says it needs. That is the whole difference from the first version, where the list
/// <em>was</em> the order, and where "why is this line here" was answered by a comment rather than by
/// anything a program could check.
/// </para>
///
/// <para>
/// <b>Failure is loud and contained.</b> A module that throws does not take the shard with it and does
/// not half-run: it is reported by name and anything that declared it as a requirement is refused with
/// the same clarity. The failure mode this exists to prevent is the one the first version had — a
/// subsystem that quietly read an empty list and then behaved plausibly for eight hours.
/// </para>
/// </summary>
public static class BotModules
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotModules));

    private static readonly List<BotModule> _all = [];

    private static readonly Dictionary<string, BotModule> _byName =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Modules registered, whether or not they are switched on.</summary>
    public static IReadOnlyList<BotModule> All => _all;

    /// <summary>
    /// Takes a module and reads its switch. Called from the entry point, in any order.
    ///
    /// The switch is read here rather than by the module itself for the reason given on
    /// <see cref="BotModule.Enabled"/>: a module cannot read its own configuration before it has
    /// started, and asking it to would put the answer behind the question.
    /// </summary>
    public static void Register(BotModule module)
    {
        if (module == null)
        {
            return;
        }

        if (_byName.TryGetValue(module.Name, out var clash))
        {
            logger.Error(
                "Two modules are both called {Name} ({First} and {Second}); the second is ignored",
                module.Name,
                clash.GetType().Name,
                module.GetType().Name
            );

            return;
        }

        module.Enabled = ServerConfiguration.GetOrUpdateSetting(
            $"bots.{module.Name.ToLowerInvariant()}.enabled",
            true
        );

        _all.Add(module);
        _byName[module.Name] = module;
    }

    /// <summary>
    /// Starts everything due in this phase, in dependency order. Returns how many actually started.
    /// </summary>
    public static int Start(BotPhase phase)
    {
        var due = Ordered(phase);
        var started = 0;

        for (var i = 0; i < due.Count; i++)
        {
            var module = due[i];

            if (module.Ready)
            {
                continue;
            }

            if (!module.Enabled)
            {
                logger.Information("Module {Name} is switched off", module.Name);
                continue;
            }

            var missing = Unmet(module);

            if (missing != null)
            {
                // Named on both sides on purpose. "Trade did not start" is a mystery; "Trade needs
                // Classes, which is not ready" is a sentence somebody can act on.
                logger.Error(
                    "Module {Name} needs {Missing}, which is not ready; {Name} did not start",
                    module.Name,
                    missing
                );

                continue;
            }

            try
            {
                module.Start();
                module.Ready = true;
                started++;
            }
            catch (Exception e)
            {
                logger.Error(e, "Module {Name} threw while starting; it is not running", module.Name);
            }
        }

        logger.Information(
            "Bot modules, {Phase}: {Started} of {Due} started",
            phase,
            started,
            due.Count
        );

        return started;
    }

    /// <summary>
    /// Puts this phase back to before it ran: every running module in it is reset and marked as needing
    /// to start again.
    ///
    /// <para>
    /// Both halves are needed and the second is the one that is easy to miss. Resetting alone leaves the
    /// modules marked ready, and <see cref="Start"/> skips anything already ready — so a world reload
    /// would have wiped every counter and then started nothing at all, leaving a shard with no
    /// population and no error to explain it. Clearing readiness is what makes a phase re-runnable.
    /// </para>
    ///
    /// <para>
    /// Phase-by-phase rather than everything at once, because the phases mean different things. A world
    /// reload is a second world, not a second process: what was read from a settings file is still true,
    /// and re-reading it would be work at best and a second set of overrides at worst.
    /// </para>
    /// </summary>
    public static void Rewind(BotPhase phase)
    {
        var rewound = 0;

        for (var i = 0; i < _all.Count; i++)
        {
            var module = _all[i];

            if (!module.Ready || module.Phase != phase)
            {
                continue;
            }

            try
            {
                module.Reset();
            }
            catch (Exception e)
            {
                logger.Error(e, "Module {Name} threw while resetting", module.Name);
            }

            module.Ready = false;
            rewound++;
        }

        if (rewound > 0)
        {
            logger.Information("Bot modules, {Phase}: {Count} rewound to start again", phase, rewound);
        }
    }

    /// <summary>The first requirement of this module that is not ready, or null if all of them are.</summary>
    private static string Unmet(BotModule module)
    {
        var requires = module.Requires;

        for (var i = 0; i < requires.Length; i++)
        {
            var name = requires[i];

            // An unknown name is unmet as well, and says something different: not "it failed" but "you
            // are depending on something that does not exist", which is a typo rather than a fault.
            if (!_byName.TryGetValue(name, out var needed) || !needed.Ready)
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>
    /// The modules of this phase, each after everything it requires from the same phase.
    ///
    /// Only within the phase: a requirement that lives in an earlier phase is already satisfied by the
    /// phases themselves, and one that lives in a later phase cannot be satisfied at all — which is
    /// caught by the readiness check rather than by the sort, and reported as what it is.
    /// </summary>
    private static List<BotModule> Ordered(BotPhase phase)
    {
        List<BotModule> ordered = new(_all.Count);
        Dictionary<string, int> state = new(_all.Count, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < _all.Count; i++)
        {
            if (_all[i].Phase == phase)
            {
                Visit(_all[i], phase, state, ordered);
            }
        }

        return ordered;
    }

    private const int Visiting = 1;

    private const int Placed = 2;

    private static void Visit(
        BotModule module,
        BotPhase phase,
        Dictionary<string, int> state,
        List<BotModule> ordered
    )
    {
        if (state.TryGetValue(module.Name, out var seen))
        {
            if (seen == Visiting)
            {
                // A ring. Reported and then broken rather than followed, and the consequence is
                // deliberately mild: the modules in the ring will fail their readiness check in turn and
                // be refused by name. A cycle is a design mistake, and the useful response to one is a
                // sentence, not a stack overflow.
                logger.Error(
                    "Module {Name} is part of a circular dependency; the ring is broken here and the modules in it will not start",
                    module.Name
                );
            }

            return;
        }

        state[module.Name] = Visiting;

        var requires = module.Requires;

        for (var i = 0; i < requires.Length; i++)
        {
            if (_byName.TryGetValue(requires[i], out var needed) && needed.Phase == phase)
            {
                Visit(needed, phase, state, ordered);
            }
        }

        state[module.Name] = Placed;
        ordered.Add(module);
    }

    /// <summary>One line about what is running, for the boot log and for the summary.</summary>
    public static string Describe()
    {
        var running = 0;
        var off = 0;
        var broken = 0;

        for (var i = 0; i < _all.Count; i++)
        {
            var module = _all[i];

            if (module.Ready)
            {
                running++;
            }
            else if (!module.Enabled)
            {
                off++;
            }
            else
            {
                broken++;
            }
        }

        return $"{running} modules running, {off} switched off, {broken} that should be running and are not";
    }
}
