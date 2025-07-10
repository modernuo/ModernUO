/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PetOrders.cs                                                    *
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
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Network;
using Server.Engines.Spawners;

namespace Server.Mobiles;

public abstract partial class BaseAI
{
     public virtual bool Obey()
     {
          if (m_Mobile.Deleted)
          {
               return false;
          }

          switch (m_Mobile.ControlOrder)
          {
               case OrderType.None:
               {
                    return DoOrderNone();
               }
               case OrderType.Come:
               {
                    return DoOrderCome();
               }
               case OrderType.Drop:
               {
                    return DoOrderDrop();
               }
               case OrderType.Friend:
               {
                    return DoOrderFriend();
               }
               case OrderType.Unfriend:
               {
                    return DoOrderUnfriend();
               }
               case OrderType.Guard:
               {
                    return DoOrderGuard();
               }
               case OrderType.Attack:
               {
                    return DoOrderAttack();
               }
               case OrderType.Release:
               {
                    return DoOrderRelease();
               }
               case OrderType.Stay:
               {
                    return DoOrderStay();
               }
               case OrderType.Stop:
               {
                    return DoOrderStop();
               }
               case OrderType.Follow:
               {
                    return DoOrderFollow();
               }
               case OrderType.Transfer:
               {
                    return DoOrderTransfer();
               }
               default:
               {
                    return false;
               }
          }
     }

     public virtual bool DoOrderNone()
     {
          DebugSay("I currently have no orders.");

          m_Mobile.Warmode = IsValidCombatant(m_Mobile.Combatant);

          WalkRandom(3, 2, 1);
          return true;
     }

     public virtual bool DoOrderCome()
     {
          if (m_Mobile.ControlMaster?.Deleted != false)
          {
               return true;
          }

          int currentDistance = (int)m_Mobile.GetDistanceToSqrt(m_Mobile.ControlMaster);

          if (currentDistance > m_Mobile.RangePerception)
          {
               HandleLostMaster();
          }
          else
          {
               HandleComeOrder(currentDistance);
          }

          return true;
     }

     private void HandleLostMaster()
     {
          DebugSay($"Master {m_Mobile.ControlMaster?.Name ?? "Unknown"} is missing. Staying put.");

          m_Mobile.ControlOrder = OrderType.None;
     }

     private void HandleComeOrder(int currentDistance)
     {
          DebugSay($"{m_Mobile.ControlTarget?.Name ?? "Unknown"}, has ordered me to come here.");

          if (WalkMobileRange(m_Mobile.ControlMaster, 1, currentDistance > 2, 1, 2))
          {
               m_Mobile.Warmode = IsValidCombatant(m_Mobile.Combatant);
          }
     }

     public virtual bool DoOrderFollow()
     {
          if (CheckHerding())
          {
               DebugSay($"I am being herded by {m_Mobile.ControlTarget?.Name ?? "Unknown"}.");
               return true;
          }

          if (m_Mobile.ControlTarget?.Deleted == false && m_Mobile.ControlTarget != m_Mobile)
          {
               FollowTarget();
          }
          else
          {
               DebugSay("I have no one to follow.");

               m_Mobile.ControlOrder = OrderType.None;
          }

          return true;
     }

     private void FollowTarget()
     {
          var currentDistance = (int)m_Mobile.GetDistanceToSqrt(m_Mobile.ControlTarget);

          if (currentDistance > m_Mobile.RangePerception)
          {
               DebugSay($"Master {m_Mobile.ControlMaster?.Name ?? "Unknown"} is missing. Staying put.");
               return;
          }

          DebugSay($"I am ordered to follow {m_Mobile.ControlTarget?.Name}.");

          if (currentDistance > 1)
          {
               WalkMobileRange(m_Mobile.ControlTarget, 1, currentDistance > 2, 1, 2);
          }
     }

     public virtual bool DoOrderDrop()
     {
          if (m_Mobile.IsDeadPet || !m_Mobile.CanDrop)
          {
               return true;
          }

          DebugSay($"I am ordered to drop my items by {m_Mobile.ControlMaster?.Name ?? "Unknown"}.");

          m_Mobile.ControlOrder = OrderType.None;

          DropItems();
          return true;
     }

     private void DropItems()
     {
          var pack = m_Mobile.Backpack;

          if (pack == null)
          {
               return;
          }

          var items = pack.Items;

          for (int i = items.Count - 1; i >= 0; --i)
          {
               if (i < items.Count)
               {
                    items[i].MoveToWorld(m_Mobile.Location, m_Mobile.Map);
               }
          }
     }

     public virtual bool DoOrderFriend()
     {
          var from = m_Mobile.ControlMaster;
          var to = m_Mobile.ControlTarget;

          HandleFriendRequest(from, to);
          return true;
     }

