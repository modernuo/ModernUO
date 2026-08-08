/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PasswordVerificationWorker.cs                                   *
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
using Server.Misc;
using Server.Network;

namespace Server.Accounting.Security;

/// <summary>
/// Work handed to the verification thread. Everything here is either an immutable string or a
/// reference the worker only carries -- the worker reads no game state and writes none.
/// </summary>
internal sealed class PasswordVerificationJob
{
    public Account Account;
    public NetState State;

    /// <summary>The stored hash at dispatch, used to verify and to guard the upgrade against a
    /// password change that lands while this runs.</summary>
    public string StoredHash;

    /// <summary>Phrase to verify against <see cref="StoredHash"/>.</summary>
    public string VerifyPhrase;

    /// <summary>Phrase to rehash from, or null when no upgrade is due.</summary>
    public string RehashPhrase;

    public PasswordProtectionAlgorithm TargetAlgorithm;
}

internal readonly struct PasswordVerificationOutcome
{
    public readonly bool Verified;

    /// <summary>The new hash, or null when the password did not verify or needed no upgrade.</summary>
    public readonly string UpgradedPassword;

    public PasswordVerificationOutcome(bool verified, string upgradedPassword)
    {
        Verified = verified;
        UpgradedPassword = upgradedPassword;
    }
}

/// <summary>
/// Runs Argon2 off the game loop.
///
/// An Argon2 verify is ~8.9 ms, which is more than half a frame of frozen world for every login
/// attempt, successful or not. Measurement (docs/handoffs/2026-08-07-off-loop-argon2-hashing.md)
/// puts the on-loop saving at 3.5-8.9 ms per login: the hand-off costs ~220 ns, and the only real
/// residue is the loop's own work slowing while a memory-hard KDF evicts shared L3.
///
/// One worker, deliberately, for three reasons that agree:
///   - the per-login contention tax falls with concurrency but total loop damage rises, so one
///     hasher does the least harm to the loop;
///   - a single background hasher cannot cost the loop more than the inline verify under any
///     scheduling regime, because at worst it takes an equal share of one core -- which is what
///     makes the measurement extrapolate to hardware we cannot inspect. A pool breaks that bound;
///   - exactly one 16 MiB Argon2 arena is live at a time whatever the login volume, which answers
///     memory-exhaustion without a separate cap.
///
/// Throughput is ~110 verifies/sec. Wall-clock login latency is explicitly not a concern, so
/// head-of-line blocking during a rush costs nothing.
/// </summary>
internal sealed class PasswordVerificationWorker
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(PasswordVerificationWorker));

    /// <summary>
    /// Pending cap. Overflow rejects the login rather than verifying it inline: falling back to the
    /// loop would let anyone who fills the queue steer the work back onto the thread this exists to
    /// protect.
    /// </summary>
    internal const int MaxPending = 128;

    // Nothing signals the worker when a save freeze ends, so it re-checks on this interval -- but
    // only while a save is in progress, never in steady state.
    private const int SaveGatePollMs = 50;

    private static PasswordVerificationWorker _instance;

    /// <summary>
    /// Off-loop verification needs a spare core to move work to, which a 1-2 core host does not
    /// have, and is pointless on a dev box or test shard where logins are rare and the simpler
    /// path is easier to reason about.
    /// </summary>
    internal static bool Enabled { get; } =
#if DEBUG
        false;
#else
        Environment.ProcessorCount >= 4;
#endif

    private readonly Thread _thread;
    private readonly AutoResetEvent _work = new(false);
    private readonly ConcurrentQueue<PasswordVerificationJob> _queue = new();

    // Its own Argon2, sharing no RNG with the loop's. Verification is static-backed and would be
    // safe either way; hashing is not.
    private readonly IPasswordProtection _argon2 = Argon2PasswordProtection.CreateIsolated();

    private int _pending;
    private volatile bool _exit;

    private PasswordVerificationWorker()
    {
        _thread = new Thread(Execute)
        {
            IsBackground = true,
            Name = "Password Verification"
        };

        _thread.Start();
    }

    // Created on first use, so a shard that never takes the off-loop path never allocates a thread.
    private static PasswordVerificationWorker Instance => _instance ??= new PasswordVerificationWorker();

    internal static int Pending => _instance?._pending ?? 0;

    /// <summary>
    /// Queues a job. False when the queue is full, in which case the caller must reject the login
    /// without verifying.
    /// </summary>
    internal static bool TryEnqueue(PasswordVerificationJob job) => Instance.TryEnqueueCore(job);

    private bool TryEnqueueCore(PasswordVerificationJob job)
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
    /// Argon2 only, and only outside the save freeze. The freeze runs on the loop, so nothing new
    /// can be queued while it holds; checking before each job bounds the overlap to whichever hash
    /// was already in flight. PendingSave counts too -- the serialization threads are awake and
    /// spinning on an empty queue by then, which is the worst moment to add a competitor.
    /// </summary>
    private static bool CanRunNow() =>
        World.WorldState is WorldState.Running or WorldState.WritingSave;

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

            PasswordVerificationOutcome outcome;

            try
            {
                outcome = Compute(job);
            }
            catch (Exception ex)
            {
                // A verdict must still come back, or the connection waits forever for a reply.
                logger.Error(ex, "Password verification failed for {Username}", job.Account?.Username);
                outcome = new PasswordVerificationOutcome(false, null);
            }

            Core.LoopContext.Post(() => Apply(job, outcome));
        }
    }

    private PasswordVerificationOutcome Compute(PasswordVerificationJob job)
    {
        if (!_argon2.ValidatePassword(job.StoredHash, job.VerifyPhrase))
        {
            return new PasswordVerificationOutcome(false, null);
        }

        return new PasswordVerificationOutcome(
            true,
            job.RehashPhrase == null ? null : _argon2.EncryptPassword(job.RehashPhrase)
        );
    }

    private static void Apply(PasswordVerificationJob job, PasswordVerificationOutcome outcome)
    {
        var state = job.State;

        // The connection may have gone while the hash ran. A dead NetState must not be revived, and
        // nothing may be written on its behalf.
        if (state?.Running != true)
        {
            return;
        }

        if (outcome.Verified && outcome.UpgradedPassword != null)
        {
            job.Account.ApplyPasswordUpgrade(job.StoredHash, outcome.UpgradedPassword, job.TargetAlgorithm);
        }

        AccountHandler.CompleteDeferredAccountLogin(state, job.Account, outcome.Verified);
    }

    /// <summary>Runs a job on the calling thread. The seam the tests drive, and the path taken when
    /// off-loop verification is gated off.</summary>
    internal static PasswordVerificationOutcome ComputeInline(PasswordVerificationJob job) =>
        Instance.Compute(job);

    internal static void Exit()
    {
        var instance = _instance;

        if (instance == null)
        {
            return;
        }

        instance._exit = true;
        instance._work.Set();
        instance._thread.Join(TimeSpan.FromSeconds(5));
        _instance = null;
    }
}
