/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AIMovement.cs                                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program. If not, see <http://www.gnu.org/licenses/>.  *
 ************************************************************************/

using System.Runtime.CompilerServices;
using Server.Collections;
using Server.Items;
using MoveImpl = Server.Movement.MovementImpl;

namespace Server.Mobiles;

public abstract partial class BaseAI
{
    // --- Centralized progress-based approach state (see ApproachTarget) ---------------
    // Consecutive move-eligible ticks a creature may fail to improve its best distance to a
    // STATIONARY goal before it gives up and idles. A moving goal (an active chase) never
    // triggers give-up. Must exceed the longest no-improvement stretch of a valid detour
    // (the Britain Inn detour's is ~13 ticks), with margin; this also bounds the largest
    // concave detour a creature will navigate before idling on a stationary goal.
    private const int ApproachGiveUpTicks = 40;

    private Mobile _approachGoal;
    private Point3D _approachGoalLoc;
    private double _approachBestDist;
    private int _approachStallTicks;
    private bool _approachGaveUp;
    private Point3D _approachGaveUpGoalLoc;

    public static double BadlyHurtMoveDelay(BaseCreature bc)
    {
        var statMin = Core.HS ? bc.Stam : bc.Hits;
        var statMax = Core.HS ? bc.StamMax : bc.HitsMax;

        if (!bc.IsDeadPet && (bc.ReduceSpeedWithDamage || bc.IsSubdued)
                          && statMax > 0 && statMin < statMax * 0.3)
        {
            var hits = (double)statMin / statMax;

            if (hits < 0.1) { return bc.CurrentSpeed + 0.15; }
            if (hits < 0.2) { return bc.CurrentSpeed + 0.1; }
            if (hits < 0.3) { return bc.CurrentSpeed + 0.05; }
        }

        return bc.CurrentSpeed;
    }

    public bool CanMoveNow(out double delay)
    {
        delay = 0.0;
        return Core.TickCount >= NextMove;
    }

    public virtual bool CheckMove() => !(Mobile.Deleted || Mobile.DisallowAllMoves);

    public virtual bool DoMove(Direction d, bool badStateOk = false) => IsMoveSuccessful(DoMoveImpl(d, badStateOk), badStateOk);

    private static bool IsMoveSuccessful(MoveResult res, bool badStateOk) =>
        res is MoveResult.Success or MoveResult.SuccessAutoTurn
        || badStateOk && res == MoveResult.BadState;

    public virtual MoveResult DoMoveImpl(Direction d, bool badStateOk)
    {
        if (IsInBadState() || !CanMoveNow(out _))
        {
            return MoveResult.BadState;
        }

        if ((Mobile.Direction & Direction.Mask) != (d & Direction.Mask))
        {
            Mobile.Direction = d;
        }

        Mobile.Pushing = false;
        var mobDirection = Mobile.Direction;

        if (TryMove(d))
        {
            if (Core.AOS && IsFollowingMaster())
            {
                Mobile.CurrentSpeed = 0.1;
            }
            else if (Mobile.Hits < Mobile.HitsMax * 0.3)
            {
                Mobile.CurrentSpeed = BadlyHurtMoveDelay(Mobile);
            }
            else if (Mobile.Warmode || Mobile.Combatant != null)
            {
                Mobile.CurrentSpeed = Mobile.ActiveSpeed;
            }
            else
            {
                Mobile.CurrentSpeed = Mobile.PassiveSpeed;
            }

            return MoveResult.Success;
        }

        if ((mobDirection & Direction.Mask) != (d & Direction.Mask))
        {
            Mobile.Direction = d;
            return MoveResult.SuccessAutoTurn;
        }

        return HandleBlockedMovement(d);
    }

    private bool TryMove(Direction d)
    {
        MoveImpl.IgnoreMovableImpassables = Mobile.CanMoveOverObstacles && !Mobile.CanDestroyObstacles;

        var result = Mobile.Move(d);

        MoveImpl.IgnoreMovableImpassables = false;
        return result;
    }

