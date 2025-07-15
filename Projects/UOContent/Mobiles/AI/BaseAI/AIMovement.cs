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

using System;
using System.Collections.Generic;
using Server.Collections;
using Server.Items;
using Server.Mobiles;
using Server;
using MoveImpl = Server.Movement.MovementImpl;

namespace Server.Mobiles
{
     public abstract partial class BaseAI
     {
          public static double BadlyHurtMoveDelay(BaseCreature bc)
          {
               int statMin = Core.HS ? bc.Stam : bc.Hits;
               int statMax = Core.HS ? bc.StamMax : bc.HitsMax;

               if (!bc.IsDeadPet && (bc.ReduceSpeedWithDamage || bc.IsSubdued)
                    && statMax > 0 && statMin < statMax * 0.3)
               {
                    double hits = (double)statMin / statMax;

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

          public virtual bool CheckMove()
          {
               return !(m_Mobile.Deleted || m_Mobile.DisallowAllMoves);
          }

          public virtual bool DoMove(Direction d, bool badStateOk = false)
          {
               return IsMoveSuccessful(DoMoveImpl(d, badStateOk), badStateOk);
          }

          private static bool IsMoveSuccessful(MoveResult res, bool badStateOk)
          {
               return res is MoveResult.Success or MoveResult.SuccessAutoTurn
                    || (badStateOk && res == MoveResult.BadState);
          }

          public virtual MoveResult DoMoveImpl(Direction d, bool badStateOk)
          {
               if (IsInBadState() || !CanMoveNow(out _))
               {
                    return MoveResult.BadState;
               }

               if ((m_Mobile.Direction & Direction.Mask) != (d & Direction.Mask))
               {
                    m_Mobile.Direction = d;
               }

               m_Mobile.Pushing = false;

               var mobDirection = m_Mobile.Direction;

               if (TryMove(d))
               {
                    m_Mobile.CurrentSpeed = m_Mobile.Hits < m_Mobile.HitsMax * 0.3
                         ? BadlyHurtMoveDelay(m_Mobile)
                         : (m_Mobile.Warmode || m_Mobile.Combatant != null ? m_Mobile.ActiveSpeed : m_Mobile.PassiveSpeed);

                    return MoveResult.Success;
               }

               if ((mobDirection & Direction.Mask) != (d & Direction.Mask))
               {
                    m_Mobile.Direction = d;
                    return MoveResult.SuccessAutoTurn;
               }

               return HandleBlockedMovement(d, mobDirection);
          }

          private bool TryMove(Direction d)
          {
               MoveImpl.IgnoreMovableImpassables = m_Mobile.CanMoveOverObstacles && !m_Mobile.CanDestroyObstacles;

               var result = m_Mobile.Move(d);

               MoveImpl.IgnoreMovableImpassables = false;
               return result;
          }

          private bool IsInBadState()
          {
               return m_Mobile == null || m_Mobile.Deleted || m_Mobile.Frozen || m_Mobile.Paralyzed ||
               m_Mobile.Spell?.IsCasting == true || m_Mobile.DisallowAllMoves;
          }

          private MoveResult HandleBlockedMovement(Direction d, Direction mobDirection)
          {
               var wasPushing = m_Mobile.Pushing;

               if ((m_Mobile.CanOpenDoors || m_Mobile.CanDestroyObstacles) && !TryClearObstacles(d))
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
                    m_Mobile.TurnInternal(offset);

                    if (m_Mobile.Move(m_Mobile.Direction))
                    {
                         return MoveResult.SuccessAutoTurn;
                    }
               }

               return wasPushing ? MoveResult.BadState : MoveResult.Blocked;
          }

          private bool TryClearObstacles(Direction d)
          {
               DebugSay("My movement is blocked. Trying to push through.");

               var map = m_Mobile.Map;

               if (map == null) { return true; }

               var (x, y) = GetOffsetLocation(d);

               using var queue = PooledRefQueue<Item>.Create();

               var destroyables = GatherObstacles(x, y, queue);

               if (destroyables > 0)
               {
                    Effects.PlaySound(new Point3D(x, y, m_Mobile.Z), m_Mobile.Map, 0x3B3);
               }

               return ProcessObstacles(queue, d);
          }

          private (int x, int y) GetOffsetLocation(Direction d)
          {
               var x = m_Mobile.X;
               var y = m_Mobile.Y;
               Movement.Movement.Offset(d, ref x, ref y);
               return (x, y);
          }

          private int GatherObstacles(int x, int y, PooledRefQueue<Item> queue)
          {
               var destroyables = 0;

               foreach (var item in m_Mobile.Map.GetItemsInRange(new Point2D(x, y), 1))
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
               if (!m_Mobile.CanOpenDoors || item is not BaseDoor door)
               {
                    return false;
               }

               if (door.Z + door.ItemData.Height <= m_Mobile.Z || m_Mobile.Z + 16 <= door.Z)
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
               if (!m_Mobile.CanDestroyObstacles || !item.Movable || !item.ItemData.Impassable)
               {
                    return false;
               }

               if (item.Z + item.ItemData.Height <= m_Mobile.Z || m_Mobile.Z + 16 <= item.Z)
               {
                    return false;
               }

               return m_Mobile.InRange(item.GetWorldLocation(), 1);
          }

          private bool ProcessObstacles(PooledRefQueue<Item> queue, Direction d)
          {
               if (queue.Count == 0) { return true; }

               while (queue.Count > 0)
               {
                    ProcessObstacle(queue.Dequeue(), queue);
               }

               return !m_Mobile.Move(d);
          }

          private void ProcessObstacle(Item item, PooledRefQueue<Item> queue)
          {
               if (item is BaseDoor door)
               {
                    DebugSay("Opening the door.");
                    door.Use(m_Mobile);
               }
               else
               {
                    DebugSay($"Destroying item: {item.GetType().Name}");

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
                    if (check.Movable && check.ItemData.Impassable && cont.Z + check.ItemData.Height > m_Mobile.Z)
                    {
                         queue.Enqueue(check);
                    }
               }
          }

          public virtual bool MoveTo(Mobile m, bool run, int range)
          {
               if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves || m?.Deleted != false)
               {
                    return false;
               }

               int distance = (int)m_Mobile.GetDistanceToSqrt(m);

               bool shouldRun = run && distance > 5;

               if (m_Mobile.InRange(m, range))
               {
                    m_Path = null;
                    return true;
               }

               if (UseGroupMovement(m))
               {
                    return MoveToWithGroup(this, m, shouldRun, range);
               }

               if (m_Path == null && m_Mobile.InLOS(m) && DoMove(m_Mobile.GetDirectionTo(m), true))
               {
                    return true;
               }

               if (m_Path?.Goal != m)
               {
                    m_Path = new PathFollower(m_Mobile, m) { Mover = DoMoveImpl };
               }

               if (m_Path.Follow(shouldRun, 1))
               {
                    m_Path = null;
                    return true;
               }

               return false;
          }

