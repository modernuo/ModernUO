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

namespace Server.Mobiles;

public abstract partial class BaseAI
{
    public virtual bool Obey()
    {
        if (_mobile.Deleted)
        {
            return false;
        }

        switch (_mobile.ControlOrder)
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

        _mobile.Warmode = IsValidCombatant(_mobile.Combatant);

        WalkRandom(3, 2, 1);
        return true;
    }

    public virtual bool DoOrderCome()
    {
        if (CheckHerding())
        {
            DebugSay($"I am being herded by {_mobile.ControlTarget?.Name ?? "Unknown"}.");
            return true;
        }

        if (_mobile.ControlMaster?.Deleted != false)
        {
            return true;
        }

        WalkMobileRange(_mobile.ControlMaster, 1, false, 1, 2);

        if (_mobile.GetDistanceToSqrt(_mobile.ControlMaster) <= 2)
        {
            _mobile.ControlOrder = OrderType.Stay;
        }

        return true;
    }

    public virtual bool DoOrderFollow()
    {
        if (CheckHerding())
        {
            DebugSay($"I am being herded by {_mobile.ControlTarget?.Name ?? "Unknown"}.");
            return true;
        }

        if (_mobile.ControlTarget?.Deleted == false && _mobile.ControlTarget != _mobile)
        {
            FollowTarget();
        }
        else
        {
            DebugSay("I have no one to follow.");

            _mobile.ControlOrder = OrderType.None;
        }

        return true;
    }

    private void FollowTarget()
    {
        var currentDistance = (int)_mobile.GetDistanceToSqrt(_mobile.ControlTarget);

        if (currentDistance > _mobile.RangePerception)
        {
            DebugSay($"Master {_mobile.ControlMaster?.Name ?? "Unknown"} is missing. Staying put.");
            return;
        }

        DebugSay($"I am ordered to follow {_mobile.ControlTarget?.Name}.");

        if (currentDistance > 1)
        {
            WalkMobileRange(_mobile.ControlTarget, 1, currentDistance > 2, 1, 2);
        }
    }

    public virtual bool DoOrderDrop()
    {
        if (_mobile.IsDeadPet || !_mobile.CanDrop)
        {
            return true;
        }

        DebugSay($"I am ordered to drop my items by {_mobile.ControlMaster?.Name ?? "Unknown"}.");

        _mobile.ControlOrder = OrderType.None;

        DropItems();
        return true;
    }

    private void DropItems()
    {
        var pack = _mobile.Backpack;

        if (pack == null)
        {
            return;
        }

        var items = pack.Items;

        for (var i = items.Count - 1; i >= 0; --i)
        {
            if (i < items.Count)
            {
                items[i].MoveToWorld(_mobile.Location, _mobile.Map);
            }
        }
    }

    public virtual bool DoOrderFriend()
    {
        var from = _mobile.ControlMaster;
        var to = _mobile.ControlTarget;

        HandleFriendRequest(from, to);
        return true;
    }

    private void HandleFriendRequest(Mobile from, Mobile to)
    {
        var youngFrom = from is PlayerMobile mobile && mobile.Young;
        var youngTo = to is PlayerMobile playerMobile && playerMobile.Young;

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

        if (from?.Deleted != false || to?.Deleted != false || from == to || !to.Player)
        {
            _mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039);
            // *looks confused*
            return;
        }

        if (from.HasTrade || to.HasTrade)
        {
            (from.HasTrade ? from : to).SendLocalizedMessage(1070947);
            // You cannot friend a pet with a trade pending
            return;
        }

        if (_mobile.IsPetFriend(to))
        {
            from.SendLocalizedMessage(1049691);
            // That person is already a friend.
            _mobile.ControlOrder = OrderType.None;
            return;
        }

        if (!_mobile.AllowNewPetFriend)
        {
            from.SendLocalizedMessage(1005482);
            // Your pet does not seem to be interested in making new friends right now.
            return;
        }