    private bool IsInBadState() =>
        Mobile == null || Mobile.Deleted || Mobile.Frozen || Mobile.Paralyzed ||
        Mobile.Spell?.IsCasting == true || Mobile.DisallowAllMoves;

    private MoveResult HandleBlockedMovement(Direction d)
    {
        var wasPushing = Mobile.Pushing;

        if ((Mobile.CanOpenDoors || Mobile.CanDestroyObstacles) && !TryClearObstacles(d))
        {
            return MoveResult.Success;
        }

        return TryAlternateMovement(wasPushing);
    }

    private MoveResult TryAlternateMovement(bool wasPushing)
    {
        var offset = Utility.Random(2) == 0 ? 1 : -1;

        for (var i = 0; i < 2; ++i)
        {
            Mobile.TurnInternal(offset);

            if (Mobile.Move(Mobile.Direction))
            {
                return MoveResult.SuccessAutoTurn;
            }
        }

        return wasPushing ? MoveResult.BadState : MoveResult.Blocked;
    }

    private bool TryClearObstacles(Direction d)
    {
        DebugSay("My movement is blocked. Trying to push through.");

        var map = Mobile.Map;

        if (map == null) { return true; }

        var (x, y) = GetOffsetLocation(d);

        var queue = GatherObstacles(x, y, out var destroyables);

        if (destroyables > 0)
        {
            Effects.PlaySound(new Point3D(x, y, Mobile.Z), Mobile.Map, 0x3B3);
        }

        try
        {
            return ProcessObstacles(ref queue, d);
        }
        finally
        {
            queue.Dispose();
        }
    }

    private (int x, int y) GetOffsetLocation(Direction d)
    {
        var x = Mobile.X;
        var y = Mobile.Y;
        Movement.Movement.Offset(d, ref x, ref y);
        return (x, y);
    }

    private PooledRefQueue<Item> GatherObstacles(int x, int y, out int destroyables)
    {
        var queue = PooledRefQueue<Item>.Create();
        destroyables = 0;

        foreach (var item in Mobile.Map.GetItemsInRange(new Point2D(x, y), 1))
        {
            if (IsValidDoor(item, x, y) || IsValidDestroyableItem(item))
            {
                queue.Enqueue(item);
                if (item is not BaseDoor)
                {
                    destroyables++;
                }
            }
        }

        return queue;
    }

    private bool IsValidDoor(Item item, int x, int y)
    {
        if (!Mobile.CanOpenDoors || item is not BaseDoor door)
        {
            return false;
        }

        if (door.Z + door.ItemData.Height <= Mobile.Z || Mobile.Z + 16 <= door.Z)
        {
            return false;
        }

        if (door.X != x || door.Y != y)
        {
            return false;
        }

        return !door.Locked || !door.UseLocks();
    }

    private bool IsValidDestroyableItem(Item item)
    {
        if (!Mobile.CanDestroyObstacles || !item.Movable || !item.ItemData.Impassable)
        {
            return false;
        }

        if (item.Z + item.ItemData.Height <= Mobile.Z || Mobile.Z + 16 <= item.Z)
        {
            return false;
        }

        return Mobile.InRange(item.GetWorldLocation(), 1);
    }

    private bool ProcessObstacles(ref PooledRefQueue<Item> queue, Direction d)
    {
        if (queue.Count == 0) { return true; }

        while (queue.Count > 0)
        {
            ProcessObstacle(queue.Dequeue(), ref queue);
        }

        return !Mobile.Move(d);
    }

    private void ProcessObstacle(Item item, ref PooledRefQueue<Item> queue)
    {
        if (item is BaseDoor door)
        {
            DebugSay("Opening the door.");
            door.Use(Mobile);
        }
        else
        {
            this.DebugSayFormatted($"Destroying item: {item.GetType().Name}");

            if (item is Container cont)
            {
                ProcessContainer(cont, ref queue);
                cont.Destroy();
            }
            else
            {
                item.Delete();
            }
        }
    }

