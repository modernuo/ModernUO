/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BaseAI.cs                                                       *
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
using Server.Engines.Quests.Necro;
using Server.Engines.Spawners;
using Server.Engines.Virtues;
using Server.Factions;
using Server.Spells;
using Server.Spells.Spellweaving;
using Server.Targets;

namespace Server.Mobiles;

public abstract partial class BaseAI
{
    private ActionType _action;
    public long _nextDetectHidden;
    public PathFollower Path { get; protected set; }
    public readonly Timer _timer;
    public DateTime _lastOrder = DateTime.MinValue;
    public Mobile _commandIssuer;
    public long NextMove { get; set; }

    public BaseCreature Mobile { get; }

    public long NextDebugMessage { get; set; }

    public virtual bool CanDetectHidden => Mobile.Skills.DetectHidden.Value > 0;

    public BaseAI(BaseCreature m)
    {
        Mobile = m;
        _timer = new AITimer(this);

        if (!m.PlayerRangeSensitive || !World.Loading && m.Map != null && m.Map != Map.Internal && m.Map.GetSector(m.Location).Active)
        {
            _timer.Start();
        }

        if (Action != ActionType.Wander)
        {
            Action = ActionType.Wander;
        }
    }

    public ActionType Action
    {
        get => _action;
        set
        {
            if (_action != value)
            {
                _action = value;
                OnActionChanged();
            }
        }
    }

    public virtual bool WasNamed(string speech) => !string.IsNullOrEmpty(Mobile.Name) && speech.InsensitiveStartsWith(Mobile.Name);

    public virtual void BeginPickTarget(Mobile from, OrderType order)
    {
        if (!IsValidTarget(from, order))
        {
            return;
        }

        if (from.Target == null)
        {
            SendOrderMessage(from, order);
            from.Target = new AIControlMobileTarget(this, order);
        }
        else if (from.Target is AIControlMobileTarget t && t.Order == order)
        {
            t.AddAI(this);
        }
    }

    private static void SendOrderMessage(Mobile from, OrderType order)
    {
        switch (order)
        {
            case OrderType.Transfer:
                {
                    from.SendLocalizedMessage(502038);
                    // Click on the person to transfer ownership to.
                    break;
                }
            case OrderType.Friend:
                {
                    from.SendLocalizedMessage(502020);
                    // Click on the player whom you wish to make a co-owner.
                    break;
                }
            case OrderType.Unfriend:
                {
                    from.SendLocalizedMessage(1070948);
                    // Click on the player whom you wish to remove as a co-owner.
                    break;
                }
        }
    }

    public virtual void OnAggressiveAction(Mobile aggressor)
    {
        if (aggressor.Hidden)
        {
            return;
        }

        var currentCombat = Mobile.Combatant;

        if (currentCombat == null || currentCombat == aggressor)
        {
            return;
        }

        if (Mobile.GetDistanceToSqrt(aggressor) < Mobile.GetDistanceToSqrt(currentCombat))
        {
            Mobile.Combatant = aggressor;
        }
    }

    public virtual void EndPickTarget(Mobile from, Mobile target, OrderType order)
    {
        if (!IsValidTarget(from, order) || order == OrderType.Attack && !CanAttackTarget(from, target))
        {
            return;
        }

        if (Mobile.CheckControlChance(from))
        {
            Mobile.ControlTarget = target;
            Mobile.ControlOrder = order;

            if (order == OrderType.Attack)
            {
                Mobile.FocusMob = target;
                Mobile.Combatant = target;
                Action = ActionType.Combat;
            }
        }
    }

    private bool IsValidTarget(Mobile from, OrderType order)
    {
        if (Mobile.Deleted || !Mobile.Controlled || !from.InRange(Mobile, 14)
            || from.Map != Mobile.Map || !from.CheckAlive())
        {
            return false;
        }

        var isOwner = from == Mobile.ControlMaster;
        var isFriend = !isOwner && Mobile.IsPetFriend(from);

        if (!isOwner && !isFriend)
        {
            return false;
        }

        if (isFriend && order is not (OrderType.Follow or OrderType.Stay or OrderType.Stop))
        {
            return false;
        }

        return true;
    }

