/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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

using Server.Collections;

namespace Server.Mobiles;

public abstract partial class BaseAI
{
    // The standing command a pet falls back to when a transient order (Attack/Come/Drop)
    // completes: None, Stay, Follow, or Guard. Runtime-only (not serialized); reset to None
    // on load and derived from master proximity on login. See PetLoginHandler.
    internal OrderType PersistentOrder { get; private set; } = OrderType.None;

    // Guards anchor/persistent derivation while we resume a fallback order, so a resume
    // never re-derives the persistent command or re-anchors Home. See OnCurrentOrderChanged.
    private bool _resolvingOrder;

    // The controlled-pet wander anchor (Home) is a pure function of the persistent command.
    internal void SetPersistentOrder(OrderType order)
    {
        PersistentOrder = order;
        Mobile.Home = order is OrderType.Follow or OrderType.Guard ? Point3D.Zero : Mobile.Location;
    }

    // Resume the persistent command without re-deriving the persistent order or anchor.
    private void ResumePersistentOrder()
    {
        _resolvingOrder = true;
        Mobile.ControlOrder = PersistentOrder;
        _resolvingOrder = false;
    }

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

        // Pure idle: gently wander near the anchor, with CheckIdle rest periods. Pets resume
        // a standing order via ResumePersistentOrder, not by re-deriving it here.
        WalkRandomIdle();
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

        WalkMobileRange(Mobile.ControlMaster, 1, 1, 2);

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

        // AOS: sprint after the master (bespoke 0.1 paces both clocks).
        if (Core.AOS && Mobile.ControlTarget == Mobile.ControlMaster && Mobile.Combatant == null)
        {
            Mobile.CurrentSpeed = 0.1;
        }

