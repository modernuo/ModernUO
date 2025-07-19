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

public abstract class BaseAI
{
    // How many milliseconds until our next move can we consider it ok to move without deferring/blocking.
    private const int FuzzyTimeUntilNextMove = 24;
    private static readonly SkillName[] _keywordTable =
    {
        SkillName.Parry,
        SkillName.Healing,
        SkillName.Hiding,
        SkillName.Stealing,
        SkillName.Alchemy,
        SkillName.AnimalLore,
        SkillName.ItemID,
        SkillName.ArmsLore,
        SkillName.Begging,
        SkillName.Blacksmith,
        SkillName.Fletching,
        SkillName.Peacemaking,
        SkillName.Camping,
        SkillName.Carpentry,
        SkillName.Cartography,
        SkillName.Cooking,
        SkillName.DetectHidden,
        SkillName.Discordance, // ??
        SkillName.EvalInt,
        SkillName.Fishing,
        SkillName.Provocation,
        SkillName.Lockpicking,
        SkillName.Magery,
        SkillName.MagicResist,
        SkillName.Tactics,
        SkillName.Snooping,
        SkillName.RemoveTrap,
        SkillName.Musicianship,
        SkillName.Poisoning,
        SkillName.Archery,
        SkillName.SpiritSpeak,
        SkillName.Tailoring,
        SkillName.AnimalTaming,
        SkillName.TasteID,
        SkillName.Tinkering,
        SkillName.Veterinary,
        SkillName.Forensics,
        SkillName.Herding,
        SkillName.Tracking,
        SkillName.Stealth,
        SkillName.Inscribe,
        SkillName.Swords,
        SkillName.Macing,
        SkillName.Fencing,
        SkillName.Wrestling,
        SkillName.Lumberjacking,
        SkillName.Mining,
        SkillName.Meditation
    };

    protected ActionType _action;

    public BaseCreature Mobile { get; }

    private long _nextDetectHidden;
    private long _nextStopGuard;

    protected PathFollower _path;
    public Timer _timer;

    public BaseAI(BaseCreature m)
    {
        Mobile = m;

        _timer = new AITimer(this);

        bool activate;

        if (!m.PlayerRangeSensitive)
        {
            activate = true;
        }
        else if (World.Loading)
        {
            activate = false;
        }
        else if (m.Map == null || m.Map == Map.Internal || !m.Map.GetSector(m.Location).Active)
        {
            activate = false;
        }
        else
        {
            activate = true;
        }

        if (activate)
        {
            _timer.Start();
        }

        Action = ActionType.Wander;
    }

    public ActionType Action
    {
        get => _action;
        set
        {
            _action = value;
            OnActionChanged();
        }
    }

    public long NextMove { get; set; }

    public virtual bool CanDetectHidden => Mobile.Skills.DetectHidden.Value > 0;

    public virtual bool WasNamed(string speech)
    {
        var name = Mobile.Name;

        return name != null && speech.InsensitiveStartsWith(name);
    }

    public virtual void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        if (!from.Alive || !Mobile.Controlled || !from.InRange(Mobile, 14))
        {
            return;
        }

        var isDeadPet = Mobile.IsDeadPet;

