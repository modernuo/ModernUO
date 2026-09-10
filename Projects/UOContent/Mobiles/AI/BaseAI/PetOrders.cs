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

using System;

namespace Server.Mobiles;

// One place per order, two phases. Issue runs once, synchronously, from BaseCreature.SetControlOrder:
// sets state, emits, returns the order to rest in. Tick (DoOrderXxx) runs from Obey while Controlled:
// moves, fights, transitions. Only None/Come/Guard/Attack/Stay/Follow rest; every other order resolves
// inside Issue and never reaches Obey.
public abstract partial class BaseAI
{
    // The standing command a transient order falls back to (None/Stay/Follow/Guard). Runtime-only:
    // None after load, derived at login. See PetLoginHandler.
    internal OrderType PersistentOrder { get; private set; } = OrderType.None;

    // Orders that may rest between ticks.
    public static bool IsRestableOrder(OrderType order) =>
        order is OrderType.None or OrderType.Come or OrderType.Guard or OrderType.Attack or OrderType.Stay
            or OrderType.Follow;

    // Orders a pet friend (not the master) may give.
    public static bool IsFriendOrder(OrderType order) =>
        order is OrderType.Follow or OrderType.Stay or OrderType.Stop;

    // Orders a dead bonded pet refuses.
    // Orders that are not a change of what the pet is doing: the two combat orders, plus the
    // administrative ones. Everything else stands the pet down before it runs.
    private static bool KeepsCombatPosture(OrderType order) =>
        order is OrderType.Attack or OrderType.Guard or OrderType.Drop or OrderType.Friend
            or OrderType.Unfriend or OrderType.Rename;

    // Publish 51's stand-down commands: told to do any of these, a pet will not attack anything,
    // even if it is attacked. Stop resolves to None, which is its resting form ("may wander").
    public static bool IsStandDownOrder(OrderType order) =>
        order is OrderType.Follow or OrderType.Come or OrderType.Stay or OrderType.None;

    public static bool IsDeadPetOrder(OrderType order) =>
        order is OrderType.Guard or OrderType.Attack or OrderType.Transfer or OrderType.Drop;

    // Target of the standing Follow; targeted commands overwrite ControlTarget, a resume restores it from here.
    private Mobile _persistentTarget;

    // The controlled-pet wander anchor (Home) is a pure function of the persistent command.
    internal void SetPersistentOrder(OrderType order)
    {
        PersistentOrder = order;
        _persistentTarget = order == OrderType.Follow ? Mobile.ControlTarget : null;
        Mobile.Home = order is OrderType.Follow or OrderType.Guard ? Point3D.Zero : Mobile.Location;
    }

    // Adopt a saved standing order; Home and ControlTarget were saved with it.
    internal void RestorePersistentOrder(OrderType order)
    {
        PersistentOrder = order;
        _persistentTarget = order == OrderType.Follow ? Mobile.ControlTarget : null;
    }

    // Where an administrative command (Drop, Friend, Unfriend, Rename) hands control back:
    // to whatever the pet was doing, target and all. The standing order is the fallback for
    // an interrupted order that cannot resume — a transient, or an attack whose target is gone.
    private OrderType ResumeInterrupted(OrderType previous, Mobile interruptedTarget)
    {
        if (!IsRestableOrder(previous) ||
            previous == OrderType.Attack && IsInvalidControlTarget(interruptedTarget))
        {
            return PersistentOrder;
        }

        Mobile.ControlTarget = interruptedTarget;
        return previous;
    }

    // Resume the standing command without re-deriving it or re-anchoring Home.
    private void ResumePersistentOrder() => Mobile.SetControlOrder(PersistentOrder, null, true);

