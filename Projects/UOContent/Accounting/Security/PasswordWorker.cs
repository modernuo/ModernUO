/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PasswordWorker.cs                                   *
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
/// Work handed to the password thread. Strings and references it only carries: the worker reads no
/// game state and writes none.
///
/// Either half is optional, which is what lets one job type serve both callers. A login verifies
/// and may rehash; an explicit password change only hashes.
/// </summary>
internal sealed class PasswordJob
{
    public Account Account;

    /// <summary>Ties the job to a connection. Null when the work is not gated on one, such as a
    /// password change by an admin.</summary>
    public NetState State;

    /// <summary>Hash to verify against, with <see cref="VerifyPhrase"/>.</summary>
    public string StoredHash;

    /// <summary>Phrase to verify, or null to skip verification.</summary>
    public string VerifyPhrase;

    /// <summary>Phrase to hash, or null when nothing needs writing.</summary>
    public string HashPhrase;

    public PasswordProtectionAlgorithm TargetAlgorithm;

    /// <summary>Write slot claimed at dispatch, checked by
    /// <see cref="Account.ApplyPasswordWrite"/>.</summary>
    public int Sequence;

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
/// Runs Argon2 off the game loop, where a verify costs ~8.9 ms of frozen world per login attempt.
///
/// Exactly one worker. A single background hasher cannot cost the loop more than the inline verify
/// under any scheduling regime, because at worst it takes an equal share of one core; a pool breaks
/// that bound and is what would make the gain hardware-dependent. It also caps live Argon2 arenas
/// at one, and does the least total harm to the loop's own cache footprint.
///
/// ~110 verifies/sec, which is ample: login latency is not a concern, only loop time.
/// </summary>
internal sealed class PasswordWorker
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(PasswordWorker));

    /// <summary>
    /// Backstop, not a flood defense. <c>SentFirstPacket</c> holds a connection to one pending
    /// verify and the engine caps connections at 4096 (<c>NetState.Network.cs</c>), so this matches
    /// that bound and can only trip if the one-per-connection invariant breaks. A cap low enough to
    /// blunt an attack would reject real players first -- during a mass reconnect they are the
    /// queue. Flood defense belongs at the connection layer.
    /// </summary>
    private const int MaxPending = 4096;

    // Nothing signals the worker when a save freeze ends, so it re-checks on this interval -- but
    // only while a save is in progress, never in steady state.
    private const int SaveGatePollMs = 50;

    private static PasswordWorker _instance;

    // Needs a spare core to move work to, which a 1-2 core host does not have. Off in DEBUG:
    // dev boxes and test shards have few logins and are better served by the simpler path.
    internal static readonly bool Enabled =
#if DEBUG
        false;
#else
        Environment.ProcessorCount >= 4;