    private bool CanAttackTarget(Mobile from, Mobile target)
    {
        if (target is BaseCreature creature && creature.IsScaryToPets && Mobile.IsScaredOfScaryThings)
        {
            Mobile.SayTo(from, "Your pet refuses to attack this creature!");
            return false;
        }

        if (SolenHelper.CheckRedFriendship(from) &&
            target is RedSolenInfiltratorQueen or RedSolenInfiltratorWarrior or RedSolenQueen or RedSolenWarrior or RedSolenWorker ||
            SolenHelper.CheckBlackFriendship(from) &&
            target is BlackSolenInfiltratorQueen or BlackSolenInfiltratorWarrior or BlackSolenQueen or BlackSolenWarrior or BlackSolenWorker)
        {
            from.SendLocalizedMessage(1063106);
            // You can not force your pet to attack a creature you are protected from.
            return false;
        }

        if (target is BaseFactionGuard)
        {
            Mobile.SayTo(from, "Your pet refuses to attack the guard.");
            return false;
        }

        return true;
    }

    public void DebugSay(string message, int cooldownMs = 5000)
    {
        if (Mobile.Debug && NextDebugMessage - Core.TickCount <= 0)
        {
            Mobile.PublicOverheadMessage(MessageType.Regular, 41, false, message);
            NextDebugMessage = Core.TickCount + cooldownMs;
        }
    }

    public virtual bool Think()
    {
        if (Mobile.Deleted || Mobile.Map == null)
        {
            return false;
        }

        if (CheckFlee())
        {
            return true;
        }

        switch (Action)
        {
            case ActionType.Wander:
                {
                    Mobile.OnActionWander();
                    return DoActionWander();
                }
            case ActionType.Combat:
                {
                    Mobile.OnActionCombat();
                    return DoActionCombat();
                }
            case ActionType.Guard:
                {
                    Mobile.OnActionGuard();
                    return DoActionGuard();
                }
            case ActionType.Flee:
                {
                    Mobile.OnActionFlee();
                    return DoActionFlee();
                }
            case ActionType.Interact:
                {
                    Mobile.OnActionInteract();
                    return DoActionInteract();
                }
            case ActionType.Backoff:
                {
                    Mobile.OnActionBackoff();
                    return DoActionBackoff();
                }
        }

        return false;
    }

    public virtual void OnActionChanged()
    {
        switch (Action)
        {
            case ActionType.Wander:
                {
                    HandleWanderAction();
                    break;
                }
            case ActionType.Combat:
                {
                    HandleCombatAction();
                    break;
                }
            case ActionType.Guard:
                {
                    HandleGuardAction();
                    break;
                }
            case ActionType.Flee:
                {
                    HandleFleeAction();
                    break;
                }
            case ActionType.Interact:
                {
                    HandleInteractAction();
                    break;
                }
            case ActionType.Backoff:
                {
                    HandleBackoffAction();
                    break;
                }
        }
    }

    private void HandleWanderAction()
    {
        Mobile.FocusMob = null;
        Mobile.Warmode = false;
        Mobile.Combatant = null;
    }

    private void HandleCombatAction()
    {
        Mobile.Warmode = true;
    }

    private void HandleGuardAction()
    {
        Mobile.Warmode = true;
        Mobile.Combatant = null;
    }

    private void HandleFleeAction()
    {
        Mobile.FocusMob = null;
        Mobile.Warmode = true;
    }

    private void HandleInteractAction()
    {
        Mobile.Warmode = false;
    }

    private void HandleBackoffAction()
    {
        Mobile.Warmode = false;
    }

    public virtual bool DoActionWander()
    {
        if (CheckHerding())
        {
            this.DebugSayFormatted($"I am being herded by {Mobile.ControlTarget?.Name ?? "Unknown"}.");
        }
        else if (Mobile.CurrentWayPoint != null)
        {
            HandleWayPoint();
        }
        else if (Mobile.IsAnimatedDead)
        {
            FollowMaster();
        }
        else if (CheckMove() && CanMoveNow(out _) && !Mobile.CheckIdle())
        {
            WalkRandomInHome(3, 2, 1);
        }

        return true;
    }

