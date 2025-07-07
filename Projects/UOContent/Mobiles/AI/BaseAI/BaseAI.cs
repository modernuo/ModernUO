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
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.Quests.Necro;
using Server.Engines.Spawners;
using Server.Engines.Virtues;
using Server.Factions;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Spells;
using Server.Spells.Spellweaving;
using Server.Targets;
using MoveImpl = Server.Movement.MovementImpl;

namespace Server.Mobiles;

public enum AIType
{
    AI_Use_Default,
    AI_Melee,
    AI_Animal,
    AI_Archer,
    AI_Healer,
    AI_Vendor,
    AI_Mage,
    AI_Berserk,
    AI_Predator,
    AI_Thief
}

public enum ActionType
{
    Wander,
    Combat,
    Guard,
    Flee,
    Backoff,
    Interact
}

public abstract partial class BaseAI
{
    protected ActionType m_Action;
    public readonly BaseCreature m_Mobile;
    public long m_NextDetectHidden;
    private long m_NextStopGuard;
    protected PathFollower m_Path;
    public Timer m_Timer;
    private long m_NextDebugMessage;
    private string _lastDebugMessage;
    public DateTime _lastOrder = DateTime.MinValue;
    public Mobile _lastCommandIssuer;

    private static readonly Dictionary<ActionType, ActionHandler> _staticActionHandlers = new()
    {
        { ActionType.Wander, (ai) => { ai.m_Mobile.OnActionWander(); return ai.DoActionWander(); } },
        { ActionType.Combat, (ai) => { ai.m_Mobile.OnActionCombat(); return ai.DoActionCombat(); } },
        { ActionType.Guard, (ai) => { ai.m_Mobile.OnActionGuard(); return ai.DoActionGuard(); } },
        { ActionType.Flee, (ai) => { ai.m_Mobile.OnActionFlee(); return ai.DoActionFlee(); } },
        { ActionType.Interact, (ai) => { ai.m_Mobile.OnActionInteract(); return ai.DoActionInteract(); } },
        { ActionType.Backoff, (ai) => { ai.m_Mobile.OnActionBackoff(); return ai.DoActionBackoff(); } }
    };

    private static readonly Dictionary<ActionType, ActionChange> _staticActionChanges = new()
    {
        { ActionType.Wander, (ai) => ai.HandleWanderAction() },
        { ActionType.Combat, (ai) => ai.HandleCombatAction() },
        { ActionType.Guard, (ai) => ai.HandleGuardAction() },
        { ActionType.Flee, (ai) => ai.HandleFleeAction() },
        { ActionType.Interact, (ai) => ai.HandleInteractAction() },
        { ActionType.Backoff, (ai) => ai.HandleBackoffAction() }
    };

    private delegate bool ActionHandler(BaseAI ai);
    private delegate void ActionChange(BaseAI ai);

    public BaseAI(BaseCreature m)
    {
        m_Mobile = m;
        m_Timer = new AITimer(this);

        var activate = !m.PlayerRangeSensitive || 
            (!World.Loading && m.Map != null && m.Map != Map.Internal && 
             m.Map.GetSector(m.Location).Active);

        if (activate)
        {   
            m_Timer.Start();
        }

        if (Action != ActionType.Wander)
        {
            Action = ActionType.Wander;
        }
    }

    public ActionType Action
    {
        get => m_Action;
        set
        {
            if (m_Action != value)
            {
                m_Action = value;
                OnActionChanged();
            }
        }
    }

    public long NextMove { get; set; }

    // OSI, DetectHidden >= 50.0?
    public virtual bool CanDetectHidden => m_Mobile.Skills.DetectHidden.Value > 0;
    
