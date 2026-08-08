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
/// Work handed to the verification thread. Strings and references it only carries: the worker
/// reads no game state and writes none.
/// </summary>
internal sealed class PasswordVerificationJob
{
    public Account Account;
    public NetState State;

    /// <summary>The hash at dispatch. Also guards the upgrade against a password change landing
    /// while this runs.</summary>
    public string StoredHash;

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
/// Runs Argon2 off the game loop, where a verify costs ~8.9 ms of frozen world per login attempt.
///
/// Exactly one worker. A single background hasher cannot cost the loop more than the inline verify
/// under any scheduling regime, because at worst it takes an equal share of one core; a pool breaks
/// that bound and is what would make the gain hardware-dependent. It also caps live Argon2 arenas
/// at one, and does the least total harm to the loop's own cache footprint.
///
/// ~110 verifies/sec, which is ample: login latency is not a concern, only loop time.
/// </summary>
internal sealed class PasswordVerificationWorker
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(PasswordVerificationWorker));

    /// <summary>
    /// Backstop, not a flood defence. <c>SentFirstPacket</c> holds a connection to one pending
    /// verify and the engine caps connections at 4096 (<c>NetState.Network.cs</c>), so this matches
    /// that bound and can only trip if the one-per-connection invariant breaks. A cap low enough to
    /// blunt an attack would reject real players first -- during a mass reconnect they are the
    /// queue. Flood defence belongs at the connection layer.
    /// </summary>
    internal const int MaxPending = 4096;

    // Nothing signals the worker when a save freeze ends, so it re-checks on this interval -- but
    // only while a save is in progress, never in steady state.
    private const int SaveGatePollMs = 50;

    private static PasswordVerificationWorker _instance;

    // Needs a spare core to move work to, which a 1-2 core host does not have. Off in DEBUG:
    // dev boxes and test shards have few logins and are better served by the simpler path.
    internal static bool Enabled { get; } =
#if DEBUG
        false;
#else
        Environment.ProcessorCount >= 4;
#endif

    private readonly Thread _thread;
    private readonly AutoResetEvent _work = new(false);
    private readonly ConcurrentQueue<PasswordVerificationJob> _queue = new();

    // Its own Argon2: verification is static-backed and safe to share, hashing draws from a
    // per-instance RNG and is not.
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
    /// Checked before each job, which bounds a save overlap to whichever hash was already running:
    /// the freeze holds the loop, so nothing new can be queued during it. PendingSave counts too --
    /// the serialization threads are already awake and spinning on an empty queue by then.
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

            // Gone while it waited: skip it rather than spend ~9 ms on a verdict nobody receives.
            // Running only goes true -> false, so a stale read wastes a hash but can never skip a
            // live connection.
            if (job.State?.Running != true)
            {
                continue;
            }

            PasswordVerificationOutcome outcome;

            try
            {
                outcome = Compute(job);
            }
            catch (Exception ex)
            {
                // A verdict must still come back, or the connection never gets a reply.
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

        // Re-checked: the connection can also drop while the verdict sits in the loop queue.
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

    /// <summary>Runs a job on the calling thread. The seam the tests drive.</summary>
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