        from.SendLocalizedMessage(1049676, $"{_mobile.Name}\t{to.Name}");
        // ~1_NAME~ will now accept movement commands from ~2_NAME~.

        to.SendLocalizedMessage(1043246, $"{from.Name}\t{_mobile.Name}");
        // ~1_NAME~ has granted you the ability to give orders to their pet ~2_PET_NAME~.
        // This creature will now consider you as a friend.

        _mobile.AddPetFriend(to);

        _mobile.ControlTarget = to;
        _mobile.ControlOrder = OrderType.Follow;
    }

    public virtual bool DoOrderUnfriend()
    {
        var from = _mobile.ControlMaster;
        var to = _mobile.ControlTarget;

        HandleUnfriendRequest(from, to);
        return true;
    }

    private void HandleUnfriendRequest(Mobile from, Mobile to)
    {
        if (from?.Deleted != false || to?.Deleted != false || from == to || !to.Player)
        {
            _mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039);
            // *looks confused*
            return;
        }

        if (!_mobile.IsPetFriend(to))
        {
            from.SendLocalizedMessage(1070953);
            // That person is not a friend.
            _mobile.ControlOrder = OrderType.None;
            return;
        }

        from.SendLocalizedMessage(1070951, $"{_mobile.Name}\t{to.Name}");
        // ~1_NAME~ will no longer accept movement commands from ~2_NAME~.

        to.SendLocalizedMessage(1070952, $"{from.Name}\t{_mobile.Name}");
        // ~1_NAME~ has no longer granted you the ability to give orders to their pet ~2_PET_NAME~.
        // This creature will no longer consider you as a friend.

        _mobile.RemovePetFriend(to);

        _mobile.ControlTarget = from;
        _mobile.ControlOrder = OrderType.Follow;
    }

    public virtual bool DoOrderGuard()
    {
        if (_mobile.IsDeadPet || _mobile.ControlMaster?.Deleted != false)
        {
            return true;
        }

        var controlMaster = _mobile.ControlMaster;
        var combatant = _mobile.Combatant;
        FindCombatant();

        if (IsValidCombatant(_mobile.Combatant))
        {
            combatant = _mobile.Combatant;

            DebugSay($"Attacking target: {combatant.Name}");

            _mobile.Combatant = combatant;
            _mobile.FocusMob = combatant;
            Action = ActionType.Combat;

            Think();
        }
        else
        {
            DebugSay($"Guarding my master, {controlMaster.Name}.");

            var guardLocation = controlMaster.Location;

            var distance = (int)_mobile.GetDistanceToSqrt(guardLocation);

            if (distance > 3)
            {
                DoMove(_mobile.GetDirectionTo(guardLocation));
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
        if (_mobile.IsDeadPet)
        {
            return false;
        }

        if (IsInvalidControlTarget(_mobile.ControlTarget))
        {
            HandleInvalidControlTarget();
        }
        else
        {
            _mobile.Combatant = _mobile.ControlTarget;

            DebugSay($"Attacking target: {_mobile.ControlTarget?.Name}");

            Think();
        }

        return true;
    }

    private bool IsInvalidControlTarget(Mobile target) => target?.Deleted != false || target.Map != _mobile.Map || !target.Alive || target.IsDeadBondedPet;

    private void HandleInvalidControlTarget()
    {
        DebugSay("Target is either dead, hidden, or out of range.");

        _mobile.ControlOrder = Core.AOS || _mobile.IsBonded ? OrderType.Follow : OrderType.None;

        if (_mobile.FightMode is FightMode.Closest or FightMode.Aggressor)
        {
            FindCombatant();
        }
    }

    private void FindCombatant()
    {
        foreach (var aggr in _mobile.GetMobilesInRange(_mobile.RangePerception))
        {
            if (!_mobile.CanSee(aggr) || aggr.Combatant != _mobile || aggr.IsDeadBondedPet || !aggr.Alive)
            {
                continue;
            }

            if (_mobile.InLOS(aggr))
            {
                _mobile.ControlTarget = aggr;
                _mobile.ControlOrder = OrderType.Attack;
                _mobile.Combatant = aggr;

                DebugSay($"{aggr.Name} is still alive. Resuming attacks...");

                Think();
                break;
            }
        }
    }

    public virtual bool DoOrderRelease()
    {
        DebugSay("I have been released to the wild.");

        var spawner = _mobile.Spawner;

        if (spawner != null && spawner.HomeLocation != Point3D.Zero)
        {
            _mobile.Home = spawner.HomeLocation;
            _mobile.RangeHome = spawner.HomeRange;
        }
        else
        {
            Action = ActionType.Wander;
        }

        if (_mobile.DeleteOnRelease || _mobile.IsDeadPet)
        {
            _mobile.Delete();
        }
        else
        {
            _mobile.BeginDeleteTimer();

            if (_mobile.CanDrop)
            {
                _mobile.DropBackpack();
            }
        }

        return true;
    }

    public virtual bool DoOrderStay()
    {
        if (CheckHerding())
        {
            DebugSay($"I am being herded by {_mobile.ControlTarget?.Name ?? "Unknown"}.");
        }
        else
        {
            DebugSay($"I have been ordered to stay by {_mobile.ControlMaster?.Name ?? "Unknown"}.");
        }

        WalkRandomInHome(3, 2, 1);
        return true;
    }

    public virtual bool DoOrderStop()
    {
        if (CheckHerding())
        {
            DebugSay($"I am being herded by {_mobile.ControlTarget?.Name ?? "Unknown"}.");
        }
        else
        {
            DebugSay($"I have been ordered to stop by {_mobile.ControlMaster?.Name ?? "Unknown"}.");
        }

        if (Core.ML)
        {
            WalkRandomInHome(5, 2, 1);
        }

        return true;
    }

    public virtual bool DoOrderTransfer()
    {
        if (_mobile.IsDeadPet)
        {
            return true;
        }

        var from = _mobile.ControlMaster;
        var to = _mobile.ControlTarget;

        if (from?.Deleted == false && to?.Deleted == false && from != to && to.Player)
        {
            DebugSay($"Beginning transfer with {to.Name}");

            var youngFrom = from is PlayerMobile mobile && mobile.Young;
            var youngTo = to is PlayerMobile playerMobile && playerMobile.Young;

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

            if (!_mobile.CanBeControlledBy(to))
            {
                SendTransferRefusalMessages(from, to, 1043248, 1043249);
                // 1043248: The pet refuses to be transferred because it will not obey ~1_NAME~.~3_BLANK~
                // 1043249: The pet will not accept you as a master because it does not trust you.~3_BLANK~
                return false;
            }

            if (!_mobile.CanBeControlledBy(from))
            {
                SendTransferRefusalMessages(from, to, 1043250, 1043251);
                // 1043250: The pet refuses to be transferred because it will not obey you sufficiently.~3_BLANK~
                // 1043251: The pet will not accept you as a master because it does not trust ~2_NAME~.~3_BLANK~
                return false;
            }

            if (_mobile.Combatant != null || _mobile.Aggressors.Count > 0 ||
                _mobile.Aggressed.Count > 0 || Core.TickCount < _mobile.NextCombatTime)
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
            container.DropItem(new TransferItem(_mobile));
        }

        _mobile.ControlOrder = OrderType.Stay;
        return true;
    }

    private static void SendTransferRefusalMessages(Mobile from, Mobile to, int fromMessage, int toMessage)
    {
        var args = $"{to.Name}\t{from.Name}\t ";

        from.SendLocalizedMessage(fromMessage, args);
        to.SendLocalizedMessage(toMessage, args);
    }
}