    public virtual bool OnAtWayPoint() => true;

    private void HandleWayPoint()
    {
        var point = Mobile.CurrentWayPoint;

        if ((point.X != Mobile.Location.X || point.Y != Mobile.Location.Y)
            && point.Map == Mobile.Map && point.Parent == null && !point.Deleted)
        {
            this.DebugSayFormatted($"Moving towards waypoint {point.X}, {point.Y}.");

            DoMove(Mobile.GetDirectionTo(point));
        }
        else if (OnAtWayPoint())
        {
            this.DebugSayFormatted($"I have reached waypoint {point.X}, {point.Y}.");

            Mobile.CurrentWayPoint = point.NextPoint;
            if (point.NextPoint?.Deleted == true)
            {
                Mobile.CurrentWayPoint = point.NextPoint = point.NextPoint.NextPoint;
            }
        }
    }

    private void FollowMaster()
    {
        var master = Mobile.SummonMaster;
        if (master != null && master.Map == Mobile.Map && master.InRange(Mobile, Mobile.RangePerception))
        {
            MoveTo(master, false, 1);
        }
        else
        {
            WalkRandomInHome(3, 2, 1);
        }
    }

    public virtual bool DoActionCombat()
    {
        if (Core.AOS && CheckHerding())
        {
            this.DebugSayFormatted($"I am being herded by {Mobile.ControlTarget?.Name ?? "Unknown"}.");
            return true;
        }

        var combatant = Mobile.Combatant;
        if (!IsValidCombatant(combatant))
        {
            DebugSay("My combatant is missing. Returning home...");

            Mobile.FocusMob = null;
            Mobile.Warmode = false;
            Mobile.Combatant = null;

            WalkRandomInHome(3, 2, 1);
            return true;
        }

        if (Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, combatant))
        {
            this.DebugSayFormatted($"I used my abilities on {combatant.Name}!");
        }