        if (from == Mobile.ControlMaster)
        {
            list.Add(new InternalEntry(6107, 14, OrderType.Guard, !isDeadPet));  // Command: Guard
            list.Add(new InternalEntry(6108, 14, OrderType.Follow, true)); // Command: Follow

            if (Mobile.CanDrop)
            {
                list.Add(new InternalEntry(6109, 14, OrderType.Drop, !isDeadPet)); // Command: Drop
            }

            list.Add(new InternalEntry(6111, 14, OrderType.Attack, !isDeadPet)); // Command: Kill

            list.Add(new InternalEntry(6112, 14, OrderType.Stop, true)); // Command: Stop
            list.Add(new InternalEntry(6114, 14, OrderType.Stay, true)); // Command: Stay

            if (!Mobile.Summoned && Mobile is not GrizzledMare)
            {
                list.Add(new InternalEntry(6110, 14, OrderType.Friend, true));         // Add Friend
                list.Add(new InternalEntry(6099, 14, OrderType.Unfriend, true));       // Remove Friend
                list.Add(new InternalEntry(6113, 14, OrderType.Transfer, !isDeadPet)); // Transfer
            }

            list.Add(new InternalEntry(6118, 14, OrderType.Release, true)); // Release
        }
        else if (Mobile.IsPetFriend(from))
        {
            list.Add(new InternalEntry(6108, 14, OrderType.Follow, true));     // Command: Follow
            list.Add(new InternalEntry(6112, 14, OrderType.Stop, !isDeadPet)); // Command: Stop
            list.Add(new InternalEntry(6114, 14, OrderType.Stay, true));       // Command: Stay
        }
    }

    public virtual void BeginPickTarget(Mobile from, OrderType order)
    {
        if (Mobile.Deleted || !Mobile.Controlled || !from.InRange(Mobile, 14) || from.Map != Mobile.Map)
        {
            return;
        }

        var isOwner = from == Mobile.ControlMaster;
        var isFriend = !isOwner && Mobile.IsPetFriend(from);

        if (!isOwner && !isFriend)
        {
            return;
        }

        if (isFriend && order != OrderType.Follow && order != OrderType.Stay && order != OrderType.Stop)
        {
            return;
        }

        if (from.Target == null)
        {
            if (order == OrderType.Transfer)
            {
                from.SendLocalizedMessage(502038); // Click on the person to transfer ownership to.
            }
            else if (order == OrderType.Friend)
            {
                from.SendLocalizedMessage(502020); // Click on the player whom you wish to make a co-owner.
            }
            else if (order == OrderType.Unfriend)
            {
                from.SendLocalizedMessage(1070948); // Click on the player whom you wish to remove as a co-owner.
            }

            from.Target = new AIControlMobileTarget(this, order);
        }
        else if (from.Target is AIControlMobileTarget t)
        {
            if (t.Order == order)
            {
                t.AddAI(this);
            }
        }
    }

    public virtual void OnAggressiveAction(Mobile aggressor)
    {
        var currentCombat = Mobile.Combatant;

        if (currentCombat != null && !aggressor.Hidden && currentCombat != aggressor &&
            Mobile.GetDistanceToSqrt(currentCombat) > Mobile.GetDistanceToSqrt(aggressor))
        {
            Mobile.Combatant = aggressor;
        }
    }

    public virtual void EndPickTarget(Mobile from, Mobile target, OrderType order)
    {
        if (Mobile.Deleted || !Mobile.Controlled || !from.InRange(Mobile, 14) || from.Map != Mobile.Map ||
            !from.CheckAlive())
        {
            return;
        }

        var isOwner = from == Mobile.ControlMaster;
        var isFriend = !isOwner && Mobile.IsPetFriend(from);

        if (!isOwner && !isFriend)
        {
            return;
        }

        if (isFriend && order != OrderType.Follow && order != OrderType.Stay && order != OrderType.Stop)
        {
            return;
        }

        if (order == OrderType.Attack)
        {
            if (target is BaseCreature creature && creature.IsScaryToPets && Mobile.IsScaredOfScaryThings)
            {
                Mobile.SayTo(from, "Your pet refuses to attack this creature!");
                return;
            }

            if (SolenHelper.CheckRedFriendship(from) &&
                target is RedSolenInfiltratorQueen or RedSolenInfiltratorWarrior or RedSolenQueen or RedSolenWarrior or RedSolenWorker
                || SolenHelper.CheckBlackFriendship(from) &&
                target is BlackSolenInfiltratorQueen or BlackSolenInfiltratorWarrior or BlackSolenQueen or BlackSolenWarrior or BlackSolenWorker)
            {
                from.SendAsciiMessage("You can not force your pet to attack a creature you are protected from.");
                return;
            }

            if (target is BaseFactionGuard)
            {
                Mobile.SayTo(from, "Your pet refuses to attack the guard.");
                return;
            }
        }

        if (Mobile.CheckControlChance(from))
        {
            Mobile.ControlTarget = target;
            Mobile.ControlOrder = order;
        }
    }

    public virtual bool HandlesOnSpeech(Mobile from)
    {
        if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            return true;
        }

        if (from.Alive && Mobile.Controlled && Mobile.Commandable &&
            (from == Mobile.ControlMaster || Mobile.IsPetFriend(from)))
        {
            return true;
        }

        return from.Alive && from.InRange(Mobile.Location, 3) && Mobile.IsHumanInTown();
    }

    public virtual void OnSpeech(SpeechEventArgs e)
    {
        if (e.Mobile.Alive && e.Mobile.InRange(Mobile.Location, 3) && Mobile.IsHumanInTown())
        {
            if (e.HasKeyword(0x9D) && WasNamed(e.Speech)) // *move*
            {
                if (Mobile.Combatant != null)
                {
                    // I am too busy fighting to deal with thee!
                    Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                }
                else
                {
                    // Excuse me?
                    Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501516);
                    WalkRandomInHome(2, 2, 1);
                }
            }
            else if (e.HasKeyword(0x9E) && WasNamed(e.Speech)) // *time*
            {
                if (Mobile.Combatant != null)
                {
                    // I am too busy fighting to deal with thee!
                    Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                }
                else
                {
                    Clock.GetTime(Mobile, out var generalNumber, out _);

                    Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, generalNumber);
                }
            }
            else if (e.HasKeyword(0x6C) && WasNamed(e.Speech)) // *train
            {
                if (Mobile.Combatant != null)
                {
                    // I am too busy fighting to deal with thee!
                    Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                }
                else
                {
                    var foundSomething = false;

                    var ourSkills = Mobile.Skills;
                    var theirSkills = e.Mobile.Skills;

                    for (var i = 0; i < ourSkills.Length && i < theirSkills.Length; ++i)
                    {
                        var skill = ourSkills[i];
                        var theirSkill = theirSkills[i];

                        if (skill?.Base >= 60.0 && Mobile.CheckTeach(skill.SkillName, e.Mobile))
                        {
                            var toTeach = skill.Base / 3.0;

                            if (toTeach > 42.0)
                            {
                                toTeach = 42.0;
                            }

                            if (toTeach > theirSkill.Base)
                            {
                                var number = 1043059 + i;

                                if (number > 1043107)
                                {
                                    continue;
                                }

                                if (!foundSomething)
                                {
                                    Mobile.Say(1043058); // I can train the following:
                                }

                                Mobile.Say(number);

                                foundSomething = true;
                            }
                        }
                    }

                    if (!foundSomething)
                    {
                        Mobile.Say(501505); // Alas, I cannot teach thee anything.
                    }
                }
            }
            else
            {
                var toTrain = (SkillName)(-1);

                for (var i = 0; toTrain == (SkillName)(-1) && i < e.Keywords.Length; ++i)
                {
                    var keyword = e.Keywords[i];

                    if (keyword == 0x154)
                    {
                        toTrain = SkillName.Anatomy;
                    }
                    else if (keyword >= 0x6D && keyword <= 0x9C)
                    {
                        var index = keyword - 0x6D;

                        if (index >= 0 && index < _keywordTable.Length)
                        {
                            toTrain = _keywordTable[index];
                        }
                    }
                }

                if (toTrain != (SkillName)(-1) && WasNamed(e.Speech))
                {
                    if (Mobile.Combatant != null)
                    {
                        // I am too busy fighting to deal with thee!
                        Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                    }
                    else
                    {
                        var skills = Mobile.Skills;
                        var skill = skills[toTrain];

                        if (skill == null || skill.Base < 60.0 || !Mobile.CheckTeach(toTrain, e.Mobile))
                        {
                            Mobile.Say(501507); // 'Tis not something I can teach thee of.
                        }
                        else
                        {
                            Mobile.Teach(toTrain, e.Mobile, 0, false);
                        }
                    }
                }
            }
        }

        if (Mobile.Controlled && Mobile.Commandable)
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("Listening...");
            }

            var isOwner = e.Mobile == Mobile.ControlMaster;
            var isFriend = !isOwner && Mobile.IsPetFriend(e.Mobile);

            if (e.Mobile.Alive && (isOwner || isFriend))
            {
                if (Mobile.Debug)
                {
                    Mobile.DebugSay("It's from my master");
                }

                var keywords = e.Keywords;
                var speech = e.Speech;

                // First, check the all*
                for (var i = 0; i < keywords.Length; ++i)
                {
                    var keyword = keywords[i];

                    switch (keyword)
                    {
                        case 0x164: // all come
                            {
                                if (!isOwner)
                                {
                                    break;
                                }

                                if (Mobile.CheckControlChance(e.Mobile))
                                {
                                    Mobile.ControlTarget = null;
                                    Mobile.ControlOrder = OrderType.Come;
                                }

                                return;
                            }
                        case 0x165: // all follow
                            {
                                BeginPickTarget(e.Mobile, OrderType.Follow);
                                return;
                            }
                        case 0x166: // all guard
                        case 0x16B: // all guard me
                            {
                                if (!isOwner)
                                {
                                    break;
                                }

                                if (Mobile.CheckControlChance(e.Mobile))
                                {
                                    Mobile.ControlTarget = null;
                                    Mobile.ControlOrder = OrderType.Guard;
                                }

                                return;
                            }
                        case 0x167: // all stop
                            {
                                if (Mobile.CheckControlChance(e.Mobile))
                                {
                                    Mobile.ControlTarget = null;
                                    Mobile.ControlOrder = OrderType.Stop;
                                }

                                return;
                            }
                        case 0x168: // all kill
                        case 0x169: // all attack
                            {
                                if (!isOwner)
                                {
                                    break;
                                }

                                BeginPickTarget(e.Mobile, OrderType.Attack);
                                return;
                            }
                        case 0x16C: // all follow me
                            {
                                if (Mobile.CheckControlChance(e.Mobile))
                                {
                                    Mobile.ControlTarget = e.Mobile;
                                    Mobile.ControlOrder = OrderType.Follow;
                                }

                                return;
                            }
                        case 0x170: // all stay
                            {
                                if (Mobile.CheckControlChance(e.Mobile))
                                {
                                    Mobile.ControlTarget = null;
                                    Mobile.ControlOrder = OrderType.Stay;
                                }

                                return;
                            }
                    }
                }

                // No all*, so check *command
                for (var i = 0; i < keywords.Length; ++i)
                {
                    var keyword = keywords[i];

                    switch (keyword)
                    {
                        case 0x155: // *come
                            {
                                if (!isOwner)
                                {
                                    break;
                                }

                                if (WasNamed(speech) && Mobile.CheckControlChance(e.Mobile))
                                {
                                    Mobile.ControlTarget = null;
                                    Mobile.ControlOrder = OrderType.Come;
                                }

                                return;
                            }
                        case 0x156: // *drop
                            {
                                if (!isOwner)
                                {
                                    break;
                                }

                                if (!Mobile.IsDeadPet && !Mobile.Summoned && WasNamed(speech) &&
                                    Mobile.CheckControlChance(e.Mobile))
                                {
                                    Mobile.ControlTarget = null;
                                    Mobile.ControlOrder = OrderType.Drop;
                                }

                                return;
                            }
                        case 0x15A: // *follow
                            {
                                if (WasNamed(speech) && Mobile.CheckControlChance(e.Mobile))
                                {
                                    BeginPickTarget(e.Mobile, OrderType.Follow);
                                }

                                return;
                            }
                        case 0x15B: // *friend
                            {
                                if (!isOwner)
                                {
                                    break;
                                }

                                if (WasNamed(speech) && Mobile.CheckControlChance(e.Mobile))
                                {
                                    if (Mobile.Summoned || Mobile is GrizzledMare)
                                    {
                                        // Summoned creatures are loyal only to their summoners.
                                        e.Mobile.SendLocalizedMessage(1005481);
                                    }
                                    else if (e.Mobile.HasTrade)
                                    {
                                        // You cannot friend a pet with a trade pending
                                        e.Mobile.SendLocalizedMessage(1070947);
                                    }
                                    else
                                    {
                                        BeginPickTarget(e.Mobile, OrderType.Friend);
                                    }
                                }

                                return;
                            }
                        case 0x15C: // *guard
                            {
                                if (!isOwner)
                                {
                                    break;
                                }

                                if (!Mobile.IsDeadPet && WasNamed(speech) && Mobile.CheckControlChance(e.Mobile))
                                {
                                    Mobile.ControlTarget = null;
                                    Mobile.ControlOrder = OrderType.Guard;
                                }

                                return;
                            }
                        case 0x15D: // *kill
                        case 0x15E: // *attack
                            {
                                if (!isOwner)
                                {
                                    break;
                                }

                                if (!Mobile.IsDeadPet && WasNamed(speech) && Mobile.CheckControlChance(e.Mobile))
                                {
                                    BeginPickTarget(e.Mobile, OrderType.Attack);
                                }

                                return;
                            }
                        case 0x15F: // *patrol
                            {
                                if (!isOwner)
                                {
                                    break;
                                }

                                if (WasNamed(speech) && Mobile.CheckControlChance(e.Mobile))
                                {
                                    Mobile.ControlTarget = null;
                                    Mobile.ControlOrder = OrderType.Patrol;
                                }

                                return;
                            }
                        case 0x161: // *stop
                            {
                                if (WasNamed(speech) && Mobile.CheckControlChance(e.Mobile))
                                {
                                    Mobile.ControlTarget = null;
                                    Mobile.ControlOrder = OrderType.Stop;
                                }

                                return;
                            }
                        case 0x163: // *follow me
                            {
                                if (WasNamed(speech) && Mobile.CheckControlChance(e.Mobile))
                                {
                                    Mobile.ControlTarget = e.Mobile;
                                    Mobile.ControlOrder = OrderType.Follow;
                                }

                                return;
                            }
                        case 0x16D: // *release
                            {
                                if (!isOwner)
                                {
                                    break;
                                }

                                if (WasNamed(speech) && Mobile.CheckControlChance(e.Mobile))
                                {
                                    if (!Mobile.Summoned)
                                    {
                                        e.Mobile.SendGump(new ConfirmReleaseGump(e.Mobile, Mobile));
                                    }
                                    else
                                    {
                                        Mobile.ControlTarget = null;
                                        Mobile.ControlOrder = OrderType.Release;
                                    }
                                }

                                return;
                            }
                        case 0x16E: // *transfer
                            {
                                if (!isOwner)
                                {
                                    break;
                                }

                                if (!Mobile.IsDeadPet && WasNamed(speech) && Mobile.CheckControlChance(e.Mobile))
                                {
                                    if (Mobile.Summoned || Mobile is GrizzledMare)
                                    {
                                        // You cannot transfer ownership of a summoned creature.
                                        e.Mobile.SendLocalizedMessage(1005487);
                                    }
                                    else if (e.Mobile.HasTrade)
                                    {
                                        // You cannot transfer a pet with a trade pending
                                        e.Mobile.SendLocalizedMessage(1010507);
                                    }
                                    else
                                    {
                                        BeginPickTarget(e.Mobile, OrderType.Transfer);
                                    }
                                }

                                return;
                            }
                        case 0x16F: // *stay
                            {
                                if (WasNamed(speech) && Mobile.CheckControlChance(e.Mobile))
                                {
                                    Mobile.ControlTarget = null;
                                    Mobile.ControlOrder = OrderType.Stay;
                                }

                                return;
                            }
                    }
                }
            }
        }
        else
        {
            if (e.Mobile.AccessLevel >= AccessLevel.GameMaster)
            {
                if (Mobile.Debug)
                {
                    Mobile.DebugSay("It's from a GM");
                }

                if (Mobile.FindMyName(e.Speech, true))
                {
                    var str = e.Speech.Split(' ');
                    int i;

                    for (i = 0; i < str.Length; i++)
                    {
                        var word = str[i];

                        if (word.InsensitiveEquals("obey"))
                        {
                            Mobile.SetControlMaster(e.Mobile);

                            if (Mobile.Summoned)
                            {
                                Mobile.SummonMaster = e.Mobile;
                            }

                            return;
                        }
                    }
                }
            }
        }
    }

    public virtual bool Think()
    {
        if (Mobile.Deleted)
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

            default:
                {
                    return false;
                }
        }
    }

    public virtual void OnActionChanged()
    {
        switch (Action)
        {
            case ActionType.Wander:
                {
                    Mobile.Warmode = false;
                    Mobile.Combatant = null;
                    Mobile.FocusMob = null;
                    Mobile.SetCurrentSpeedToPassive();
                    break;
                }

            case ActionType.Combat:
                {
                    Mobile.Warmode = true;
                    Mobile.FocusMob = null;
                    Mobile.SetCurrentSpeedToActive();
                    break;
                }

            case ActionType.Guard:
                {
                    Mobile.Warmode = true;
                    Mobile.FocusMob = null;
                    Mobile.Combatant = null;
                    _nextStopGuard = Core.TickCount + (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
                    Mobile.SetCurrentSpeedToActive();
                    break;
                }

            case ActionType.Flee:
                {
                    Mobile.Warmode = true;
                    Mobile.FocusMob = null;
                    Mobile.SetCurrentSpeedToActive();
                    break;
                }

            case ActionType.Interact:
                {
                    Mobile.Warmode = false;
                    Mobile.SetCurrentSpeedToPassive();
                    break;
                }

            case ActionType.Backoff:
                {
                    Mobile.Warmode = false;
                    Mobile.SetCurrentSpeedToPassive();
                    break;
                }
        }
    }

    public virtual bool OnAtWayPoint() => true;

    public virtual bool DoActionWander()
    {
        if (CheckHerding())
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("Praise the shepherd!");
            }
        }
        else if (Mobile.CurrentWayPoint != null)
        {
            var point = Mobile.CurrentWayPoint;
            if ((point.X != Mobile.Location.X || point.Y != Mobile.Location.Y) && point.Map == Mobile.Map &&
                point.Parent == null && !point.Deleted)
            {
                if (Mobile.Debug)
                {
                    Mobile.DebugSay("I will move towards my waypoint.");
                }

                DoMove(Mobile.GetDirectionTo(Mobile.CurrentWayPoint));
            }
            else if (OnAtWayPoint())
            {
                if (Mobile.Debug)
                {
                    Mobile.DebugSay("I will go to the next waypoint");
                }

                Mobile.CurrentWayPoint = point.NextPoint;
                if (point.NextPoint?.Deleted == true)
                {
                    Mobile.CurrentWayPoint = point.NextPoint = point.NextPoint.NextPoint;
                }
            }
        }
        else if (Mobile.IsAnimatedDead)
        {
            // animated dead follow their master
            var master = Mobile.SummonMaster;

            if (master != null && master.Map == Mobile.Map && master.InRange(Mobile, Mobile.RangePerception))
            {
                MoveTo(master, false, 1);
            }
            else
            {
                WalkRandomInHome(2, 2, 1);
            }
        }
        else if (CheckMove() && CanMoveNow(out _) && !Mobile.CheckIdle())
        {
            WalkRandomInHome(2, 2, 1);
        }

        if (Mobile.Combatant?.Deleted == false && Mobile.Combatant.Alive &&
            !Mobile.Combatant.IsDeadBondedPet)
        {
            Mobile.Direction = Mobile.GetDirectionTo(Mobile.Combatant);
        }

        return true;
    }

    public virtual bool DoActionCombat()
    {
        if (Core.AOS && CheckHerding())
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("Praise the shepherd!");
            }

            return true;
        }

        var combatant = Mobile.Combatant;

        if (combatant == null || combatant.Deleted || combatant.Map != Mobile.Map || !combatant.Alive ||
            combatant.IsDeadBondedPet)
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("My combatant is gone!");
            }

            Action = ActionType.Wander;
            return true;
        }

        Mobile.Direction = Mobile.GetDirectionTo(combatant);
        if (Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, combatant))
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay($"I used my abilities on {combatant.Name}!");
            }
        }

        return true;
    }

    public virtual bool DoActionGuard()
    {
        if (Core.AOS && CheckHerding())
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("Praise the shepherd!");
            }
        }
        else if (Core.TickCount - _nextStopGuard < 0)
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("I am on guard");
            }
            // m_Mobile.Turn( Utility.Random(0, 2) - 1 );
        }
        else
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("I stopped being on guard");
            }

            Action = ActionType.Wander;
        }

        return true;
    }

    public virtual bool DoActionFlee()
    {
        var from = Mobile.FocusMob;

        if (from?.Deleted != false || from.Map != Mobile.Map)
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("I have lost him");
            }

            Action = ActionType.Guard;
            return true;
        }

        if (WalkMobileRange(from, 1, true, Mobile.RangePerception * 2, Mobile.RangePerception * 3))
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("I have fled");
            }

            Action = ActionType.Guard;
            return true;
        }

        if (Mobile.Debug)
        {
            Mobile.DebugSay("I am fleeing!");
        }

        return true;
    }

    public virtual bool DoActionInteract() => true;

    public virtual bool DoActionBackoff() => true;

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
            OrderType.Patrol   => DoOrderPatrol(),
            OrderType.Release  => DoOrderRelease(),
            OrderType.Stay     => DoOrderStay(),
            OrderType.Stop     => DoOrderStop(),
            OrderType.Follow   => DoOrderFollow(),
            OrderType.Transfer => DoOrderTransfer(),
            _                  => false
        };

    public virtual void OnCurrentOrderChanged()
    {
        if (Mobile.Deleted || Mobile.ControlMaster?.Deleted != false)
        {
            return;
        }

        switch (Mobile.ControlOrder)
        {
            case OrderType.None:
                {
                    Mobile.ControlMaster.RevealingAction();
                    Mobile.Home = Mobile.Location;
                    Mobile.SetCurrentSpeedToPassive();
                    Mobile.PlaySound(Mobile.GetIdleSound());
                    Mobile.Warmode = false;
                    Mobile.Combatant = null;
                    break;
                }

            case OrderType.Come:
                {
                    Mobile.ControlMaster.RevealingAction();
                    Mobile.SetCurrentSpeedToActive();
                    Mobile.PlaySound(Mobile.GetIdleSound());
                    Mobile.Warmode = false;
                    Mobile.Combatant = null;
                    break;
                }

            case OrderType.Drop:
                {
                    Mobile.ControlMaster.RevealingAction();
                    Mobile.SetCurrentSpeedToPassive();
                    Mobile.PlaySound(Mobile.GetIdleSound());
                    Mobile.Warmode = false;
                    Mobile.Combatant = null;
                    break;
                }

            case OrderType.Friend:
            case OrderType.Unfriend:
                {
                    Mobile.ControlMaster.RevealingAction();
                    break;
                }

            case OrderType.Guard:
                {
                    Mobile.ControlMaster.RevealingAction();
                    Mobile.SetCurrentSpeedToActive();
                    Mobile.PlaySound(Mobile.GetIdleSound());
                    Mobile.Warmode = true;
                    Mobile.Combatant = null;
                    Mobile.ControlMaster.SendLocalizedMessage(1049671, Mobile.Name); // ~1_PETNAME~ is now guarding you.
                    break;
                }

            case OrderType.Attack:
                {
                    Mobile.ControlMaster.RevealingAction();
                    Mobile.SetCurrentSpeedToActive();
                    Mobile.PlaySound(Mobile.GetIdleSound());

                    Mobile.Warmode = true;
                    Mobile.Combatant = null;
                    break;
                }

            case OrderType.Patrol:
                {
                    Mobile.ControlMaster.RevealingAction();
                    Mobile.SetCurrentSpeedToActive();
                    Mobile.PlaySound(Mobile.GetIdleSound());
                    Mobile.Warmode = false;
                    Mobile.Combatant = null;
                    break;
                }

            case OrderType.Release:
                {
                    Mobile.ControlMaster.RevealingAction();
                    Mobile.SetCurrentSpeedToPassive();
                    Mobile.PlaySound(Mobile.GetIdleSound());
                    Mobile.Warmode = false;
                    Mobile.Combatant = null;
                    break;
                }

            case OrderType.Stay:
                {
                    Mobile.ControlMaster.RevealingAction();
                    Mobile.SetCurrentSpeedToPassive();
                    Mobile.PlaySound(Mobile.GetIdleSound());
                    Mobile.Warmode = false;
                    Mobile.Combatant = null;
                    break;
                }

            case OrderType.Stop:
                {
                    Mobile.ControlMaster.RevealingAction();
                    Mobile.Home = Mobile.Location;
                    Mobile.SetCurrentSpeedToPassive();
                    Mobile.PlaySound(Mobile.GetIdleSound());
                    Mobile.Warmode = false;
                    Mobile.Combatant = null;
                    break;
                }

            case OrderType.Follow:
                {
                    Mobile.ControlMaster.RevealingAction();
                    Mobile.SetCurrentSpeedToActive();
                    Mobile.PlaySound(Mobile.GetIdleSound());

                    Mobile.Warmode = false;
                    Mobile.Combatant = null;
                    break;
                }

            case OrderType.Transfer:
                {
                    Mobile.ControlMaster.RevealingAction();
                    Mobile.SetCurrentSpeedToPassive();
                    Mobile.PlaySound(Mobile.GetIdleSound());

                    Mobile.Warmode = false;
                    Mobile.Combatant = null;
                    break;
                }
        }
    }

    public virtual bool DoOrderNone()
    {
        if (Mobile.Debug)
        {
            Mobile.DebugSay("I have no order");
        }

        WalkRandomInHome(3, 2, 1);

        if (Mobile.Combatant?.Deleted == false && Mobile.Combatant.Alive &&
            !Mobile.Combatant.IsDeadBondedPet)
        {
            Mobile.Warmode = true;
            Mobile.Direction = Mobile.GetDirectionTo(Mobile.Combatant);
        }
        else
        {
            Mobile.Warmode = false;
        }

        return true;
    }

    public virtual bool DoOrderCome()
    {
        if (Mobile.ControlMaster?.Deleted != false)
        {
            return true;
        }

        var iCurrDist = (int)Mobile.GetDistanceToSqrt(Mobile.ControlMaster);

        if (iCurrDist > Mobile.RangePerception)
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("I have lost my master. I stay here");
            }

            Mobile.ControlTarget = null;
            Mobile.ControlOrder = OrderType.None;
        }
        else
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("My master told me come");
            }

            // Not exactly OSI style, but better than nothing.
            var bRun = iCurrDist > 5;

            if (WalkMobileRange(Mobile.ControlMaster, 1, bRun, 0, 1))
            {
                if (Mobile.Combatant?.Deleted == false && Mobile.Combatant.Alive &&
                    !Mobile.Combatant.IsDeadBondedPet)
                {
                    Mobile.Warmode = true;
                    // m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.Combatant, bRun);
                }
                else
                {
                    Mobile.Warmode = false;
                }
            }
        }

        return true;
    }

    public virtual bool DoOrderDrop()
    {
        if (Mobile.IsDeadPet || !Mobile.CanDrop)
        {
            return true;
        }

        if (Mobile.Debug)
        {
            Mobile.DebugSay("I drop my stuff for my master");
        }

        var pack = Mobile.Backpack;

        if (pack != null)
        {
            var list = pack.Items;

            for (var i = list.Count - 1; i >= 0; --i)
            {
                if (i < list.Count)
                {
                    list[i].MoveToWorld(Mobile.Location, Mobile.Map);
                }
            }
        }

        Mobile.ControlTarget = null;
        Mobile.ControlOrder = OrderType.None;

        return true;
    }

    public virtual bool CheckHerding()
    {
        var target = Mobile.TargetLocation;

        if (target == null)
        {
            return false; // Creature is not being herded
        }

        var distance = Mobile.GetDistanceToSqrt(target);

        if (distance is not (< 1 or > 15))
        {
            DoMove(Mobile.GetDirectionTo(target));
            return true;
        }

        if (distance < 1 && target.X == 1076 && target.Y == 450 && Mobile is HordeMinionFamiliar)
        {
            if (Mobile.ControlMaster is PlayerMobile pm)
            {
                var qs = pm.Quest;

                if (qs is DarkTidesQuest)
                {
                    var obj = qs.FindObjective<FetchAbraxusScrollObjective>();

                    if (obj?.Completed == false)
                    {
                        Mobile.AddToBackpack(new ScrollOfAbraxus());
                        obj.Complete();
                    }
                }
            }
        }

        Mobile.TargetLocation = null;
        return false; // At the target or too far away
    }

    public virtual bool DoOrderFollow()
    {
        if (CheckHerding())
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("Praise the shepherd!");
            }
        }
        else if (Mobile.ControlTarget?.Deleted == false && Mobile.ControlTarget != Mobile)
        {
            var iCurrDist = (int)Mobile.GetDistanceToSqrt(Mobile.ControlTarget);

            if (iCurrDist > Mobile.RangePerception)
            {
                if (Mobile.Debug)
                {
                    Mobile.DebugSay("I have lost the one to follow. I stay here");
                }

                if (Mobile.Combatant?.Deleted == false && Mobile.Combatant.Alive &&
                    !Mobile.Combatant.IsDeadBondedPet)
                {
                    Mobile.Warmode = true;
                    Mobile.Direction = Mobile.GetDirectionTo(Mobile.Combatant);
                }
                else
                {
                    Mobile.Warmode = false;
                }
            }
            else
            {
                if (Mobile.Debug)
                {
                    Mobile.DebugSay($"My master told me to follow: {Mobile.ControlTarget.Name}");
                }

                // Not exactly OSI style, but better than nothing.
                var bRun = iCurrDist > 5;

                if (WalkMobileRange(Mobile.ControlTarget, 1, bRun, 0, 1))
                {
                    if (Mobile.Combatant?.Deleted == false && Mobile.Combatant.Alive &&
                        !Mobile.Combatant.IsDeadBondedPet)
                    {
                        Mobile.Warmode = true;
                        // m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.Combatant, bRun);
                    }
                    else
                    {
                        Mobile.Warmode = false;
                        if (Core.AOS)
                        {
                            Mobile.CurrentSpeed = 0.1;
                        }
                    }
                }
            }
        }
        else
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("I have nobody to follow");
            }

            Mobile.ControlTarget = null;
            Mobile.ControlOrder = OrderType.None;
        }

        return true;
    }

    public virtual bool DoOrderFriend()
    {
        var from = Mobile.ControlMaster;
        var to = Mobile.ControlTarget;

        if (from?.Deleted != false || to?.Deleted != false || from == to || !to.Player)
        {
            Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039); // *looks confused*
        }
        else
        {
            var youngFrom = from is PlayerMobile mobile && mobile.Young;
            var youngTo = to is PlayerMobile playerMobile && playerMobile.Young;

            if (youngFrom && !youngTo)
            {
                from.SendLocalizedMessage(502040); // As a young player, you may not friend pets to older players.
            }
            else if (!youngFrom && youngTo)
            {
                from.SendLocalizedMessage(502041); // As an older player, you may not friend pets to young players.
            }
            else if (from.CanBeBeneficial(to, true))
            {
                NetState fromState = from.NetState, toState = to.NetState;

                if (fromState != null && toState != null)
                {
                    if (from.HasTrade)
                    {
                        from.SendLocalizedMessage(1070947); // You cannot friend a pet with a trade pending
                    }
                    else if (to.HasTrade)
                    {
                        to.SendLocalizedMessage(1070947); // You cannot friend a pet with a trade pending
                    }
                    else if (Mobile.IsPetFriend(to))
                    {
                        from.SendLocalizedMessage(1049691); // That person is already a friend.
                    }
                    else if (!Mobile.AllowNewPetFriend)
                    {
                        from.SendLocalizedMessage(
                            1005482
                        ); // Your pet does not seem to be interested in making new friends right now.
                    }
                    else
                    {
                        // ~1_NAME~ will now accept movement commands from ~2_NAME~.
                        from.SendLocalizedMessage(1049676, $"{Mobile.Name}\t{to.Name}");

                        /* ~1_NAME~ has granted you the ability to give orders to their pet ~2_PET_NAME~.
                         * This creature will now consider you as a friend.
                         */
                        to.SendLocalizedMessage(1043246, $"{from.Name}\t{Mobile.Name}");

                        Mobile.AddPetFriend(to);

                        Mobile.ControlTarget = to;
                        Mobile.ControlOrder = OrderType.Follow;

                        return true;
                    }
                }
            }
        }

        Mobile.ControlTarget = from;
        Mobile.ControlOrder = OrderType.Follow;

        return true;
    }

    public virtual bool DoOrderUnfriend()
    {
        var from = Mobile.ControlMaster;
        var to = Mobile.ControlTarget;

        if (from?.Deleted != false || to?.Deleted != false || from == to || !to.Player)
        {
            Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039); // *looks confused*
        }
        else if (!Mobile.IsPetFriend(to))
        {
            from.SendLocalizedMessage(1070953); // That person is not a friend.
        }
        else
        {
            // ~1_NAME~ will no longer accept movement commands from ~2_NAME~.
            from.SendLocalizedMessage(1070951, $"{Mobile.Name}\t{to.Name}");

            /* ~1_NAME~ has no longer granted you the ability to give orders to their pet ~2_PET_NAME~.
             * This creature will no longer consider you as a friend.
             */
            to.SendLocalizedMessage(1070952, $"{from.Name}\t{Mobile.Name}");

            Mobile.RemovePetFriend(to);
        }

        Mobile.ControlTarget = from;
        Mobile.ControlOrder = OrderType.Follow;

        return true;
    }

    public virtual bool DoOrderGuard()
    {
        if (Mobile.IsDeadPet)
        {
            return true;
        }

        var controlMaster = Mobile.ControlMaster;

        if (controlMaster?.Deleted != false)
        {
            return true;
        }

        var combatant = Mobile.Combatant;

        var aggressors = controlMaster.Aggressors;

        if (aggressors.Count > 0)
        {
            for (var i = 0; i < aggressors.Count; ++i)
            {
                var info = aggressors[i];
                var attacker = info.Attacker;

                if (attacker?.Deleted == false &&
                    attacker.GetDistanceToSqrt(Mobile) <= Mobile.RangePerception)
                {
                    if (combatant == null || attacker.GetDistanceToSqrt(controlMaster) <
                        combatant.GetDistanceToSqrt(controlMaster))
                    {
                        combatant = attacker;
                    }
                }
            }

            if (combatant != null && Mobile.Debug)
            {
                Mobile.DebugSay("Crap, my master has been attacked! I will attack one of those bastards!");
            }
        }

        if (combatant?.Deleted == false && combatant != Mobile && combatant != Mobile.ControlMaster &&
            combatant.Alive && !combatant.IsDeadBondedPet && Mobile.CanSee(combatant) &&
            Mobile.CanBeHarmful(combatant, false) && combatant.Map == Mobile.Map)
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("Guarding from target...");
            }

            Mobile.Combatant = combatant;
            Mobile.FocusMob = combatant;
            Action = ActionType.Combat;

            /*
             * We need to call Think() here or spell casting monsters will not use
             * spells when guarding because their target is never processed.
             */
            Think();
        }
        else
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("Nothing to guard from");
            }

            Mobile.Warmode = false;
            if (Core.AOS)
            {
                Mobile.CurrentSpeed = 0.1;
            }

            WalkMobileRange(controlMaster, 1, false, 0, 1);
        }

        return true;
    }

    public virtual bool DoOrderAttack()
    {
        if (Mobile.IsDeadPet)
        {
            return true;
        }

        if (Mobile.ControlTarget?.Deleted != false || Mobile.ControlTarget.Map != Mobile.Map ||
            !Mobile.ControlTarget.Alive || Mobile.ControlTarget.IsDeadBondedPet)
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay(
                    "I think he might be dead. He's not anywhere around here at least. That's cool. I'm glad he's dead."
                );
            }

            if (Core.AOS)
            {
                Mobile.ControlTarget = Mobile.ControlMaster;
                Mobile.ControlOrder = OrderType.Follow;
            }
            else
            {
                Mobile.ControlTarget = null;
                Mobile.ControlOrder = OrderType.None;
            }

            if (Mobile.FightMode is FightMode.Closest or FightMode.Aggressor)
            {
                Mobile newCombatant = null;
                var newScore = 0.0;

                foreach (var aggr in Mobile.GetMobilesInRange(Mobile.RangePerception))
                {
                    if (!Mobile.CanSee(aggr) || aggr.Combatant != Mobile)
                    {
                        continue;
                    }

                    if (aggr.IsDeadBondedPet || !aggr.Alive)
                    {
                        continue;
                    }

                    var aggrScore = Mobile.GetFightModeRanking(aggr, FightMode.Closest, false);

                    if ((newCombatant == null || aggrScore > newScore) && Mobile.InLOS(aggr))
                    {
                        newCombatant = aggr;
                        newScore = aggrScore;
                    }
                }

                if (newCombatant != null)
                {
                    Mobile.ControlTarget = newCombatant;
                    Mobile.ControlOrder = OrderType.Attack;
                    Mobile.Combatant = newCombatant;
                    if (Mobile.Debug)
                    {
                        Mobile.DebugSay("But -that- is not dead. Here we go again...");
                    }

                    Think();
                }
            }
        }
        else
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("Attacking target...");
            }

            Think();
        }

        return true;
    }

    public virtual bool DoOrderPatrol()
    {
        if (Mobile.Debug)
        {
            Mobile.DebugSay("This order is not yet coded");
        }

        return true;
    }

    public virtual bool DoOrderRelease()
    {
        if (Mobile.Debug)
        {
            Mobile.DebugSay("I have been released");
        }

        Mobile.PlaySound(Mobile.GetAngerSound());

        Mobile.SetControlMaster(null);
        Mobile.SummonMaster = null;

        Mobile.BondingBegin = DateTime.MinValue;
        Mobile.OwnerAbandonTime = DateTime.MinValue;
        Mobile.IsBonded = false;

        var spawner = Mobile.Spawner;

        if (spawner != null && spawner.HomeLocation != Point3D.Zero)
        {
            Mobile.Home = spawner.HomeLocation;
            Mobile.RangeHome = spawner.HomeRange;
        }

        if (Mobile.DeleteOnRelease || Mobile.IsDeadPet)
        {
            Mobile.Delete();
        }

        Mobile.BeginDeleteTimer();

        if (Mobile.CanDrop)
        {
            Mobile.DropBackpack();
        }

        return true;
    }

    public virtual bool DoOrderStay()
    {
        if (Mobile.Debug)
        {
            if (CheckHerding())
            {
                Mobile.DebugSay("Praise the shepherd!");
            }
            else
            {
                Mobile.DebugSay("My master told me to stay");
            }
        }

        return true;
    }

    public virtual bool DoOrderStop()
    {
        if (Mobile.ControlMaster?.Deleted != false)
        {
            return true;
        }

        if (Mobile.Debug)
        {
            Mobile.DebugSay("My master told me to stop.");
        }

        Mobile.Direction = Mobile.GetDirectionTo(Mobile.ControlMaster);
        Mobile.Home = Mobile.Location;

        Mobile.ControlTarget = null;

        if (Core.ML)
        {
            WalkRandomInHome(3, 2, 1);
        }
        else
        {
            Mobile.ControlOrder = OrderType.None;
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
            if (Mobile.Debug)
            {
                Mobile.DebugSay($"Begin transfer with {to.Name}");
            }

            var youngFrom = from is PlayerMobile mobile && mobile.Young;
            var youngTo = to is PlayerMobile playerMobile && playerMobile.Young;

            if (youngFrom && !youngTo)
            {
                from.SendLocalizedMessage(502051); // As a young player, you may not transfer pets to older players.
            }
            else if (!youngFrom && youngTo)
            {
                from.SendLocalizedMessage(502052); // As an older player, you may not transfer pets to young players.
            }
            else if (!Mobile.CanBeControlledBy(to))
            {
                var args = $"{to.Name}\t{from.Name}\t ";

                from.SendLocalizedMessage(
                    1043248,
                    args
                ); // The pet refuses to be transferred because it will not obey ~1_NAME~.~3_BLANK~
                to.SendLocalizedMessage(
                    1043249,
                    args
                ); // The pet will not accept you as a master because it does not trust you.~3_BLANK~
            }
            else if (!Mobile.CanBeControlledBy(from))
            {
                var args = $"{to.Name}\t{from.Name}\t ";

                from.SendLocalizedMessage(
                    1043250,
                    args
                ); // The pet refuses to be transferred because it will not obey you sufficiently.~3_BLANK~
                to.SendLocalizedMessage(
                    1043251,
                    args
                ); // The pet will not accept you as a master because it does not trust ~2_NAME~.~3_BLANK~
            }
            else if (TransferItem.IsInCombat(Mobile))
            {
                from.SendMessage("You may not transfer a pet that has recently been in combat.");
                to.SendMessage("The pet may not be transferred to you because it has recently been in combat.");
            }
            else
            {
                NetState fromState = from.NetState, toState = to.NetState;

                if (fromState != null && toState != null)
                {
                    if (from.HasTrade)
                    {
                        from.SendLocalizedMessage(1010507); // You cannot transfer a pet with a trade pending
                    }
                    else if (to.HasTrade)
                    {
                        to.SendLocalizedMessage(1010507); // You cannot transfer a pet with a trade pending
                    }
                    else
                    {
                        fromState.AddTrade(toState).DropItem(new TransferItem(Mobile));
                    }
                }
            }
        }

        Mobile.ControlTarget = null;
        Mobile.ControlOrder = OrderType.Stay;

        return true;
    }

    public virtual bool DoBardPacified()
    {
        if (Core.Now < Mobile.BardEndTime)
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("I am pacified, I wait");
            }

            Mobile.Combatant = null;
            Mobile.Warmode = false;
        }
        else
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("I'm not pacified any longer");
            }

            Mobile.BardPacified = false;
        }

        return true;
    }

    public virtual bool DoBardProvoked()
    {
        if (Core.Now >= Mobile.BardEndTime &&
            (Mobile.BardMaster?.Deleted != false ||
             Mobile.BardMaster.Map != Mobile.Map || Mobile.GetDistanceToSqrt(Mobile.BardMaster) >
             Mobile.RangePerception))
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("I have lost my provoker");
            }

            Mobile.BardProvoked = false;
            Mobile.BardMaster = null;
            Mobile.BardTarget = null;

            Mobile.Combatant = null;
            Mobile.Warmode = false;
        }
        else if (Mobile.BardTarget?.Deleted != false || Mobile.BardTarget.Map != Mobile.Map ||
                 Mobile.GetDistanceToSqrt(Mobile.BardTarget) > Mobile.RangePerception)
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("I have lost my provoke target");
            }

            Mobile.BardProvoked = false;
            Mobile.BardMaster = null;
            Mobile.BardTarget = null;

            Mobile.Combatant = null;
            Mobile.Warmode = false;
        }
        else
        {
            Mobile.Combatant = Mobile.BardTarget;
            _action = ActionType.Combat;

            Mobile.OnThink();
            Think();
        }

        return true;
    }

    public virtual void WalkRandom(int iChanceToNotMove, int iChanceToDir, int iSteps)
    {
        if (Mobile.Deleted || Mobile.DisallowAllMoves)
        {
            return;
        }

        if (iChanceToNotMove <= 0)
        {
            return;
        }

        for (var i = 0; i < iSteps; i++)
        {
            if (Utility.Random(1 + iChanceToNotMove) == 0)
            {
                // iChanceToDir = 2, total weight is 18
                // 7/26 chance of other direction
                var iRndMove = Utility.Random(8 * (iChanceToDir + 1));
                Direction direction = iRndMove < 8 ? (Direction)iRndMove : Mobile.Direction;
                DoMove(direction);
            }
        }
    }

    public static double TransformMoveDelay(BaseCreature bc, bool isPassive = false)
    {
        if (bc == null)
        {
            return 0.1;
        }

        if (bc.MoveSpeedMod > 0)
        {
            return bc.MoveSpeedMod;
        }

        var moveSpeed = bc.CurrentSpeed;
        var isControlled = bc.Controlled || bc.Summoned;

        /*
         * Movement Speed Table based on RunUO
         * <0.075 -> 0.0375
         * 0.075 -> 0.05
         * 0.1 -> 0.125
         * 0.2 -> 0.3
         * 0.25 -> 0.45
         * 0.3 -> 0.6
         * 0.4 -> 0.85
         * 0.5 -> 1.05
         * 0.6 -> 1.2,
         * 0.8 -> 1.5
         * 1.0 -> 1.8
         * Above 1.0 is linear
         */

        // Linear interpolated
        var movementSpeed = moveSpeed switch
        {
            >= 1.0   => 1.8 * moveSpeed,
            >= 0.5   => 1.05 + (moveSpeed - 0.5) * 1.5,
            >= 0.4   => 0.85 + (moveSpeed - 0.4) * 2,
            >= 0.3   => 0.6 + (moveSpeed - 0.3) * 2.5,
            >= 0.2   => 0.3 + (moveSpeed - 0.2) * 3,
            >= 0.1   => 0.125 + (moveSpeed - 0.1) * 1.75,
            >= 0.075 => 0.05 + (moveSpeed - 0.075) * 3,
            _        => 0.0375 // 30 ticks, 37.5ms
        };

        if (isPassive)
        {
            movementSpeed += 0.2;
        }

        if (!isControlled)
        {
            movementSpeed += 0.1;
        }
        else if (!bc.Summoned)
        {
            if (bc.ControlOrder == OrderType.Follow && bc.ControlTarget == bc.ControlMaster)
            {
                movementSpeed *= 0.5;
            }

            movementSpeed -= 0.075;
        }

        if (!bc.IsDeadPet && (bc.ReduceSpeedWithDamage || bc.IsSubdued))
        {
            int stats, statsMax;
            if (Core.HS)
            {
                stats = bc.Stam;
                statsMax = bc.StamMax;
            }
            else
            {
                stats = bc.Hits;
                statsMax = bc.HitsMax;
            }

            var offset = statsMax <= 0 ? 1.0 : Math.Max(0, stats) / (double)statsMax;

            movementSpeed += (1.0 - offset) * 0.8;
        }

        return movementSpeed;
    }

    public bool CanMoveNow(out long delay) => (delay = NextMove - Core.TickCount) <= FuzzyTimeUntilNextMove;

    public virtual bool CheckMove() => true;

    public virtual bool DoMove(Direction d, bool badStateOk = false)
    {
        var res = DoMoveImpl(d, badStateOk);

        return res is MoveResult.Success or MoveResult.SuccessAutoTurn || badStateOk && res == MoveResult.BadState;
    }

    public virtual MoveResult DoMoveImpl(Direction d, bool badStateOk)
    {
        if (Mobile == null || Mobile.Deleted || Mobile.Frozen || Mobile.Paralyzed ||
            Mobile.Spell?.IsCasting == true || Mobile.DisallowAllMoves)
        {
            return MoveResult.BadState;
        }

        if (!CheckMove())
        {
            return MoveResult.BadState;
        }

        var delay = (int)(TransformMoveDelay(Mobile) * 1000);

        if (!CanMoveNow(out var timeUntilMove))
        {
            // When bad state is ok, we can still move if the time until move is less than the fuzzy time
            if (badStateOk)
            {
                AIMovementTimerPool.GetTimer(TimeSpan.FromMilliseconds(timeUntilMove), this, d).Start();
            }

            return MoveResult.BadState;
        }

        // This makes them always move one step, never any direction changes
        // TODO: This is firing off deltas which aren't needed. Look into replacing/removing this
        Mobile.Direction = d;
        NextMove += delay;

        if (Core.TickCount - NextMove > 0)
        {
            NextMove = Core.TickCount;
        }

        Mobile.Pushing = false;
        var mobDirection = Mobile.Direction;

        // Do the actual move
        MoveImpl.IgnoreMovableImpassables = Mobile.CanMoveOverObstacles && !Mobile.CanDestroyObstacles;
        var moveResult = Mobile.Move(d);
        MoveImpl.IgnoreMovableImpassables = false;

        if (moveResult)
        {
            // If we don't delay combat, then a direction change will happen and cause a glitchy sliding effect.
            if (Mobile.Warmode && Mobile.Combatant != null)
            {
                var remaining = Mobile.NextCombatTime - Core.TickCount;
                var maxWait = Math.Min(delay, 400);
                if (remaining < maxWait)
                {
                    Mobile.NextCombatTime = Core.TickCount + maxWait;
                }
            }
            return MoveResult.Success;
        }

        if ((mobDirection & Direction.Mask) != (d & Direction.Mask))
        {
            return MoveResult.Blocked;
        }

        var wasPushing = Mobile.Pushing;

        var blocked = true;

        var canOpenDoors = Mobile.CanOpenDoors;
        var canDestroyObstacles = Mobile.CanDestroyObstacles;

        if (canOpenDoors || canDestroyObstacles)
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("My movement was blocked, I will try to clear some obstacles.");
            }

            var map = Mobile.Map;

            if (map != null)
            {
                var x = Mobile.X;
                var y = Mobile.Y;
                Movement.Movement.Offset(d, ref x, ref y);

                using var queue = PooledRefQueue<Item>.Create();
                var destroyables = 0;
                foreach (var item in map.GetItemsInRange(new Point2D(x, y), 1))
                {
                    if (canOpenDoors && item is BaseDoor door && door.Z + door.ItemData.Height > Mobile.Z &&
                        Mobile.Z + 16 > door.Z)
                    {
                        if (door.X != x || door.Y != y)
                        {
                            continue;
                        }

                        if (!door.Locked || !door.UseLocks())
                        {
                            queue.Enqueue(door);
                        }

                        if (!canDestroyObstacles)
                        {
                            break;
                        }
                    }
                    else if (canDestroyObstacles && item.Movable && item.ItemData.Impassable &&
                             item.Z + item.ItemData.Height > Mobile.Z && Mobile.Z + 16 > item.Z)
                    {
                        if (!Mobile.InRange(item.GetWorldLocation(), 1))
                        {
                            continue;
                        }

                        queue.Enqueue(item);
                        ++destroyables;
                    }
                }

                if (destroyables > 0)
                {
                    Effects.PlaySound(new Point3D(x, y, Mobile.Z), Mobile.Map, 0x3B3);
                }

                if (queue.Count > 0)
                {
                    blocked = false; // retry movement
                }

                while (queue.Count > 0)
                {
                    var item = queue.Dequeue();

                    if (item is BaseDoor door)
                    {
                        if (Mobile.Debug)
                        {
                            Mobile.DebugSay(
                                "Little do they expect, I've learned how to open doors. Didn't they read the script??"
                            );
                        }

                        if (Mobile.Debug)
                        {
                            Mobile.DebugSay("*twist*");
                        }

                        door.Use(Mobile);
                    }
                    else
                    {
                        if (Mobile.Debug)
                        {
                            Mobile.DebugSay(
                                $"Ugabooga. I'm so big and tough I can destroy it: {item.GetType().Name}"
                            );
                        }

                        if (item is Container cont)
                        {
                            for (var i = 0; i < cont.Items.Count; ++i)
                            {
                                var check = cont.Items[i];

                                if (check.Movable && check.ItemData.Impassable &&
                                    cont.Z + check.ItemData.Height > Mobile.Z)
                                {
                                    queue.Enqueue(check);
                                }
                            }

                            cont.Destroy();
                        }
                        else
                        {
                            item.Delete();
                        }
                    }
                }

                if (!blocked)
                {
                    blocked = !Mobile.Move(d);
                }
            }
        }

        if (blocked)
        {
            var offset = Utility.RandomDouble() < 0.4 ? 1 : -1;

            for (var i = 0; i < 2; ++i)
            {
                Mobile.TurnInternal(offset);

                if (Mobile.Move(Mobile.Direction))
                {
                    return MoveResult.SuccessAutoTurn;
                }
            }

            return wasPushing ? MoveResult.BadState : MoveResult.Blocked;
        }

        return MoveResult.Success;
    }

    public virtual void WalkRandomInHome(int iChanceToNotMove, int iChanceToDir, int iSteps)
    {
        if (Mobile.Deleted || Mobile.DisallowAllMoves)
        {
            return;
        }

        if (Mobile.Home == Point3D.Zero)
        {
            if (Mobile.Spawner is RegionSpawner rs)
            {
                Region region = rs.SpawnRegion;

                if (Mobile.Region.AcceptsSpawnsFrom(region))
                {
                    Mobile.WalkRegion = region;
                    WalkRandom(iChanceToNotMove, iChanceToDir, iSteps);
                    Mobile.WalkRegion = null;
                }
                else
                {
                    if (region.GoLocation != Point3D.Zero && Utility.RandomBool())
                    {
                        DoMove(Mobile.GetDirectionTo(region.GoLocation));
                    }
                    else
                    {
                        WalkRandom(iChanceToNotMove, iChanceToDir, 1);
                    }
                }
            }
            else
            {
                WalkRandom(iChanceToNotMove, iChanceToDir, iSteps);
            }
        }
        else
        {
            for (var i = 0; i < iSteps; i++)
            {
                if (Mobile.RangeHome != 0)
                {
                    var iCurrDist = (int)Mobile.GetDistanceToSqrt(Mobile.Home);

                    if (iCurrDist < Mobile.RangeHome * 2 / 3)
                    {
                        WalkRandom(iChanceToNotMove, iChanceToDir, 1);
                    }
                    else if (iCurrDist > Mobile.RangeHome)
                    {
                        DoMove(Mobile.GetDirectionTo(Mobile.Home));
                    }
                    else if (Utility.Random(10) > 5)
                    {
                        DoMove(Mobile.GetDirectionTo(Mobile.Home));
                    }
                    else
                    {
                        WalkRandom(iChanceToNotMove, iChanceToDir, 1);
                    }
                }
                else
                {
                    if (Mobile.Location != Mobile.Home)
                    {
                        DoMove(Mobile.GetDirectionTo(Mobile.Home));
                    }
                }
            }
        }
    }

    public virtual bool CheckFlee()
    {
        if (Mobile.CheckFlee())
        {
            var combatant = Mobile.Combatant;

            if (combatant == null)
            {
                WalkRandom(1, 2, 1);
            }
            else
            {
                var d = combatant.GetDirectionTo(Mobile);

                d = (Direction)((int)d + Utility.RandomMinMax(-1, +1));

                Mobile.Direction = d;
                Mobile.Move(d);
            }

            return true;
        }

        return false;
    }

    public virtual void OnTeleported()
    {
        if (_path != null)
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("Teleported; repathing");
            }

            _path.ForceRepath();
        }
    }

    public virtual bool MoveTo(Mobile m, bool run, int range)
    {
        if (Mobile.Deleted || Mobile.DisallowAllMoves || m?.Deleted != false)
        {
            return false;
        }

        if (Mobile.InRange(m, range))
        {
            _path = null;
            return true;
        }

        if (_path?.Goal == m)
        {
            if (_path.Follow(run, 1))
            {
                _path = null;
                return true;
            }
        }
        else if (!DoMove(Mobile.GetDirectionTo(m), true))
        {
            _path = new PathFollower(Mobile, m) { Mover = DoMoveImpl };

            if (_path.Follow(run, 1))
            {
                _path = null;
                return true;
            }
        }
        else
        {
            _path = null;
            return true;
        }

        return false;
    }

    /*
     *  Walk at range distance from mobile
     *
     *	iSteps : Number of steps
     *	bRun   : Do we run
     *	iWantDistMin : The minimum distance we want to be
     *  iWantDistMax : The maximum distance we want to be
     *
     */
    public virtual bool WalkMobileRange(Mobile m, int iSteps, bool run, int iWantDistMin, int iWantDistMax)
    {
        if (Mobile.Deleted || Mobile.DisallowAllMoves)
        {
            return false;
        }

        if (m == null)
        {
            return false;
        }

        for (var i = 0; i < iSteps; i++)
        {
            // Get the current distance
            var iCurrDist = (int)Mobile.GetDistanceToSqrt(m);

            if (iCurrDist < iWantDistMin || iCurrDist > iWantDistMax)
            {
                var needCloser = iCurrDist > iWantDistMax;

                if (needCloser && _path != null && _path.Goal == m)
                {
                    if (_path.Follow(run, 1))
                    {
                        _path = null;
                    }
                }
                else
                {
                    var dirTo = iCurrDist > iWantDistMax ?
                        Mobile.GetDirectionTo(m, run) : m.GetDirectionTo(Mobile, run);

                    if (!DoMove(dirTo, true) && needCloser)
                    {
                        _path = new PathFollower(Mobile, m) { Mover = DoMoveImpl };

                        if (_path.Follow(run, 1))
                        {
                            _path = null;
                        }
                    }
                    else
                    {
                        _path = null;
                    }
                }
            }
            else
            {
                return true;
            }
        }

        // Get the current distance
        var iNewDist = (int)Mobile.GetDistanceToSqrt(m);

        return iNewDist >= iWantDistMin && iNewDist <= iWantDistMax;
    }

    /*
     * Here we check to acquire a target from our surrounding
     *
     *  iRange : The range
     *  acqType : A type of acquire we want (closest, strongest, etc)
     *  bPlayerOnly : Don't bother with other creatures or NPCs, want a player
     *  bFacFriend : Check people in my faction
     *  bFacFoe : Check people in other factions
     *
     */
    public virtual bool AcquireFocusMob(int iRange, FightMode acqType, bool bPlayerOnly, bool bFacFriend, bool bFacFoe)
    {
        if (Mobile.Deleted)
        {
            return false;
        }

        if (Mobile.BardProvoked)
        {
            if (Mobile.BardTarget?.Deleted != false)
            {
                Mobile.FocusMob = null;
                return false;
            }

            Mobile.FocusMob = Mobile.BardTarget;
            return Mobile.FocusMob != null;
        }

        if (Mobile.Controlled)
        {
            if (Mobile.ControlTarget?.Deleted != false || Mobile.ControlTarget.Hidden ||
                !Mobile.ControlTarget.Alive || Mobile.ControlTarget.IsDeadBondedPet ||
                !Mobile.InRange(Mobile.ControlTarget, Mobile.RangePerception * 2))
            {
                if (Mobile.ControlTarget != null && Mobile.ControlTarget != Mobile.ControlMaster)
                {
                    Mobile.ControlTarget = null;
                }

                Mobile.FocusMob = null;
                return false;
            }

            Mobile.FocusMob = Mobile.ControlTarget;
            return Mobile.FocusMob != null;
        }

        if (Mobile.ConstantFocus != null)
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("Acquired my constant focus");
            }

            Mobile.FocusMob = Mobile.ConstantFocus;
            return true;
        }

        if (acqType == FightMode.None)
        {
            Mobile.FocusMob = null;
            return false;
        }

        if (acqType == FightMode.Aggressor && Mobile.Aggressors.Count == 0 && Mobile.Aggressed.Count == 0 &&
            Mobile.FactionAllegiance == null && Mobile.EthicAllegiance == null)
        {
            Mobile.FocusMob = null;
            return false;
        }

        if (Core.TickCount - Mobile.NextReacquireTime < 0)
        {
            Mobile.FocusMob = null;
            return false;
        }

        Mobile.NextReacquireTime = Core.TickCount + (int)Mobile.ReacquireDelay.TotalMilliseconds;

        if (Mobile.Debug)
        {
            Mobile.DebugSay("Acquiring...");
        }

        var map = Mobile.Map;

        if (map == null)
        {
            // TODO: Is this correct? Maybe it should return false?
            return Mobile.FocusMob != null;
        }

        Mobile newFocusMob = null;
        var val = double.MinValue;
        Mobile enemySummonMob = null;
        var enemySummonVal = double.MinValue;

        foreach (var m in map.GetMobilesInRange(Mobile.Location, iRange))
        {
            if (m.Deleted || m.Blessed)
            {
                continue;
            }

            // Let's not target ourselves...
            if (m == Mobile || m is BaseFamiliar)
            {
                continue;
            }

            // Dead targets are invalid.
            if (!m.Alive || m.IsDeadBondedPet)
            {
                continue;
            }

            // Staff members cannot be targeted.
            if (m.AccessLevel > AccessLevel.Player)
            {
                continue;
            }

            // Does it have to be a player?
            if (bPlayerOnly && !m.Player)
            {
                continue;
            }

            // Can't acquire a target we can't see.
            if (!Mobile.CanSee(m))
            {
                continue;
            }

            var bc = m as BaseCreature;
            var pm = m as PlayerMobile;

            // Monster don't attack it's own summon or the summon of another monster
            if (Core.AOS && bc != null && bc.Summoned && (bc.SummonMaster == Mobile || !bc.SummonMaster.Player && IsHostile(bc.SummonMaster)))
            {
                continue;
            }

            if (Mobile.Summoned && Mobile.SummonMaster != null)
            {
                // Animated creatures cannot attack players directly.
                if (pm != null && Mobile.IsAnimatedDead)
                {
                    continue;
                }

                // Animated creatures cannot attack other animated creatures or pets of other players
                if (Mobile.IsAnimatedDead && bc != null && (bc.IsAnimatedDead || bc.Controlled))
                {
                    continue;
                }

                // If this is a summon, it can't target its controller or invalid targets.
                if (Mobile.FollowsAcquireRules && (m == Mobile.SummonMaster || !SpellHelper.ValidIndirectTarget(Mobile, m)))
                {
                    continue;
                }
            }

            // If we only want faction friends, make sure it's one.
            if (bFacFriend && !Mobile.IsFriend(m))
            {
                continue;
            }

            // Ignore anyone under EtherealVoyage
            if (TransformationSpellHelper.UnderTransformation(m, typeof(EtherealVoyageSpell)))
            {
                continue;
            }

            // Ignore players with activated honor
            if (Mobile.Combatant != m && VirtueSystem.GetVirtues(pm)?.HonorActive == true)
            {
                continue;
            }

            if (acqType is FightMode.Aggressor or FightMode.Evil)
            {
                var bValid = IsHostile(m);

                if (!bValid)
                {
                    bValid = Mobile.GetFactionAllegiance(m) == BaseCreature.Allegiance.Enemy ||
                             Mobile.GetEthicAllegiance(m) == BaseCreature.Allegiance.Enemy;
                }

                if (acqType == FightMode.Evil && !bValid)
                {
                    if (bc?.Controlled == true && bc?.ControlMaster != null)
                    {
                        bValid = bc.ControlMaster.Karma < 0;
                    }
                    else
                    {
                        bValid = m.Karma < 0;
                    }
                }

                if (!bValid)
                {
                    continue;
                }
            }
            else
            {
                // Same goes for faction enemies.
                if (bFacFoe && !Mobile.IsEnemy(m))
                {
                    continue;
                }

                // If it's an enemy factioned mobile, make sure we can be harmful to it.
                if (bFacFoe && !bFacFriend && !Mobile.CanBeHarmful(m, false))
                {
                    continue;
                }
            }

            var theirVal = Mobile.GetFightModeRanking(m, acqType, bPlayerOnly);

            // Always prefer someone else over the summon master (EV/BS)
            if ((theirVal > val || newFocusMob == Mobile.SummonMaster) && Mobile.InLOS(m))
            {
                newFocusMob = m;
                val = theirVal;
            }
            // The summon is targeted when nothing else around. Otherwise this monster enters idle mode.
            // Do a check for this edge case so players cannot abuse by casting EVs offscreen to kill an idle monster.
            else if (Core.AOS && theirVal > enemySummonVal && Mobile.InLOS(m) && bc?.Summoned == true && bc?.Controlled != true)
            {
                enemySummonMob = m;
                enemySummonVal = theirVal;
            }
        }

        Mobile.FocusMob = newFocusMob ?? enemySummonMob;
        return Mobile.FocusMob != null;
    }

    private bool IsHostile(Mobile from)
    {
        if (Mobile.Combatant == from || from.Combatant == Mobile)
        {
            return true;
        }

        var count = Math.Max(Mobile.Aggressors.Count, Mobile.Aggressed.Count);

        for (var a = 0; a < count; ++a)
        {
            if (a < Mobile.Aggressed.Count && Mobile.Aggressed[a].Attacker == from)
            {
                return true;
            }

            if (a < Mobile.Aggressors.Count && Mobile.Aggressors[a].Defender == from)
            {
                return true;
            }
        }

        return false;
    }

    public virtual void DetectHidden()
    {
        if (Mobile.Deleted || Mobile.Map == null)
        {
            return;
        }

        if (Mobile.Debug)
        {
            Mobile.DebugSay("Checking for hidden players");
        }

        var srcSkill = Mobile.Skills.DetectHidden.Value;

        if (srcSkill <= 0)
        {
            return;
        }

        foreach (var trg in Mobile.GetMobilesInRange(Mobile.RangePerception))
        {
            if (trg != Mobile && trg.Player && trg.Alive && trg.Hidden && trg.AccessLevel == AccessLevel.Player &&
                Mobile.InLOS(trg))
            {
                if (Mobile.Debug)
                {
                    Mobile.DebugSay($"Trying to detect {trg.Name}");
                }

                var trgHiding = trg.Skills.Hiding.Value / 2.9;
                var trgStealth = trg.Skills.Stealth.Value / 1.8;

                var chance = srcSkill / 1.2 - Math.Min(trgHiding, trgStealth);

                if (chance < srcSkill / 10)
                {
                    chance = srcSkill / 10;
                }

                chance /= 100;

                if (chance > Utility.RandomDouble())
                {
                    trg.RevealingAction();
                    trg.SendLocalizedMessage(500814); // You have been revealed!
                }
            }
        }
    }

    public virtual void Deactivate()
    {
        if (Mobile.PlayerRangeSensitive)
        {
            _timer.Stop();

            var spawner = Mobile.Spawner;

            if (spawner?.ReturnOnDeactivate == true && !Mobile.Controlled && (
                    spawner.HomeLocation == Point3D.Zero && !Mobile.Region.AcceptsSpawnsFrom(spawner.Region) ||
                    !Mobile.InRange(spawner.HomeLocation, spawner.HomeRange)
                ))
            {
                Timer.StartTimer(ReturnToHome);
            }
        }
    }

    private void ReturnToHome()
    {
        if (Mobile.Spawner != null)
        {
            var loc = Mobile.Spawner.GetSpawnPosition(Mobile, Mobile.Spawner.Map);

            if (loc != Point3D.Zero)
            {
                Mobile.MoveToWorld(loc, Mobile.Spawner.Map);
            }
        }
    }

    public virtual void Activate()
    {
        if (!_timer.Running)
        {
            // We want to randomize the time at which the AI activates.
            // This triggers when a mob is first created since it moves from the internal map to it's added location
            // If we spawn lots of mobs, we don't want their AI synchronized exactly.
            _timer.Delay = TimeSpan.FromMilliseconds(Utility.Random(48) * 8);
            _timer.Start();
        }
    }

    public virtual void OnCurrentSpeedChanged()
    {
        _timer.Interval = TimeSpan.FromSeconds(Math.Max(0.008, Mobile.CurrentSpeed));
    }

    private class InternalEntry : ContextMenuEntry
    {
        private readonly OrderType _order;

        public InternalEntry(int number, int range, OrderType order, bool enabled) : base(number, range)
        {
            _order = order;
            Enabled = enabled;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (!from.CheckAlive() || target is not BaseCreature { Deleted: not true, Controlled: true } bc)
            {
                return;
            }

            // Just in case
            if (bc.IsDeadPet && _order is OrderType.Guard or OrderType.Attack or OrderType.Transfer or OrderType.Drop)
            {
                return;
            }

            var isOwner = from == bc.ControlMaster;
            var isFriend = !isOwner && bc.IsPetFriend(from);

            if (!isOwner && !isFriend)
            {
                return;
            }

            if (isFriend && _order != OrderType.Follow && _order != OrderType.Stay && _order != OrderType.Stop)
            {
                return;
            }

            switch (_order)
            {
                case OrderType.Follow:
                case OrderType.Attack:
                case OrderType.Transfer:
                case OrderType.Friend:
                case OrderType.Unfriend:
                    {
                        if (_order == OrderType.Transfer && from.HasTrade)
                        {
                            from.SendLocalizedMessage(1010507); // You cannot transfer a pet with a trade pending
                        }
                        else if (_order == OrderType.Friend && from.HasTrade)
                        {
                            from.SendLocalizedMessage(1070947); // You cannot friend a pet with a trade pending
                        }
                        else
                        {
                            bc.AIObject.BeginPickTarget(from, _order);
                        }

                        break;
                    }
                case OrderType.Release:
                    {
                        if (bc.Summoned)
                        {
                            goto default;
                        }

                        from.SendGump(new ConfirmReleaseGump(from, bc));

                        break;
                    }
                default:
                    {
                        if (bc.CheckControlChance(from))
                        {
                            bc.ControlTarget = null;
                            bc.ControlOrder = _order;
                        }

                        break;
                    }
            }
        }
    }

    private class TransferItem : Item
    {
        private readonly BaseCreature _creature;

        public TransferItem(BaseCreature creature)
            : base(ShrinkTable.Lookup(creature))
        {
            _creature = creature;

            Movable = false;

            if (!Core.AOS)
            {
                Name = creature.Name;
            }
            else if (ItemID == ShrinkTable.DefaultItemID ||
                     creature.GetType().IsDefined(typeof(FriendlyNameAttribute), false) || creature is Reptalon)
            {
                Name = FriendlyNameAttribute.GetFriendlyNameFor(creature.GetType()).ToString();
            }

            // (As Per OSI)No name.  Normally, set by the ItemID of the Shrink Item unless we either explicitly set it with an Attribute, or, no lookup found

            Hue = creature.Hue & 0x0FFF;
        }

        public TransferItem(Serial serial)
            : base(serial)
        {
        }

        public static bool IsInCombat(BaseCreature creature) =>
            creature != null && (creature.Aggressors.Count > 0 || creature.Aggressed.Count > 0);

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Delete();
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1041603);                  // This item represents a pet currently in consideration for trade
            list.Add(1041601, _creature.Name); // Pet Name: ~1_val~

            if (_creature.ControlMaster != null)
            {
                list.Add(1041602, _creature.ControlMaster.Name); // Owner: ~1_val~
            }
        }

        public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
            if (!base.AllowSecureTrade(from, to, newOwner, accepted))
            {
                return false;
            }

            if (Deleted || _creature?.Deleted != false || _creature.ControlMaster != from ||
                !from.CheckAlive() || !to.CheckAlive())
            {
                return false;
            }

            if (from.Map != _creature.Map || !from.InRange(_creature, 14))
            {
                return false;
            }

            var youngFrom = from is PlayerMobile mobile && mobile.Young;
            var youngTo = to is PlayerMobile playerMobile && playerMobile.Young;

            if (accepted && youngFrom && !youngTo)
            {
                from.SendLocalizedMessage(502051); // As a young player, you may not transfer pets to older players.
            }
            else if (accepted && !youngFrom && youngTo)
            {
                from.SendLocalizedMessage(502052); // As an older player, you may not transfer pets to young players.
            }
            else if (accepted && !_creature.CanBeControlledBy(to))
            {
                var args = $"{to.Name}\t{from.Name}\t ";

                // The pet refuses to be transferred because it will not obey ~1_NAME~.~3_BLANK~
                from.SendLocalizedMessage(1043248, args);
                // The pet will not accept you as a master because it does not trust you.~3_BLANK~
                to.SendLocalizedMessage(1043249, args);

                return false;
            }
            else if (accepted && !_creature.CanBeControlledBy(from))
            {
                var args = $"{to.Name}\t{from.Name}\t ";

                // The pet refuses to be transferred because it will not obey you sufficiently.~3_BLANK~
                from.SendLocalizedMessage(1043250, args);
                // The pet will not accept you as a master because it does not trust ~2_NAME~.~3_BLANK~
                to.SendLocalizedMessage(1043251, args);
            }
            else if (accepted && to.Followers + _creature.ControlSlots > to.FollowersMax)
            {
                to.SendLocalizedMessage(1049607); // You have too many followers to control that creature.

                return false;
            }
            else if (accepted && IsInCombat(_creature))
            {
                from.SendMessage("You may not transfer a pet that has recently been in combat.");
                to.SendMessage("The pet may not be transferred to you because it has recently been in combat.");

                return false;
            }

            return true;
        }

        public override void OnSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
            if (Deleted)
            {
                return;
            }

            Delete();

            if (_creature?.Deleted != false || _creature.ControlMaster != from || !from.CheckAlive() ||
                !to.CheckAlive())
            {
                return;
            }

            if (from.Map != _creature.Map || !from.InRange(_creature, 14))
            {
                return;
            }

            if (accepted && _creature.SetControlMaster(to))
            {
                if (_creature.Summoned)
                {
                    _creature.SummonMaster = to;
                }

                _creature.ControlTarget = to;
                _creature.ControlOrder = OrderType.Follow;

                _creature.BondingBegin = DateTime.MinValue;
                _creature.OwnerAbandonTime = DateTime.MinValue;
                _creature.IsBonded = false;

                _creature.PlaySound(_creature.GetIdleSound());

                var args = $"{from.Name}\t{_creature.Name}\t{to.Name}";

                from.SendLocalizedMessage(1043253, args); // You have transferred your pet to ~3_GETTER~.
                // ~1_NAME~ has transferred the allegiance of ~2_PET_NAME~ to you.
                to.SendLocalizedMessage(1043252, args);
            }
        }
    }

    /*
     *  The Timer object
     */
    private class AITimer : Timer
    {
        private readonly BaseAI _owner;

        public AITimer(BaseAI owner) : base(
            TimeSpan.FromMilliseconds(Utility.Random(96) * 8),
            TimeSpan.FromSeconds(Math.Max(0.0, owner.Mobile.CurrentSpeed))
        )
        {
            _owner = owner;
            _owner._nextDetectHidden = Core.TickCount;
        }

        protected override void OnTick()
        {
            if (_owner.Mobile.Deleted)
            {
                Stop();
                return;
            }

            if (_owner.Mobile.Map == null || _owner.Mobile.Map == Map.Internal)
            {
                _owner.Deactivate();
                return;
            }

            if (_owner.Mobile.PlayerRangeSensitive) // have to check this in the timer....
            {
                var sect = _owner.Mobile.Map.GetSector(_owner.Mobile.Location);
                if (!sect.Active)
                {
                    _owner.Deactivate();
                    return;
                }
            }

            _owner.Mobile.OnThink();

            if (_owner.Mobile.Deleted)
            {
                Stop();
                return;
            }

            if (_owner.Mobile.Map == null || _owner.Mobile.Map == Map.Internal)
            {
                _owner.Deactivate();
                return;
            }

            if (_owner.Mobile.BardPacified)
            {
                _owner.DoBardPacified();
            }
            else if (_owner.Mobile.BardProvoked)
            {
                _owner.DoBardProvoked();
            }
            else if (!_owner.Mobile.Controlled)
            {
                if (!_owner.Think())
                {
                    Stop();
                    return;
                }
            }
            else if (!_owner.Obey())
            {
                Stop();
                return;
            }

            if (_owner.CanDetectHidden && Core.TickCount - _owner._nextDetectHidden >= 0)
            {
                _owner.DetectHidden();

                // Not exactly OSI style, approximation.
                var delay = Math.Min(15000 / _owner.Mobile.Int, 60);

                var min = delay * 900; // 13s at 1000 int, 33s at 400 int, 54s at <250 int
                var max = delay * 1100; // 16s at 1000 int, 41s at 400 int, 66s at <250 int

                _owner._nextDetectHidden = Core.TickCount + Utility.RandomMinMax(min, max);
            }
        }
    }

    public static class AIMovementTimerPool
    {
        private const int _poolSize = 1024;
        private static readonly Queue<AIMovementTimer> _pool = new (_poolSize);

        public static void Configure()
        {
            var i = 0;
            while (i++ < _poolSize)
            {
                _pool.Enqueue(new AIMovementTimer());
            }
        }

        public static AIMovementTimer GetTimer(TimeSpan delay, BaseAI ai, Direction direction)
        {
            AIMovementTimer timer;
            if (_pool.Count > 0)
            {
                timer = _pool.Dequeue();
            }
            else
            {
                timer = new AIMovementTimer();
            }

            timer.Set(delay, ai, direction);
            return timer;
        }

        public class AIMovementTimer : Timer
        {
            public BaseAI AI { get; private set; }
            public Direction Direction { get; private set; }

            public AIMovementTimer() : base(TimeSpan.Zero)
            {
            }

            public void Set(TimeSpan delay, BaseAI ai, Direction direction)
            {
                if (Running)
                {
                    return;
                }

                Delay = delay;
                AI = ai;
                Direction = direction;
            }

            protected override void OnTick()
            {
                AI?.DoMove(Direction);
                AI = null;
                _pool.Enqueue(this);
            }
        }
    }
}
