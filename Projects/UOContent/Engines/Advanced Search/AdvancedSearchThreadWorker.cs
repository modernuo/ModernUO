using System;
using System.Collections.Concurrent;
using System.Threading;
using Server.Logging;

namespace Server.Engines.AdvancedSearch;

/// <summary>
/// Runs the search filter off the loop over the entities <see cref="AdvancedSearchGump" /> pushes to it.
/// Each entity is evaluated with <see cref="AdvancedSearchMatcher" /> in worker mode, which reads only
/// members proven pure for the entity's runtime type: a decided match goes to the results queue, and
/// anything that needs a loop-only read goes to the deferred queue for <see cref="AdvancedSearchLoopPhase" />.
/// Ignore sources are queued for the loop phase as well, since collecting what they hide enumerates live lists.
/// </summary>
public class AdvancedSearchThreadWorker
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(AdvancedSearchThreadWorker));

    private readonly Thread _thread;
    private readonly AutoResetEvent _startEvent; // Main thread tells the thread to start working
    private readonly AutoResetEvent _stopEvent; // Main thread waits for the worker finish draining
    private bool _pause;
    private bool _exit;
    private readonly ConcurrentQueue<IEntity> _entities;
    private ConcurrentQueue<AdvancedSearchResult> _results;
    private ConcurrentQueue<IEntity> _deferred;
    private ConcurrentQueue<IEntity> _ignoreSources;
    private WorldLocation _worldLocation;
    private AdvancedSearchFilter _filter;
    private AdvancedSearchConditions.Cache _predicates;

    public AdvancedSearchThreadWorker()
    {
        _startEvent = new AutoResetEvent(false);
        _stopEvent = new AutoResetEvent(false);
        _entities = new ConcurrentQueue<IEntity>();
        _thread = new Thread(Execute)
        {
            IsBackground = true
        };
        _thread.Start(this);
    }

    /// <param name="predicates">
    /// Compiled property-test memo for this search. Shared across the workers so a type is
    /// compiled once per search rather than once per worker. It carries the searcher's access level,
    /// so it is required.
    /// </param>
    public void Wake(
        WorldLocation worldLocation,
        AdvancedSearchFilter filter,
        ConcurrentQueue<AdvancedSearchResult> results,
        ConcurrentQueue<IEntity> deferred,
        ConcurrentQueue<IEntity> ignoreSources,
        AdvancedSearchConditions.Cache predicates
    )
    {
        _worldLocation = worldLocation;
        _filter = filter;
        _results = results ?? throw new ArgumentNullException(nameof(results));
        _deferred = deferred ?? throw new ArgumentNullException(nameof(deferred));
        _ignoreSources = ignoreSources ?? throw new ArgumentNullException(nameof(ignoreSources));
        _predicates = predicates ?? throw new ArgumentNullException(nameof(predicates));
        _startEvent.Set();
    }

    public void Sleep()
    {
        Volatile.Write(ref _pause, true);
        _stopEvent.WaitOne();
    }

    public void Exit()
    {
        Volatile.Write(ref _exit, true);

        _startEvent.Set();

        // Tolerate a worker that has already terminated (e.g. Core.Closing raced us) so
        // shutdown can't deadlock waiting on a stopEvent that will never be set.
        if (_thread.IsAlive)
        {
            Sleep();
        }
    }

    public void Push(IEntity entity)
    {
        _entities.Enqueue(entity);
    }

    private static void Execute(object obj)
    {
        var worker = (AdvancedSearchThreadWorker)obj;

        var reader = worker._entities;

        while (worker._startEvent.WaitOne())
        {
            while (true)
            {
                var pauseRequested = Volatile.Read(ref worker._pause);
                if (reader.TryDequeue(out var entity))
                {
                    worker.DoEntitySearch(entity);
                }
                else if (pauseRequested) // Break when finished
                {
                    worker._results = null;
                    worker._deferred = null;
                    worker._ignoreSources = null;
                    worker._filter = null;
                    worker._predicates = null; // the memo belongs to one search
                    break;
                }
                else
                {
                    // Transiently empty but not yet paused: yield rather than busy-spin.
                    Thread.Yield();
                }
            }

            // The owning thread may start another cycle the moment _stopEvent is set (Exit does exactly
            // that). Clear _pause and sample the exit condition before signaling, or the new cycle's
            // pause request is clobbered / its Sleep orphaned. Matches SerializationThreadWorker.
            var exiting = Core.Closing || Volatile.Read(ref worker._exit);
            Volatile.Write(ref worker._pause, false);

            worker._stopEvent.Set(); // Allow the main thread to continue now that we are finished

            if (exiting)
            {
                return;
            }
        }
    }

    private void DoEntitySearch(IEntity entity)
    {
        var filter = _filter;

        // A worker that was never woken, or has gone back to sleep, has no filter; an entity it dequeues
        // then bails here.
        if (entity == null || filter == null || entity.Deleted)
        {
            return;
        }

        try
        {
            if (filter.HideValidInternalMap && AdvancedSearchMatcher.IsIgnoreSource(entity))
            {
                _ignoreSources.Enqueue(entity);
            }

            var outcome = AdvancedSearchMatcher.Evaluate(entity, filter, _worldLocation, _predicates, false, out var result);

            if (outcome == MatchOutcome.Match)
            {
                _results.Enqueue(result);
            }
            else if (outcome == MatchOutcome.Defer)
            {
                _deferred.Enqueue(entity);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "AdvancedSearch: filter threw for {Entity}; skipping", entity);
        }
    }
}