        return true;
    }

    public bool IsValidCombatant(Mobile combatant) =>
        IsValidFocusMob(combatant) && Mobile.InLOS(combatant);

    public bool IsValidFocusMob(Mobile focusMob) =>
        focusMob != null
        && !focusMob.Deleted
        && focusMob.Map == Mobile.Map
        && focusMob.Alive
        && (focusMob is not BaseCreature bc || !bc.IsDeadPet)
        && focusMob.AccessLevel == AccessLevel.Player
        && Mobile.CanSee(focusMob)
        && Mobile.InRange(focusMob, Mobile.RangePerception);

    public virtual bool DoActionGuard()
    {
        if (Mobile.Combatant == null)
        {
            DebugSay("No threats found. Going home...");
            Action = ActionType.Wander;
        }

        DebugSay("I stopped being on guard.");
        Action = ActionType.Wander;

        return true;
    }

    public virtual bool DoActionFlee()
    {
        var from = Mobile.FocusMob;

        if (!IsValidFocusMob(from))
        {
            DebugSay("Focus target is missing.");

            WalkRandomInHome(3, 2, 1);
            return true;
        }

        DebugSay("I am fleeing!");

        DoMove(from.GetDirectionTo(Mobile));
        return true;
    }

    public virtual bool DoActionInteract() => true;

    public virtual bool DoActionBackoff() => true;

    public virtual bool CheckHerding()
    {
        var target = Mobile.TargetLocation;

        if (target == null)
        {
            return false;
        }

        var distance = Mobile.GetDistanceToSqrt(target);

        if (distance >= 1 && distance <= 15)
        {
            DoMove(Mobile.GetDirectionTo(target));
            return true;
        }

        if (distance < 1 && IsSpecialHerdingCase(target))
        {
            HandleSpecialHerdingCase();
        }

        Mobile.TargetLocation = null;
        return false;
    }

    private bool IsSpecialHerdingCase(IPoint2D target) => target.X == 1076 && target.Y == 450 && Mobile is HordeMinionFamiliar;

    private void HandleSpecialHerdingCase()
    {
        if (Mobile.ControlMaster is PlayerMobile pm && pm.Quest is DarkTidesQuest qs)
        {
            var obj = qs.FindObjective<FetchAbraxusScrollObjective>();

            if (obj?.Completed == false)
            {
                Mobile.AddToBackpack(new ScrollOfAbraxus());
                obj.Complete();
            }
        }
    }

    public virtual void DoBardPacified()
    {
        if (Core.Now < Mobile.BardEndTime)
        {
            DebugSay("I am pacified. Can not fight.");

            Mobile.Warmode = false;
            Mobile.Combatant = null;
        }
        else
        {
            DebugSay("I am free from pacification.");

            Mobile.BardPacified = false;
        }
    }

    public virtual void DoBardProvoked()
    {
        if (Core.Now >= Mobile.BardEndTime && IsProvokerLost())
        {
            DebugSay("Provoker missing.");

            Mobile.BardProvoked = false;
            Mobile.BardMaster = null;
            Mobile.BardTarget = null;
            Mobile.Warmode = false;
            Mobile.Combatant = null;
        }
        else if (IsProvokeTargetLost())
        {
            DebugSay("Provoke target missing.");

            Mobile.BardProvoked = false;
            Mobile.BardMaster = null;
            Mobile.BardTarget = null;
            Mobile.Warmode = false;
            Mobile.Combatant = null;
        }
        else
        {
            Mobile.Combatant = Mobile.BardTarget;
            Action = ActionType.Combat;
        }
    }

    private bool IsProvokerLost() =>
        Mobile.BardMaster?.Deleted != false
        || Mobile.BardMaster.Map != Mobile.Map
        || Mobile.GetDistanceToSqrt(Mobile.BardMaster) > Mobile.RangePerception;

    private bool IsProvokeTargetLost() =>
        Mobile.BardTarget?.Deleted != false
        || Mobile.BardTarget.Map != Mobile.Map
        || Mobile.GetDistanceToSqrt(Mobile.BardTarget) > Mobile.RangePerception;

    public virtual bool CheckFlee()
    {
        if (!Mobile.CheckFlee())
        {
            return false;
        }

        if (Mobile.Combatant == null)
        {
            WalkRandomInHome(3, 2, 1);
        }

        return true;
    }

    public virtual void OnTeleported()
    {
        DebugSay("Teleported; recalculating path...");

        Path?.ForceRepath();
    }

    public virtual bool AcquireFocusMob(int iRange, FightMode acqType, bool bPlayerOnly, bool bFacFriend, bool bFacFoe)
    {
        if (Mobile.Deleted || Mobile.Map == null)
        {
            return false;
        }

        if (Mobile.BardPacified)
        {
            return false;
        }

        if (HandleBardProvoked() || HandleControlled() || HandleConstantFocus())
        {
            return true;
        }

        if (acqType == FightMode.None)
        {
            Mobile.FocusMob = null;
            return false;
        }

        if (HandleAggressor(acqType))
        {
            return false;
        }

        if (Core.TickCount - Mobile.NextReacquireTime < 0)
        {
            Mobile.FocusMob = null;
            return false;
        }

        Mobile.NextReacquireTime = Core.TickCount + (int)Mobile.ReacquireDelay.TotalMilliseconds;

        DebugSay("Acquiring new target...");

        if (Mobile.Map == null)
        {
            return Mobile.FocusMob != null;
        }

        return AcquireNewFocusMob(Mobile.Map, iRange, acqType, bPlayerOnly, bFacFriend, bFacFoe);
    }

    private bool HandleBardProvoked()
    {
        if (!Mobile.BardProvoked)
        {
            return false;
        }

        if (Mobile.BardTarget?.Deleted != false)
        {
            Mobile.FocusMob = null;
            return false;
        }

        Mobile.FocusMob = Mobile.BardTarget;
        return true;
    }

    private bool HandleControlled()
    {
        if (!Mobile.Controlled)
        {
            return false;
        }

        if (Mobile.ControlTarget?.Deleted == false &&
            Mobile.ControlTarget?.Hidden != true &&
            Mobile.ControlTarget?.Alive == true &&
            Mobile.ControlTarget?.IsDeadBondedPet != true &&
            Mobile.InRange(Mobile.ControlTarget, Mobile.RangePerception * 2))
        {
            Mobile.FocusMob = Mobile.ControlTarget;
            return true;
        }

        if (Mobile.ControlTarget != null && Mobile.ControlTarget != Mobile.ControlMaster)
        {
            Mobile.ControlTarget = null;
        }

        Mobile.FocusMob = null;
        return false;
    }

    private bool HandleConstantFocus()
    {
        if (Mobile.ConstantFocus == null)
        {
            return false;
        }

        this.DebugSayFormatted($"Acquired focused target: {Mobile.ConstantFocus.Name}.");

        Mobile.FocusMob = Mobile.ConstantFocus;
        return true;
    }

    private bool HandleAggressor(FightMode acqType)
    {
        if (acqType != FightMode.Aggressor ||
            Mobile.Aggressors.Count > 0 ||
            Mobile.Aggressed.Count > 0 ||
            Mobile.FactionAllegiance != null ||
            Mobile.EthicAllegiance != null)
        {
            return false;
        }

        Mobile.FocusMob = null;
        return true;
    }

    private bool AcquireNewFocusMob(Map map, int iRange, FightMode acqType, bool bPlayerOnly, bool bFacFriend, bool bFacFoe)
    {
        Mobile newFocusMob = null, enemySummonMob = null;
        double val = double.MinValue, enemySummonVal = double.MinValue;

        foreach (var m in map.GetMobilesInRange(Mobile.Location, iRange))
        {
            if (IsInvalidTarget(m, bPlayerOnly))
            {
                continue;
            }

            var bc = m as BaseCreature;
            var pm = m as PlayerMobile;

            if (IsInvalidSummonTarget(m, bc, pm) || IsInvalidFactionTarget(m, bFacFriend, bFacFoe)
                                                 || IsInvalidFightModeTarget(m, acqType, bc))
            {
                continue;
            }

            var theirVal = Mobile.GetFightModeRanking(m, acqType, bPlayerOnly);

            if (theirVal > val && Mobile.InLOS(m))
            {
                newFocusMob = m;
                val = theirVal;
            }
            else if (Core.AOS && theirVal > enemySummonVal
                              && Mobile.InLOS(m) && bc?.Summoned == true && bc.Controlled != true)
            {
                enemySummonMob = m;
                enemySummonVal = theirVal;
            }
        }

        Mobile.FocusMob = newFocusMob ?? enemySummonMob;
        return Mobile.FocusMob != null;
    }

    private bool IsInvalidTarget(Mobile m, bool bPlayerOnly) =>
        m.Deleted || m.Blessed || m == Mobile || m is BaseFamiliar || !m.Alive || m.IsDeadBondedPet ||
        m.AccessLevel > AccessLevel.Player || bPlayerOnly && !m.Player || !Mobile.CanSee(m);

    private bool IsInvalidSummonTarget(Mobile m, BaseCreature bc, PlayerMobile pm)
    {
        if (Core.AOS && bc?.Summoned == true &&
            (bc.SummonMaster == Mobile || !bc.SummonMaster.Player && IsHostile(bc.SummonMaster)))
        {
            return true;
        }

        if (!Mobile.Summoned || Mobile.SummonMaster == null)
        {
            return false;
        }

        return m == Mobile.SummonMaster || !SpellHelper.ValidIndirectTarget(Mobile.SummonMaster, m) ||
               Mobile.IsAnimatedDead && (pm != null || bc?.IsAnimatedDead == true || bc?.Controlled == true);
    }

    private bool IsInvalidFactionTarget(Mobile m, bool bFacFriend, bool bFacFoe)
    {
        if (bFacFriend && !Mobile.IsFriend(m))
        {
            return true;
        }

        if (TransformationSpellHelper.UnderTransformation(m, typeof(EtherealVoyageSpell)) ||
            Mobile.Combatant != m && VirtueSystem.GetVirtues(m as PlayerMobile)?.HonorActive == true)
        {
            return true;
        }

        return bFacFoe && (!Mobile.IsEnemy(m) || !bFacFriend && !Mobile.CanBeHarmful(m, false));
    }

    private bool IsInvalidFightModeTarget(Mobile m, FightMode acqType, BaseCreature bc)
    {
        if (acqType is not (FightMode.Aggressor or FightMode.Evil))
        {
            return false;
        }

        var valid = IsHostile(m) || Mobile.GetFactionAllegiance(m) == BaseCreature.Allegiance.Enemy
                                 || Mobile.GetEthicAllegiance(m) == BaseCreature.Allegiance.Enemy;

        // Valid if FightMode is Evil and the target's karma is negative
        return !valid && (acqType != FightMode.Evil || (bc?.GetMaster()?.Karma ?? m.Karma) >= 0);
    }

    private bool IsHostile(Mobile from) => Mobile.Combatant == from || from.Combatant == Mobile || IsAggressor(from) || IsAggressed(from);

    private bool IsAggressor(Mobile from)
    {
        foreach (var aggressor in Mobile.Aggressors)
        {
            if (aggressor.Defender == from)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsAggressed(Mobile from)
    {
        foreach (var aggressed in Mobile.Aggressed)
        {
            if (aggressed.Attacker == from)
            {
                return true;
            }
        }

        return false;
    }

    public virtual void DetectHidden()
    {
        if (Mobile.Deleted || Mobile.Map == null || !CanDetectHidden)
        {
            return;
        }

        DebugSay("Checking for hidden entities...");

        var srcSkill = Mobile.Skills.DetectHidden.Value;

        if (srcSkill <= 0)
        {
            return;
        }

        foreach (var trg in Mobile.GetMobilesInRange(Mobile.RangePerception))
        {
            if (IsValidTargetCombatTarget(trg))
            {
                TryDetectHidden(trg, srcSkill);
            }
        }
    }

    private bool IsValidTargetCombatTarget(Mobile trg) => trg != Mobile && trg.Player && trg.Alive && trg.Hidden &&
                                                          trg.AccessLevel == AccessLevel.Player && Mobile.InLOS(trg);

    private void TryDetectHidden(Mobile trg, double srcSkill)
    {
        this.DebugSayFormatted($"Trying to detect: {trg.Name}");

        var trgHiding = trg.Skills.Hiding.Value / 2.9;
        var trgStealth = trg.Skills.Stealth.Value / 1.8;
        var chance = Math.Max(srcSkill / 10, srcSkill / 1.2 - Math.Min(trgHiding, trgStealth)) / 100;

        if (chance > Utility.RandomDouble())
        {
            trg.RevealingAction();
            trg.SendLocalizedMessage(500814);
            // You have been revealed!
        }
    }

    public virtual void Deactivate()
    {
        if (!Mobile.PlayerRangeSensitive)
        {
            return;
        }

        if (Mobile.Map == Map.Internal || !Mobile.Controlled && !Mobile.Map.GetSector(Mobile.Location).Active)
        {
            _timer.Stop();
        }

        if (ShouldReturnToHome(Mobile.Spawner))
        {
            Timer.StartTimer(ReturnToHome);
        }
    }

    private bool ShouldReturnToHome(ISpawner spawner) =>
        spawner?.ReturnOnDeactivate == true && !Mobile.Controlled &&
        !spawner.IsInSpawnBounds(Mobile.Location);

    private void ReturnToHome()
    {
        if (Mobile.Spawner is not Spawner spawner)
        {
            return;
        }

        var loc = spawner.GetSpawnPosition(Mobile, spawner.Map);

        if (loc != Point3D.Zero)
        {
            Mobile.MoveToWorld(loc, spawner.Map);
        }

        _timer.Start();
    }

    public virtual void Activate()
    {
        if (!_timer.Running)
        {
            _timer.Start();
        }
    }

    public virtual void OnCurrentSpeedChanged()
    {
        _timer.Interval = TimeSpan.FromMilliseconds(Mobile.CurrentSpeed * 1000);
    }
}