    /// <summary>
    /// Issue phase. <paramref name="issuer"/> is the only mobile revealed (null = system-issued);
    /// <paramref name="resuming"/> marks a fallback to the standing order. Returns the order to rest in.
    /// </summary>
    public virtual OrderType IssueOrder(
        OrderType order, OrderType previous, Mobile issuer, bool resuming, Mobile interruptedTarget
    )
    {
        if (Mobile.Deleted)
        {
            return order;
        }

        AITimer.Prod();

        issuer?.RevealingAction();

        // Neutral posture for every command except Attack and Guard: dropping Warmode nulls Combatant through
        // the Mobile setter, which would turn Attack's single Combatant write into a re-write (DoHarmful again)
        // and flap Guard's war stance.
        Mobile.FocusMob = null;

        if (!KeepsCombatPosture(order))
        {
            Mobile.Warmode = false; // also nulls Combatant via the setter
            Mobile.Combatant = null;
        }

        return order switch
        {
            OrderType.None     => IssueNone(),
            OrderType.Come     => IssueCome(),
            OrderType.Drop     => IssueDrop(previous, interruptedTarget),
            OrderType.Friend   => IssueFriend(previous, interruptedTarget),
            OrderType.Unfriend => IssueUnfriend(previous, interruptedTarget),
            OrderType.Guard    => IssueGuard(resuming),
            OrderType.Attack   => IssueAttack(resuming),
            OrderType.Release  => IssueRelease(),
            OrderType.Stay     => IssueStay(resuming),
            OrderType.Stop     => IssueStop(previous),
            OrderType.Follow   => IssueFollow(resuming),
            OrderType.Transfer => IssueTransfer(),
            OrderType.Rename   => IssueRename(issuer, previous, interruptedTarget),
            _                  => PersistentOrder // Patrol and anything unimplemented
        };
    }

    private OrderType IssueNone()
    {
        Mobile.ControlTarget = null;
        Mobile.SetCurrentSpeedToPassive();
        return OrderType.None;
    }

    private OrderType IssueCome()
    {
        Mobile.SetCurrentSpeedToActive();
        return OrderType.Come;
    }

    private OrderType IssueStay(bool resuming)
    {
        Mobile.SetCurrentSpeedToPassive();

        if (resuming)
        {
            Mobile.ControlTarget = null; // a transient's target does not carry over
        }
        else
        {
            SetPersistentOrder(OrderType.Stay); // anchors Home at the post
            Mobile.PlaySound(Mobile.GetIdleSound());
        }

        return OrderType.Stay;
    }

    private OrderType IssueFollow(bool resuming)
    {
        Mobile.SetCurrentSpeedToActive();

        if (resuming)
        {
            // the standing Follow's target, never a transient's
            Mobile.ControlTarget = _persistentTarget?.Deleted == false ? _persistentTarget : Mobile.ControlMaster;
        }
        else
        {
            SetPersistentOrder(OrderType.Follow); // Home = Zero, remembers the target
            Mobile.PlaySound(Mobile.GetIdleSound());
        }

        return OrderType.Follow;
    }

    private OrderType IssueGuard(bool resuming)
    {
        Mobile.Warmode = true; // the guard order opens in war stance
        Mobile.SetCurrentSpeedToActive();

        if (resuming)
        {
            Mobile.ControlTarget = null;
        }
        else
        {
            SetPersistentOrder(OrderType.Guard);
            Mobile.PlaySound(Mobile.GetAttackSound());
            Mobile.ControlMaster?.SendLocalizedMessage(1049671, Mobile.Name);
            // ~1_NAME~ is now guarding you.
        }

        return OrderType.Guard;
    }

    private OrderType IssueAttack(bool resuming)
    {
        var target = Mobile.ControlTarget;
        var valid = target?.Deleted == false && target.Alive;

        Mobile.FocusMob = valid ? target : null;
        Mobile.Combatant = valid ? target : null; // the one Combatant write of the Attack command

        if (valid)
        {
            Action = ActionType.Combat;
        }

        Mobile.Warmode = true;
        Mobile.SetCurrentSpeedToActive();

        // Resuming an interrupted attack is not a new command: no bark. The Combatant write
        // above is idempotent - the setter early-outs on an unchanged value - so the harm the
        // original order did is not repeated either.
        if (!resuming)
        {
            Mobile.PlaySound(Mobile.GetAttackSound());
        }

        return OrderType.Attack;
    }

