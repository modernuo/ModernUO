/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PetOrderHandlers.cs                                             *
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
using Server.Mobiles;

namespace Server.Mobiles;

public abstract partial class BaseAI
{
     public virtual void OnCurrentOrderChanged()
     {
          if (m_Mobile.Deleted || m_Mobile.ControlMaster?.Deleted != false)
          {
               return;
          }

          switch (m_Mobile.ControlOrder)
          {
               case OrderType.None:
               {
                    HandleNoOrder();
                    break;
               }
               case OrderType.Come:
               case OrderType.Drop:
               case OrderType.Friend:
               case OrderType.Unfriend:
               {
                    break;
               }
               case OrderType.Release:
               {
                    HandleReleaseOrder();
                    break;
               }
               case OrderType.Stop:
               {
                    HandleStopOrder();
                    break;
               }
               case OrderType.Transfer:
               {
                    HandleTransferOrder();
                    break;
               }
               case OrderType.Stay:
               {
                    HandleStayOrder();
                    break;
               }
               case OrderType.Guard:
               {
                    HandleGuardOrder();
                    break;
               }
               case OrderType.Attack:
               {
                    HandleAttackOrder();
                    break;
               }
               case OrderType.Follow:
               {
                    HandleFollowOrder();
                    break;
               }
               case OrderType.Rename:
               {
                    HandleRenameOrder();
                    break;
               }
          }
     }

     private void HandleNoOrder()
     {
          m_Mobile.ControlTarget = null;
          m_Mobile.FocusMob = null;
          m_Mobile.Warmode = false;
          m_Mobile.Combatant = null;
     }

     private void HandleTransferOrder()
     {
          if (m_Mobile.ControlMaster?.Alive != true)
          {
               return;
          }

          _lastCommandIssuer?.RevealingAction();
          m_Mobile.ControlTarget = null;
          m_Mobile.FocusMob = null;
          m_Mobile.Warmode = false;
          m_Mobile.Combatant = null;
          m_Mobile.PlaySound(m_Mobile.GetIdleSound());
          _lastCommandIssuer = null;
     }

     private void HandleGuardOrder()
     {
          if (m_Mobile.ControlMaster?.Alive != true)
          {
               return;
          }

          _lastCommandIssuer?.RevealingAction();
          m_Mobile.FocusMob = null;
          m_Mobile.Warmode = true;
          m_Mobile.PlaySound(m_Mobile.GetAttackSound());
          m_Mobile.ControlMaster?.SendLocalizedMessage(1049671, m_Mobile.Name);
          // ~1_NAME~ is now guarding you.
          _lastCommandIssuer = null;
     }

     private void HandleAttackOrder()
     {
          if (m_Mobile.ControlMaster?.Alive != true)
          {
               return;
          }

          _lastCommandIssuer?.RevealingAction();
          m_Mobile.FocusMob = null;
          m_Mobile.Warmode = true;
          m_Mobile.PlaySound(m_Mobile.GetAttackSound());
          _lastCommandIssuer = null;
     }

     private void HandleFollowOrder()
     {
          if (m_Mobile.ControlMaster?.Alive != true)
          {
               return;
          }

          _lastCommandIssuer?.RevealingAction();
          m_Mobile.FocusMob = null;
          m_Mobile.Warmode = false;
          m_Mobile.Combatant = null;
          m_Mobile.PlaySound(m_Mobile.GetIdleSound());
          _lastCommandIssuer = null;
     }

     private void HandleStayOrder()
     {
          if (m_Mobile.ControlMaster?.Alive != true)
          {
               return;
          }

          _lastCommandIssuer?.RevealingAction();
          m_Mobile.FocusMob = null;
          m_Mobile.Warmode = false;
          m_Mobile.Combatant = null;
          m_Mobile.PlaySound(m_Mobile.GetIdleSound());
          m_Mobile.Home = m_Mobile.Location;
          _lastCommandIssuer = null;
     }

     private void HandleStopOrder()
     {
          if (m_Mobile.ControlMaster?.Alive != true)
          {
               return;
          }

          _lastCommandIssuer?.RevealingAction();
          m_Mobile.ControlTarget = null;
          m_Mobile.FocusMob = null;
          m_Mobile.Warmode = false;
          m_Mobile.Combatant = null;
          m_Mobile.PlaySound(m_Mobile.GetIdleSound());
          _lastCommandIssuer = null;
     }

     private void HandleReleaseOrder()
     {
          if (m_Mobile.ControlMaster?.Alive != true)
          {
               return;
          }

          if (m_Mobile.Summoned)
          {
               m_Mobile.Kill();
               return;
          }

          if (!string.IsNullOrEmpty(m_Mobile.Name))
          {
               m_Mobile.Name = null;
          }

          _lastCommandIssuer?.RevealingAction();
          m_Mobile.ControlTarget = null;
          m_Mobile.FocusMob = null;
          m_Mobile.Warmode = false;
          m_Mobile.Combatant = null;
          m_Mobile.PlaySound(m_Mobile.GetIdleSound());
          m_Mobile.BondingBegin = DateTime.MinValue;
          m_Mobile.OwnerAbandonTime = DateTime.MinValue;
          m_Mobile.IsBonded = false;
          m_Mobile.SetControlMaster(null);
          _lastCommandIssuer = null;
     }

     public virtual void HandleRenameOrder()
     {
          if (m_Mobile.Summoned)
          {
               m_Mobile.ControlMaster?.SendMessage("You cannot rename a summoned creature.");
          }
          else
          {
               m_Mobile.ControlMaster?.SendMessage("Change name on pet health bar.");
          }
     }
}