        if (currentDistance > 1)
        {
            WalkMobileRange(Mobile.ControlTarget, 1, 1, 2);
        }
    }

    public virtual bool DoOrderDrop()
    {
        if (Mobile.IsDeadPet || !Mobile.CanDrop)
        {
            return true;
        }

        this.DebugSayFormatted($"I am ordered to drop my items by {Mobile.ControlMaster?.Name ?? "Unknown"}.");

        DropItems();
        ResumePersistentOrder();
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
            ResumePersistentOrder();
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
            ResumePersistentOrder();
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

        var combatant = FindGuardTarget();

        if (combatant != null)
        {
            this.DebugSayFormatted($"Attacking target: {combatant.Name}");

            // Engage without leaving the Guard order so tags, recall handling, and retargeting persist.
            Mobile.Combatant = combatant;
            Mobile.FocusMob = combatant;
            Action = ActionType.Combat;

            Think();
        }
        else
        {
            this.DebugSayFormatted($"Guarding my master, {controlMaster.Name}.");

            // Stand down; a stale Warmode would skew the return pace.
            Mobile.FocusMob = null;
            Mobile.Warmode = false;
            Mobile.Combatant = null;

            var distance = (int)Mobile.GetDistanceToSqrt(controlMaster);

            if (distance > 3)
            {
                // AOS: sprint back (bespoke 0.1 paces both clocks); earlier eras run active.
                if (Core.AOS)
                {
                    Mobile.CurrentSpeed = 0.1;
                }
                else
                {
                    Mobile.SetCurrentSpeedToActive();
                }

                WalkMobileRange(controlMaster, 1, 1, 3);
            }
            else
            {
                Mobile.SetCurrentSpeedToActive(); // alert at the master's side
                WalkRandom(3, 1, 1);
            }
        }

        return true;
    }

    public virtual bool DoOrderAttack()
    {
        if (Mobile.IsDeadPet)
        {
            return true;
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
        || !target.Alive || target.IsDeadBondedPet || target.Hidden;

    private void HandleInvalidControlTarget()
    {
        DebugSay("Target is either dead, hidden, or out of range.");

        Mobile.ControlTarget = Mobile.ControlMaster;
        ResumePersistentOrder();

        // A resumed Guard engages through its own scan; other fallbacks chain an explicit Attack.
        if (Mobile.ControlOrder == OrderType.Guard ||
            Mobile.FightMode is not (FightMode.Closest or FightMode.Aggressor))
        {
            return;
        }

        var next = FindGuardTarget();

        if (next != null)
        {
            Mobile.ControlTarget = next;
            Mobile.ControlOrder = OrderType.Attack;
            Mobile.Combatant = next;

            this.DebugSayFormatted($"{next.Name} is still hostile! Engaging...");

            Think();
        }
    }

    /// <summary>
    /// Selects the aggressor closest to the master. The current combatant is kept
    /// unless a strictly closer one exists. Never mutates order state.
    /// </summary>
    private Mobile FindGuardTarget()
    {
        var controlMaster = Mobile.ControlMaster;
        var anchor = controlMaster ?? Mobile;

        var current = Mobile.Combatant;
        var best = current != controlMaster && IsValidCombatant(current) ? current : null;
        var bestDist = best?.GetDistanceToSqrt(anchor) ?? double.MaxValue;

        foreach (var aggr in Mobile.GetMobilesInRange(Mobile.RangePerception))
        {
            if (aggr == best || aggr == Mobile || aggr == controlMaster ||
                aggr.IsDeadBondedPet || !aggr.Alive ||
                aggr.Combatant != Mobile && (controlMaster == null || aggr.Combatant != controlMaster))
            {
                continue;
            }

            var dist = aggr.GetDistanceToSqrt(anchor);

            if (dist < bestDist && Mobile.CanSee(aggr) && Mobile.InLOS(aggr))
            {
                best = aggr;
                bestDist = dist;
            }
        }

        if (controlMaster != null)
        {
            foreach (var info in controlMaster.Aggressors)
            {
                var aggressor = info.Attacker;

                if (aggressor == best || aggressor?.Deleted != false || !aggressor.Alive ||
                    aggressor.IsDeadBondedPet || !Mobile.InRange(aggressor, Mobile.RangePerception))
                {
                    continue;
                }

                var dist = aggressor.GetDistanceToSqrt(anchor);

                if (dist < bestDist && Mobile.CanSee(aggressor) && Mobile.InLOS(aggressor))
                {
                    best = aggressor;
                    bestDist = dist;
                }
            }
        }

        return best;
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
            // No spawner to return to: anchor where it stands so it idle-wanders here
            // instead of pathing toward a stale (e.g. former stay) anchor.
            Mobile.Home = Mobile.Location;
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

        // Hold position at the post (Home). Stand still when there; only walk back if displaced
        // (e.g. after chasing a kill). No idle shuffle.
        if (Mobile.Home != Point3D.Zero && Mobile.Location != Mobile.Home)
        {
            DoMove(Mobile.GetDirectionTo(Mobile.Home));
        }

        return true;
    }

    // Stop is resolved into another order in OnCurrentOrderChanged and never rests as the
    // active order; this is a defensive no-op.
    public virtual bool DoOrderStop() => true;

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
                ResumePersistentOrder();
                return true;
            }

            if (!youngFrom && youngTo)
            {
                from.SendLocalizedMessage(502041);
                // As an older player, you may not friend pets to young players.
                ResumePersistentOrder();
                return true;
            }

            if (!Mobile.CanBeControlledBy(to))
            {
                SendTransferRefusalMessages(from, to, 1043248, 1043249);
                // 1043248: The pet refuses to be transferred because it will not obey ~1_NAME~.~3_BLANK~
                // 1043249: The pet will not accept you as a master because it does not trust you.~3_BLANK~
                ResumePersistentOrder();
                return true;
            }

            if (!Mobile.CanBeControlledBy(from))
            {
                SendTransferRefusalMessages(from, to, 1043250, 1043251);
                // 1043250: The pet refuses to be transferred because it will not obey you sufficiently.~3_BLANK~
                // 1043251: The pet will not accept you as a master because it does not trust ~2_NAME~.~3_BLANK~
                ResumePersistentOrder();
                return true;
            }

            if (Mobile.Combatant != null || Mobile.HasAggressors ||
                Mobile.HasAggressed || Core.TickCount < Mobile.NextCombatTime)
            {
                from.SendMessage("You can not transfer a pet while in combat.");
                to.SendMessage("You can not transfer a pet while in combat.");
                ResumePersistentOrder();
                return true;
            }

            var fromState = from.NetState;
            var toState = to.NetState;

            if (fromState == null || toState == null)
            {
                ResumePersistentOrder();
                return true;
            }

            if (from.HasTrade || to.HasTrade)
            {
                from.SendLocalizedMessage(1010507);
                // You cannot transfer a pet with a trade pending
                to.SendLocalizedMessage(1010507);
                // You cannot transfer a pet with a trade pending
                ResumePersistentOrder();
                return true;
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
