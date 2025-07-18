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

    public virtual bool CheckMove() => !(_mobile.Deleted || _mobile.DisallowAllMoves);

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

        if ((_mobile.Direction & Direction.Mask) != (d & Direction.Mask))
        {
            _mobile.Direction = d;
        }

        _mobile.Pushing = false;

        var mobDirection = _mobile.Direction;

        if (TryMove(d))
        {
            _mobile.CurrentSpeed = _mobile.Hits < _mobile.HitsMax * 0.3
                ? BadlyHurtMoveDelay(_mobile)
                : _mobile.Warmode || _mobile.Combatant != null ? _mobile.ActiveSpeed : _mobile.PassiveSpeed;

            return MoveResult.Success;
        }

        if ((mobDirection & Direction.Mask) != (d & Direction.Mask))
        {
            _mobile.Direction = d;
            return MoveResult.SuccessAutoTurn;
        }

        return HandleBlockedMovement(d, mobDirection);
    }

    private bool TryMove(Direction d)
    {
        MoveImpl.IgnoreMovableImpassables = _mobile.CanMoveOverObstacles && !_mobile.CanDestroyObstacles;

        var result = _mobile.Move(d);

        MoveImpl.IgnoreMovableImpassables = false;
        return result;
    }

    private bool IsInBadState() =>
        _mobile == null || _mobile.Deleted || _mobile.Frozen || _mobile.Paralyzed ||
        _mobile.Spell?.IsCasting == true || _mobile.DisallowAllMoves;

    private MoveResult HandleBlockedMovement(Direction d, Direction mobDirection)
    {
        var wasPushing = _mobile.Pushing;

        if ((_mobile.CanOpenDoors || _mobile.CanDestroyObstacles) && !TryClearObstacles(d))
        {
            return MoveResult.Success;
        }

        return TryAlternateMovement(wasPushing);
    }

    private MoveResult TryAlternateMovement(bool wasPushing)
    {
        var offset = Utility.Random(100) < 40 ? 1 : -1;

        for (var i = 0; i < 2; ++i)
        {
            _mobile.TurnInternal(offset);

            if (_mobile.Move(_mobile.Direction))
            {
                return MoveResult.SuccessAutoTurn;
            }
        }

        return wasPushing ? MoveResult.BadState : MoveResult.Blocked;
    }

    private bool TryClearObstacles(Direction d)
    {
        DebugSay("My movement is blocked. Trying to push through.");

        var map = _mobile.Map;

        if (map == null) { return true; }

        var (x, y) = GetOffsetLocation(d);

        using var queue = PooledRefQueue<Item>.Create();

        var destroyables = GatherObstacles(x, y, queue);

        if (destroyables > 0)
        {
            Effects.PlaySound(new Point3D(x, y, _mobile.Z), _mobile.Map, 0x3B3);
        }

        return ProcessObstacles(queue, d);
    }

    private (int x, int y) GetOffsetLocation(Direction d)
    {
        var x = _mobile.X;
        var y = _mobile.Y;
        Movement.Movement.Offset(d, ref x, ref y);
        return (x, y);
    }

    private int GatherObstacles(int x, int y, PooledRefQueue<Item> queue)
    {
        var destroyables = 0;

        foreach (var item in _mobile.Map.GetItemsInRange(new Point2D(x, y), 1))
        {
            if (IsValidDoor(item, x, y) || IsValidDestroyableItem(item))
            {
                queue.Enqueue(item);
                if (item is not BaseDoor) { destroyables++; }
            }
        }

        return destroyables;
    }

    private bool IsValidDoor(Item item, int x, int y)
    {
        if (!_mobile.CanOpenDoors || item is not BaseDoor door)
        {
            return false;
        }

        if (door.Z + door.ItemData.Height <= _mobile.Z || _mobile.Z + 16 <= door.Z)
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
        if (!_mobile.CanDestroyObstacles || !item.Movable || !item.ItemData.Impassable)
        {
            return false;
        }

        if (item.Z + item.ItemData.Height <= _mobile.Z || _mobile.Z + 16 <= item.Z)
        {
            return false;
        }

        return _mobile.InRange(item.GetWorldLocation(), 1);
    }

    private bool ProcessObstacles(PooledRefQueue<Item> queue, Direction d)
    {
        if (queue.Count == 0) { return true; }

        while (queue.Count > 0)
        {
            ProcessObstacle(queue.Dequeue(), queue);
        }

        return !_mobile.Move(d);
    }

    private void ProcessObstacle(Item item, PooledRefQueue<Item> queue)
    {
        if (item is BaseDoor door)
        {
            DebugSay("Opening the door.");
            door.Use(_mobile);
        }
        else
        {
            this.DebugSayFormatted($"Destroying item: {item.GetType().Name}");

            if (item is Container cont)
            {
                ProcessContainer(cont, queue);
                cont.Destroy();
            }
            else
            {
                item.Delete();
            }
        }
    }

    private void ProcessContainer(Container cont, PooledRefQueue<Item> queue)
    {
        foreach (var check in cont.Items)
        {
            if (check.Movable && check.ItemData.Impassable && cont.Z + check.ItemData.Height > _mobile.Z)
            {
                queue.Enqueue(check);
            }
        }
    }

    public virtual bool MoveTo(Mobile m, bool run, int range)
    {
        if (_mobile.Deleted || _mobile.DisallowAllMoves || m?.Deleted != false)
        {
            return false;
        }

        var distance = (int)_mobile.GetDistanceToSqrt(m);

        var shouldRun = run && distance > 5;

        if (_mobile.InRange(m, range))
        {
            _path = null;
            return true;
        }

        if (UseGroupMovement(m))
        {
            return MoveToWithGroup(this, m, shouldRun, range);
        }

        if (_path == null && _mobile.InLOS(m) && DoMove(_mobile.GetDirectionTo(m), true))
        {
            return true;
        }

        if (_path?.Goal != m)
        {
            _path = new PathFollower(_mobile, m) { Mover = DoMoveImpl };
        }

        if (_path.Follow(shouldRun, 1))
        {
            _path = null;
            return true;
        }

        return false;
    }

    private bool MoveToWithCollisionAvoidance(Mobile target, bool run, int range)
    {
        var distance = (int)_mobile.GetDistanceToSqrt(target);

        var shouldRun = run && distance > 5;

        var direction = _mobile.GetDirectionTo(target);

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

        if (_path?.Goal != target)
        {
            _path = new PathFollower(_mobile, target) { Mover = DoMoveImpl };
        }

        if (_path.Follow(shouldRun, 1))
        {
            _path = null;
            return true;
        }

        return false;
    }

    public virtual bool WalkMobileRange(Mobile m, int iSteps, bool run, int iWantDistMin, int iWantDistMax)
    {
        if (_mobile.Deleted || _mobile.DisallowAllMoves || m == null)
        {
            return false;
        }

        for (var i = 0; i < iSteps; i++)
        {
            var iCurrDist = (int)_mobile.GetDistanceToSqrt(m);

            var shouldRun = run && iCurrDist > 5;

            if (iCurrDist < iWantDistMin || iCurrDist > iWantDistMax)
            {
                if (!MoveTowardsOrAwayFrom(m, shouldRun, iCurrDist, iWantDistMax))
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        var dist = _mobile.GetDistanceToSqrt(m);

        return dist >= iWantDistMin && dist <= iWantDistMax;
    }

    private bool MoveTowardsOrAwayFrom(Mobile m, bool run, int iCurrDist, int iWantDistMax)
    {
        var shouldRun = run && iCurrDist > 5;

        var needCloser = iCurrDist > iWantDistMax;

        if (needCloser && _path?.Goal == m)
        {
            if (_path.Follow(shouldRun, 1))
            {
                _path = null;
                return true;
            }
        }
        else
        {
            var dirTo = needCloser ? _mobile.GetDirectionTo(m, shouldRun) : m.GetDirectionTo(_mobile, shouldRun);

            if (DoMove(dirTo, true))
            {
                _path = null;
                return true;
            }

            if (needCloser)
            {
                _path = new PathFollower(_mobile, m) { Mover = DoMoveImpl };

                if (_path.Follow(shouldRun, 1))
                {
                    _path = null;
                    return true;
                }
            }
        }

        return false;
    }
}