    private void ProcessContainer(Container cont, ref PooledRefQueue<Item> queue)
    {
        foreach (var check in cont.Items)
        {
            if (check.Movable && check.ItemData.Impassable && cont.Z + check.ItemData.Height > Mobile.Z)
            {
                queue.Enqueue(check);
            }
        }
    }

    /// <summary>
    /// Centralized "move toward <paramref name="target"/> until within
    /// <paramref name="range"/>" decision shared by MoveTo and WalkMobileRange. A greedy
    /// step is taken only when it actually gets the creature closer; a blocked step or an
    /// auto-turn sidestep that made no progress falls through to a persistent PathFollower
    /// that routes around the obstacle and is never discarded by a greedy step. A
    /// best-distance stall counter idles the creature if an in-range goal is genuinely
    /// unreachable, without ever abandoning a real chase or detour.
    /// </summary>
    protected bool ApproachTarget(Mobile target, bool run, int range)
    {
        if (Mobile.Deleted || Mobile.DisallowAllMoves || target?.Deleted != false)
        {
            return false;
        }

        if (Mobile.InRange(target, range))
        {
            ResetApproach();
            return true;
        }

        // Already gave up on this exact (unreachable) goal: idle until it moves.
        if (_approachGaveUp && _approachGoal == target)
        {
            if (target.Location == _approachGaveUpGoalLoc)
            {
                return false;
            }

            ResetApproach(); // target moved — try again fresh
        }

        // FAST PATH: greedy step toward the target, counted as success ONLY when the move
        // fully succeeded (not an auto-turn sidestep) and actually got us closer. An
        // auto-turn sidestep can reduce Euclidean distance while moving in the wrong
        // direction (e.g., east when the true route requires going south-first around a
        // concave obstacle); treating it as progress would discard a PathFollower that is
        // the only way to navigate. A blocked step or a non-Success result falls through to
        // the planner immediately.
        if (Path == null && Mobile.InLOS(target))
        {
            var distBefore = Mobile.GetDistanceToSqrt(target);
            var res = DoMoveImpl(Mobile.GetDirectionTo(target, run), true);

            if (res == MoveResult.BadState)
            {
                return false; // not allowed to move this tick; not a stall
            }

            if (res == MoveResult.Success && Mobile.GetDistanceToSqrt(target) < distBefore)
            {
                ResetApproach();
                return Mobile.InRange(target, range);
            }
            // else: fall through; let the PathFollower route around the obstacle.
        }

        // PLANNING PATH: a persistent PathFollower, never discarded by a greedy step.
        if (Path == null || Path.Goal != target)
        {
            Path = new PathFollower(Mobile, target) { Mover = DoMoveImpl };
        }

        if (Path.Follow(run, range))
        {
            ResetApproach();
            return true;
        }

        TrackApproachProgress(target);
        return false;
    }

    /// <summary>
    /// Best-distance stuck detection. A creature making real headway keeps lowering its
    /// closest-ever distance to the goal (a detour's outbound leg pauses that, but it
    /// resumes once the creature rounds the obstacle). A creature that cannot reach a
    /// STATIONARY goal never lowers it and, after <see cref="ApproachGiveUpTicks"/> ticks,
    /// gives up and idles. A MOVING goal (an active chase) resets the baseline every tick,
    /// so chases never give up even when the gap holds constant.
    /// </summary>
    private void TrackApproachProgress(Mobile target)
    {
        if (!CanMoveNow(out _))
        {
            return; // a not-yet-due move (stun) is not a stall
        }

        var dist = Mobile.GetDistanceToSqrt(target);
        var goalLoc = target.Location;

        // New goal, or the goal moved (active chase): reset the stall baseline. Clearing the
        // give-up flag here prevents a prior goal's give-up state from leaking onto a new one.
        if (_approachGoal != target || goalLoc != _approachGoalLoc)
        {
            _approachGoal = target;
            _approachGoalLoc = goalLoc;
            _approachBestDist = dist;
            _approachStallTicks = 0;
            _approachGaveUp = false;
            return;
        }

        // Stationary goal: getting closer than ever resets the stall.
        if (dist < _approachBestDist)
        {
            _approachBestDist = dist;
            _approachStallTicks = 0;
            return;
        }

        if (++_approachStallTicks >= ApproachGiveUpTicks)
        {
            _approachGaveUp = true;
            _approachGaveUpGoalLoc = goalLoc;
            Path = null;
        }
    }

