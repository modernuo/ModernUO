/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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

namespace Server.Mobiles;

public abstract partial class BaseAI
{
    public virtual void OnCurrentOrderChanged(OrderType previous)
    {
        if (Mobile.Deleted || Mobile.ControlMaster?.Deleted != false)
        {
            return;
        }

        Activate();

        switch (Mobile.ControlOrder)
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
                    // Stop is resolved into another order; it never rests as the active order.
                    ResolveStop(previous);
                    return;
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

        // A freshly issued standing command becomes the persistent fallback and (re)anchors
        // Home. Skipped while resuming a fallback so a resume never re-anchors. See
        // ResumePersistentOrder.
        if (!_resolvingOrder && Mobile.ControlOrder is OrderType.Stay or OrderType.Follow or OrderType.Guard)
        {
            SetPersistentOrder(Mobile.ControlOrder);
        }
    }

    // "Stop" cancels the active order, mapping to a resting order based on what the pet was
    // doing: Attack/Come/etc. -> resume the persistent command; Follow/Guard -> cancel to idle
    // (None) where it stands; Stay -> remain staying at its post.
    private void ResolveStop(OrderType previous)
    {
        _commandIssuer?.RevealingAction();
        _commandIssuer = null;
        Mobile.ControlTarget = null;

        switch (previous)
        {
            case OrderType.Stay:
                {
                    _resolvingOrder = true;
                    Mobile.ControlOrder = OrderType.Stay; // remain staying; anchor untouched
                    _resolvingOrder = false;
                    break;
                }
            case OrderType.Follow:
            case OrderType.Guard:
                {
                    SetPersistentOrder(OrderType.None); // cancel standing order; anchor = current
                    _resolvingOrder = true;
                    Mobile.ControlOrder = OrderType.None; // idle
                    _resolvingOrder = false;
                    break;
                }
            default: // Attack / Come / Drop / None / etc. -> resume the standing order
                {
                    ResumePersistentOrder();
                    break;
                }
        }
    }

    private void HandleNoOrder()
    {
        Mobile.ControlTarget = null;
        Mobile.FocusMob = null;
        Mobile.Warmode = false;
        Mobile.Combatant = null;
    }

    private void HandleTransferOrder()
    {
        if (Mobile.ControlMaster?.Alive != true)
        {
            return;
        }

        _commandIssuer?.RevealingAction();
        Mobile.FocusMob = null;
        Mobile.Warmode = false;
        Mobile.Combatant = null;
        Mobile.PlaySound(Mobile.GetIdleSound());
        _commandIssuer = null;
    }

    private void HandleGuardOrder()
    {
        if (Mobile.ControlMaster?.Alive != true)
        {
            return;
        }

        _commandIssuer?.RevealingAction();
        Mobile.FocusMob = null;
        Mobile.Warmode = true;
        Mobile.PlaySound(Mobile.GetAttackSound());
        Mobile.ControlMaster?.SendLocalizedMessage(1049671, Mobile.Name);
        // ~1_NAME~ is now guarding you.
        _commandIssuer = null;
    }

    private void HandleAttackOrder()
    {
        if (Mobile.ControlMaster?.Alive != true)
        {
            return;
        }

        _commandIssuer?.RevealingAction();

        if (Mobile.ControlTarget != null &&
            !Mobile.ControlTarget.Deleted &&
            Mobile.ControlTarget.Alive)
        {
            Mobile.FocusMob = Mobile.ControlTarget;
            Mobile.Combatant = Mobile.ControlTarget;
        }
        else
        {
            Mobile.FocusMob = null;
            Mobile.Combatant = null;
        }

        Mobile.Warmode = true;
        Mobile.PlaySound(Mobile.GetAttackSound());
        _commandIssuer = null;
    }

    private void HandleFollowOrder()
    {
        if (Mobile.ControlMaster?.Alive != true)
        {
            return;
        }

        _commandIssuer?.RevealingAction();
        Mobile.FocusMob = null;
        Mobile.Warmode = false;
        Mobile.Combatant = null;
        Mobile.PlaySound(Mobile.GetIdleSound());
        _commandIssuer = null;
    }

    private void HandleStayOrder()
    {
        if (Mobile.ControlMaster?.Alive != true)
        {
            return;
        }

        _commandIssuer?.RevealingAction();
        Mobile.FocusMob = null;
        Mobile.Warmode = false;
        Mobile.Combatant = null;
        Mobile.PlaySound(Mobile.GetIdleSound());
        _commandIssuer = null;
        // Home (the stay anchor) is owned by SetPersistentOrder, not this handler.
    }

    private void HandleReleaseOrder()
    {
        if (Mobile.ControlMaster?.Alive != true)
        {
            return;
        }

        if (Mobile.Summoned)
        {
            Mobile.Kill();
            return;
        }

        if (!string.IsNullOrEmpty(Mobile.Name))
        {
            Mobile.Name = null;
        }

        _commandIssuer?.RevealingAction();
        Mobile.ControlTarget = null;
        Mobile.FocusMob = null;
        Mobile.Warmode = false;
        Mobile.Combatant = null;
        Mobile.PlaySound(Mobile.GetIdleSound());
        Mobile.BondingBegin = DateTime.MinValue;
        Mobile.OwnerAbandonTime = DateTime.MinValue;
        Mobile.IsBonded = false;
        Mobile.SetControlMaster(null);
        _commandIssuer = null;
    }

    public virtual void HandleRenameOrder()
    {
        if (Mobile.Summoned)
        {
            Mobile.ControlMaster?.SendMessage("You cannot rename a summoned creature.");
        }
        else
        {
            Mobile.ControlMaster?.SendMessage("Change name on pet health bar.");
        }
    }
}
