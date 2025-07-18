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

namespace Server.Mobiles;

public abstract partial class BaseAI
{
    public virtual void OnCurrentOrderChanged()
    {
        if (_mobile.Deleted || _mobile.ControlMaster?.Deleted != false)
        {
            return;
        }

        switch (_mobile.ControlOrder)
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
        _mobile.ControlTarget = null;
        _mobile.FocusMob = null;
        _mobile.Warmode = false;
        _mobile.Combatant = null;
    }

    private void HandleTransferOrder()
    {
        if (_mobile.ControlMaster?.Alive != true)
        {
            return;
        }

        _commandIssuer?.RevealingAction();
        _mobile.FocusMob = null;
        _mobile.Warmode = false;
        _mobile.Combatant = null;
        _mobile.PlaySound(_mobile.GetIdleSound());
        _commandIssuer = null;
    }

    private void HandleGuardOrder()
    {
        if (_mobile.ControlMaster?.Alive != true)
        {
            return;
        }

        _commandIssuer?.RevealingAction();
        _mobile.FocusMob = null;
        _mobile.Warmode = true;
        _mobile.PlaySound(_mobile.GetAttackSound());
        _mobile.ControlMaster?.SendLocalizedMessage(1049671, _mobile.Name);
        // ~1_NAME~ is now guarding you.
        _commandIssuer = null;
    }

    private void HandleAttackOrder()
    {
        if (_mobile.ControlMaster?.Alive != true)
        {
            return;
        }

        _commandIssuer?.RevealingAction();

        if (_mobile.ControlTarget != null && !_mobile.ControlTarget.Deleted
                                           && _mobile.ControlTarget.Alive)
        {
            _mobile.FocusMob = _mobile.ControlTarget;
            _mobile.Combatant = _mobile.ControlTarget;
        }
        else
        {
            _mobile.FocusMob = null;
            _mobile.Combatant = null;
        }

        _mobile.Warmode = true;
        _mobile.PlaySound(_mobile.GetAttackSound());
        _commandIssuer = null;
    }

    private void HandleFollowOrder()
    {
        if (_mobile.ControlMaster?.Alive != true)
        {
            return;
        }

        _commandIssuer?.RevealingAction();
        _mobile.FocusMob = null;
        _mobile.Warmode = false;
        _mobile.Combatant = null;
        _mobile.PlaySound(_mobile.GetIdleSound());
        _commandIssuer = null;
    }

    private void HandleStayOrder()
    {
        if (_mobile.ControlMaster?.Alive != true)
        {
            return;
        }

        _commandIssuer?.RevealingAction();
        _mobile.FocusMob = null;
        _mobile.Warmode = false;
        _mobile.Combatant = null;
        _mobile.PlaySound(_mobile.GetIdleSound());
        _mobile.Home = _mobile.Location;
        _commandIssuer = null;
    }

    private void HandleStopOrder()
    {
        if (_mobile.ControlMaster?.Alive != true)
        {
            return;
        }

        _commandIssuer?.RevealingAction();
        _mobile.ControlTarget = null;
        _mobile.FocusMob = null;
        _mobile.Warmode = false;
        _mobile.Combatant = null;
        _mobile.PlaySound(_mobile.GetIdleSound());
        _commandIssuer = null;
    }

    private void HandleReleaseOrder()
    {
        if (_mobile.ControlMaster?.Alive != true)
        {
            return;
        }

        if (_mobile.Summoned)
        {
            _mobile.Kill();
            return;
        }

        if (!string.IsNullOrEmpty(_mobile.Name))
        {
            _mobile.Name = null;
        }

        _commandIssuer?.RevealingAction();
        _mobile.ControlTarget = null;
        _mobile.FocusMob = null;
        _mobile.Warmode = false;
        _mobile.Combatant = null;
        _mobile.PlaySound(_mobile.GetIdleSound());
        _mobile.BondingBegin = DateTime.MinValue;
        _mobile.OwnerAbandonTime = DateTime.MinValue;
        _mobile.IsBonded = false;
        _mobile.SetControlMaster(null);
        _commandIssuer = null;
    }

    public virtual void HandleRenameOrder()
    {
        if (_mobile.Summoned)
        {
            _mobile.ControlMaster?.SendMessage("You cannot rename a summoned creature.");
        }
        else
        {
            _mobile.ControlMaster?.SendMessage("Change name on pet health bar.");
        }
    }
}