          private bool MoveToWithCollisionAvoidance(Mobile target, bool run, int range)
          {
               int distance = (int)m_Mobile.GetDistanceToSqrt(target);

               bool shouldRun = run && distance > 5;

               var direction = m_Mobile.GetDirectionTo(target);

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

               if (m_Path?.Goal != target)
               {
                    m_Path = new PathFollower(m_Mobile, target) { Mover = DoMoveImpl };
               }

               if (m_Path.Follow(shouldRun, 1))
               {
                    m_Path = null;
                    return true;
               }

               return false;
          }

          public virtual bool WalkMobileRange(Mobile m, int iSteps, bool run, int iWantDistMin, int iWantDistMax)
          {
               if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves || m == null)
               {
                    return false;
               }

               for (var i = 0; i < iSteps; i++)
               {
                    var iCurrDist = (int)m_Mobile.GetDistanceToSqrt(m);

                    bool shouldRun = run && iCurrDist > 5;

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

               var dist = m_Mobile.GetDistanceToSqrt(m);

               return dist >= iWantDistMin && dist <= iWantDistMax;
          }

          private bool MoveTowardsOrAwayFrom(Mobile m, bool run, int iCurrDist, int iWantDistMax)
          {
               bool shouldRun = run && iCurrDist > 5;

               var needCloser = iCurrDist > iWantDistMax;

               if (needCloser && m_Path?.Goal == m)
               {
                    if (m_Path.Follow(shouldRun, 1))
                    {
                         m_Path = null;
                         return true;
                    }
               }
               else
               {
                    var dirTo = needCloser ? m_Mobile.GetDirectionTo(m, shouldRun) : m.GetDirectionTo(m_Mobile, shouldRun);

                    if (DoMove(dirTo, true))
                    {
                         m_Path = null;
                         return true;
                    }

                    if (needCloser)
                    {
                         m_Path = new PathFollower(m_Mobile, m) { Mover = DoMoveImpl };

                         if (m_Path.Follow(shouldRun, 1))
                         {
                              m_Path = null;
                              return true;
                         }
                    }
               }

               return false;
          }
     }
}