    public virtual bool WasNamed(string speech)
    {
        var name = m_Mobile.Name;
        return !string.IsNullOrEmpty(name) && speech.InsensitiveStartsWith(name);
    }

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
                from.SendLocalizedMessage(502038);
                // 502038: Click on the person to transfer ownership to.
                break;
            case OrderType.Friend:
                from.SendLocalizedMessage(502020);
                // 502020: Click on the player whom you wish to make a co-owner.
                break;
            case OrderType.Unfriend:
                from.SendLocalizedMessage(1070948);
                // 1070948: Click on the player whom you wish to remove as a co-owner.
                break;
        }
    }

    public virtual void OnAggressiveAction(Mobile aggressor)
    {
        if (aggressor.Hidden)
        {
            return;
        }
    
        var currentCombat = m_Mobile.Combatant;
    
        if (currentCombat == null || currentCombat == aggressor)
        {
            return;
        }
    
        var currentDistance = m_Mobile.GetDistanceToSqrt(currentCombat);
        var aggressorDistance = m_Mobile.GetDistanceToSqrt(aggressor);
    
        if (aggressorDistance < currentDistance)
        {
            m_Mobile.Combatant = aggressor;
        }
    }

    public virtual void EndPickTarget(Mobile from, Mobile target, OrderType order)
    {
        if (!IsValidTarget(from, order) || 
            (order == OrderType.Attack && !CanAttackTarget(from, target)))
        {
            return;
        }
    
        if (m_Mobile.CheckControlChance(from))
        {
            m_Mobile.ControlTarget = target;
            m_Mobile.ControlOrder = order;
    
            if (order == OrderType.Attack)
            {
                m_Mobile.FocusMob = target;
                m_Mobile.Combatant = target;
                Action = ActionType.Wander;
            }
        }
    }

    private bool IsValidTarget(Mobile from, OrderType order)
    {
        if (m_Mobile.Deleted || !m_Mobile.Controlled || !from.InRange(m_Mobile, 14)
            || from.Map != m_Mobile.Map || !from.CheckAlive())
        {
            return false;
        }

        var isOwner = from == m_Mobile.ControlMaster;
        var isFriend = !isOwner && m_Mobile.IsPetFriend(from);

        if (!isOwner && !isFriend)
        {
            return false;
        }

        if (isFriend && order != OrderType.Follow && order != OrderType.Stay && order != OrderType.Stop)
        {
            return false;
        }
    
        return true;
    }

    private bool CanAttackTarget(Mobile from, Mobile target)
    {
        if (target is BaseCreature creature && creature.IsScaryToPets && m_Mobile.IsScaredOfScaryThings)
        {
            m_Mobile.SayTo(from, "Your pet refuses to attack this creature!");
            return false;
        }
    
        if (SolenHelper.CheckRedFriendship(from) &&
                target is RedSolenInfiltratorQueen or RedSolenInfiltratorWarrior
                    or RedSolenQueen or RedSolenWarrior or RedSolenWorker ||
            SolenHelper.CheckBlackFriendship(from) &&
                target is BlackSolenInfiltratorQueen or BlackSolenInfiltratorWarrior
                    or BlackSolenQueen or BlackSolenWarrior or BlackSolenWorker)
        {
            from.SendLocalizedMessage(1063106);
            // 1063106: You can not force your pet to attack a creature you are protected from.
            return false;
        }

        if (target is BaseFactionGuard)
        {
            m_Mobile.SayTo(from, "Your pet refuses to attack the guard.");
            return false;
        }

        return true;
    }

    public void DebugSay(string message, int cooldownMs = 5000)
    {
        if (m_Mobile.Debug && (Core.TickCount >= m_NextDebugMessage 
            || !string.Equals(_lastDebugMessage, message, StringComparison.Ordinal)))
        {
            m_Mobile.DebugSay(message);
            m_NextDebugMessage = Core.TickCount + cooldownMs;
            _lastDebugMessage = message;
        }
    }

    public virtual bool Think()
    {
        if (m_Mobile.Deleted != false || m_Mobile.Map == null)
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
        m_Mobile.FocusMob = null;
        m_Mobile.Warmode = false;
        m_Mobile.Combatant = null;
    }
    
    private void HandleCombatAction()
    {
        m_Mobile.FocusMob = null;
        m_Mobile.Warmode = true;
    }
    
    private void HandleGuardAction()
    {
        m_Mobile.FocusMob = null;
        m_Mobile.Warmode = true;
        m_Mobile.Combatant = null;
    }
    
    private void HandleFleeAction()
    {
        m_Mobile.FocusMob = null;
        m_Mobile.Warmode = true;
    }
    
    private void HandleInteractAction()
    {
        m_Mobile.Warmode = false;
    }
    
    private void HandleBackoffAction()
    {
        m_Mobile.Warmode = false;
    }

    public virtual bool DoActionWander()
    {
        if (CheckHerding())
        {
            DebugSay($"I am being herded by {m_Mobile.ControlTarget?.Name ?? "Unknown"}.");
        }
        else if (m_Mobile.CurrentWayPoint != null)
        {
            HandleWayPoint();
        }
        else if (m_Mobile.IsAnimatedDead)
        {
            FollowMaster();
        }
        else if (CheckMove() && CanMoveNow(out _) && !m_Mobile.CheckIdle())
        {
            WalkRandomInHome(3, 2, 1);
        }
    
        return true;
    }

    public virtual bool OnAtWayPoint() => true;

    private void HandleWayPoint()
    {
        var point = m_Mobile.CurrentWayPoint;
    
        if ((point.X != m_Mobile.Location.X || point.Y != m_Mobile.Location.Y)
            && point.Map == m_Mobile.Map && point.Parent == null && !point.Deleted)
        {
            DebugSay($"Moving towards waypoint {point.X}, {point.Y}.");

            DoMove(m_Mobile.GetDirectionTo(point));
        }
        else if (OnAtWayPoint())
        {
            DebugSay($"I have reached waypoint {point.X}, {point.Y}.");

            m_Mobile.CurrentWayPoint = point.NextPoint;
    
            if (point.NextPoint?.Deleted == true)
            {
                m_Mobile.CurrentWayPoint = point.NextPoint = point.NextPoint.NextPoint;
            }
        }
    }
    
    private void FollowMaster()
    {
        var master = m_Mobile.SummonMaster;
    
        if (master != null && master.Map == m_Mobile.Map && master.InRange(m_Mobile, m_Mobile.RangePerception))
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
            DebugSay($"I am being herded by {m_Mobile.ControlTarget?.Name ?? "Unknown"}.");
            return true;
        }
    
        var combatant = m_Mobile.Combatant;
    
        if (!IsValidCombatant(combatant))
        {
            DebugSay("My combatant is missing. Returning home...");

            m_Mobile.FocusMob = null;
            m_Mobile.Warmode = false;
            m_Mobile.Combatant = null;
            WalkRandomInHome(3, 2, 1);
            return true;
        }
    
        if (m_Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, combatant))
        {
            DebugSay($"I used my abilities on {combatant.Name}!");
        }
    
        return true;
    }
    
    private bool IsValidCombatant(Mobile combatant)
    {
        return combatant != null
            && !combatant.Deleted
            && combatant.Map == m_Mobile.Map
            && combatant.Alive
            && (!(combatant is BaseCreature bc) || !bc.IsDeadPet)
            && combatant.AccessLevel == AccessLevel.Player
            && m_Mobile.CanSee(combatant)
            && m_Mobile.InLOS(combatant)
            && m_Mobile.InRange(combatant, m_Mobile.RangePerception);
    }

    public virtual bool DoActionGuard()
    {
        if (Core.TickCount - m_NextStopGuard < 0)
        {
            DebugSay("I am still on guard.");

            m_Mobile.Turn(Utility.Random(0, 2) - 1); // added for immersion
        }
        else
        {
            DebugSay("I stopped being on guard.");

            Action = ActionType.Wander;
        }
    
        return true;
    }

    public virtual bool DoActionFlee()
    {
        var from = m_Mobile.FocusMob;

        bool IsValidFocusMob(Mobile focusMob)
        {
            return focusMob != null
                && !focusMob.Deleted
                && focusMob.Map == m_Mobile.Map
                && focusMob.Alive
                && (!(focusMob is BaseCreature bc) || !bc.IsDeadPet)
                && focusMob.AccessLevel == AccessLevel.Player
                && m_Mobile.CanSee(focusMob)
                && m_Mobile.InRange(focusMob, m_Mobile.RangePerception);
        }

        if (!IsValidFocusMob(from))
        {
            DebugSay("Focus target is missing.");
            WalkRandomInHome(3, 2, 1);
            return true;
        }

        DebugSay("I am fleeing!");
        var direction = from.GetDirectionTo(m_Mobile);
        DoMove(direction);
        return true;
    }

    public virtual bool DoActionInteract() => true;
    
    public virtual bool DoActionBackoff() => true;
    
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
        m_Mobile.ControlMaster.SendLocalizedMessage(1049671, m_Mobile.Name);
        // 1049671: ~1_NAME~ is now guarding you.
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
    
        var currentDistance = (int)m_Mobile.GetDistanceToSqrt(m_Mobile.ControlMaster);
    
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
        DebugSay($"{m_Mobile.ControlTarget?.Name ?? "Unknown"}, has orderd me to come here.");
    
        if (WalkMobileRange(m_Mobile.ControlMaster, 1, currentDistance > 2, 1, 2))
        {
            m_Mobile.Warmode = IsValidCombatant(m_Mobile.Combatant);
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
    
        for (var i = items.Count - 1; i >= 0; --i)
        {
            if (i < items.Count)
            {
                items[i].MoveToWorld(m_Mobile.Location, m_Mobile.Map);
            }
        }
    }

    public virtual bool CheckHerding()
    {
        var target = m_Mobile.TargetLocation;
    
        if (target == null)
        {
            return false;
        }
    
        var distance = m_Mobile.GetDistanceToSqrt(target);
    
        if (distance >= 1 && distance <= 15)
        {
            DoMove(m_Mobile.GetDirectionTo(target));
            return true;
        }
    
        if (distance < 1 && IsSpecialHerdingCase(target))
        {
            HandleSpecialHerdingCase();
        }
    
        m_Mobile.TargetLocation = null;
        
        return false;
    }
    
    private bool IsSpecialHerdingCase(IPoint2D target)
    {
        return target.X == 1076 && target.Y == 450 && m_Mobile is HordeMinionFamiliar;
    }
    
    private void HandleSpecialHerdingCase()
    {
        if (m_Mobile.ControlMaster is PlayerMobile pm && pm.Quest is DarkTidesQuest qs)
        {
            var obj = qs.FindObjective<FetchAbraxusScrollObjective>();
    
            if (obj?.Completed == false)
            {
                m_Mobile.AddToBackpack(new ScrollOfAbraxus());
                obj.Complete();
            }
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

    public virtual bool DoOrderFriend()
    {
        var from = m_Mobile.ControlMaster;
        var to = m_Mobile.ControlTarget;
        
        if (IsYoungPlayer(from, to))
        {
            return true;
        }
        else if (from?.Deleted != false || to?.Deleted != false || from == to || !to.Player)
        {
            m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039); // *looks confused*
        }
        else if (from.CanBeBeneficial(to, true))
        {
            HandleFriendRequest(from, to);
        }
    
        m_Mobile.ControlTarget = from;
        m_Mobile.ControlOrder = OrderType.Follow;
    
        return true;
    }
    
    private static bool IsYoungPlayer(Mobile from, Mobile to)
    {
        var youngFrom = from is PlayerMobile mobile && mobile.Young;
        var youngTo = to is PlayerMobile playerMobile && playerMobile.Young;
    
        if (youngFrom && !youngTo)
        {
            from.SendLocalizedMessage(502040);
            // 502040: As a young player, you may not friend pets to older players.
            return true;
        }
        else if (!youngFrom && youngTo)
        {
            from.SendLocalizedMessage(502041);
            // 502041: As an older player, you may not friend pets to young players.
            return true;
        }
    
        return false;
    }
    
    private void HandleFriendRequest(Mobile from, Mobile to)
    {
        if (from.HasTrade)
        {
            from.SendLocalizedMessage(1070947);
            // 1070947: You cannot friend a pet with a trade pending
        }
        else if (to.HasTrade)
        {
            to.SendLocalizedMessage(1070947);
            // 1070947: You cannot friend a pet with a trade pending
        }
        else if (m_Mobile.IsPetFriend(to))
        {
            from.SendLocalizedMessage(1049691);
            // 1049691: That person is already a friend.
        }
        else if (!m_Mobile.AllowNewPetFriend)
        {
            from.SendLocalizedMessage(1005482);
            // 1005482: Your pet does not seem to be interested in making new friends right now.
        }
        else
        {
            from.SendLocalizedMessage(1049676, $"{m_Mobile.Name}\t{to.Name}");
            to.SendLocalizedMessage(1043246, $"{from.Name}\t{m_Mobile.Name}");
            // 1049676: ~1_NAME~ will now accept movement commands from ~2_NAME~.
            // 1043246: ~1_NAME~ has granted you the ability to give orders to their pet ~2_PET_NAME~.
    
            m_Mobile.AddPetFriend(to);
    
            m_Mobile.ControlTarget = to;
            m_Mobile.ControlOrder = OrderType.Follow;
        }
    }

    public virtual bool DoOrderUnfriend()
    {
        var from = m_Mobile.ControlMaster;
        var to = m_Mobile.ControlTarget;
    
        if (IsInvalidUnfriendRequest(from, to))
        {
            m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039);
            // 502039: *looks confused*
        }
        else if (!m_Mobile.IsPetFriend(to))
        {
            from.SendLocalizedMessage(1070953);
            // 1070953: That person is not a friend.
        }
        else
        {
            HandleUnfriendRequest(from, to);
        }
    
        m_Mobile.ControlTarget = from;
        m_Mobile.ControlOrder = OrderType.Follow;
    
        return true;
    }
    
    private static bool IsInvalidUnfriendRequest(Mobile from, Mobile to)
    {
        return from?.Deleted != false || to?.Deleted != false || from == to || !to.Player;
    }
    
    private void HandleUnfriendRequest(Mobile from, Mobile to)
    {
        from.SendLocalizedMessage(1070951, $"{m_Mobile.Name}\t{to.Name}");
        to.SendLocalizedMessage(1070952, $"{from.Name}\t{m_Mobile.Name}");
        // 1070951: ~1_NAME~ will no longer accept movement commands from ~2_NAME~.
        // 1070952: ~1_NAME~ has no longer granted you the ability to give orders to their pet ~2_PET_NAME~.
    
        m_Mobile.RemovePetFriend(to);
    }

    public virtual bool DoOrderGuard()
    {
        if (m_Mobile.IsDeadPet)
        {
            return true;
        }
    
        var controlMaster = m_Mobile.ControlMaster;
    
        if (m_Mobile.ControlMaster?.Deleted != false)
        {
            return true;
        }
    
        var combatant = FindCombatant(controlMaster);
    
        if (IsValidCombatant(combatant))
        {
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
            var distanceFromGuardLocation = (int)m_Mobile.GetDistanceToSqrt(guardLocation);
            
            if (distanceFromGuardLocation > 3)
            {
                var direction = m_Mobile.GetDirectionTo(guardLocation);

                DoMove(direction);
            }
            else
            {
                WalkRandom(3, 1, 1);
            }
        }
    
        return true;
    }

    private Mobile FindCombatant(Mobile controlMaster)
    {
        var combatant = m_Mobile.Combatant;
        var aggressors = controlMaster.Aggressors;
    
        if (aggressors.Count > 0)
        {
            for (var i = 0; i < aggressors.Count; ++i)
            {
                var info = aggressors[i];
                var attacker = info.Attacker;
    
                if (attacker?.Deleted == false &&
                    attacker.GetDistanceToSqrt(m_Mobile) <= m_Mobile.RangePerception)
                {
                    if (combatant == null || attacker.GetDistanceToSqrt(controlMaster) <
                        combatant.GetDistanceToSqrt(controlMaster))
                    {
                        combatant = attacker;
                    }
                }
            }
    
            DebugSay($"Master {controlMaster.Name} is under attack by {combatant.Name}. Assiting...");
        }
    
        return combatant;
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
    
        if (Core.AOS || m_Mobile.IsBonded)
        {
            m_Mobile.ControlOrder = OrderType.Follow;
        }
        else
        {
            m_Mobile.ControlOrder = OrderType.None;
        }
    
        if (m_Mobile.FightMode is FightMode.Closest or FightMode.Aggressor)
        {
            FindNewCombatant();
        }
    }
    
    private void FindNewCombatant()
    {
        Mobile newCombatant = null;
        var newScore = 0.0;
    
        foreach (var aggr in m_Mobile.GetMobilesInRange(m_Mobile.RangePerception))
        {
            if (!m_Mobile.CanSee(aggr) || aggr.Combatant != m_Mobile || aggr.IsDeadBondedPet || !aggr.Alive)
            {
                continue;
            }
    
            var aggrScore = m_Mobile.GetFightModeRanking(aggr, FightMode.Closest, false);
    
            if ((newCombatant == null || aggrScore > newScore) && m_Mobile.InLOS(aggr))
            {
                newCombatant = aggr;
                newScore = aggrScore;
            }
        }
    
        if (newCombatant != null)
        {
            m_Mobile.ControlTarget = newCombatant;
            m_Mobile.ControlOrder = OrderType.Attack;
            m_Mobile.Combatant = newCombatant;
    
            DebugSay($"{newCombatant.Name} is still alive. Resuming attacks...");
    
            Think();
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

        if (IsValidTransferRequest(from, to))
        {
            DebugSay($"Beginning transfer with {to.Name}");

            if (IsYoungPlayer(from, to))
            {
                return true;
            }
            else if (!m_Mobile.CanBeControlledBy(to))
            {
                SendTransferRefusalMessages(from, to, 1043248, 1043249);
                // 1043248: The pet refuses to be transferred because it will not obey ~1_NAME~.~3_BLANK~
                // 1043249: The pet will not accept you as a master because it does not trust you.~3_BLANK~
                return false;
            }
            else if (!m_Mobile.CanBeControlledBy(from))
            {
                SendTransferRefusalMessages(from, to, 1043250, 1043251);
                // 1043250: The pet refuses to be transferred because it will not obey you sufficiently.~3_BLANK~
                // 1043251: The pet will not accept you as a master because it does not trust ~2_NAME~.~3_BLANK~
                return false;
            }
            else if (m_Mobile.Combatant != null || m_Mobile.Aggressors.Count > 0 ||
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
                // 1010507: You cannot transfer a pet with a trade pending
                to.SendLocalizedMessage(1010507);
                // 1010507: You cannot transfer a pet with a trade pending
                return false;
            }
            else
            {
                var container = fromState.AddTrade(toState);
                container.DropItem(new TransferItem(m_Mobile));
            }
        }

        m_Mobile.ControlOrder = OrderType.Stay;

        return true;
    }
    
    private static bool IsValidTransferRequest(Mobile from, Mobile to)
    {
        return from?.Deleted == false && to?.Deleted == false && from != to && to.Player;
    }
    
    private static void SendTransferRefusalMessages(Mobile from, Mobile to, int fromMessage, int toMessage)
    {
        var args = $"{to.Name}\t{from.Name}\t ";
        from.SendLocalizedMessage(fromMessage, args);
        to.SendLocalizedMessage(toMessage, args);
    }

    public virtual bool DoBardPacified()
    {
        if (Core.Now < m_Mobile.BardEndTime)
        {
            DebugSay("I am pacified. Can not fight.");

            m_Mobile.Warmode = false;
            m_Mobile.Combatant = null;
        }
        else
        {
            DebugSay("I am free from pacification.");
    
            m_Mobile.BardPacified = false;
        }
    
        return true;
    }
    
    public virtual bool DoBardProvoked()
    {
        if (Core.Now >= m_Mobile.BardEndTime && IsProvokerLost())
        {
            DebugSay("Provoker missing.");
    
            m_Mobile.BardProvoked = false;
            m_Mobile.BardMaster = null;
            m_Mobile.BardTarget = null;
            m_Mobile.Warmode = false;
            m_Mobile.Combatant = null;
        }
        else if (IsProvokeTargetLost())
        {
            DebugSay("Provoke target missing.");
    
            m_Mobile.BardProvoked = false;
            m_Mobile.BardMaster = null;
            m_Mobile.BardTarget = null;
            m_Mobile.Warmode = false;
            m_Mobile.Combatant = null;
        }
        else
        {
            m_Mobile.Combatant = m_Mobile.BardTarget;
            Action = ActionType.Combat;
        }
    
        return true;
    }
    
    private bool IsProvokerLost()
    {
        return m_Mobile.BardMaster?.Deleted != false ||
               m_Mobile.BardMaster.Map != m_Mobile.Map ||
               m_Mobile.GetDistanceToSqrt(m_Mobile.BardMaster) > m_Mobile.RangePerception;
    }
    
    private bool IsProvokeTargetLost()
    {
        return m_Mobile.BardTarget?.Deleted != false ||
               m_Mobile.BardTarget.Map != m_Mobile.Map ||
               m_Mobile.GetDistanceToSqrt(m_Mobile.BardTarget) > m_Mobile.RangePerception;
    }
    
    private Direction GetRandomDirection(int chanceToDir)
    {
        var randomMove = Utility.Random(8 * (chanceToDir + 1));
        return randomMove < 8 ? (Direction)randomMove : m_Mobile.Direction;
    }

    public static double BadlyHurtMoveDelay(BaseCreature bc)
    {
        int statMin = Core.HS ? bc.Stam : bc.Hits;
        int statMax = Core.HS ? bc.StamMax : bc.HitsMax;
    
        if (!bc.IsDeadPet && (bc.ReduceSpeedWithDamage || bc.IsSubdued) 
            && statMax > 0 && statMin < statMax * 0.3) // 30% hp
        {
            double hits = (double)statMin / statMax;

            if (hits < 0.1)
            {
                return bc.CurrentSpeed + 0.15; // 150ms
            }
            else if (hits < 0.2)
            {
                return bc.CurrentSpeed + 0.1; // 100ms
            }
            else if (hits < 0.3)
            {
                return bc.CurrentSpeed + 0.05; // 50ms
            }
        }
    
        return bc.CurrentSpeed;
    }

    public bool CanMoveNow(out double delay)
    {
        delay = 0.0;
        return Core.TickCount >= NextMove;
    }

    public virtual bool CheckMove() 
    {
        if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves)
        {
            return false;
        }

        return true;
    }
    
    public virtual bool DoMove(Direction d, bool badStateOk = false)
    {
        var res = DoMoveImpl(d, badStateOk);
        return IsMoveSuccessful(res, badStateOk);
    }
    
    private static bool IsMoveSuccessful(MoveResult res, bool badStateOk)
    {
        return res is MoveResult.Success or MoveResult.SuccessAutoTurn 
            || (badStateOk && res == MoveResult.BadState);
    }

    public virtual MoveResult DoMoveImpl(Direction d, bool badStateOk)
    {
        var isInBadState = IsInBadState();

        if (isInBadState)
        {
            return MoveResult.BadState;
        }
        
        if (!CanMoveNow(out _))
        {
            return MoveResult.BadState;
        }

        if (Core.TickCount - _lastGroupUpdateTime > 1000)
        {
            CleanupReservedPositions();
            _lastGroupUpdateTime = Core.TickCount;
        }
    
        if ((m_Mobile.Direction & Direction.Mask) != (d & Direction.Mask))
        {
            m_Mobile.Direction = d;
        }
    
        m_Mobile.Pushing = false;
        var mobDirection = m_Mobile.Direction;
    
        var moveResult = TryMove(d);
    
        if (moveResult)
        {
            if (m_Mobile.Hits < m_Mobile.HitsMax * 0.3)
            {
                m_Mobile.CurrentSpeed = BadlyHurtMoveDelay(m_Mobile);
            }
            else if (m_Mobile.Warmode || m_Mobile.Combatant != null)
            {
                m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
            }
            else
            {
                m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
            }
        
            return MoveResult.Success;
        }
    
        if ((mobDirection & Direction.Mask) != (d & Direction.Mask))
        {
            m_Mobile.Direction = d;
            return MoveResult.SuccessAutoTurn;
        }
    
        return HandleBlockedMovement(d, mobDirection);
    }

    private static void CleanupReservedPositions()
    {
        var toRemove = new List<BaseCreature>();
    
        foreach (var kvp in _reservedPositions)
        {
            if (kvp.Key?.Deleted != false || kvp.Key.GetDistanceToSqrt(kvp.Value) < 1)
            {
                toRemove.Add(kvp.Key);
            }
        }
    
        foreach (var creature in toRemove)
        {
            _reservedPositions.Remove(creature);
        }
    }

    private bool TryMove(Direction d)
    {
        MoveImpl.IgnoreMovableImpassables = m_Mobile.CanMoveOverObstacles && !m_Mobile.CanDestroyObstacles;
        var result = m_Mobile.Move(d);
        MoveImpl.IgnoreMovableImpassables = false;
        return result;
    }

    private bool IsInBadState() =>
        m_Mobile == null || m_Mobile.Deleted || m_Mobile.Frozen || m_Mobile.Paralyzed ||
        m_Mobile.Spell?.IsCasting == true || m_Mobile.DisallowAllMoves;

    private MoveResult HandleBlockedMovement(Direction d, Direction mobDirection)
    {
        var wasPushing = m_Mobile.Pushing;

        if ((m_Mobile.CanOpenDoors || m_Mobile.CanDestroyObstacles) && !TryClearObstacles(d))
        {
            return MoveResult.Success;
        }

        return TryAlternateMovement(wasPushing);
    }

    private MoveResult TryAlternateMovement(bool wasPushing)
    {
        var offset = Utility.RandomDouble() < 0.4 ? 1 : -1;

        for (var i = 0; i < 2; ++i)
        {
            m_Mobile.TurnInternal(offset);

            if (m_Mobile.Move(m_Mobile.Direction))
            {
                return MoveResult.SuccessAutoTurn;
            }
        }

        return wasPushing ? MoveResult.BadState : MoveResult.Blocked;
    }

    private bool TryClearObstacles(Direction d)
    {
        DebugSay("My movement is blocked. Trying to push through.");

        var map = m_Mobile.Map;
        if (map == null)
        {
            return true;
        }

        var (x, y) = GetOffsetLocation(d);
        using var queue = PooledRefQueue<Item>.Create();
        var destroyables = GatherObstacles(x, y, queue);

        if (destroyables > 0)
        {
            Effects.PlaySound(new Point3D(x, y, m_Mobile.Z), m_Mobile.Map, 0x3B3);
        }

        return ProcessObstacles(queue, d);
    }

    private (int x, int y) GetOffsetLocation(Direction d)
    {
        var x = m_Mobile.X;
        var y = m_Mobile.Y;
        Movement.Movement.Offset(d, ref x, ref y);
        return (x, y);
    }

    private int GatherObstacles(int x, int y, PooledRefQueue<Item> queue)
    {
        var destroyables = 0;

        foreach (var item in m_Mobile.Map.GetItemsInRange(new Point2D(x, y), 1))
        {
            if (IsValidDoor(item, x, y) || IsValidDestroyableItem(item))
            {
                queue.Enqueue(item);
                if (item is not BaseDoor)
                {
                    destroyables++;
                }
            }
        }

        return destroyables;
    }

    private bool IsValidDoor(Item item, int x, int y)
    {
        if (!m_Mobile.CanOpenDoors || item is not BaseDoor door)
        {
            return false;
        }

        if (door.Z + door.ItemData.Height <= m_Mobile.Z || m_Mobile.Z + 16 <= door.Z)
        {
            return false; 
        }

        if (door.X != x || door.Y != y)
        {
            return false;
        }

        return !door.Locked || !door.UseLocks();
    }

    private bool IsValidDestroyableItem(Item item) 
    {
        if (!m_Mobile.CanDestroyObstacles || !item.Movable || !item.ItemData.Impassable)
        {
            return false;
        }

        if (item.Z + item.ItemData.Height <= m_Mobile.Z || m_Mobile.Z + 16 <= item.Z)
        {
            return false;
        }
        
        return m_Mobile.InRange(item.GetWorldLocation(), 1);
    }

    private bool ProcessObstacles(PooledRefQueue<Item> queue, Direction d)
    {
        if (queue.Count == 0)
        {
            return true;
        }

        while (queue.Count > 0)
        {
            var item = queue.Dequeue();
            ProcessObstacle(item, queue);
        }

        return !m_Mobile.Move(d);
    }

    private void ProcessObstacle(Item item, PooledRefQueue<Item> queue)
    {
        if (item is BaseDoor door)
        {
            DebugSay("Opening the door.");
            
            door.Use(m_Mobile);
        }
        else
        {
            DebugSay($"Destroying item: {item.GetType().Name}");

            if (item is Container cont)
            {
                ProcessContainer(cont, queue);
                cont.Destroy();
            }
            else
            {
                item.Delete();
            }
        }
    }

    private void ProcessContainer(Container cont, PooledRefQueue<Item> queue)
    {
        for (var i = 0; i < cont.Items.Count; ++i)
        {
            var check = cont.Items[i];
            if (check.Movable && check.ItemData.Impassable && cont.Z + check.ItemData.Height > m_Mobile.Z)
            {
                queue.Enqueue(check);
            }
        }
    }

    public virtual void WalkRandom(int chanceToNotMove, int chanceToDir, int steps)
    {
        if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves || chanceToNotMove <= 0)
        {
            return;
        }

        for (var i = 0; i < steps; i++)
        {
            if (Utility.Random(1 + chanceToNotMove) == 0)
            {
                var direction = GetRandomDirection(chanceToDir);
                DoMove(direction);
            }
        }
    }

    public virtual void WalkRandomInHome(int iChanceToNotMove, int iChanceToDir, int iSteps)
    {
        if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves)
        {
            return;
        }

        if (m_Mobile.Home == Point3D.Zero)
        {
            HandleNoHome(iChanceToNotMove, iChanceToDir, iSteps);
        }
        else
        {
            HandleHomeMovement(iChanceToNotMove, iChanceToDir, iSteps);
        }
    }

    private void HandleNoHome(int iChanceToNotMove, int iChanceToDir, int iSteps)
    {
        if (m_Mobile.Spawner is RegionSpawner rs)
        {
            Region region = rs.SpawnRegion;

            if (m_Mobile.Region.AcceptsSpawnsFrom(region))
            {
                m_Mobile.WalkRegion = region;
                WalkRandom(iChanceToNotMove, iChanceToDir, iSteps);
                m_Mobile.WalkRegion = null;
            }
            else if (region.GoLocation != Point3D.Zero && Utility.RandomBool())
            {
                DoMove(m_Mobile.GetDirectionTo(region.GoLocation));
            }
            else
            {
                WalkRandom(iChanceToNotMove, iChanceToDir, 1);
            }
        }
        else
        {
            WalkRandom(iChanceToNotMove, iChanceToDir, iSteps);
        }
    }

    private void HandleHomeMovement(int iChanceToNotMove, int iChanceToDir, int iSteps)
    {
        if (m_Mobile.RangeHome == 0)
        {
            if (m_Mobile.Location != m_Mobile.Home)
            {
                DoMove(m_Mobile.GetDirectionTo(m_Mobile.Home));
            }
            return;
        }

        for (var i = 0; i < iSteps; i++)
        {
            var iCurrDist = (int)m_Mobile.GetDistanceToSqrt(m_Mobile.Home);

            if (iCurrDist > m_Mobile.RangeHome)
            {
                DoMove(m_Mobile.GetDirectionTo(m_Mobile.Home));
            }
            else if (iCurrDist < m_Mobile.RangeHome * 2 / 3 || Utility.Random(10) <= 5)
            {
                WalkRandom(iChanceToNotMove, iChanceToDir, 1);
            }
            else
            {
                DoMove(m_Mobile.GetDirectionTo(m_Mobile.Home));
            }
        }
    }

    public virtual bool CheckFlee()
    {
        if (!m_Mobile.CheckFlee())
        {
            return false;
        }
    
        if (m_Mobile.Combatant == null)
        {
            WalkRandomInHome(3, 2, 1);
        }
    
        return true;
    }

    public virtual void OnTeleported()
    {
        DebugSay("Teleported; recalculating path...");

        m_Path?.ForceRepath();
    }

    private static readonly Dictionary<BaseCreature, Point3D> _reservedPositions = new();
    private static long _lastGroupUpdateTime = 0;

    public virtual bool MoveTo(Mobile m, bool run, int range)
    {
        if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves || m?.Deleted != false)
        {
            return false;
        }

        if (m_Mobile.InRange(m, range))
        {
            m_Path = null;
            return true;
        }

        if (UseGroupMovement(m))
        {
            return MoveToWithGroup(m, run, range);
        }

        if (m_Path == null && m_Mobile.InLOS(m) && DoMove(m_Mobile.GetDirectionTo(m), true))
        {
            return true;
        }

        if (m_Path?.Goal != m)
        {
            m_Path = new PathFollower(m_Mobile, m) { Mover = DoMoveImpl };
        }

        if (m_Path.Follow(run, 1))
        {
            m_Path = null;
            return true;
        }

        return false;
    }

    private bool UseGroupMovement(Mobile target)
    {
        return m_Mobile.Combatant == target && 
            !m_Mobile.Controlled && GetNearbyAllies(target).Count > 0;
    }

    private bool MoveToWithGroup(Mobile target, bool run, int range)
    {
        var allies = GetNearbyAllies(target);
        var optimalPosition = CalculateOptimalPosition(target, allies, range);
    
        if (optimalPosition != Point3D.Zero)
        {
            _reservedPositions[m_Mobile] = optimalPosition;
        
            var direction = m_Mobile.GetDirectionTo(optimalPosition);
        
            if (Utility.RandomDouble() < 0.3)
            {
                direction = GetAdjustedDirection(direction);
            }
        
            return DoMove(direction, true);
        }
    
        return MoveToWithCollisionAvoidance(target, run, range);
    }

    private List<BaseCreature> GetNearbyAllies(Mobile target)
    {
        var allies = new List<BaseCreature>();
    
        foreach (var mobile in m_Mobile.GetMobilesInRange(8))
        {
            if (mobile is BaseCreature bc && 
                bc != m_Mobile && 
                bc.Combatant == target &&
                !bc.Controlled &&
                bc.Team == m_Mobile.Team)
            {
                allies.Add(bc);
            }
        }
    
        return allies;
    }

    private Point3D CalculateOptimalPosition(Mobile target, List<BaseCreature> allies, int range)
    {
        var targetLoc = target.Location;
        var positions = new List<Point3D>();
    
        for (var x = -range - 0; x <= range + 0; x++)
        {
            for (var y = -range - 0; y <= range + 0; y++)
            {
                var testLoc = new Point3D(targetLoc.X + x, targetLoc.Y + y, targetLoc.Z);
                var distance = m_Mobile.GetDistanceToSqrt(testLoc);
            
                if (distance >= range && distance <= range + 3)
                {
                    positions.Add(testLoc);
                }
            }
        }
    
        Point3D bestPosition = Point3D.Zero;
        var bestScore = double.MinValue;
    
        foreach (var pos in positions)
        {
            var score = ScorePosition(pos, target, allies);
            if (score > bestScore && CanMoveTo(pos))
            {
                bestScore = score;
                bestPosition = pos;
            }
        }
    
        return bestPosition;
    }
    
    private double ScorePosition(Point3D position, Mobile target, List<BaseCreature> allies)
    {
        var score = 0.0;
    
        var currentDistance = m_Mobile.GetDistanceToSqrt(position);
        score -= currentDistance * 2;
    
        foreach (var ally in allies)
        {
            var allyDistance = ally.GetDistanceToSqrt(position);
            if (allyDistance < 2)
            {
                score -= 50;
            }
            else if (allyDistance < 3)
            {
                score -= 20;
            }
        }
    
        foreach (var kvp in _reservedPositions)
        {
            if (kvp.Key != m_Mobile && GetDistanceToSqrt(kvp.Value, position) < 2)
            {
                score -= 30;
            }
        }
    
        if (m_Mobile.Map?.LineOfSight(position, target.Location) == true)
        {
            score += 10;
        }
    
        score += Utility.RandomDouble() * 5;
    
        return score;
    }

    private static double GetDistanceToSqrt(Point3D from, Point3D to)
    {
        var xDelta = from.X - to.X;
        var yDelta = from.Y - to.Y;
        return Math.Sqrt(xDelta * xDelta + yDelta * yDelta);
    }

    private bool CanMoveTo(Point3D location)
    {
        var map = m_Mobile.Map;
        return map?.CanFit(location.X, location.Y, location.Z, 16, false, false, true) == true;
    }

    private static Direction GetAdjustedDirection(Direction original)
    {
        var adjustment = Utility.Random(3) - 1; // -1, 0, or 1
        var newDir = (int)original + adjustment;
    
        if (newDir < 0)
        {
            newDir += 8;
        }
        else if (newDir >= 8)
        {
            newDir -= 8;
        }
        
        return (Direction)newDir;
    }

    private bool MoveToWithCollisionAvoidance(Mobile target, bool run, int range)
    {
        var direction = m_Mobile.GetDirectionTo(target);
    
        if (DoMove(direction, true))
        {
            return true;
        }
    
        for (var i = 1; i <= 3; i++)
        {
            var clockwise = (Direction)(((int)direction + i) % 8);
            if (DoMove(clockwise, true))
            {
                return true;
            }
        
            var counterclockwise = (Direction)(((int)direction - i + 8) % 8);
            if (DoMove(counterclockwise, true))
            {
                return true;
            }
        }
    
        if (m_Path?.Goal != target)
        {
            m_Path = new PathFollower(m_Mobile, target) { Mover = DoMoveImpl };
        }

        if (m_Path.Follow(run, 1))
        {
            m_Path = null;
            return true;
        }

        return false;
    }

    public virtual bool WalkMobileRange(Mobile m, int iSteps, bool run, int iWantDistMin, int iWantDistMax)
    {
        if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves || m == null)
        {
            return false;
        }

        for (var i = 0; i < iSteps; i++)
        {
            var iCurrDist = (int)m_Mobile.GetDistanceToSqrt(m);

            if (iCurrDist < iWantDistMin || iCurrDist > iWantDistMax)
            {
                if (!MoveTowardsOrAwayFrom(m, run, iCurrDist, iWantDistMax))
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        return m_Mobile.GetDistanceToSqrt(m) is var dist && dist >= iWantDistMin && dist <= iWantDistMax;
    }

    private bool MoveTowardsOrAwayFrom(Mobile m, bool run, int iCurrDist, int iWantDistMax)
    {
        var needCloser = iCurrDist > iWantDistMax;

        if (needCloser && m_Path?.Goal == m)
        {
            if (m_Path.Follow(run, 1))
            {
                m_Path = null;
                return true;
            }
        }
        else
        {
            var dirTo = needCloser ? m_Mobile.GetDirectionTo(m, run) : m.GetDirectionTo(m_Mobile, run);

            if (DoMove(dirTo, true))
            {
                m_Path = null;
                return true;
            }

            if (needCloser)
            {
                m_Path = new PathFollower(m_Mobile, m) { Mover = DoMoveImpl };
                if (m_Path.Follow(run, 1))
                {
                    m_Path = null;
                    return true;
                }
            }
        }

        return false;
    }

    public virtual bool AcquireFocusMob(int iRange, FightMode acqType, bool bPlayerOnly, bool bFacFriend, bool bFacFoe)
    {
        if (m_Mobile.Deleted != false || m_Mobile.Map == null || acqType == FightMode.None)
        {
            return false;
        }

        if (HandleBardProvoked() || HandleControlled() || HandleConstantFocus())
        {
            return true;
        }

        if (acqType == FightMode.None)
        {
            m_Mobile.FocusMob = null;
            return false;
        }

        if (HandleAggressor(acqType))
        {
            return false;
        }

        if (Core.TickCount - m_Mobile.NextReacquireTime < 0)
        {
            m_Mobile.FocusMob = null;
            return false;
        }

        m_Mobile.NextReacquireTime = Core.TickCount + (int)m_Mobile.ReacquireDelay.TotalMilliseconds;

        DebugSay("Acquiring new target...");

        if (m_Mobile.Map == null)
        {
            return m_Mobile.FocusMob != null;
        }

        return AcquireNewFocusMob(m_Mobile.Map, iRange, acqType, bPlayerOnly, bFacFriend, bFacFoe);
    }
    
    private bool HandleBardProvoked()
    {
        if (!m_Mobile.BardProvoked)
        {
            return false;
        }

        if (m_Mobile.BardTarget?.Deleted != false)
        {
            m_Mobile.FocusMob = null;
            return false;
        }

        m_Mobile.FocusMob = m_Mobile.BardTarget;
        return true;
    }
    
    private bool HandleControlled()
    {
        if (!m_Mobile.Controlled)
        {
            return false;
        }

        if (m_Mobile.ControlTarget?.Deleted != false || m_Mobile.ControlTarget?.Hidden == true ||
            m_Mobile.ControlTarget?.Alive != true || m_Mobile.ControlTarget?.IsDeadBondedPet == true ||
            !m_Mobile.InRange(m_Mobile.ControlTarget, m_Mobile.RangePerception * 2))
        {
            if (m_Mobile.ControlTarget != null && m_Mobile.ControlTarget != m_Mobile.ControlMaster)
            {
                m_Mobile.ControlTarget = null;
            }

            m_Mobile.FocusMob = null;
            return false;
        }

        m_Mobile.FocusMob = m_Mobile.ControlTarget;
        return true;
    }
    
    private bool HandleConstantFocus()
    {
        if (m_Mobile.ConstantFocus == null)
        {
            return false;
        }

        DebugSay($"Acquired focused target: {m_Mobile.ConstantFocus.Name}.");

        m_Mobile.FocusMob = m_Mobile.ConstantFocus;
        return true;
    }
    
    private bool HandleAggressor(FightMode acqType)
    {
        if (acqType != FightMode.Aggressor || 
            m_Mobile.Aggressors.Count > 0 || 
            m_Mobile.Aggressed.Count > 0 ||
            m_Mobile.FactionAllegiance != null || 
            m_Mobile.EthicAllegiance != null)
        {
            return false;
        }

        m_Mobile.FocusMob = null;
        return true;
    }
    
    private bool AcquireNewFocusMob(Map map, int iRange, FightMode acqType, bool bPlayerOnly, bool bFacFriend, bool bFacFoe)
    {
        Mobile newFocusMob = null;
        var val = double.MinValue;
        Mobile enemySummonMob = null;
        var enemySummonVal = double.MinValue;
    
        foreach (var m in map.GetMobilesInRange(m_Mobile.Location, iRange))
        {
            var isInvalidTarget = IsInvalidTarget(m, bPlayerOnly);
            if (isInvalidTarget)
            {
                continue;
            }
    
            var bc = m as BaseCreature;
            var pm = m as PlayerMobile;
    
            var isInvalidSummonTarget = IsInvalidSummonTarget(m, bc, pm);
            var isInvalidFactionTarget = IsInvalidFactionTarget(m, bFacFriend, bFacFoe);
            var isInvalidFightModeTarget = IsInvalidFightModeTarget(m, acqType, bc);
    
            if (isInvalidSummonTarget || isInvalidFactionTarget || isInvalidFightModeTarget)
            {
                continue;
            }
    
            var theirVal = m_Mobile.GetFightModeRanking(m, acqType, bPlayerOnly);
            if (theirVal > val && m_Mobile.InLOS(m))
            {
                newFocusMob = m;
                val = theirVal;
            }
            else if (Core.AOS && theirVal > enemySummonVal && m_Mobile.InLOS(m) && bc?.Summoned == true && bc?.Controlled != true)
            {
                enemySummonMob = m;
                enemySummonVal = theirVal;
            }
        }
    
        m_Mobile.FocusMob = newFocusMob ?? enemySummonMob;
        return m_Mobile.FocusMob != null;
    }
    
    private bool IsInvalidTarget(Mobile m, bool bPlayerOnly)
    {
        return m.Deleted || m.Blessed || m == m_Mobile || m is BaseFamiliar || !m.Alive || m.IsDeadBondedPet ||
               m.AccessLevel > AccessLevel.Player || (bPlayerOnly && !m.Player) || !m_Mobile.CanSee(m);
    }
    
    private bool IsInvalidSummonTarget(Mobile m, BaseCreature bc, PlayerMobile pm)
    {
        if (Core.AOS && bc?.Summoned == true && 
            (bc.SummonMaster == m_Mobile || (!bc.SummonMaster.Player && IsHostile(bc.SummonMaster))))
        {
            return true;
        }

        if (m_Mobile.Summoned && m_Mobile.SummonMaster != null)
        {
            return m == m_Mobile.SummonMaster || 
                   !SpellHelper.ValidIndirectTarget(m_Mobile.SummonMaster, m) ||
                   (pm != null && m_Mobile.IsAnimatedDead) || 
                   (m_Mobile.IsAnimatedDead && (bc?.IsAnimatedDead == true) || bc?.Controlled == true);
        }

        return false;
    }
    
    private bool IsInvalidFactionTarget(Mobile m, bool bFacFriend, bool bFacFoe)
    {
        if (bFacFriend && !m_Mobile.IsFriend(m))
        {
            return true;
        }

        if (TransformationSpellHelper.UnderTransformation(m, typeof(EtherealVoyageSpell)) ||
            (m_Mobile.Combatant != m && VirtueSystem.GetVirtues(m as PlayerMobile)?.HonorActive == true))
        {
            return true;
        }

        return bFacFoe && (!m_Mobile.IsEnemy(m) || (!bFacFriend && !m_Mobile.CanBeHarmful(m, false)));
    }
    
    private bool IsInvalidFightModeTarget(Mobile m, FightMode acqType, BaseCreature bc)
    {
        if (acqType is not (FightMode.Aggressor or FightMode.Evil))
        {
            return false;
        }

        var bValid = IsHostile(m) || 
                     m_Mobile.GetFactionAllegiance(m) == BaseCreature.Allegiance.Enemy ||
                     m_Mobile.GetEthicAllegiance(m) == BaseCreature.Allegiance.Enemy;

        if (!bValid)
        {
            bValid = acqType switch
            {
                FightMode.Evil => bc?.Controlled == true && bc?.ControlMaster != null 
                    ? bc.ControlMaster.Karma < 0 
                    : m.Karma < 0,
                _ => false
            };
        }

        return !bValid;
    }

    private bool IsHostile(Mobile from)
    {
        return m_Mobile.Combatant == from || 
               from.Combatant == m_Mobile || 
               IsAggressor(from) || 
               IsAggressed(from);
    }
    
    private bool IsAggressor(Mobile from)
    {
        foreach (var aggressor in m_Mobile.Aggressors)
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
        foreach (var aggressed in m_Mobile.Aggressed)
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
        if (m_Mobile.Deleted != false || m_Mobile.Map == null || !CanDetectHidden)
        {
            return;
        }
    
        DebugSay("Checking for hidden entities...");
    
        var srcSkill = m_Mobile.Skills.DetectHidden.Value;
        if (srcSkill <= 0)
        {
            return;
        }
    
        foreach (var trg in m_Mobile.GetMobilesInRange(m_Mobile.RangePerception))
        {
            if (IsValidTargetCombatTarget(trg))
            {
                TryDetectHidden(trg, srcSkill);
            }
        }
    }

    private bool IsValidTargetCombatTarget(Mobile trg)
    {
        return trg != m_Mobile &&
               trg.Player &&
               trg.Alive &&
               trg.Hidden &&
               trg.AccessLevel == AccessLevel.Player &&
               m_Mobile.InLOS(trg);
    }

    private void TryDetectHidden(Mobile trg, double srcSkill)
    {
        DebugSay($"Trying to detect: {trg.Name}");

        var trgHiding = trg.Skills.Hiding.Value / 2.9;
        var trgStealth = trg.Skills.Stealth.Value / 1.8;
        var chance = Math.Max(srcSkill / 10, srcSkill / 1.2 - Math.Min(trgHiding, trgStealth)) / 100;

        if (chance > Utility.RandomDouble())
        {
            trg.RevealingAction();
            trg.SendLocalizedMessage(500814); 
            // 500814: You have been revealed!
        }
    }

    public virtual void Deactivate()
    {
        if (!m_Mobile.PlayerRangeSensitive)
        {
            return;
        }

        m_Timer.Stop();

        var spawner = m_Mobile.Spawner;

        if (ShouldReturnToHome((Server.Engines.Spawners.Spawner)spawner))
        {
            Timer.StartTimer(ReturnToHome);
        }
    }

    private bool ShouldReturnToHome(Spawner spawner)
    {
        return spawner?.ReturnOnDeactivate == true && 
               !m_Mobile.Controlled && (
                   spawner.HomeLocation == Point3D.Zero || 
                   !m_Mobile.InRange(spawner.HomeLocation, spawner.HomeRange)
               );
    }

    private void ReturnToHome()
    {
        if (m_Mobile.Spawner is not { } spawner)
        {
            return;
        }

        var loc = spawner.GetSpawnPosition(m_Mobile, spawner.Map);
        
        if (loc != Point3D.Zero)
        {
            m_Mobile.MoveToWorld(loc, spawner.Map);
        }

        m_Timer.Start();
    }

    public virtual void Activate()
    {
        if (!m_Timer.Running)
        {
            m_Timer.Start();
        }
    }
    
    public virtual void OnCurrentSpeedChanged()
    {
        m_Timer.Interval = TimeSpan.FromMilliseconds(m_Mobile.CurrentSpeed * 1000);
    }

    public virtual int OnPoolTick()
    {
        if (m_Mobile.Deleted || m_Mobile.Map == null)
        {
            return 1000;
        }

        m_Mobile.OnThink();

        if (m_Mobile.Controlled ? !Obey() : !Think())
        {
            return 1000;
        }

        return (int)(m_Mobile.CurrentSpeed * 1000);
    }

    public virtual void Cleanup()
    {
        m_Timer?.Stop();
        m_Path = null;
    }
}
