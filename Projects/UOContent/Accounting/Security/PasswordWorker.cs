/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PasswordWorker.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Threading;
using Server.Logging;
using Server.Network;

namespace Server.Accounting.Security;

/// <summary>
/// Work handed to the password thread, which reads no game state and writes none.
///
/// Verify and hash are independently optional: a login verifies and may rehash, an explicit change
/// only hashes.
/// </summary>
internal sealed class PasswordJob
{
    public Account Account;

    /// <summary>Ties the job to a connection. Null when the work is not gated on one, such as a
    /// password change by an admin.</summary>
    public NetState State;

    /// <summary>Hash to verify against, with <see cref="VerifyPhrase"/>.</summary>
    public string StoredHash;

    /// <summary>Algorithm <see cref="StoredHash"/> was written with. Both algorithms are resolved on
    /// the loop; <c>AccountSecurity.CurrentAlgorithm</c> is mutable state the worker must not read.</summary>
    public PasswordProtectionAlgorithm StoredAlgorithm;

    /// <summary>Phrase to verify, or null to skip verification.</summary>
    public string VerifyPhrase;

    /// <summary>Phrase to hash, or null when nothing needs writing.</summary>
    public string HashPhrase;

    public PasswordProtectionAlgorithm TargetAlgorithm;

    /// <summary>Runs on the game loop with the result. Free to touch game state.</summary>
    public Action<PasswordJob, PasswordOutcome> OnComplete;
}

internal readonly struct PasswordOutcome
{
    /// <summary>True when no verification was asked for, or it succeeded.</summary>
    public readonly bool Verified;

    /// <summary>The derived hash, or null when nothing was hashed or verification failed.</summary>
    public readonly string Hash;

    public PasswordOutcome(bool verified, string hash)
    {
        Verified = verified;
        Hash = hash;
    }
}

/// <summary>
/// Runs password hashing off the game loop. An Argon2 verify costs ~8.9 ms of frozen world per
/// login attempt, successful or not.
///
/// Exactly one worker, and that is load-bearing three times over. It cannot cost the loop more than
/// an inline verify under any scheduling regime, because at worst it takes an equal share of one
/// core -- which is what lets the measurement hold on hardware we cannot inspect. It caps live
/// hashing arenas at one. And writes apply in dispatch order only because a single thread drains
/// FIFO, so a second would need ordering reintroduced.
///
/// ~110 verifies/sec, which is ample: only loop time matters, not login latency.
/// </summary>
internal sealed class PasswordWorker
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(PasswordWorker));

    /// <summary>
    /// Backstop, not a flood defense. <c>SentFirstPacket</c> holds a connection to one pending
    /// verify and the engine caps connections at 4096 (<c>NetState.Network.cs</c>), so this matches
    /// that bound and can only trip if that invariant breaks. A cap low enough to blunt an attack
    /// would reject real players first; flood defense belongs at the connection layer.
    /// </summary>
    private const int MaxPending = 4096;

    // Nothing signals the worker when a save freeze ends, so it re-checks on this interval -- but
    // only while a save is in progress, never in steady state.
    private const int SaveGatePollMs = 50;

    private static PasswordWorker _instance;

    // Needs a spare core to move work to, which a 1-2 core host does not have. Off in DEBUG, where
    // logins are rare and the inline path is easier to follow.
    internal static readonly bool Enabled =
#if DEBUG
        false;
#else
        Environment.ProcessorCount >= 4;
