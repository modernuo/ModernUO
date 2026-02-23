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
    private OrderType _lastPetOrder = OrderType.None;

    public virtual bool Obey() =>
        !Mobile.Deleted && Mobile.ControlOrder switch
        {
            OrderType.None     => DoOrderNone(),
            OrderType.Come     => DoOrderCome(),
            OrderType.Drop     => DoOrderDrop(),
            OrderType.Friend   => DoOrderFriend(),
            OrderType.Unfriend => DoOrderUnfriend(),
            OrderType.Guard    => DoOrderGuard(),
            OrderType.Attack   => DoOrderAttack(),
            OrderType.Release  => DoOrderRelease(),
            OrderType.Stay     => DoOrderStay(),
            OrderType.Stop     => DoOrderStop(),
            OrderType.Follow   => DoOrderFollow(),
            OrderType.Transfer => DoOrderTransfer(),
            _                  => false
        };

    public virtual bool DoOrderNone()
    {
        DebugSay("I currently have no orders.");

        Mobile.Warmode = IsValidCombatant(Mobile.Combatant);

        if (_lastPetOrder == OrderType.Guard)
        {
            DebugSay("Target lost, resuming guard duty.");

            Mobile.ControlOrder = OrderType.Guard;
            _lastPetOrder = OrderType.None;
            return true;
        }
        else if (_lastPetOrder == OrderType.Stay)
        {
            DebugSay("Target lost, resuming stay position.");

            Mobile.ControlOrder = OrderType.Stay;
            _lastPetOrder = OrderType.None;
            return true;
        }
        else if (_lastPetOrder == OrderType.Follow)
        {
            DebugSay("Target lost, resuming follow command.");

            Mobile.ControlTarget = Mobile.ControlMaster;
            Mobile.ControlOrder = OrderType.Follow;
            _lastPetOrder = OrderType.None;
            return true;
        }

        WalkRandom(3, 2, 1);
        return true;
    }

    public virtual bool DoOrderCome()
    {
        if (CheckHerding())
        {
            this.DebugSayFormatted($"I am being herded by {Mobile.ControlTarget?.Name ?? "Unknown"}.");
            return true;
        }

        if (Mobile.ControlMaster?.Deleted != false)
        {
            return true;
        }

        WalkMobileRange(Mobile.ControlMaster, 1, false, 1, 2);

        if (Mobile.GetDistanceToSqrt(Mobile.ControlMaster) <= 2)
        {
            Mobile.ControlOrder = OrderType.Stay;
        }

        return true;
    }

    public virtual bool DoOrderFollow()
    {
        if (CheckHerding())
        {
            this.DebugSayFormatted($"I am being herded by {Mobile.ControlTarget?.Name ?? "Unknown"}.");
            return true;
        }

        if (Mobile.ControlTarget?.Deleted == false && Mobile.ControlTarget != Mobile)
        {
            _lastPetOrder = OrderType.Follow;

            FollowTarget();
        }
        else
        {
            DebugSay("I have no one to follow.");

            Mobile.ControlOrder = OrderType.None;
        }

        return true;
    }

    private void FollowTarget()
    {
        var currentDistance = (int)Mobile.GetDistanceToSqrt(Mobile.ControlTarget);

        if (currentDistance > Mobile.RangePerception)
        {
            this.DebugSayFormatted($"Master {Mobile.ControlMaster?.Name ?? "Unknown"} is missing. Staying put.");
            return;
        }

        this.DebugSayFormatted($"I am ordered to follow {Mobile.ControlTarget?.Name}.");

        if (currentDistance > 1)
        {
            WalkMobileRange(Mobile.ControlTarget, 1, currentDistance > 2, 1, 2);
        }
    }

    public virtual bool DoOrderDrop()
    {
        if (Mobile.IsDeadPet || !Mobile.CanDrop)
        {
            return true;
        }

        this.DebugSayFormatted($"I am ordered to drop my items by {Mobile.ControlMaster?.Name ?? "Unknown"}.");

        Mobile.ControlOrder = OrderType.None;

        DropItems();
        return true;
    }

    private void DropItems()
    {
        var pack = Mobile.Backpack;

        if (pack == null)
        {
            return;
        }

        var items = pack.Items;

        for (var i = items.Count - 1; i >= 0; --i)
        {
            if (i < items.Count)
            {
                items[i].MoveToWorld(Mobile.Location, Mobile.Map);
            }
        }
    }

    public virtual bool DoOrderFriend()
    {
        var from = Mobile.ControlMaster;
        var to = Mobile.ControlTarget;

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

        if (to?.Deleted != false || from == to || !to.Player)
        {
            Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039);
            // *looks confused*
            return;
        }

        if (from.HasTrade || to.HasTrade)
        {
            (from.HasTrade ? from : to).SendLocalizedMessage(1070947);
            // You cannot friend a pet with a trade pending
            return;
        }

        if (Mobile.IsPetFriend(to))
        {
            from.SendLocalizedMessage(1049691);
            // That person is already a friend.
            Mobile.ControlOrder = OrderType.None;
            return;
        }

        if (!Mobile.AllowNewPetFriend)
        {
            from.SendLocalizedMessage(1005482);
            // Your pet does not seem to be interested in making new friends right now.
            return;
        }

        from.SendLocalizedMessage(1049676, $"{Mobile.Name}\t{to.Name}");
        // ~1_NAME~ will now accept movement commands from ~2_NAME~.

        to.SendLocalizedMessage(1043246, $"{from.Name}\t{Mobile.Name}");
        // ~1_NAME~ has granted you the ability to give orders to their pet ~2_PET_NAME~.
        // This creature will now consider you as a friend.

        Mobile.AddPetFriend(to);

        Mobile.ControlTarget = to;
        Mobile.ControlOrder = OrderType.Follow;
    }

    public virtual bool DoOrderUnfriend()
    {
        var from = Mobile.ControlMaster;
        var to = Mobile.ControlTarget;

        HandleUnfriendRequest(from, to);
        return true;
    }

    private void HandleUnfriendRequest(Mobile from, Mobile to)
    {
        if (from?.Deleted != false || to?.Deleted != false || from == to || !to.Player)
        {
            Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039);
            // *looks confused*
            return;
        }

        if (!Mobile.IsPetFriend(to))
        {
            from.SendLocalizedMessage(1070953);
            // That person is not a friend.
            Mobile.ControlOrder = OrderType.None;
            return;
        }

        from.SendLocalizedMessage(1070951, $"{Mobile.Name}\t{to.Name}");
        // ~1_NAME~ will no longer accept movement commands from ~2_NAME~.

        to.SendLocalizedMessage(1070952, $"{from.Name}\t{Mobile.Name}");
        // ~1_NAME~ has no longer granted you the ability to give orders to their pet ~2_PET_NAME~.
        // This creature will no longer consider you as a friend.

        Mobile.RemovePetFriend(to);

        Mobile.ControlTarget = from;
        Mobile.ControlOrder = OrderType.Follow;
    }

    public virtual bool DoOrderGuard()
    {
        var controlMaster = Mobile.ControlMaster;

        if (Mobile.IsDeadPet || controlMaster?.Deleted != false)
        {
            return true;
        }

        _lastPetOrder = OrderType.Guard;

        FindCombatant();

        if (IsValidCombatant(Mobile.Combatant))
        {
            var combatant = Mobile.Combatant;

            this.DebugSayFormatted($"Attacking target: {combatant.Name}");

            Mobile.Combatant = combatant;
            Mobile.FocusMob = combatant;
            Action = ActionType.Combat;

            Think();
        }
        else
        {
            this.DebugSayFormatted($"Guarding my master, {controlMaster.Name}.");

            var guardLocation = controlMaster.Location;

            var distance = (int)Mobile.GetDistanceToSqrt(guardLocation);

            if (distance > 3)
            {
                DoMove(Mobile.GetDirectionTo(guardLocation));
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
        if (Mobile.IsDeadPet)
        {
            return false;
        }

        if (IsInvalidControlTarget(Mobile.ControlTarget))
        {
            HandleInvalidControlTarget();
        }
        else
        {
            Mobile.Combatant = Mobile.ControlTarget;

            this.DebugSayFormatted($"Attacking target: {Mobile.ControlTarget?.Name}");

            Think();
        }

        return true;
    }

    private bool IsInvalidControlTarget(Mobile target) => target?.Deleted != false || target.Map != Mobile.Map
        || !target.Alive || target.IsDeadBondedPet;

    private void HandleInvalidControlTarget()
    {
        DebugSay("Target is either dead, hidden, or out of range.");

        Mobile.ControlOrder = Core.AOS || Mobile.IsBonded ? OrderType.Follow : OrderType.None;

        if (Mobile.FightMode is FightMode.Closest or FightMode.Aggressor)
        {
            FindCombatant();
        }
    }

    private void FindCombatant()
    {
        var controlMaster = Mobile.ControlMaster;

        foreach (var aggr in Mobile.GetMobilesInRange(Mobile.RangePerception))
        {
            if (!Mobile.CanSee(aggr) || aggr.IsDeadBondedPet || !aggr.Alive)
            {
                continue;
            }

            var isAttackingPet = aggr.Combatant == Mobile;
            var isAttackingMaster = controlMaster != null && aggr.Combatant == controlMaster;

            if (isAttackingPet || isAttackingMaster)
            {
                if (Mobile.InLOS(aggr))
                {
                    Mobile.ControlTarget = aggr;
                    Mobile.ControlOrder = OrderType.Attack;
                    Mobile.Combatant = aggr;

                    var target = isAttackingMaster ? "master" : "me";
                    this.DebugSayFormatted($"{aggr.Name} is attacking my {target}! Engaging...");

                    Think();
                    return;
                }
            }
        }

        if (controlMaster?.Aggressors != null)
        {
            for (var i = 0; i < controlMaster.Aggressors.Count; i++)
            {
                var aggressor = controlMaster.Aggressors[i].Attacker;

                if (aggressor?.Deleted != false || !aggressor.Alive || aggressor.IsDeadBondedPet)
                {
                    continue;
                }

                if (Mobile.InRange(aggressor, Mobile.RangePerception) && Mobile.CanSee(aggressor) && Mobile.InLOS(aggressor))
                {
                    Mobile.ControlTarget = aggressor;
                    Mobile.ControlOrder = OrderType.Attack;
                    Mobile.Combatant = aggressor;

                    this.DebugSayFormatted($"{aggressor.Name} recently attacked my master! Retaliating...");

                    Think();
                    return;
                }
            }
        }
    }

    public virtual bool DoOrderRelease()
    {
        DebugSay("I have been released to the wild.");

        var spawner = Mobile.Spawner;

        if (spawner != null)
        {
            Mobile.Home = spawner.GetSpawnPosition(Mobile, spawner.Map);
            Mobile.RangeHome = spawner.WalkingRange;
        }
        else
        {
            Action = ActionType.Wander;
        }

        if (Mobile.DeleteOnRelease || Mobile.IsDeadPet)
        {
            Mobile.Delete();
        }
        else
        {
            Mobile.BeginDeleteTimer();

            if (Mobile.CanDrop)
            {
                Mobile.DropBackpack();
            }
        }

        return true;
    }

    public virtual bool DoOrderStay()
    {
        if (CheckHerding())
        {
            this.DebugSayFormatted($"I am being herded by {Mobile.ControlTarget?.Name ?? "Unknown"}.");
        }
        else
        {
            this.DebugSayFormatted($"I have been ordered to stay by {Mobile.ControlMaster?.Name ?? "Unknown"}.");
        }

        _lastPetOrder = OrderType.Stay;

        WalkRandomInHome(3, 2, 1);
        return true;
    }

    public virtual bool DoOrderStop()
    {
        if (CheckHerding())
        {
            this.DebugSayFormatted($"I am being herded by {Mobile.ControlTarget?.Name ?? "Unknown"}.");
        }
        else
        {
            this.DebugSayFormatted($"I have been ordered to stop by {Mobile.ControlMaster?.Name ?? "Unknown"}.");
        }

        if (Core.ML)
        {
            WalkRandomInHome(5, 2, 1);
        }

        return true;
    }

    public virtual bool DoOrderTransfer()
    {
        if (Mobile.IsDeadPet)
        {
            return true;
        }

        var from = Mobile.ControlMaster;
        var to = Mobile.ControlTarget;

        if (from?.Deleted == false && to?.Deleted == false && from != to && to.Player)
        {
            this.DebugSayFormatted($"Beginning transfer with {to.Name}");

            var youngFrom = from is PlayerMobile mobile && mobile.Young;
            var youngTo = to is PlayerMobile playerMobile && playerMobile.Young;

            if (youngFrom && !youngTo)
            {
                from.SendLocalizedMessage(502040);
                // As a young player, you may not friend pets to older players.
                Mobile.ControlOrder = OrderType.None;
                return true;
            }

            if (!youngFrom && youngTo)
            {
                from.SendLocalizedMessage(502041);
                // As an older player, you may not friend pets to young players.
                Mobile.ControlOrder = OrderType.None;
                return true;
            }

            if (!Mobile.CanBeControlledBy(to))
            {
                SendTransferRefusalMessages(from, to, 1043248, 1043249);
                // 1043248: The pet refuses to be transferred because it will not obey ~1_NAME~.~3_BLANK~
                // 1043249: The pet will not accept you as a master because it does not trust you.~3_BLANK~
                Mobile.ControlOrder = OrderType.None;
                return false;
            }

            if (!Mobile.CanBeControlledBy(from))
            {
                SendTransferRefusalMessages(from, to, 1043250, 1043251);
                // 1043250: The pet refuses to be transferred because it will not obey you sufficiently.~3_BLANK~
                // 1043251: The pet will not accept you as a master because it does not trust ~2_NAME~.~3_BLANK~
                Mobile.ControlOrder = OrderType.None;
                return false;
            }

            if (Mobile.Combatant != null || Mobile.Aggressors.Count > 0 ||
                Mobile.Aggressed.Count > 0 || Core.TickCount < Mobile.NextCombatTime)
            {
                from.SendMessage("You can not transfer a pet while in combat.");
                to.SendMessage("You can not transfer a pet while in combat.");
                Mobile.ControlOrder = OrderType.None;
                return false;
            }

            var fromState = from.NetState;
            var toState = to.NetState;

            if (fromState == null || toState == null)
            {
                Mobile.ControlOrder = OrderType.None;
                return false;
            }

            if (from.HasTrade || to.HasTrade)
            {
                from.SendLocalizedMessage(1010507);
                // You cannot transfer a pet with a trade pending
                to.SendLocalizedMessage(1010507);
                // You cannot transfer a pet with a trade pending
                Mobile.ControlOrder = OrderType.None;
                return false;
            }

            var container = fromState.AddTrade(toState);
            container.DropItem(new TransferItem(Mobile));
        }

        Mobile.ControlOrder = OrderType.Stay;
        return true;
    }

    private static void SendTransferRefusalMessages(Mobile from, Mobile to, int fromMessage, int toMessage)
    {
        var args = $"{to.Name}\t{from.Name}\t ";

        from.SendLocalizedMessage(fromMessage, args);
        to.SendLocalizedMessage(toMessage, args);
    }
}
