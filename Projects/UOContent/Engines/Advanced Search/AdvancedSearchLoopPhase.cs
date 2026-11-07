using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Server.Logging;
using Server.Saves;

namespace Server.Engines.AdvancedSearch;

/// <summary>
/// Finishes a search on the game loop once the workers have drained: collects what the ignore sources
/// hide, runs the full filter on the entities the workers deferred, then merges, filters and publishes the
/// results. The work is cut into slices of at most <see cref="LoopBudgetMs" /> each, one slice per timer
/// tick, so neither a large deferred remainder nor a large result set stalls the loop. The loop's own results
/// are sorted on the thread pool between the drain and the merge, and the phase resumes when that sort posts
/// back.
/// </summary>
public sealed class AdvancedSearchLoopPhase
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(AdvancedSearchLoopPhase));

    // Stopwatch.GetTimestamp is cheap but not free; sampling it per entity would cost more than most entities.
    private const int BudgetCheckInterval = 64;

    // A block copy of this many references takes microseconds, so the budget is checked once per block.
    private const int CopyBlockSize = 4096;

    private enum Stage
    {
        Drain,
        Sort,
        Allocate,
        Merge,
        Copy,
        Publish
    }

    public static int LoopBudgetMs { get; private set; } = 2;

    private readonly Mobile _from;
    private readonly AdvancedSearchGump _gump;
    private readonly AdvancedSearchFilter _filter;
    private readonly WorldLocation _origin;
    private readonly AdvancedSearchConditions.Cache _predicates;
    private readonly AdvancedSearchResult[] _workerResults;
    private readonly List<AdvancedSearchResult> _loopResults = [];
    private readonly ConcurrentQueue<IEntity> _deferred;
    private readonly ConcurrentQueue<IEntity> _ignoreSources;
    private readonly bool _restoreAutoSave;
    private readonly HashSet<IEntity> _ignored = [];

    private bool _finished;
    private bool _selfScheduled;
    private Stage _stage;
    private int _entitiesThisSlice;

    private AdvancedSearchResult[] _merged;
    private int _mergedCount;
    private int _workerCursor;
    private int _loopCursor;
    private AdvancedSearchResult[] _final;
    private int _copied;

    /// <summary>Runs the loop-result sort off the loop. Test seam.</summary>
    internal Action<Action> RunOffLoop { get; init; } = static work => ThreadPool.QueueUserWorkItem(static w => w(), work, false);

    /// <summary>Hands the sort's completion back to the loop. Test seam.</summary>
    internal Action<Action> PostToLoop { get; init; } = static resume => Core.LoopContext.Post(resume);

    public static void Configure()
    {
        LoopBudgetMs = Math.Max(ServerConfiguration.GetOrUpdateSetting("advancedSearch.loopBudgetMs", 2), 1);
    }

    /// <param name="sortedWorkerResults">
    /// The workers' matches, already sorted with <see cref="AdvancedSearchResultSerialComparer" /> off the
    /// loop by the caller (serials never change, so the sort needs no game state).
    /// </param>
    public AdvancedSearchLoopPhase(
        Mobile from,
        AdvancedSearchGump gump,
        AdvancedSearchFilter filter,
        WorldLocation origin,
        AdvancedSearchConditions.Cache predicates,
        AdvancedSearchResult[] sortedWorkerResults,
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
        _workerResults = sortedWorkerResults ?? [];
        _deferred = deferred;
        _ignoreSources = ignoreSources;
        _restoreAutoSave = restoreAutoSave;
    }

    public void Start()
    {
        _selfScheduled = true;

        // While the sort is out, its completion resumes the phase instead of a timer.
        if (!RunSlice(Stopwatch.Frequency * LoopBudgetMs / 1000) && _stage != Stage.Sort)
        {
            Timer.StartTimer(Start);
        }
    }

    /// <summary>
    /// Runs one slice. Returns true once the search has been published and released. Each stage after the
    /// drains begins in a fresh slice, since the slice that ended the previous stage may have spent its budget.
    /// </summary>
    internal bool RunSlice(long budgetTimestampTicks)
    {
        if (_finished)
        {
            return true;
        }

        var start = Stopwatch.GetTimestamp();
        _entitiesThisSlice = 0;

        var published = false;

        try
        {
            switch (_stage)
            {
                case Stage.Drain:
                    {
                        if (DrainIgnoreSources(start, budgetTimestampTicks) &&
                            DrainDeferred(start, budgetTimestampTicks))
                        {
                            BeginSort();
                        }

                        return false;
                    }
                case Stage.Sort:
                    {
                        return false;
                    }
                case Stage.Allocate:
                    {
                        _merged = new AdvancedSearchResult[_workerResults.Length + _loopResults.Count];
                        _stage = Stage.Merge;
                        return false;
                    }
                case Stage.Merge:
                    {
                        if (!Merge(start, budgetTimestampTicks))
                        {
                            return false;
                        }

                        _ignored.Clear();

                        if (_mergedCount == _merged.Length)
                        {
                            _final = _merged;
                            _stage = Stage.Publish;
                        }
                        else
                        {
                            _stage = Stage.Copy;
                        }

                        return false;
                    }
                case Stage.Copy:
                    {
                        if (CopyToFinal(start, budgetTimestampTicks))
                        {
                            _stage = Stage.Publish;
                        }

                        return false;
                    }
                default:
                    {
                        _gump.SearchResults = _final;
                        published = true;
                        break;
                    }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "AdvancedSearch: loop phase failed");
        }

        Finish(published);
        return true;
    }

    private bool OutOfBudget(long start, long budget) =>
        ++_entitiesThisSlice % BudgetCheckInterval == 0 && Stopwatch.GetTimestamp() - start >= budget;

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
                _loopResults.Add(result);
            }

            if (OutOfBudget(start, budget))
            {
                return false;
            }
        }

        return true;
    }

    private void BeginSort()
    {
        if (_loopResults.Count < 2)
        {
            _stage = Stage.Allocate;
            return;
        }

        _stage = Stage.Sort;
        RunOffLoop(SortLoopResults);
    }

    // Thread pool. The comparer reads only Entity.Serial, which never changes, and the loop leaves
    // _loopResults alone until the completion below runs on it.
    private void SortLoopResults()
    {
        Exception failure = null;

        try
        {
            _loopResults.Sort(AdvancedSearchResultSerialComparer.Instance);
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        try
        {
            PostToLoop(() => ResumeAfterSort(failure));
        }
        catch (Exception ex)
        {
            // A throw here would terminate the process; nothing else can reach the loop.
            _logger.Error(ex, "AdvancedSearch: could not hand the sorted results back to the loop");
        }
    }

    private void ResumeAfterSort(Exception failure)
    {
        if (_finished)
        {
            return;
        }

        if (failure != null)
        {
            _logger.Warning(failure, "AdvancedSearch: sorting the loop results failed");
            Finish(false);
            return;
        }

        _stage = Stage.Allocate;

        if (_selfScheduled)
        {
            Start();
        }
    }

    // Merges the caller-sorted worker results with the loop's own (normally few) results, dropping entities
    // deleted since they matched and entities an ignore source hides.
    private bool Merge(long start, long budget)
    {
        var comparer = AdvancedSearchResultSerialComparer.Instance;

        while (_workerCursor < _workerResults.Length || _loopCursor < _loopResults.Count)
        {
            // Worker results win ties, keeping equal serials in a stable order.
            var takeWorker = _loopCursor >= _loopResults.Count ||
                             _workerCursor < _workerResults.Length &&
                             comparer.Compare(_workerResults[_workerCursor], _loopResults[_loopCursor]) <= 0;

            var result = takeWorker ? _workerResults[_workerCursor++] : _loopResults[_loopCursor++];
            var entity = result.Entity;

            if (entity?.Deleted == false && !_ignored.Contains(entity))
            {
                _merged[_mergedCount++] = result;
            }

            if (OutOfBudget(start, budget))
            {
                return false;
            }
        }

        return true;
    }

    private bool CopyToFinal(long start, long budget)
    {
        _final ??= new AdvancedSearchResult[_mergedCount];

        while (_copied < _mergedCount)
        {
            var count = Math.Min(CopyBlockSize, _mergedCount - _copied);
            Array.Copy(_merged, _copied, _final, _copied, count);
            _copied += count;

            if (_copied < _mergedCount && Stopwatch.GetTimestamp() - start >= budget)
            {
                return false;
            }
        }

        return true;
    }

    private void Finish(bool published)
    {
        _finished = true;
        _merged = null;

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