    // Stop: Follow/Guard -> idle here; Stay -> keep the post; anything transient -> the standing order.
    private OrderType IssueStop(OrderType previous)
    {
        Mobile.ControlTarget = null;

        switch (previous)
        {
            case OrderType.Stay:
                {
                    return OrderType.Stay; // resumed: anchor untouched
                }
            case OrderType.Follow:
            case OrderType.Guard:
                {
                    SetPersistentOrder(OrderType.None); // cancel the standing order; idle anchor = here
                    return OrderType.None;
                }
            default:
                {
                    // No standing order: idle here, anchored (a Zero Home wanders without bounds).
                    if (PersistentOrder == OrderType.None)
                    {
                        SetPersistentOrder(OrderType.None);
                    }

                    return PersistentOrder;
                }
        }
    }

    private OrderType IssueDrop(OrderType previous, Mobile interruptedTarget)
    {
        if (!Mobile.IsDeadPet && Mobile.CanDrop)
        {
            this.DebugSayFormatted($"I am ordered to drop my items by {Mobile.ControlMaster?.Name ?? "Unknown"}.");
            DropItems();
        }

        return ResumeInterrupted(previous, interruptedTarget);
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

    private OrderType IssueFriend(OrderType previous, Mobile interruptedTarget)
    {
        var from = Mobile.ControlMaster;
        var to = Mobile.ControlTarget;

        if (from?.Deleted != false)
        {
            return ResumeInterrupted(previous, interruptedTarget);
        }

        var youngFrom = from is PlayerMobile { Young: true };
        var youngTo = to is PlayerMobile { Young: true };

        if (youngFrom && !youngTo)
        {
            from.SendLocalizedMessage(502040);
            // As a young player, you may not friend pets to older players.
            return ResumeInterrupted(previous, interruptedTarget);
        }

        if (!youngFrom && youngTo)
        {
            from.SendLocalizedMessage(502041);
            // As an older player, you may not friend pets to young players.
            return ResumeInterrupted(previous, interruptedTarget);
        }

        if (to?.Deleted != false || from == to || !to.Player)
        {
            Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039);
            // *looks confused*
            return ResumeInterrupted(previous, interruptedTarget);
        }

        if (!from.CanBeBeneficial(to, true))
        {
            return ResumeInterrupted(previous, interruptedTarget);
        }

        if (from.HasTrade || to.HasTrade)
        {
            (from.HasTrade ? from : to).SendLocalizedMessage(1070947);
            // You cannot friend a pet with a trade pending
            return ResumeInterrupted(previous, interruptedTarget);
        }

        if (Mobile.IsPetFriend(to))
        {
            from.SendLocalizedMessage(1049691);
            // That person is already a friend.
            return ResumeInterrupted(previous, interruptedTarget);
        }

        if (!Mobile.AllowNewPetFriend)
        {
            from.SendLocalizedMessage(1005482);
            // Your pet does not seem to be interested in making new friends right now.
            return ResumeInterrupted(previous, interruptedTarget);
        }

        from.SendLocalizedMessage(1049676, $"{Mobile.Name}\t{to.Name}");
        // ~1_NAME~ will now accept movement commands from ~2_NAME~.

        to.SendLocalizedMessage(1043246, $"{from.Name}\t{Mobile.Name}");
        // ~1_NAME~ has granted you the ability to give orders to their pet ~2_PET_NAME~.
        // This creature will now consider you as a friend.

        Mobile.AddPetFriend(to);

        Mobile.ControlTarget = to;
        SetPersistentOrder(OrderType.Follow);
        return OrderType.Follow;
    }

    private OrderType IssueUnfriend(OrderType previous, Mobile interruptedTarget)
    {
        var from = Mobile.ControlMaster;
        var to = Mobile.ControlTarget;

        if (from?.Deleted != false || to?.Deleted != false || from == to || !to.Player)
        {
            Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039);
            // *looks confused*
            return ResumeInterrupted(previous, interruptedTarget);
        }

