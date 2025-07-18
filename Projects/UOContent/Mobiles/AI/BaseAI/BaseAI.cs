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
using System.Collections.Generic;
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
    protected ActionType _action;
    public readonly BaseCreature _mobile;
    public long _nextDetectHidden;
    protected PathFollower _path;
    public Timer _timer;
    public DateTime _lastOrder = DateTime.MinValue;
    public Mobile _commandIssuer;
    public long NextMove { get; set; }

    public Mobile Mobile => _mobile;

    public long NextDebugMessage { get; set; }

    public virtual bool CanDetectHidden => _mobile.Skills.DetectHidden.Value > 0;

    private static readonly Dictionary<ActionType, Action<BaseAI>> _staticActionChanges = new()
    {
        { ActionType.Wander, ai => { ai.HandleWanderAction(); } },
        { ActionType.Combat, ai => { ai.HandleCombatAction(); } },
        { ActionType.Guard, ai => { ai.HandleGuardAction(); } },
        { ActionType.Flee, ai => { ai.HandleFleeAction(); } },
        { ActionType.Interact, ai => { ai.HandleInteractAction(); } },
        { ActionType.Backoff, ai => { ai.HandleBackoffAction(); } }
    };

    private static readonly Dictionary<ActionType, Func<BaseAI, bool>> _staticActionHandlers = new()
    {
        { ActionType.Wander, ai => { ai._mobile.OnActionWander(); return ai.DoActionWander(); } },
        { ActionType.Combat, ai => { ai._mobile.OnActionCombat(); return ai.DoActionCombat(); } },
        { ActionType.Guard, ai => { ai._mobile.OnActionGuard(); return ai.DoActionGuard(); } },
        { ActionType.Flee, ai => { ai._mobile.OnActionFlee(); return ai.DoActionFlee(); } },
        { ActionType.Interact, ai => { ai._mobile.OnActionInteract(); return ai.DoActionInteract(); } },
        { ActionType.Backoff, ai => { ai._mobile.OnActionBackoff(); return ai.DoActionBackoff(); } }
    };

    public BaseAI(BaseCreature m)
    {
        _mobile = m;
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

    public virtual bool WasNamed(string speech) => !string.IsNullOrEmpty(_mobile.Name) && speech.InsensitiveStartsWith(_mobile.Name);

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

        var currentCombat = _mobile.Combatant;

        if (currentCombat == null || currentCombat == aggressor)
        {
            return;
        }

        if (_mobile.GetDistanceToSqrt(aggressor) < _mobile.GetDistanceToSqrt(currentCombat))
        {
            _mobile.Combatant = aggressor;
        }
    }

    public virtual void EndPickTarget(Mobile from, Mobile target, OrderType order)
    {
        if (!IsValidTarget(from, order) || order == OrderType.Attack && !CanAttackTarget(from, target))
        {
            return;
        }

        if (_mobile.CheckControlChance(from))
        {
            _mobile.ControlTarget = target;
            _mobile.ControlOrder = order;

            if (order == OrderType.Attack)
            {
                _mobile.FocusMob = target;
                _mobile.Combatant = target;
                Action = ActionType.Combat;
            }
        }
    }

    private bool IsValidTarget(Mobile from, OrderType order)
    {
        if (_mobile.Deleted || !_mobile.Controlled || !from.InRange(_mobile, 14)
            || from.Map != _mobile.Map || !from.CheckAlive())
        {
            return false;
        }

        var isOwner = from == _mobile.ControlMaster;
        var isFriend = !isOwner && _mobile.IsPetFriend(from);

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
        if (target is BaseCreature creature && creature.IsScaryToPets && _mobile.IsScaredOfScaryThings)
        {
            _mobile.SayTo(from, "Your pet refuses to attack this creature!");
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
            _mobile.SayTo(from, "Your pet refuses to attack the guard.");
            return false;
        }

        return true;
    }

    public void DebugSay(string message, int cooldownMs = 5000)
    {
        if (_mobile.Debug && Core.TickCount >= NextDebugMessage)
        {
            _mobile.PublicOverheadMessage(MessageType.Regular, 41, false, message);
            NextDebugMessage = Core.TickCount + cooldownMs;
        }
    }

    public virtual bool Think()
    {
        if (_mobile.Deleted || _mobile.Map == null)
        {
            return false;
        }

        if (CheckFlee())
        {
            return true;
        }

        return _staticActionHandlers.TryGetValue(Action, out var handler) && handler(this);
    }

    public virtual void OnActionChanged()
    {
        if (_staticActionChanges.TryGetValue(Action, out var handler))
        {
            handler(this);
        }
    }

    private void HandleWanderAction()
    {
        _mobile.FocusMob = null;
        _mobile.Warmode = false;
        _mobile.Combatant = null;
    }

    private void HandleCombatAction()
    {
        _mobile.Warmode = true;
    }

    private void HandleGuardAction()
    {
        _mobile.Warmode = true;
        _mobile.Combatant = null;
    }

    private void HandleFleeAction()
    {
        _mobile.FocusMob = null;
        _mobile.Warmode = true;
    }

    private void HandleInteractAction()
    {
        _mobile.Warmode = false;
    }

    private void HandleBackoffAction()
    {
        _mobile.Warmode = false;
    }

    public virtual bool DoActionWander()
    {
        if (CheckHerding())
        {
            this.DebugSayFormatted($"I am being herded by {_mobile.ControlTarget?.Name ?? "Unknown"}.");
        }
        else if (_mobile.CurrentWayPoint != null)
        {
            HandleWayPoint();
        }
        else if (_mobile.IsAnimatedDead)
        {
            FollowMaster();
        }
        else if (CheckMove() && CanMoveNow(out _) && !_mobile.CheckIdle())
        {
            WalkRandomInHome(3, 2, 1);
        }

        return true;
    }

    public virtual bool OnAtWayPoint() => true;

    private void HandleWayPoint()
    {
        var point = _mobile.CurrentWayPoint;

        if ((point.X != _mobile.Location.X || point.Y != _mobile.Location.Y)
            && point.Map == _mobile.Map && point.Parent == null && !point.Deleted)
        {
            this.DebugSayFormatted($"Moving towards waypoint {point.X}, {point.Y}.");

            DoMove(_mobile.GetDirectionTo(point));
        }
        else if (OnAtWayPoint())
        {
            this.DebugSayFormatted($"I have reached waypoint {point.X}, {point.Y}.");

            _mobile.CurrentWayPoint = point.NextPoint;
            if (point.NextPoint?.Deleted == true)
            {
                _mobile.CurrentWayPoint = point.NextPoint = point.NextPoint.NextPoint;
            }
        }
    }

    private void FollowMaster()
    {
        var master = _mobile.SummonMaster;
        if (master != null && master.Map == _mobile.Map && master.InRange(_mobile, _mobile.RangePerception))
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
            this.DebugSayFormatted($"I am being herded by {_mobile.ControlTarget?.Name ?? "Unknown"}.");
            return true;
        }

        var combatant = _mobile.Combatant;
        if (!IsValidCombatant(combatant))
        {
            DebugSay("My combatant is missing. Returning home...");

            _mobile.FocusMob = null;
            _mobile.Warmode = false;
            _mobile.Combatant = null;

            WalkRandomInHome(3, 2, 1);
            return true;
        }

        if (_mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, combatant))
        {
            this.DebugSayFormatted($"I used my abilities on {combatant.Name}!");
        }

        return true;
    }

    private bool IsValidCombatant(Mobile combatant) =>
        IsValidFocusMob(combatant) && _mobile.InLOS(combatant);

    bool IsValidFocusMob(Mobile focusMob) =>
        focusMob != null
        && !focusMob.Deleted
        && focusMob.Map == _mobile.Map
        && focusMob.Alive
        && (focusMob is not BaseCreature bc || !bc.IsDeadPet)
        && focusMob.AccessLevel == AccessLevel.Player
        && _mobile.CanSee(focusMob)
        && _mobile.InRange(focusMob, _mobile.RangePerception);

    public virtual bool DoActionGuard()
    {
        DebugSay("I am still on guard.");
        if (Utility.Random(10) == 0)
        {
            _mobile.Turn(Utility.Random(0, 2) - 1);
        }

        return true;
    }

    public virtual bool DoActionFlee()
    {
        var from = _mobile.FocusMob;

        if (!IsValidFocusMob(from))
        {
            DebugSay("Focus target is missing.");

            WalkRandomInHome(3, 2, 1);
            return true;
        }

        DebugSay("I am fleeing!");

        DoMove(from.GetDirectionTo(_mobile));
        return true;
    }

    public virtual bool DoActionInteract() => true;

    public virtual bool DoActionBackoff() => true;

    public virtual bool CheckHerding()
    {
        var target = _mobile.TargetLocation;

        if (target == null)
        {
            return false;
        }

        var distance = _mobile.GetDistanceToSqrt(target);

        if (distance >= 1 && distance <= 15)
        {
            DoMove(_mobile.GetDirectionTo(target));
            return true;
        }

        if (distance < 1 && IsSpecialHerdingCase(target))
        {
            HandleSpecialHerdingCase();
        }

        _mobile.TargetLocation = null;
        return false;
    }

    private bool IsSpecialHerdingCase(IPoint2D target) => target.X == 1076 && target.Y == 450 && _mobile is HordeMinionFamiliar;

    private void HandleSpecialHerdingCase()
    {
        if (_mobile.ControlMaster is PlayerMobile pm && pm.Quest is DarkTidesQuest qs)
        {
            var obj = qs.FindObjective<FetchAbraxusScrollObjective>();

            if (obj?.Completed == false)
            {
                _mobile.AddToBackpack(new ScrollOfAbraxus());
                obj.Complete();
            }
        }
    }

    public virtual bool DoBardPacified()
    {
        if (Core.Now < _mobile.BardEndTime)
        {
            DebugSay("I am pacified. Can not fight.");

            _mobile.Warmode = false;
            _mobile.Combatant = null;
        }
        else
        {
            DebugSay("I am free from pacification.");

            _mobile.BardPacified = false;
        }
        return true;
    }

    public virtual bool DoBardProvoked()
    {
        if (Core.Now >= _mobile.BardEndTime && IsProvokerLost())
        {
            DebugSay("Provoker missing.");

            _mobile.BardProvoked = false;
            _mobile.BardMaster = null;
            _mobile.BardTarget = null;
            _mobile.Warmode = false;
            _mobile.Combatant = null;
        }
        else if (IsProvokeTargetLost())
        {
            DebugSay("Provoke target missing.");

            _mobile.BardProvoked = false;
            _mobile.BardMaster = null;
            _mobile.BardTarget = null;
            _mobile.Warmode = false;
            _mobile.Combatant = null;
        }
        else
        {
            _mobile.Combatant = _mobile.BardTarget;
            Action = ActionType.Combat;
        }
        return true;
    }

    private bool IsProvokerLost() =>
        _mobile.BardMaster?.Deleted != false
        || _mobile.BardMaster.Map != _mobile.Map
        || _mobile.GetDistanceToSqrt(_mobile.BardMaster) > _mobile.RangePerception;

    private bool IsProvokeTargetLost() =>
        _mobile.BardTarget?.Deleted != false
        || _mobile.BardTarget.Map != _mobile.Map
        || _mobile.GetDistanceToSqrt(_mobile.BardTarget) > _mobile.RangePerception;

    public virtual bool CheckFlee()
    {
        if (!_mobile.CheckFlee())
        {
            return false;
        }

        if (_mobile.Combatant == null)
        {
            WalkRandomInHome(3, 2, 1);
        }

        return true;
    }

    public virtual void OnTeleported()
    {
        DebugSay("Teleported; recalculating path...");

        _path?.ForceRepath();
    }

    public virtual bool AcquireFocusMob(int iRange, FightMode acqType, bool bPlayerOnly, bool bFacFriend, bool bFacFoe)
    {
        if (_mobile.Deleted || _mobile.Map == null)
        {
            return false;
        }

        if (HandleBardProvoked() || HandleControlled() || HandleConstantFocus())
        {
            return true;
        }

        if (acqType == FightMode.None)
        {
            _mobile.FocusMob = null;
            return false;
        }

        if (HandleAggressor(acqType))
        {
            return false;
        }

        if (Core.TickCount - _mobile.NextReacquireTime < 0)
        {
            _mobile.FocusMob = null;
            return false;
        }

        _mobile.NextReacquireTime = Core.TickCount + (int)_mobile.ReacquireDelay.TotalMilliseconds;

        DebugSay("Acquiring new target...");

        if (_mobile.Map == null)
        {
            return _mobile.FocusMob != null;
        }

        return AcquireNewFocusMob(_mobile.Map, iRange, acqType, bPlayerOnly, bFacFriend, bFacFoe);
    }

    private bool HandleBardProvoked()
    {
        if (!_mobile.BardProvoked)
        {
            return false;
        }

        if (_mobile.BardTarget?.Deleted != false)
        {
            _mobile.FocusMob = null;
            return false;
        }

        _mobile.FocusMob = _mobile.BardTarget;
        return true;
    }

    private bool HandleControlled()
    {
        if (!_mobile.Controlled)
        {
            return false;
        }

        if (_mobile.ControlTarget?.Deleted != false || _mobile.ControlTarget?.Hidden == true
                                                     || _mobile.ControlTarget?.Alive != true || _mobile.ControlTarget?.IsDeadBondedPet == true
                                                     || !_mobile.InRange(_mobile.ControlTarget, _mobile.RangePerception * 2))
        {
            if (_mobile.ControlTarget != null && _mobile.ControlTarget != _mobile.ControlMaster)
            {
                _mobile.ControlTarget = null;
            }

            _mobile.FocusMob = null;
            return false;
        }

        _mobile.FocusMob = _mobile.ControlTarget;
        return true;
    }

    private bool HandleConstantFocus()
    {
        if (_mobile.ConstantFocus == null)
        {
            return false;
        }

        this.DebugSayFormatted($"Acquired focused target: {_mobile.ConstantFocus.Name}.");

        _mobile.FocusMob = _mobile.ConstantFocus;
        return true;
    }

    private bool HandleAggressor(FightMode acqType)
    {
        if (acqType != FightMode.Aggressor || _mobile.Aggressors.Count > 0
                                           || _mobile.Aggressed.Count > 0 || _mobile.FactionAllegiance != null
                                           || _mobile.EthicAllegiance != null)
        {
            return false;
        }

        _mobile.FocusMob = null;
        return true;
    }

    private bool AcquireNewFocusMob(Map map, int iRange, FightMode acqType, bool bPlayerOnly, bool bFacFriend, bool bFacFoe)
    {
        Mobile newFocusMob = null, enemySummonMob = null;
        double val = double.MinValue, enemySummonVal = double.MinValue;

        foreach (var m in map.GetMobilesInRange(_mobile.Location, iRange))
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

            var theirVal = _mobile.GetFightModeRanking(m, acqType, bPlayerOnly);

            if (theirVal > val && _mobile.InLOS(m))
            {
                newFocusMob = m;
                val = theirVal;
            }
            else if (Core.AOS && theirVal > enemySummonVal
                              && _mobile.InLOS(m) && bc?.Summoned == true && bc.Controlled != true)
            {
                enemySummonMob = m;
                enemySummonVal = theirVal;
            }
        }

        _mobile.FocusMob = newFocusMob ?? enemySummonMob;
        return _mobile.FocusMob != null;
    }

    private bool IsInvalidTarget(Mobile m, bool bPlayerOnly) =>
        m.Deleted || m.Blessed || m == _mobile || m is BaseFamiliar || !m.Alive || m.IsDeadBondedPet ||
        m.AccessLevel > AccessLevel.Player || bPlayerOnly && !m.Player || !_mobile.CanSee(m);

    private bool IsInvalidSummonTarget(Mobile m, BaseCreature bc, PlayerMobile pm)
    {
        if (Core.AOS && bc?.Summoned == true &&
            (bc.SummonMaster == _mobile || !bc.SummonMaster.Player && IsHostile(bc.SummonMaster)))
        {
            return true;
        }

        if (_mobile.Summoned && _mobile.SummonMaster != null)
        {
            return m == _mobile.SummonMaster || !SpellHelper.ValidIndirectTarget(_mobile.SummonMaster, m)
                                              || pm != null && _mobile.IsAnimatedDead || _mobile.IsAnimatedDead
                                              && bc?.IsAnimatedDead == true || bc?.Controlled == true;
        }

        return false;
    }

    private bool IsInvalidFactionTarget(Mobile m, bool bFacFriend, bool bFacFoe)
    {
        if (bFacFriend && !_mobile.IsFriend(m))
        {
            return true;
        }

        if (TransformationSpellHelper.UnderTransformation(m, typeof(EtherealVoyageSpell)) ||
            _mobile.Combatant != m && VirtueSystem.GetVirtues(m as PlayerMobile)?.HonorActive == true)
        {
            return true;
        }

        return bFacFoe && (!_mobile.IsEnemy(m) || !bFacFriend && !_mobile.CanBeHarmful(m, false));
    }

    private bool IsInvalidFightModeTarget(Mobile m, FightMode acqType, BaseCreature bc)
    {
        if (acqType is not (FightMode.Aggressor or FightMode.Evil))
        {
            return false;
        }

        var valid = IsHostile(m) || _mobile.GetFactionAllegiance(m) == BaseCreature.Allegiance.Enemy
                                 || _mobile.GetEthicAllegiance(m) == BaseCreature.Allegiance.Enemy;

        // Valid if FightMode is Evil and the target's karma is negative
        return !valid && acqType != FightMode.Evil || (bc?.GetMaster()?.Karma ?? m.Karma) >= 0;
    }

    private bool IsHostile(Mobile from) => _mobile.Combatant == from || from.Combatant == _mobile || IsAggressor(from) || IsAggressed(from);

    private bool IsAggressor(Mobile from)
    {
        foreach (var aggressor in _mobile.Aggressors)
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
        foreach (var aggressed in _mobile.Aggressed)
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
        if (_mobile.Deleted || _mobile.Map == null || !CanDetectHidden)
        {
            return;
        }

        DebugSay("Checking for hidden entities...");

        var srcSkill = _mobile.Skills.DetectHidden.Value;

        if (srcSkill <= 0)
        {
            return;
        }

        foreach (var trg in _mobile.GetMobilesInRange(_mobile.RangePerception))
        {
            if (IsValidTargetCombatTarget(trg))
            {
                TryDetectHidden(trg, srcSkill);
            }
        }
    }

    private bool IsValidTargetCombatTarget(Mobile trg) => trg != _mobile && trg.Player && trg.Alive && trg.Hidden &&
                                                          trg.AccessLevel == AccessLevel.Player && _mobile.InLOS(trg);

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
        if (!_mobile.PlayerRangeSensitive)
        {
            return;
        }

        _timer.Stop();

        if (ShouldReturnToHome(_mobile.Spawner as Spawner))
        {
            Timer.StartTimer(ReturnToHome);
        }
    }

    private bool ShouldReturnToHome(Spawner spawner) =>
        spawner?.ReturnOnDeactivate == true && !_mobile.Controlled &&
        (spawner.HomeLocation == Point3D.Zero || !_mobile.InRange(spawner.HomeLocation, spawner.HomeRange));

    private void ReturnToHome()
    {
        if (_mobile.Spawner is not Spawner spawner)
        {
            return;
        }

        var loc = spawner.GetSpawnPosition(_mobile, spawner.Map);

        if (loc != Point3D.Zero)
        {
            _mobile.MoveToWorld(loc, spawner.Map);
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
        _timer.Interval = TimeSpan.FromMilliseconds(_mobile.CurrentSpeed * 1000);
    }
}