    /// <summary>Clears all approach state (called on arrival, real greedy progress, or when
    /// a given-up goal moves).</summary>
    private void ResetApproach()
    {
        Path = null;
        _approachGoal = null;
        _approachGoalLoc = Point3D.Zero;
        _approachBestDist = 0;
        _approachStallTicks = 0;
        _approachGaveUp = false;
    }

    public virtual bool MoveTo(Mobile m, bool run, int range)
    {
        if (Mobile.Deleted || Mobile.DisallowAllMoves || m?.Deleted != false)
        {
            return false;
        }

        var distance = (int)Mobile.GetDistanceToSqrt(m);
        var distanceThreshold = Core.AOS && IsFollowingMaster() ? 1 : 5;

        var shouldRun = run && distance > distanceThreshold;

        if (Mobile.InRange(m, range))
        {
            ResetApproach();
            return true;
        }

        if (UseGroupMovement(m))
        {
            return MoveToWithGroup(this, m, shouldRun, range);
        }

        return ApproachTarget(m, shouldRun, range);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsFollowingMaster() =>
        Mobile.Controlled &&
        Mobile.ControlOrder == OrderType.Follow &&
        Mobile.ControlTarget == Mobile.ControlMaster &&
        Mobile.Combatant == null;

    private bool MoveToWithCollisionAvoidance(Mobile target, bool run, int range)
    {
        var distance = (int)Mobile.GetDistanceToSqrt(target);

        var shouldRun = run && distance > 5;

        var direction = Mobile.GetDirectionTo(target);

        if (DoMove(direction, true))
        {
            return true;
        }

        for (var i = 1; i <= 3; i++)
        {
            var clockwise = (Direction)(((int)direction + i) % 8);

            if (DoMove(clockwise, true))
            {
                return true;
            }

            var counterclockwise = (Direction)(((int)direction - i + 8) % 8);

            if (DoMove(counterclockwise, true))
            {
                return true;
            }
        }

        // Tactical sidesteps exhausted — route around the obstacle via the centralized
        // approach primitive (persistent PathFollower, no oscillation).
        return ApproachTarget(target, shouldRun, range);
    }

    public virtual bool WalkMobileRange(Mobile m, int iSteps, bool run, int iWantDistMin, int iWantDistMax)
    {
        if (Mobile.Deleted || Mobile.DisallowAllMoves || m == null)
        {
            return false;
        }

        for (var i = 0; i < iSteps; i++)
        {
            var iCurrDist = (int)Mobile.GetDistanceToSqrt(m);

            var shouldRun = run && iCurrDist > 5;

            if (iCurrDist >= iWantDistMin && iCurrDist <= iWantDistMax)
            {
                return true;
            }

            if (!MoveTowardsOrAwayFrom(m, shouldRun, iCurrDist, iWantDistMax))
            {
                return false;
            }
        }

        var dist = Mobile.GetDistanceToSqrt(m);

        return dist >= iWantDistMin && dist <= iWantDistMax;
    }

    private bool MoveTowardsOrAwayFrom(Mobile m, bool run, int iCurrDist, int iWantDistMax)
    {
        var shouldRun = run && iCurrDist > 5;

        if (iCurrDist > iWantDistMax)
        {
            // Too far: approach via the centralized progress-based primitive.
            return ApproachTarget(m, shouldRun, iWantDistMax);
        }

        // Too close: back away. Retreat keeps the simple greedy behavior (out of scope).
        if (DoMove(m.GetDirectionTo(Mobile, shouldRun), true))
        {
            Path = null;
            return true;
        }

        return false;
    }
}