     private void HandleFriendRequest(Mobile from, Mobile to)
     {
          bool youngFrom = from is PlayerMobile mobile && mobile.Young;
          bool youngTo = to is PlayerMobile playerMobile && playerMobile.Young;

          if (youngFrom && !youngTo)
          {
               from.SendLocalizedMessage(502040);
               // As a young player, you may not friend pets to older players.
               return;
          }

          if (!youngFrom && youngTo)
          {
               from.SendLocalizedMessage(502041);
               // As an older player, you may not friend pets to young players.
               return;
          }

          if (!from.CanBeBeneficial(to, true))
          {
               return;
          }
          else if (from?.Deleted != false || to?.Deleted != false || from == to || !to.Player)
          {
               m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039);
               // *looks confused*
               return;
          }
          else if (from.HasTrade || to.HasTrade)
          {
               (from.HasTrade ? from : to).SendLocalizedMessage(1070947);
               // You cannot friend a pet with a trade pending
               return;
          }
          else if (m_Mobile.IsPetFriend(to))
          {
               from.SendLocalizedMessage(1049691);
               // That person is already a friend.
               m_Mobile.ControlOrder = OrderType.None;
               return;
          }
          else if (!m_Mobile.AllowNewPetFriend)
          {
               from.SendLocalizedMessage(1005482);
               // Your pet does not seem to be interested in making new friends right now.
               return;
          }
          else
          {
               from.SendLocalizedMessage(1049676, $"{m_Mobile.Name}\t{to.Name}");
               // ~1_NAME~ will now accept movement commands from ~2_NAME~.

               to.SendLocalizedMessage(1043246, $"{from.Name}\t{m_Mobile.Name}");
               // ~1_NAME~ has granted you the ability to give orders to their pet ~2_PET_NAME~.
               // This creature will now consider you as a friend.

               m_Mobile.AddPetFriend(to);

               m_Mobile.ControlTarget = to;
               m_Mobile.ControlOrder = OrderType.Follow;
          }
     }

     public virtual bool DoOrderUnfriend()
     {
          var from = m_Mobile.ControlMaster;
          var to = m_Mobile.ControlTarget;

          HandleUnfriendRequest(from, to);
          return true;
     }

     private void HandleUnfriendRequest(Mobile from, Mobile to)
     {
          if (from?.Deleted != false || to?.Deleted != false || from == to || !to.Player)
          {
               m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039);
               // *looks confused*
               return;
          }
          else if (!m_Mobile.IsPetFriend(to))
          {
               from.SendLocalizedMessage(1070953);
               // That person is not a friend.
               m_Mobile.ControlOrder = OrderType.None;
               return;
          }
          else
          {
               from.SendLocalizedMessage(1070951, $"{m_Mobile.Name}\t{to.Name}");
               // ~1_NAME~ will no longer accept movement commands from ~2_NAME~.

               to.SendLocalizedMessage(1070952, $"{from.Name}\t{m_Mobile.Name}");
               // ~1_NAME~ has no longer granted you the ability to give orders to their pet ~2_PET_NAME~.
               // This creature will no longer consider you as a friend.

               m_Mobile.RemovePetFriend(to);

               m_Mobile.ControlTarget = from;
               m_Mobile.ControlOrder = OrderType.Follow;
          }
     }

     public virtual bool DoOrderGuard()
     {
          if (m_Mobile.IsDeadPet || m_Mobile.ControlMaster?.Deleted != false)
          {
               return true;
          }

          var controlMaster = m_Mobile.ControlMaster;
          Mobile combatant = m_Mobile.Combatant;
          FindCombatant();

          if (IsValidCombatant(m_Mobile.Combatant))
          {
               combatant = m_Mobile.Combatant;
               
               DebugSay($"Attacking target: {combatant.Name}");

               m_Mobile.Combatant = combatant;
               m_Mobile.FocusMob = combatant;
               Action = ActionType.Combat;

               Think();
          }
          else
          {
               DebugSay($"Guarding my master, {controlMaster.Name}.");

               var guardLocation = controlMaster.Location;

               int distance = (int)m_Mobile.GetDistanceToSqrt(guardLocation);

               if (distance > 3)
               {
                    DoMove(m_Mobile.GetDirectionTo(guardLocation));
               }
               else
               {
                    WalkRandom(3, 1, 1);
               }
          }

          return true;
     }

     public virtual bool DoOrderAttack()
     {
          if (m_Mobile.IsDeadPet)
          {
               return false;
          }

          if (IsInvalidControlTarget(m_Mobile.ControlTarget))
          {
               HandleInvalidControlTarget();
          }
          else
          {
               m_Mobile.Combatant = m_Mobile.ControlTarget;
               
               DebugSay($"Attacking target: {m_Mobile.ControlTarget?.Name}");

               Think();
          }

          return true;
     }

     private bool IsInvalidControlTarget(Mobile target)
     {
          return target?.Deleted != false || target.Map != m_Mobile.Map || !target.Alive || target.IsDeadBondedPet;
     }

     private void HandleInvalidControlTarget()
     {
          DebugSay("Target is either dead, hidden, or out of range.");

          m_Mobile.ControlOrder = (Core.AOS || m_Mobile.IsBonded) ? OrderType.Follow : OrderType.None;

          if (m_Mobile.FightMode is FightMode.Closest or FightMode.Aggressor)
          {
               FindCombatant();
          }
     }

     private void FindCombatant()
     {
          foreach (var aggr in m_Mobile.GetMobilesInRange(m_Mobile.RangePerception))
          {
               if (!m_Mobile.CanSee(aggr) || aggr.Combatant != m_Mobile || aggr.IsDeadBondedPet || !aggr.Alive)
               {
                    continue;
               }

               if (m_Mobile.InLOS(aggr))
               {
                    m_Mobile.ControlTarget = aggr;
                    m_Mobile.ControlOrder = OrderType.Attack;
                    m_Mobile.Combatant = aggr;

                    DebugSay($"{aggr.Name} is still alive. Resuming attacks...");

                    Think();
                    break;
               }
          }
     }

     public virtual bool DoOrderRelease()
     {
          DebugSay("I have been released to the wild.");

          var spawner = m_Mobile.Spawner;

          if (spawner != null && spawner.HomeLocation != Point3D.Zero)
          {
               m_Mobile.Home = spawner.HomeLocation;
               m_Mobile.RangeHome = spawner.HomeRange;
          }
          else
          {
               Action = ActionType.Wander;
          }

          if (m_Mobile.DeleteOnRelease || m_Mobile.IsDeadPet)
          {
               m_Mobile.Delete();
          }
          else
          {
               m_Mobile.BeginDeleteTimer();

               if (m_Mobile.CanDrop)
               {
                    m_Mobile.DropBackpack();
               }
          }

          return true;
     }

     public virtual bool DoOrderStay()
     {
          if (CheckHerding())
          {
               DebugSay($"I am being herded by {m_Mobile.ControlTarget?.Name ?? "Unknown"}.");
          }
          else
          {
               DebugSay($"I have been ordered to stay by {m_Mobile.ControlMaster?.Name ?? "Unknown"}.");
          }

          WalkRandomInHome(3, 2, 1);
          return true;
     }

     public virtual bool DoOrderStop()
     {
          if (CheckHerding())
          {
               DebugSay($"I am being herded by {m_Mobile.ControlTarget?.Name ?? "Unknown"}.");
          }
          else
          {
               DebugSay($"I have been ordered to stop by {m_Mobile.ControlMaster?.Name ?? "Unknown"}.");
          }

          if (Core.ML)
          {
               WalkRandomInHome(5, 2, 1);
          }

          return true;
     }

     public virtual bool DoOrderTransfer()
     {
          if (m_Mobile.IsDeadPet)
          {
               return true;
          }

          var from = m_Mobile.ControlMaster;
          var to = m_Mobile.ControlTarget;

          if (from?.Deleted == false && to?.Deleted == false && from != to && to.Player)
          {
               DebugSay($"Beginning transfer with {to.Name}");

               bool youngFrom = from is PlayerMobile mobile && mobile.Young;
               bool youngTo = to is PlayerMobile playerMobile && playerMobile.Young;

               if (youngFrom && !youngTo)
               {
                    from.SendLocalizedMessage(502040);
                    // As a young player, you may not friend pets to older players.
                    return true;
               }

               if (!youngFrom && youngTo)
               {
                    from.SendLocalizedMessage(502041);
                    // As an older player, you may not friend pets to young players.
                    return true;
               }

               if (!m_Mobile.CanBeControlledBy(to))
               {
                    SendTransferRefusalMessages(from, to, 1043248, 1043249);
                    // 1043248: The pet refuses to be transferred because it will not obey ~1_NAME~.~3_BLANK~
                    // 1043249: The pet will not accept you as a master because it does not trust you.~3_BLANK~
                    return false;
               }

               if (!m_Mobile.CanBeControlledBy(from))
               {
                    SendTransferRefusalMessages(from, to, 1043250, 1043251);
                    // 1043250: The pet refuses to be transferred because it will not obey you sufficiently.~3_BLANK~
                    // 1043251: The pet will not accept you as a master because it does not trust ~2_NAME~.~3_BLANK~
                    return false;
               }

               if (m_Mobile.Combatant != null || m_Mobile.Aggressors.Count > 0 ||
                    m_Mobile.Aggressed.Count > 0 || Core.TickCount < m_Mobile.NextCombatTime)
               {
                    from.SendMessage("You can not transfer a pet while in combat.");
                    to.SendMessage("You can not transfer a pet while in combat.");
                    return false;
               }

               var fromState = from.NetState;
               var toState = to.NetState;

               if (fromState == null || toState == null)
               {
                    return false;
               }

               if (from.HasTrade || to.HasTrade)
               {
                    from.SendLocalizedMessage(1010507);
                    // You cannot transfer a pet with a trade pending
                    to.SendLocalizedMessage(1010507);
                    // You cannot transfer a pet with a trade pending
                    return false;
               }

               var container = fromState.AddTrade(toState);
               container.DropItem(new TransferItem(m_Mobile));
          }

          m_Mobile.ControlOrder = OrderType.Stay;
          return true;
     }

     private static void SendTransferRefusalMessages(Mobile from, Mobile to, int fromMessage, int toMessage)
     {
          var args = $"{to.Name}\t{from.Name}\t ";

          from.SendLocalizedMessage(fromMessage, args);
          to.SendLocalizedMessage(toMessage, args);
     }
}