#endif

    private readonly Thread _thread;
    private readonly AutoResetEvent _work = new(false);
    private readonly ConcurrentQueue<PasswordJob> _queue = new();

    // Its own Argon2: verification is static-backed and safe to share, hashing draws from a
    // per-instance RNG and is not.
    private readonly IPasswordProtection _argon2 = Argon2PasswordProtection.CreateIsolated();

    private int _pending;
    private volatile bool _exit;

    private PasswordWorker()
    {
        _thread = new Thread(Execute)
        {
            IsBackground = true,
            Name = "Password Verification"
        };

        _thread.Start();
    }

    // Created on first use, so a shard that never takes the off-loop path never allocates a thread.
    private static PasswordWorker Instance => _instance ??= new PasswordWorker();

    /// <summary>
    /// Queues a job. False when the queue is full, in which case the caller must reject the login
    /// without verifying.
    /// </summary>
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

            // Gone while it waited: skip it rather than spend ~9 ms on a verdict nobody receives.
            // Running only goes true -> false, so a stale read wastes a hash but can never skip a
            // live connection. A null State means the job is not tied to a connection at all, such
            // as an admin password change, and must still run.
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
                logger.Error(ex, "Password verification failed for {Username}", job.Account?.Username);
                outcome = new PasswordOutcome(false, null);
            }

            Core.LoopContext.Post(() => Apply(job, outcome));
        }
    }

    private PasswordOutcome Compute(PasswordJob job)
    {
        if (job.VerifyPhrase != null && !_argon2.ValidatePassword(job.StoredHash, job.VerifyPhrase))
        {
            return new PasswordOutcome(false, null);
        }

        return new PasswordOutcome(
            true,
            job.HashPhrase == null ? null : _argon2.EncryptPassword(job.HashPhrase)
        );
    }

    private static void Apply(PasswordJob job, PasswordOutcome outcome)
    {
        // Re-checked: a connection can drop while the result sits in the loop queue. Jobs with no
        // connection attached, such as an admin password change, are unaffected.
        if (job.State?.Running == false)
        {
            return;
        }

        if (outcome.Verified && outcome.Hash != null)
        {
            job.Account.ApplyPasswordWrite(job.Sequence, outcome.Hash, job.TargetAlgorithm);
        }

        job.OnComplete?.Invoke(job, outcome);
    }

    /// <summary>
    /// Sets a password, off the loop when that is available and inline otherwise, invoking
    /// <paramref name="onDone"/> on the loop either way. Both branches claim a write slot first, so
    /// the newest request wins however the work was routed.
    ///
    /// The confirmation belongs in <paramref name="onDone"/>, not at the call site: off-loop it has
    /// not happened yet when the call returns.
    /// </summary>
    internal static void SetPassword(Account account, string plainPassword, Action<bool> onDone)
    {
        if (!Enabled || AccountSecurity.CurrentAlgorithm != PasswordProtectionAlgorithm.Argon2)
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
            Sequence = account.BeginPasswordWrite(),
            OnComplete = (_, outcome) => onDone?.Invoke(outcome.Hash != null)
        };

        if (!TryEnqueue(job))
        {
            // Saturated. A password change is rare and must not be silently dropped, so this one
            // pays the hash on the loop rather than failing.
            account.SetPassword(plainPassword);
            onDone?.Invoke(true);
        }
    }

    /// <summary>Runs a job on the calling thread. The seam the tests drive.</summary>
    internal static PasswordOutcome ComputeInline(PasswordJob job) =>
        Instance.Compute(job);

    /// <summary>
    /// Normal shutdown. The loop has stopped but this runs on the game thread, so pending work can
    /// be finished in place -- which is the only chance it gets, since nothing will pump the loop
    /// context again.
    ///
    /// Only writes are finished. A verify decides a login, and every connection is closing.
    /// </summary>
    internal static void Shutdown()
    {
        var instance = _instance;

        if (instance == null)
        {
            return;
        }

        instance.StopThread();

        // Results posted before the thread stopped are still queued on a loop that has exited.
        Core.LoopContext.ExecuteTasks();

        while (instance._queue.TryDequeue(out var job))
        {
            Interlocked.Decrement(ref instance._pending);

            if (job.HashPhrase == null)
            {
                continue;
            }

            var outcome = instance.Compute(job);

            if (outcome.Verified && outcome.Hash != null)
            {
                job.Account.ApplyPasswordWrite(job.Sequence, outcome.Hash, job.TargetAlgorithm);
            }
        }
    }

    /// <summary>
    /// Crash. There is no usable game thread, so nothing may be applied -- stop the thread and let
    /// whatever was pending go. Subscribed separately because <c>HandleClosed</c> skips
    /// <c>InvokeShutdown</c> when the server crashed.
    /// </summary>
    internal static void OnCrashed(ServerCrashedEventArgs e) => _instance?.StopThread();

    private void StopThread()
    {
        _exit = true;
        _work.Set();
        _thread.Join(TimeSpan.FromSeconds(5));
    }
}
