/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
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
            Mobile.CurrentSpeed = Mobile.Hits < Mobile.HitsMax * 0.3
                ? BadlyHurtMoveDelay(Mobile)
                : Mobile.Warmode || Mobile.Combatant != null ? Mobile.ActiveSpeed : Mobile.PassiveSpeed;

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
            Path = null;
            return true;
        }

        if (UseGroupMovement(m))
        {
            return MoveToWithGroup(this, m, shouldRun, range);
        }

        if (Path == null && Mobile.InLOS(m) && DoMove(Mobile.GetDirectionTo(m), true))
        {
            return true;
        }

        if (Path?.Goal != m)
        {
            Path = new PathFollower(Mobile, m) { Mover = DoMoveImpl };
        }

        if (Path.Follow(shouldRun, 1))
        {
            Path = null;
            return true;
        }

        return false;
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

        if (Path?.Goal != target)
        {
            Path = new PathFollower(Mobile, target) { Mover = DoMoveImpl };
        }

        if (Path.Follow(shouldRun, 1))
        {
            Path = null;
            return true;
        }

        return false;
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

        var needCloser = iCurrDist > iWantDistMax;

        if (needCloser && m != null && Path?.Goal == m)
        {
            if (Path.Follow(shouldRun, 1))
            {
                Path = null;
                return true;
            }
        }
        else
        {
            var dirTo = needCloser ? Mobile.GetDirectionTo(m, shouldRun) : m.GetDirectionTo(Mobile, shouldRun);

            if (DoMove(dirTo, true))
            {
                Path = null;
                return true;
            }

            if (needCloser)
            {
                Path = new PathFollower(Mobile, m) { Mover = DoMoveImpl };

                if (Path.Follow(shouldRun, 1))
                {
                    Path = null;
                    return true;
                }
            }
        }

        return false;
    }
}