#endif

    private readonly Thread _thread;
    private readonly AutoResetEvent _work = new(false);
    private readonly ConcurrentQueue<PasswordJob> _queue = [];

    private int _pending;
    private volatile bool _exit;

    private PasswordWorker()
    {
        _thread = new Thread(Execute)
        {
            IsBackground = true,
            Name = "Password Worker"
        };

        _thread.Start();
    }

    private static PasswordWorker Instance => _instance ??= new PasswordWorker();

    /// <summary>Queues a job. False when full, and the caller must then reject without verifying.</summary>
    internal static bool TryEnqueue(PasswordJob job) => Instance.TryEnqueueCore(job);

    private bool TryEnqueueCore(PasswordJob job)
    {
        if (Volatile.Read(ref _pending) >= MaxPending)
        {
            return false;
        }

        Interlocked.Increment(ref _pending);
        _queue.Enqueue(job);
        _work.Set();

        return true;
    }

    /// <summary>
    /// Checked before each job, which bounds a save overlap to whichever hash was already running:
    /// the freeze holds the loop, so nothing new can be queued during it. PendingSave counts too --
    /// the serialization threads are already awake and spinning on an empty queue by then.
    /// </summary>
    private static bool CanRunNow() => World.WorldState is WorldState.Running or WorldState.WritingSave;

    private void Execute()
    {
        while (!_exit)
        {
            if (_queue.IsEmpty)
            {
                // A kernel block at zero CPU. Set() during a hash leaves the event signalled, so a
                // wake arriving mid-job is not lost.
                _work.WaitOne();
                continue;
            }

            if (!CanRunNow())
            {
                _work.WaitOne(SaveGatePollMs);
                continue;
            }

            if (!_queue.TryDequeue(out var job))
            {
                continue;
            }

            Interlocked.Decrement(ref _pending);

            // Gone while it waited: skip it rather than hash for a verdict nobody receives. Running
            // only goes true -> false, so a stale read wastes a hash but never skips a live one. A
            // null State is a job with no connection to lose, and still runs.
            if (job.State?.Running == false)
            {
                continue;
            }

            PasswordOutcome outcome;

            try
            {
                outcome = Compute(job);
            }
            catch (Exception ex)
            {
                // A verdict must still come back, or the connection never gets a reply.
                logger.Error(ex, "Password work failed for {Username}", job.Account?.Username);
                outcome = new PasswordOutcome(false, null);
            }

            Core.LoopContext.Post(() => Apply(job, outcome));
        }
    }

    private static PasswordOutcome Compute(PasswordJob job)
    {
        if (job.VerifyPhrase != null &&
            !AccountSecurity.GetPasswordProtection(job.StoredAlgorithm)
                .ValidatePassword(job.StoredHash, job.VerifyPhrase))
        {
            return new PasswordOutcome(false, null);
        }

        return new PasswordOutcome(
            true,
            job.HashPhrase == null
                ? null : AccountSecurity.GetPasswordProtection(job.TargetAlgorithm).EncryptPassword(job.HashPhrase)
        );
    }

    private static void Apply(PasswordJob job, PasswordOutcome outcome)
    {
        // Re-checked: a connection can drop while the result sits in the loop queue.
        if (job.State?.Running == false)
        {
            return;
        }

        if (outcome.Verified && outcome.Hash != null)
        {
            job.Account.ApplyPasswordWrite(outcome.Hash, job.TargetAlgorithm);
        }

        job.OnComplete?.Invoke(job, outcome);
    }

    /// <summary>
    /// Sets a password, off the loop where available and inline otherwise, invoking
    /// <paramref name="onDone"/> on the loop either way.
    ///
    /// Confirm from <paramref name="onDone"/>, not the call site: off-loop the write has not
    /// happened when this returns.
    /// </summary>
    internal static void SetPassword(Account account, string plainPassword, Action<bool> onDone)
    {
        if (!Enabled)
        {
            account.SetPassword(plainPassword);
            onDone?.Invoke(true);
            return;
        }

        var job = new PasswordJob
        {
            Account = account,
            HashPhrase = account.GetRehashPhrase(plainPassword),
            TargetAlgorithm = AccountSecurity.CurrentAlgorithm,
            OnComplete = (_, outcome) => onDone?.Invoke(outcome.Hash != null)
        };

        if (!TryEnqueue(job))
        {
            // Saturated. Unlike a login, a password change must not be dropped, so it pays the
            // hash on the loop instead.
            account.SetPassword(plainPassword);
            onDone?.Invoke(true);
        }
    }

    /// <summary>Runs a job on the calling thread. The seam the tests drive.</summary>
    internal static PasswordOutcome ComputeInline(PasswordJob job) => Compute(job);

    /// <summary>
    /// Stops the worker on shutdown or crash. Pending jobs are dropped rather than finished:
    /// nothing saves the world after this point, so a write applied here would reach no disk.
    ///
    /// Draining the loop context is not this type's business either. That belongs in the core
    /// shutdown path, before subscriber events run -- a subscriber pumping the shared context would
    /// execute other subscribers' work at an arbitrary point in the event order.
    /// </summary>
    internal static void Stop() => _instance?.StopThread();

    // HandleClosed skips InvokeShutdown when the server crashed, so the crash path needs its own
    // subscription.
    internal static void OnCrashed(ServerCrashedEventArgs e) => Stop();

    private void StopThread()
    {
        _exit = true;
        _work.Set();
        _thread.Join(TimeSpan.FromSeconds(5));
    }
}
