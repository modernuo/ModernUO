using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Server.Logging;
using Server.Saves;

namespace Server.Engines.AdvancedSearch;

/// <summary>
/// Finishes a search on the game loop once the workers have drained: collects what the ignore sources
/// hide, runs the full filter on the entities the workers deferred, then publishes the results. The work
/// is cut into slices of at most <see cref="LoopBudgetMs" /> each, one slice per timer tick, so a large
/// remainder never stalls the loop.
/// </summary>
public sealed class AdvancedSearchLoopPhase
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(AdvancedSearchLoopPhase));

    // Stopwatch.GetTimestamp is cheap but not free; sampling it per entity would cost more than most entities.
    private const int BudgetCheckInterval = 64;

    public static int LoopBudgetMs { get; private set; } = 2;

    private readonly Mobile _from;
    private readonly AdvancedSearchGump _gump;
    private readonly AdvancedSearchFilter _filter;
    private readonly WorldLocation _origin;
    private readonly AdvancedSearchConditions.Cache _predicates;
    private readonly ConcurrentQueue<AdvancedSearchResult> _results;
    private readonly ConcurrentQueue<IEntity> _deferred;
    private readonly ConcurrentQueue<IEntity> _ignoreSources;
    private readonly bool _restoreAutoSave;
    private readonly HashSet<IEntity> _ignored = [];

    private bool _finished;
    private int _sliceCount;

    public static void Configure()
    {
        LoopBudgetMs = Math.Max(ServerConfiguration.GetOrUpdateSetting("advancedSearch.loopBudgetMs", 2), 1);
    }

    public AdvancedSearchLoopPhase(
        Mobile from,
        AdvancedSearchGump gump,
        AdvancedSearchFilter filter,
        WorldLocation origin,
        AdvancedSearchConditions.Cache predicates,
        ConcurrentQueue<AdvancedSearchResult> results,
        ConcurrentQueue<IEntity> deferred,
        ConcurrentQueue<IEntity> ignoreSources,
        bool restoreAutoSave
    )
    {
        _from = from;
        _gump = gump;
        _filter = filter;
        _origin = origin;
        _predicates = predicates;
        _results = results;
        _deferred = deferred;
        _ignoreSources = ignoreSources;
        _restoreAutoSave = restoreAutoSave;
    }

    public void Start()
    {
        if (!RunSlice(Stopwatch.Frequency * LoopBudgetMs / 1000))
        {
            Timer.StartTimer(Start);
        }
    }

    /// <summary>Runs one slice. Returns true once the search has been published and released.</summary>
    internal bool RunSlice(long budgetTimestampTicks)
    {
        if (_finished)
        {
            return true;
        }

        var start = Stopwatch.GetTimestamp();
        _sliceCount = 0;

        var published = false;

        try
        {
            if (!DrainIgnoreSources(start, budgetTimestampTicks) || !DrainDeferred(start, budgetTimestampTicks))
            {
                return false;
            }

            Publish();
            published = true;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "AdvancedSearch: loop phase failed");
        }

        Finish(published);
        return true;
    }

    private bool OutOfBudget(long start, long budget) =>
        ++_sliceCount % BudgetCheckInterval == 0 && Stopwatch.GetTimestamp() - start >= budget;

    private bool DrainIgnoreSources(long start, long budget)
    {
        while (_ignoreSources.TryDequeue(out var source))
        {
            try
            {
                // An ignore source hides entities only when it would itself pass the placement filters.
                if (_filter.HideValidInternalMap && source?.Deleted == false &&
                    AdvancedSearchMatcher.PassesPlacement(source, _filter, _origin))
                {
                    AdvancedSearchMatcher.CollectIgnored(source, _ignored);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "AdvancedSearch: collecting ignored entities threw for {Entity}; skipping", source);
            }

            if (OutOfBudget(start, budget))
            {
                return false;
            }
        }

        return true;
    }

    private bool DrainDeferred(long start, long budget)
    {
        while (_deferred.TryDequeue(out var entity))
        {
            var outcome = AdvancedSearchMatcher.Evaluate(entity, _filter, _origin, _predicates, true, out var result);

            if (outcome == MatchOutcome.Match)
            {
                _results.Enqueue(result);
            }

            if (OutOfBudget(start, budget))
            {
                return false;
            }
        }

        return true;
    }

    private void Publish()
    {
        var list = new List<AdvancedSearchResult>(_results.Count);

        while (_results.TryDequeue(out var result))
        {
            var entity = result.Entity;
            if (entity?.Deleted == false && !_ignored.Contains(entity))
            {
                list.Add(result);
            }
        }

        _ignored.Clear();

        list.Sort(AdvancedSearchResultSerialComparer.Instance);
        _gump.SearchResults = list.ToArray();
    }

    private void Finish(bool published)
    {
        _finished = true;

        // Release the guard only after the autosave restore lands, so a new search cannot capture the
        // disabled state as its own restore value.
        try
        {
            AutoSave.SavesEnabled = _restoreAutoSave;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "AdvancedSearch: could not restore autosave");
        }
        finally
        {
            AdvancedSearchGump.EndSearch();
        }

        if (!published)
        {
            return;
        }

        try
        {
            _gump.Resend(_from);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "AdvancedSearch: could not send the results gump");
        }
    }
}