        if (!Mobile.IsPetFriend(to))
        {
            from.SendLocalizedMessage(1070953);
            // That person is not a friend.
            return ResumeInterrupted(previous, interruptedTarget);
        }

        from.SendLocalizedMessage(1070951, $"{Mobile.Name}\t{to.Name}");
        // ~1_NAME~ will no longer accept movement commands from ~2_NAME~.

        to.SendLocalizedMessage(1070952, $"{from.Name}\t{Mobile.Name}");
        // ~1_NAME~ has no longer granted you the ability to give orders to their pet ~2_PET_NAME~.
        // This creature will no longer consider you as a friend.

        Mobile.RemovePetFriend(to);

        Mobile.ControlTarget = from;
        SetPersistentOrder(OrderType.Follow);
        return OrderType.Follow;
    }

    private OrderType IssueTransfer()
    {
        if (Mobile.IsDeadPet)
        {
            return PersistentOrder;
        }

        var from = Mobile.ControlMaster;
        var to = Mobile.ControlTarget;

        if (from?.Deleted != false || to?.Deleted != false || from == to || !to.Player)
        {
            return PersistentOrder;
        }

        this.DebugSayFormatted($"Beginning transfer with {to.Name}");

        var youngFrom = from is PlayerMobile { Young: true };
        var youngTo = to is PlayerMobile { Young: true };

        if (youngFrom && !youngTo)
        {
            from.SendLocalizedMessage(502040);
            // As a young player, you may not friend pets to older players.
            return PersistentOrder;
        }

        if (!youngFrom && youngTo)
        {
            from.SendLocalizedMessage(502041);
            // As an older player, you may not friend pets to young players.
            return PersistentOrder;
        }

        if (!Mobile.CanBeControlledBy(to))
        {
            SendTransferRefusalMessages(from, to, 1043248, 1043249);
            // 1043248: The pet refuses to be transferred because it will not obey ~1_NAME~.~3_BLANK~
            // 1043249: The pet will not accept you as a master because it does not trust you.~3_BLANK~
            return PersistentOrder;
        }

        if (!Mobile.CanBeControlledBy(from))
        {
            SendTransferRefusalMessages(from, to, 1043250, 1043251);
            // 1043250: The pet refuses to be transferred because it will not obey you sufficiently.~3_BLANK~
            // 1043251: The pet will not accept you as a master because it does not trust ~2_NAME~.~3_BLANK~
            return PersistentOrder;
        }

        // The stand-down already cleared Combatant; the aggressor lists and the combat cooldown gate this.
        if (Mobile.Aggressors.Count > 0 || Mobile.Aggressed.Count > 0 || Core.TickCount - Mobile.NextCombatTime < 0)
        {
            from.SendMessage("You can not transfer a pet while in combat.");
            to.SendMessage("You can not transfer a pet while in combat.");
            return PersistentOrder;
        }

        var fromState = from.NetState;
        var toState = to.NetState;

        if (fromState == null || toState == null)
        {
            return PersistentOrder;
        }

        if (from.HasTrade || to.HasTrade)
        {
            from.SendLocalizedMessage(1010507);
            // You cannot transfer a pet with a trade pending
            to.SendLocalizedMessage(1010507);
            // You cannot transfer a pet with a trade pending
            return PersistentOrder;
        }

        var container = fromState.AddTrade(toState);
        container.DropItem(new TransferItem(Mobile));

        // Hold position while the trade window is open.
        Mobile.PlaySound(Mobile.GetIdleSound());
        Mobile.SetCurrentSpeedToPassive();
        SetPersistentOrder(OrderType.Stay);
        return OrderType.Stay;
    }

    private static void SendTransferRefusalMessages(Mobile from, Mobile to, int fromMessage, int toMessage)
    {
        var args = $"{to.Name}\t{from.Name}\t ";

        from.SendLocalizedMessage(fromMessage, args);
        to.SendLocalizedMessage(toMessage, args);
    }

    // The whole release and its only entry point (player order or loyalty drain). SetControlMaster(null)
    // assigns ControlOrder = None underneath; the funnel keeps the nested write.
    private OrderType IssueRelease()
    {
        if (Mobile.Summoned)
        {
            Mobile.Kill();

            // A vetoed death leaves the summon controlled; it keeps its standing order.
            return Mobile.Deleted || !Mobile.Alive ? OrderType.None : PersistentOrder;
        }

        DebugSay("I have been released to the wild.");

        if (!string.IsNullOrEmpty(Mobile.Name))
        {
            Mobile.Name = null;
        }

        Mobile.PlaySound(Mobile.GetIdleSound());

        Mobile.ControlTarget = null;
        Mobile.BondingBegin = DateTime.MinValue;
        Mobile.OwnerAbandonTime = DateTime.MinValue;
        Mobile.IsBonded = false;
        // Nothing of the old master survives a re-tame.
        Mobile.ClearPetFriends();
        PersistentOrder = OrderType.None;
        _persistentTarget = null;
        Mobile.SetControlMaster(null);

        var spawner = Mobile.Spawner;

        if (spawner != null)
        {
            Mobile.Home = spawner.GetSpawnPosition(Mobile, spawner.Map);
            Mobile.RangeHome = spawner.WalkingRange;
        }
        else
        {
            // No spawner: anchor here rather than path toward a stale stay anchor.
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

        return OrderType.None;
    }

    protected virtual OrderType IssueRename(Mobile issuer, OrderType previous, Mobile interruptedTarget)
    {
        var to = issuer ?? Mobile.ControlMaster;

        if (Mobile.Summoned)
        {
            to?.SendMessage("You cannot rename a summoned creature.");
        }
        else
        {
            to?.SendMessage("Change name on pet health bar.");
        }

        return ResumeInterrupted(previous, interruptedTarget);
    }

    // Only restable orders arrive here; anything else is a pre-refactor save and resumes the standing order.
    public virtual bool Obey()
    {
        if (Mobile.Deleted)
        {
            return false;
        }

        switch (Mobile.ControlOrder)
        {
            case OrderType.None:
                {
                    return DoOrderNone();
                }
            case OrderType.Come:
                {
                    return DoOrderCome();
                }
            case OrderType.Guard:
                {
                    return DoOrderGuard();
                }
            case OrderType.Attack:
                {
                    return DoOrderAttack();
                }
            case OrderType.Stay:
                {
                    return DoOrderStay();
                }
            case OrderType.Follow:
                {
                    return DoOrderFollow();
                }
            default:
                {
                    ResumePersistentOrder();
                    return true;
                }
        }
    }

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

        if (currentDistance > 1)
        {
            WalkMobileRange(Mobile.ControlTarget, 1, 1, 2);
        }
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

            // Alert either way; FollowMoveSpeed caps the steps of the return itself.
            Mobile.SetCurrentSpeedToActive();

            if (distance > GuardRange)
            {
                WalkMobileRange(controlMaster, 1, 1, GuardRange);
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
            return true;
        }

        if (IsInvalidControlTarget(Mobile.ControlTarget))
        {
            HandleInvalidControlTarget();
        }
        else
        {
            this.DebugSayFormatted($"Attacking target: {Mobile.ControlTarget?.Name}");

            // OnAggressiveAction can swap Combatant; the commanded target wins.
            if (Mobile.Combatant != Mobile.ControlTarget)
            {
                Mobile.Combatant = Mobile.ControlTarget;
            }

            Think();
        }

        return true;
    }

    private bool IsInvalidControlTarget(Mobile target) => target?.Deleted != false || target.Map != Mobile.Map
        || !target.Alive || target.IsDeadBondedPet || target.Hidden;

    private void HandleInvalidControlTarget()
    {
        DebugSay("Target is either dead, hidden, or out of range.");

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
            Mobile.IssueOrder(OrderType.Attack, null, next);

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

        var aggressors = controlMaster?.Aggressors;

        if (aggressors != null)
        {
            for (var i = 0; i < aggressors.Count; i++)
            {
                var aggressor = aggressors[i].Attacker;

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
}
