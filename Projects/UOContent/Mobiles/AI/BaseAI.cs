using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Engines.Quests;
using Server.Engines.Quests.Necro;
using Server.Engines.Spawners;
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
    private static readonly SkillName[] m_KeywordTable =
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

    private static readonly Queue<Item> m_Obstacles = new();
    protected ActionType m_Action;

    public BaseCreature m_Mobile;

    private long m_NextDetectHidden;
    private long m_NextStopGuard;

    protected PathFollower m_Path;
    public Timer m_Timer;

    public BaseAI(BaseCreature m)
    {
        m_Mobile = m;

        m_Timer = new AITimer(this);

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
            m_Timer.Start();
        }

        Action = ActionType.Wander;
    }

    public ActionType Action
    {
        get => m_Action;
        set
        {
            m_Action = value;
            OnActionChanged();
        }
    }

    public long NextMove { get; set; }

    public virtual bool CanDetectHidden => m_Mobile.Skills.DetectHidden.Value > 0;

    public virtual bool WasNamed(string speech)
    {
        var name = m_Mobile.Name;

        return name != null && speech.InsensitiveStartsWith(name);
    }

    public virtual void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        if (from.Alive && m_Mobile.Controlled && from.InRange(m_Mobile, 14))
        {
            if (from == m_Mobile.ControlMaster)
            {
                list.Add(new InternalEntry(from, 6107, 14, m_Mobile, this, OrderType.Guard));  // Command: Guard
                list.Add(new InternalEntry(from, 6108, 14, m_Mobile, this, OrderType.Follow)); // Command: Follow

                if (m_Mobile.CanDrop)
                {
                    list.Add(new InternalEntry(from, 6109, 14, m_Mobile, this, OrderType.Drop)); // Command: Drop
                }

                list.Add(new InternalEntry(from, 6111, 14, m_Mobile, this, OrderType.Attack)); // Command: Kill

                list.Add(new InternalEntry(from, 6112, 14, m_Mobile, this, OrderType.Stop)); // Command: Stop
                list.Add(new InternalEntry(from, 6114, 14, m_Mobile, this, OrderType.Stay)); // Command: Stay

                if (!m_Mobile.Summoned && m_Mobile is not GrizzledMare)
                {
                    list.Add(new InternalEntry(from, 6110, 14, m_Mobile, this, OrderType.Friend));   // Add Friend
                    list.Add(new InternalEntry(from, 6099, 14, m_Mobile, this, OrderType.Unfriend)); // Remove Friend
                    list.Add(new InternalEntry(from, 6113, 14, m_Mobile, this, OrderType.Transfer)); // Transfer
                }

                list.Add(new InternalEntry(from, 6118, 14, m_Mobile, this, OrderType.Release)); // Release
            }
            else if (m_Mobile.IsPetFriend(from))
            {
                list.Add(new InternalEntry(from, 6108, 14, m_Mobile, this, OrderType.Follow)); // Command: Follow
                list.Add(new InternalEntry(from, 6112, 14, m_Mobile, this, OrderType.Stop));   // Command: Stop
                list.Add(new InternalEntry(from, 6114, 14, m_Mobile, this, OrderType.Stay));   // Command: Stay
            }
        }
    }

    public virtual void BeginPickTarget(Mobile from, OrderType order)
    {
        if (m_Mobile.Deleted || !m_Mobile.Controlled || !from.InRange(m_Mobile, 14) || from.Map != m_Mobile.Map)
        {
            return;
        }

        var isOwner = from == m_Mobile.ControlMaster;
        var isFriend = !isOwner && m_Mobile.IsPetFriend(from);

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
        var currentCombat = m_Mobile.Combatant;

        if (currentCombat != null && !aggressor.Hidden && currentCombat != aggressor &&
            m_Mobile.GetDistanceToSqrt(currentCombat) > m_Mobile.GetDistanceToSqrt(aggressor))
        {
            m_Mobile.Combatant = aggressor;
        }
    }

    public virtual void EndPickTarget(Mobile from, Mobile target, OrderType order)
    {
        if (m_Mobile.Deleted || !m_Mobile.Controlled || !from.InRange(m_Mobile, 14) || from.Map != m_Mobile.Map ||
            !from.CheckAlive())
        {
            return;
        }

        var isOwner = from == m_Mobile.ControlMaster;
        var isFriend = !isOwner && m_Mobile.IsPetFriend(from);

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
            if (target is BaseCreature creature && creature.IsScaryToPets && m_Mobile.IsScaredOfScaryThings)
            {
                m_Mobile.SayTo(from, "Your pet refuses to attack this creature!");
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
                m_Mobile.SayTo(from, "Your pet refuses to attack the guard.");
                return;
            }
        }

        if (m_Mobile.CheckControlChance(from))
        {
            m_Mobile.ControlTarget = target;
            m_Mobile.ControlOrder = order;
        }
    }

    public virtual bool HandlesOnSpeech(Mobile from)
    {
        if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            return true;
        }

        if (from.Alive && m_Mobile.Controlled && m_Mobile.Commandable &&
            (from == m_Mobile.ControlMaster || m_Mobile.IsPetFriend(from)))
        {
            return true;
        }

        return from.Alive && from.InRange(m_Mobile.Location, 3) && m_Mobile.IsHumanInTown();
    }

    public virtual void OnSpeech(SpeechEventArgs e)
    {
        if (e.Mobile.Alive && e.Mobile.InRange(m_Mobile.Location, 3) && m_Mobile.IsHumanInTown())
        {
            if (e.HasKeyword(0x9D) && WasNamed(e.Speech)) // *move*
            {
                if (m_Mobile.Combatant != null)
                {
                    // I am too busy fighting to deal with thee!
                    m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                }
                else
                {
                    // Excuse me?
                    m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501516);
                    WalkRandomInHome(2, 2, 1);
                }
            }
            else if (e.HasKeyword(0x9E) && WasNamed(e.Speech)) // *time*
            {
                if (m_Mobile.Combatant != null)
                {
                    // I am too busy fighting to deal with thee!
                    m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                }
                else
                {
                    Clock.GetTime(m_Mobile, out var generalNumber, out _);

                    m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, generalNumber);
                }
            }
            else if (e.HasKeyword(0x6C) && WasNamed(e.Speech)) // *train
            {
                if (m_Mobile.Combatant != null)
                {
                    // I am too busy fighting to deal with thee!
                    m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                }
                else
                {
                    var foundSomething = false;

                    var ourSkills = m_Mobile.Skills;
                    var theirSkills = e.Mobile.Skills;

                    for (var i = 0; i < ourSkills.Length && i < theirSkills.Length; ++i)
                    {
                        var skill = ourSkills[i];
                        var theirSkill = theirSkills[i];

                        if (skill?.Base >= 60.0 && m_Mobile.CheckTeach(skill.SkillName, e.Mobile))
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
                                    m_Mobile.Say(1043058); // I can train the following:
                                }

                                m_Mobile.Say(number);

                                foundSomething = true;
                            }
                        }
                    }

                    if (!foundSomething)
                    {
                        m_Mobile.Say(501505); // Alas, I cannot teach thee anything.
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

                        if (index >= 0 && index < m_KeywordTable.Length)
                        {
                            toTrain = m_KeywordTable[index];
                        }
                    }
                }

                if (toTrain != (SkillName)(-1) && WasNamed(e.Speech))
                {
                    if (m_Mobile.Combatant != null)
                    {
                        // I am too busy fighting to deal with thee!
                        m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                    }
                    else
                    {
                        var skills = m_Mobile.Skills;
                        var skill = skills[toTrain];

                        if (skill == null || skill.Base < 60.0 || !m_Mobile.CheckTeach(toTrain, e.Mobile))
                        {
                            m_Mobile.Say(501507); // 'Tis not something I can teach thee of.
                        }
                        else
                        {
                            m_Mobile.Teach(toTrain, e.Mobile, 0, false);
                        }
                    }
                }
            }
        }

        if (m_Mobile.Controlled && m_Mobile.Commandable)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("Listening...");
            }

            var isOwner = e.Mobile == m_Mobile.ControlMaster;
            var isFriend = !isOwner && m_Mobile.IsPetFriend(e.Mobile);

            if (e.Mobile.Alive && (isOwner || isFriend))
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay("It's from my master");
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

                                if (m_Mobile.CheckControlChance(e.Mobile))
                                {
                                    m_Mobile.ControlTarget = null;
                                    m_Mobile.ControlOrder = OrderType.Come;
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

                                if (m_Mobile.CheckControlChance(e.Mobile))
                                {
                                    m_Mobile.ControlTarget = null;
                                    m_Mobile.ControlOrder = OrderType.Guard;
                                }

                                return;
                            }
                        case 0x167: // all stop
                            {
                                if (m_Mobile.CheckControlChance(e.Mobile))
                                {
                                    m_Mobile.ControlTarget = null;
                                    m_Mobile.ControlOrder = OrderType.Stop;
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
                                if (m_Mobile.CheckControlChance(e.Mobile))
                                {
                                    m_Mobile.ControlTarget = e.Mobile;
                                    m_Mobile.ControlOrder = OrderType.Follow;
                                }

                                return;
                            }
                        case 0x170: // all stay
                            {
                                if (m_Mobile.CheckControlChance(e.Mobile))
                                {
                                    m_Mobile.ControlTarget = null;
                                    m_Mobile.ControlOrder = OrderType.Stay;
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

                                if (WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                {
                                    m_Mobile.ControlTarget = null;
                                    m_Mobile.ControlOrder = OrderType.Come;
                                }

                                return;
                            }
                        case 0x156: // *drop
                            {
                                if (!isOwner)
                                {
                                    break;
                                }

                                if (!m_Mobile.IsDeadPet && !m_Mobile.Summoned && WasNamed(speech) &&
                                    m_Mobile.CheckControlChance(e.Mobile))
                                {
                                    m_Mobile.ControlTarget = null;
                                    m_Mobile.ControlOrder = OrderType.Drop;
                                }

                                return;
                            }
                        case 0x15A: // *follow
                            {
                                if (WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
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

                                if (WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                {
                                    if (m_Mobile.Summoned || m_Mobile is GrizzledMare)
                                    {
                                        e.Mobile.SendLocalizedMessage(
                                            1005481
                                        ); // Summoned creatures are loyal only to their summoners.
                                    }
                                    else if (e.Mobile.HasTrade)
                                    {
                                        e.Mobile.SendLocalizedMessage(
                                            1070947
                                        ); // You cannot friend a pet with a trade pending
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

                                if (!m_Mobile.IsDeadPet && WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                {
                                    m_Mobile.ControlTarget = null;
                                    m_Mobile.ControlOrder = OrderType.Guard;
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

                                if (!m_Mobile.IsDeadPet && WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
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

                                if (WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                {
                                    m_Mobile.ControlTarget = null;
                                    m_Mobile.ControlOrder = OrderType.Patrol;
                                }

                                return;
                            }
                        case 0x161: // *stop
                            {
                                if (WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                {
                                    m_Mobile.ControlTarget = null;
                                    m_Mobile.ControlOrder = OrderType.Stop;
                                }

                                return;
                            }
                        case 0x163: // *follow me
                            {
                                if (WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                {
                                    m_Mobile.ControlTarget = e.Mobile;
                                    m_Mobile.ControlOrder = OrderType.Follow;
                                }

                                return;
                            }
                        case 0x16D: // *release
                            {
                                if (!isOwner)
                                {
                                    break;
                                }

                                if (WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                {
                                    if (!m_Mobile.Summoned)
                                    {
                                        e.Mobile.SendGump(new ConfirmReleaseGump(e.Mobile, m_Mobile));
                                    }
                                    else
                                    {
                                        m_Mobile.ControlTarget = null;
                                        m_Mobile.ControlOrder = OrderType.Release;
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

                                if (!m_Mobile.IsDeadPet && WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                {
                                    if (m_Mobile.Summoned || m_Mobile is GrizzledMare)
                                    {
                                        e.Mobile.SendLocalizedMessage(
                                            1005487
                                        ); // You cannot transfer ownership of a summoned creature.
                                    }
                                    else if (e.Mobile.HasTrade)
                                    {
                                        e.Mobile.SendLocalizedMessage(
                                            1010507
                                        ); // You cannot transfer a pet with a trade pending
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
                                if (WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                {
                                    m_Mobile.ControlTarget = null;
                                    m_Mobile.ControlOrder = OrderType.Stay;
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
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay("It's from a GM");
                }

                if (m_Mobile.FindMyName(e.Speech, true))
                {
                    var str = e.Speech.Split(' ');
                    int i;

                    for (i = 0; i < str.Length; i++)
                    {
                        var word = str[i];

                        if (word.InsensitiveEquals("obey"))
                        {
                            m_Mobile.SetControlMaster(e.Mobile);

                            if (m_Mobile.Summoned)
                            {
                                m_Mobile.SummonMaster = e.Mobile;
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
        if (m_Mobile.Deleted)
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
                    m_Mobile.OnActionWander();
                    return DoActionWander();
                }

            case ActionType.Combat:
                {
                    m_Mobile.OnActionCombat();
                    return DoActionCombat();
                }

            case ActionType.Guard:
                {
                    m_Mobile.OnActionGuard();
                    return DoActionGuard();
                }

            case ActionType.Flee:
                {
                    m_Mobile.OnActionFlee();
                    return DoActionFlee();
                }

            case ActionType.Interact:
                {
                    m_Mobile.OnActionInteract();
                    return DoActionInteract();
                }

            case ActionType.Backoff:
                {
                    m_Mobile.OnActionBackoff();
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
                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    m_Mobile.FocusMob = null;
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    break;
                }

            case ActionType.Combat:
                {
                    m_Mobile.Warmode = true;
                    m_Mobile.FocusMob = null;
                    m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
                    break;
                }

            case ActionType.Guard:
                {
                    m_Mobile.Warmode = true;
                    m_Mobile.FocusMob = null;
                    m_Mobile.Combatant = null;
                    m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
                    m_NextStopGuard = Core.TickCount + (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
                    m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
                    break;
                }

            case ActionType.Flee:
                {
                    m_Mobile.Warmode = true;
                    m_Mobile.FocusMob = null;
                    m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
                    break;
                }

            case ActionType.Interact:
                {
                    m_Mobile.Warmode = false;
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    break;
                }

            case ActionType.Backoff:
                {
                    m_Mobile.Warmode = false;
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    break;
                }
        }
    }

    public virtual bool OnAtWayPoint() => true;

    public virtual bool DoActionWander()
    {
        if (CheckHerding())
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("Praise the shepherd!");
            }
        }
        else if (m_Mobile.CurrentWayPoint != null)
        {
            var point = m_Mobile.CurrentWayPoint;
            if ((point.X != m_Mobile.Location.X || point.Y != m_Mobile.Location.Y) && point.Map == m_Mobile.Map &&
                point.Parent == null && !point.Deleted)
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay("I will move towards my waypoint.");
                }

                DoMove(m_Mobile.GetDirectionTo(m_Mobile.CurrentWayPoint));
            }
            else if (OnAtWayPoint())
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay("I will go to the next waypoint");
                }

                m_Mobile.CurrentWayPoint = point.NextPoint;
                if (point.NextPoint?.Deleted == true)
                {
                    m_Mobile.CurrentWayPoint = point.NextPoint = point.NextPoint.NextPoint;
                }
            }
        }
        else if (m_Mobile.IsAnimatedDead)
        {
            // animated dead follow their master
            var master = m_Mobile.SummonMaster;

            if (master != null && master.Map == m_Mobile.Map && master.InRange(m_Mobile, m_Mobile.RangePerception))
            {
                MoveTo(master, false, 1);
            }
            else
            {
                WalkRandomInHome(2, 2, 1);
            }
        }
        else if (CheckMove())
        {
            if (!m_Mobile.CheckIdle())
            {
                WalkRandomInHome(2, 2, 1);
            }
        }

        if (m_Mobile.Combatant?.Deleted == false && m_Mobile.Combatant.Alive &&
            !m_Mobile.Combatant.IsDeadBondedPet)
        {
            m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.Combatant);
        }

        return true;
    }

    public virtual bool DoActionCombat()
    {
        if (Core.AOS && CheckHerding())
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("Praise the shepherd!");
            }

            return true;
        }

        var c = m_Mobile.Combatant;

        if (c?.Deleted != false || c.Map != m_Mobile.Map || !c.Alive || c.IsDeadBondedPet)
        {
            Action = ActionType.Wander;
            return true;
        }

        m_Mobile.Direction = m_Mobile.GetDirectionTo(c);
        if (m_Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, c))
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay($"I used my abilities on {c.Name}!");
            }
        }

        return true;
    }

    public virtual bool DoActionGuard()
    {
        if (Core.AOS && CheckHerding())
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("Praise the shepherd!");
            }
        }
        else if (Core.TickCount - m_NextStopGuard < 0)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("I am on guard");
            }
            // m_Mobile.Turn( Utility.Random(0, 2) - 1 );
        }
        else
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("I stopped being on guard");
            }

            Action = ActionType.Wander;
        }

        return true;
    }

    public virtual bool DoActionFlee()
    {
        var from = m_Mobile.FocusMob;

        if (from?.Deleted != false || from.Map != m_Mobile.Map)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("I have lost him");
            }

            Action = ActionType.Guard;
            return true;
        }

        if (WalkMobileRange(from, 1, true, m_Mobile.RangePerception * 2, m_Mobile.RangePerception * 3))
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("I have fled");
            }

            Action = ActionType.Guard;
            return true;
        }

        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("I am fleeing!");
        }

        return true;
    }

    public virtual bool DoActionInteract() => true;

    public virtual bool DoActionBackoff() => true;

    public virtual bool Obey() =>
        !m_Mobile.Deleted && m_Mobile.ControlOrder switch
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
        if (m_Mobile.Deleted || m_Mobile.ControlMaster?.Deleted != false)
        {
            return;
        }

        switch (m_Mobile.ControlOrder)
        {
            case OrderType.None:
                {
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.Home = m_Mobile.Location;
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    break;
                }

            case OrderType.Come:
                {
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    break;
                }

            case OrderType.Drop:
                {
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    break;
                }

            case OrderType.Friend:
            case OrderType.Unfriend:
                {
                    m_Mobile.ControlMaster.RevealingAction();
                    break;
                }

            case OrderType.Guard:
                {
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = true;
                    m_Mobile.Combatant = null;
                    m_Mobile.ControlMaster.SendLocalizedMessage(1049671, m_Mobile.Name); // ~1_PETNAME~ is now guarding you.
                    break;
                }

            case OrderType.Attack:
                {
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());

                    m_Mobile.Warmode = true;
                    m_Mobile.Combatant = null;
                    break;
                }

            case OrderType.Patrol:
                {
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    break;
                }

            case OrderType.Release:
                {
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    break;
                }

            case OrderType.Stay:
                {
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    break;
                }

            case OrderType.Stop:
                {
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.Home = m_Mobile.Location;
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    break;
                }

            case OrderType.Follow:
                {
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());

                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    break;
                }

            case OrderType.Transfer:
                {
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());

                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    break;
                }
        }
    }

    public virtual bool DoOrderNone()
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("I have no order");
        }

        WalkRandomInHome(3, 2, 1);

        if (m_Mobile.Combatant?.Deleted == false && m_Mobile.Combatant.Alive &&
            !m_Mobile.Combatant.IsDeadBondedPet)
        {
            m_Mobile.Warmode = true;
            m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.Combatant);
        }
        else
        {
            m_Mobile.Warmode = false;
        }

        return true;
    }

    public virtual bool DoOrderCome()
    {
        if (m_Mobile.ControlMaster?.Deleted != false)
        {
            return true;
        }

        var iCurrDist = (int)m_Mobile.GetDistanceToSqrt(m_Mobile.ControlMaster);

        if (iCurrDist > m_Mobile.RangePerception)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("I have lost my master. I stay here");
            }

            m_Mobile.ControlTarget = null;
            m_Mobile.ControlOrder = OrderType.None;
        }
        else
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("My master told me come");
            }

            // Not exactly OSI style, but better than nothing.
            var bRun = iCurrDist > 5;

            if (WalkMobileRange(m_Mobile.ControlMaster, 1, bRun, 0, 1))
            {
                if (m_Mobile.Combatant?.Deleted == false && m_Mobile.Combatant.Alive &&
                    !m_Mobile.Combatant.IsDeadBondedPet)
                {
                    m_Mobile.Warmode = true;
                    // m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.Combatant, bRun);
                }
                else
                {
                    m_Mobile.Warmode = false;
                }
            }
        }

        return true;
    }

    public virtual bool DoOrderDrop()
    {
        if (m_Mobile.IsDeadPet || !m_Mobile.CanDrop)
        {
            return true;
        }

        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("I drop my stuff for my master");
        }

        var pack = m_Mobile.Backpack;

        if (pack != null)
        {
            var list = pack.Items;

            for (var i = list.Count - 1; i >= 0; --i)
            {
                if (i < list.Count)
                {
                    list[i].MoveToWorld(m_Mobile.Location, m_Mobile.Map);
                }
            }
        }

        m_Mobile.ControlTarget = null;
        m_Mobile.ControlOrder = OrderType.None;

        return true;
    }

    public virtual bool CheckHerding()
    {
        var target = m_Mobile.TargetLocation;

        if (target == null)
        {
            return false; // Creature is not being herded
        }

        var distance = m_Mobile.GetDistanceToSqrt(target);

        if (!(distance is < 1 or > 15))
        {
            DoMove(m_Mobile.GetDirectionTo(target));
            return true;
        }

        if (distance < 1 && target.X == 1076 && target.Y == 450 && m_Mobile is HordeMinionFamiliar)
        {
            if (m_Mobile.ControlMaster is PlayerMobile pm)
            {
                var qs = pm.Quest;

                if (qs is DarkTidesQuest)
                {
                    QuestObjective obj = qs.FindObjective<FetchAbraxusScrollObjective>();

                    if (obj?.Completed == false)
                    {
                        m_Mobile.AddToBackpack(new ScrollOfAbraxus());
                        obj.Complete();
                    }
                }
            }
        }

        m_Mobile.TargetLocation = null;
        return false; // At the target or too far away
    }

    public virtual bool DoOrderFollow()
    {
        if (CheckHerding())
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("Praise the shepherd!");
            }
        }
        else if (m_Mobile.ControlTarget?.Deleted == false && m_Mobile.ControlTarget != m_Mobile)
        {
            var iCurrDist = (int)m_Mobile.GetDistanceToSqrt(m_Mobile.ControlTarget);

            if (iCurrDist > m_Mobile.RangePerception)
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay("I have lost the one to follow. I stay here");
                }

                if (m_Mobile.Combatant?.Deleted == false && m_Mobile.Combatant.Alive &&
                    !m_Mobile.Combatant.IsDeadBondedPet)
                {
                    m_Mobile.Warmode = true;
                    m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.Combatant);
                }
                else
                {
                    m_Mobile.Warmode = false;
                }
            }
            else
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay($"My master told me to follow: {m_Mobile.ControlTarget.Name}");
                }

                // Not exactly OSI style, but better than nothing.
                var bRun = iCurrDist > 5;

                if (WalkMobileRange(m_Mobile.ControlTarget, 1, bRun, 0, 1))
                {
                    if (m_Mobile.Combatant?.Deleted == false && m_Mobile.Combatant.Alive &&
                        !m_Mobile.Combatant.IsDeadBondedPet)
                    {
                        m_Mobile.Warmode = true;
                        // m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.Combatant, bRun);
                    }
                    else
                    {
                        m_Mobile.Warmode = false;
                        if (Core.AOS)
                        {
                            m_Mobile.CurrentSpeed = 0.1;
                        }
                    }
                }
            }
        }
        else
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("I have nobody to follow");
            }

            m_Mobile.ControlTarget = null;
            m_Mobile.ControlOrder = OrderType.None;
        }

        return true;
    }

    public virtual bool DoOrderFriend()
    {
        var from = m_Mobile.ControlMaster;
        var to = m_Mobile.ControlTarget;

        if (from?.Deleted != false || to?.Deleted != false || from == to || !to.Player)
        {
            m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039); // *looks confused*
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
                    else if (m_Mobile.IsPetFriend(to))
                    {
                        from.SendLocalizedMessage(1049691); // That person is already a friend.
                    }
                    else if (!m_Mobile.AllowNewPetFriend)
                    {
                        from.SendLocalizedMessage(
                            1005482
                        ); // Your pet does not seem to be interested in making new friends right now.
                    }
                    else
                    {
                        // ~1_NAME~ will now accept movement commands from ~2_NAME~.
                        from.SendLocalizedMessage(1049676, $"{m_Mobile.Name}\t{to.Name}");

                        /* ~1_NAME~ has granted you the ability to give orders to their pet ~2_PET_NAME~.
                         * This creature will now consider you as a friend.
                         */
                        to.SendLocalizedMessage(1043246, $"{from.Name}\t{m_Mobile.Name}");

                        m_Mobile.AddPetFriend(to);

                        m_Mobile.ControlTarget = to;
                        m_Mobile.ControlOrder = OrderType.Follow;

                        return true;
                    }
                }
            }
        }

        m_Mobile.ControlTarget = from;
        m_Mobile.ControlOrder = OrderType.Follow;

        return true;
    }

    public virtual bool DoOrderUnfriend()
    {
        var from = m_Mobile.ControlMaster;
        var to = m_Mobile.ControlTarget;

        if (from?.Deleted != false || to?.Deleted != false || from == to || !to.Player)
        {
            m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039); // *looks confused*
        }
        else if (!m_Mobile.IsPetFriend(to))
        {
            from.SendLocalizedMessage(1070953); // That person is not a friend.
        }
        else
        {
            // ~1_NAME~ will no longer accept movement commands from ~2_NAME~.
            from.SendLocalizedMessage(1070951, $"{m_Mobile.Name}\t{to.Name}");

            /* ~1_NAME~ has no longer granted you the ability to give orders to their pet ~2_PET_NAME~.
             * This creature will no longer consider you as a friend.
             */
            to.SendLocalizedMessage(1070952, $"{from.Name}\t{m_Mobile.Name}");

            m_Mobile.RemovePetFriend(to);
        }

        m_Mobile.ControlTarget = from;
        m_Mobile.ControlOrder = OrderType.Follow;

        return true;
    }

    public virtual bool DoOrderGuard()
    {
        if (m_Mobile.IsDeadPet)
        {
            return true;
        }

        var controlMaster = m_Mobile.ControlMaster;

        if (controlMaster?.Deleted != false)
        {
            return true;
        }

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

            if (combatant != null && m_Mobile.Debug)
            {
                m_Mobile.DebugSay("Crap, my master has been attacked! I will attack one of those bastards!");
            }
        }

        if (combatant?.Deleted == false && combatant != m_Mobile && combatant != m_Mobile.ControlMaster &&
            combatant.Alive && !combatant.IsDeadBondedPet && m_Mobile.CanSee(combatant) &&
            m_Mobile.CanBeHarmful(combatant, false) && combatant.Map == m_Mobile.Map)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("Guarding from target...");
            }

            m_Mobile.Combatant = combatant;
            m_Mobile.FocusMob = combatant;
            Action = ActionType.Combat;

            /*
             * We need to call Think() here or spell casting monsters will not use
             * spells when guarding because their target is never processed.
             */
            Think();
        }
        else
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("Nothing to guard from");
            }

            m_Mobile.Warmode = false;
            if (Core.AOS)
            {
                m_Mobile.CurrentSpeed = 0.1;
            }

            WalkMobileRange(controlMaster, 1, false, 0, 1);
        }

        return true;
    }

    public virtual bool DoOrderAttack()
    {
        if (m_Mobile.IsDeadPet)
        {
            return true;
        }

        if (m_Mobile.ControlTarget?.Deleted != false || m_Mobile.ControlTarget.Map != m_Mobile.Map ||
            !m_Mobile.ControlTarget.Alive || m_Mobile.ControlTarget.IsDeadBondedPet)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay(
                    "I think he might be dead. He's not anywhere around here at least. That's cool. I'm glad he's dead."
                );
            }

            if (Core.AOS)
            {
                m_Mobile.ControlTarget = m_Mobile.ControlMaster;
                m_Mobile.ControlOrder = OrderType.Follow;
            }
            else
            {
                m_Mobile.ControlTarget = null;
                m_Mobile.ControlOrder = OrderType.None;
            }

            if (m_Mobile.FightMode is FightMode.Closest or FightMode.Aggressor)
            {
                Mobile newCombatant = null;
                var newScore = 0.0;

                foreach (var aggr in m_Mobile.GetMobilesInRange(m_Mobile.RangePerception))
                {
                    if (!m_Mobile.CanSee(aggr) || aggr.Combatant != m_Mobile)
                    {
                        continue;
                    }

                    if (aggr.IsDeadBondedPet || !aggr.Alive)
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
                    if (m_Mobile.Debug)
                    {
                        m_Mobile.DebugSay("But -that- is not dead. Here we go again...");
                    }

                    Think();
                }
            }
        }
        else
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("Attacking target...");
            }

            Think();
        }

        return true;
    }

    public virtual bool DoOrderPatrol()
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("This order is not yet coded");
        }

        return true;
    }

    public virtual bool DoOrderRelease()
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("I have been released");
        }

        m_Mobile.PlaySound(m_Mobile.GetAngerSound());

        m_Mobile.SetControlMaster(null);
        m_Mobile.SummonMaster = null;

        m_Mobile.BondingBegin = DateTime.MinValue;
        m_Mobile.OwnerAbandonTime = DateTime.MinValue;
        m_Mobile.IsBonded = false;

        var spawner = m_Mobile.Spawner;

        if (spawner != null && spawner.HomeLocation != Point3D.Zero)
        {
            m_Mobile.Home = spawner.HomeLocation;
            m_Mobile.RangeHome = spawner.HomeRange;
        }

        if (m_Mobile.DeleteOnRelease || m_Mobile.IsDeadPet)
        {
            m_Mobile.Delete();
        }

        m_Mobile.BeginDeleteTimer();
        m_Mobile.DropBackpack();

        return true;
    }

    public virtual bool DoOrderStay()
    {
        if (m_Mobile.Debug)
        {
            if (CheckHerding())
            {
                m_Mobile.DebugSay("Praise the shepherd!");
            }
            else
            {
                m_Mobile.DebugSay("My master told me to stay");
            }
        }

        return true;
    }

    public virtual bool DoOrderStop()
    {
        if (m_Mobile.ControlMaster?.Deleted != false)
        {
            return true;
        }

        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("My master told me to stop.");
        }

        m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.ControlMaster);
        m_Mobile.Home = m_Mobile.Location;

        m_Mobile.ControlTarget = null;

        if (Core.ML)
        {
            WalkRandomInHome(3, 2, 1);
        }
        else
        {
            m_Mobile.ControlOrder = OrderType.None;
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

        if (from?.Deleted == false && to?.Deleted == false && from != to && to.Player)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay($"Begin transfer with {to.Name}");
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
            else if (!m_Mobile.CanBeControlledBy(to))
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
            else if (!m_Mobile.CanBeControlledBy(from))
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
            else if (TransferItem.IsInCombat(m_Mobile))
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
                        Container c = fromState.AddTrade(toState);
                        c.DropItem(new TransferItem(m_Mobile));
                    }
                }
            }
        }

        m_Mobile.ControlTarget = null;
        m_Mobile.ControlOrder = OrderType.Stay;

        return true;
    }

    public virtual bool DoBardPacified()
    {
        if (Core.Now < m_Mobile.BardEndTime)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("I am pacified, I wait");
            }

            m_Mobile.Combatant = null;
            m_Mobile.Warmode = false;
        }
        else
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("I'm not pacified any longer");
            }

            m_Mobile.BardPacified = false;
        }

        return true;
    }

    public virtual bool DoBardProvoked()
    {
        if (Core.Now >= m_Mobile.BardEndTime &&
            (m_Mobile.BardMaster?.Deleted != false ||
             m_Mobile.BardMaster.Map != m_Mobile.Map || m_Mobile.GetDistanceToSqrt(m_Mobile.BardMaster) >
             m_Mobile.RangePerception))
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("I have lost my provoker");
            }

            m_Mobile.BardProvoked = false;
            m_Mobile.BardMaster = null;
            m_Mobile.BardTarget = null;

            m_Mobile.Combatant = null;
            m_Mobile.Warmode = false;
        }
        else
        {
            if (m_Mobile.BardTarget?.Deleted != false || m_Mobile.BardTarget.Map != m_Mobile.Map ||
                m_Mobile.GetDistanceToSqrt(m_Mobile.BardTarget) > m_Mobile.RangePerception)
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay("I have lost my provoke target");
                }

                m_Mobile.BardProvoked = false;
                m_Mobile.BardMaster = null;
                m_Mobile.BardTarget = null;

                m_Mobile.Combatant = null;
                m_Mobile.Warmode = false;
            }
            else
            {
                m_Mobile.Combatant = m_Mobile.BardTarget;
                m_Action = ActionType.Combat;

                m_Mobile.OnThink();
                Think();
            }
        }

        return true;
    }

    public virtual void WalkRandom(int iChanceToNotMove, int iChanceToDir, int iSteps)
    {
        if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves)
        {
            return;
        }

        for (var i = 0; i < iSteps; i++)
        {
            if (Utility.Random(8 * iChanceToNotMove) <= 8)
            {
                var iRndMove = Utility.Random(0, 8 + 9 * iChanceToDir);

                switch (iRndMove)
                {
                    case 0:
                        {
                            DoMove(Direction.Up);
                            break;
                        }
                    case 1:
                        {
                            DoMove(Direction.North);
                            break;
                        }
                    case 2:
                        {
                            DoMove(Direction.Left);
                            break;
                        }
                    case 3:
                        {
                            DoMove(Direction.West);
                            break;
                        }
                    case 5:
                        {
                            DoMove(Direction.Down);
                            break;
                        }
                    case 6:
                        {
                            DoMove(Direction.South);
                            break;
                        }
                    case 7:
                        {
                            DoMove(Direction.Right);
                            break;
                        }
                    case 8:
                        {
                            DoMove(Direction.East);
                            break;
                        }
                    default:
                        {
                            DoMove(m_Mobile.Direction);
                            break;
                        }
                }
            }
        }
    }

    public double TransformMoveDelay(double thinkingSpeed)
    {
        // Monster is passive
        if (m_Mobile is { Controlled: false, Summoned: false } && Math.Abs(thinkingSpeed - m_Mobile.PassiveSpeed) < 0.0001)
        {
            thinkingSpeed *= 3;
        }
        else // Movement speed is twice as slow as "thinking"
        {
            thinkingSpeed *= 2;
        }

        if (!m_Mobile.IsDeadPet && (m_Mobile.ReduceSpeedWithDamage || m_Mobile.IsSubdued))
        {
            int stats, statsMax;
            if (Core.HS)
            {
                stats = m_Mobile.Stam;
                statsMax = m_Mobile.StamMax;
            }
            else
            {
                stats = m_Mobile.Hits;
                statsMax = m_Mobile.HitsMax;
            }

            var offset = statsMax <= 0 ? 1.0 : Math.Max(0, stats) / (double)statsMax;

            if (offset < 1.0)
            {
                thinkingSpeed += m_Mobile.PassiveSpeed * (1.0 - offset);
            }
        }

        return thinkingSpeed;
    }

    public virtual bool CheckMove() => Core.TickCount - NextMove >= 0;

    public virtual bool DoMove(Direction d, bool badStateOk = false)
    {
        var res = DoMoveImpl(d);

        return res is MoveResult.Success or MoveResult.SuccessAutoTurn || badStateOk && res == MoveResult.BadState;
    }

    public virtual MoveResult DoMoveImpl(Direction d)
    {
        if (m_Mobile.Deleted || m_Mobile.Frozen || m_Mobile.Paralyzed ||
            m_Mobile.Spell?.IsCasting == true || m_Mobile.DisallowAllMoves)
        {
            return MoveResult.BadState;
        }

        if (!CheckMove())
        {
            return MoveResult.BadState;
        }

        // This makes them always move one step, never any direction changes
        // TODO: This is firing off deltas which aren't needed. Look into replacing/removing this
        m_Mobile.Direction = d;

        var delay = (int)(TransformMoveDelay(m_Mobile.CurrentSpeed) * 1000);
        NextMove += delay;

        if (Core.TickCount - NextMove > 0)
        {
            NextMove = Core.TickCount;
        }

        m_Mobile.Pushing = false;

        MoveImpl.IgnoreMovableImpassables = m_Mobile.CanMoveOverObstacles && !m_Mobile.CanDestroyObstacles;

        if ((m_Mobile.Direction & Direction.Mask) != (d & Direction.Mask))
        {
            var v = m_Mobile.Move(d);

            MoveImpl.IgnoreMovableImpassables = false;
            return v ? MoveResult.Success : MoveResult.Blocked;
        }

        if (m_Mobile.Move(d))
        {
            MoveImpl.IgnoreMovableImpassables = false;
            return MoveResult.Success;
        }

        var wasPushing = m_Mobile.Pushing;

        var blocked = true;

        var canOpenDoors = m_Mobile.CanOpenDoors;
        var canDestroyObstacles = m_Mobile.CanDestroyObstacles;

        if (canOpenDoors || canDestroyObstacles)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("My movement was blocked, I will try to clear some obstacles.");
            }

            var map = m_Mobile.Map;

            if (map != null)
            {
                int x = m_Mobile.X, y = m_Mobile.Y;
                Movement.Movement.Offset(d, ref x, ref y);

                var destroyables = 0;

                var eable = map.GetItemsInRange(new Point3D(x, y, m_Mobile.Location.Z), 1);

                foreach (var item in eable)
                {
                    if (canOpenDoors && item is BaseDoor door && door.Z + door.ItemData.Height > m_Mobile.Z &&
                        m_Mobile.Z + 16 > door.Z)
                    {
                        if (door.X != x || door.Y != y)
                        {
                            continue;
                        }

                        if (!door.Locked || !door.UseLocks())
                        {
                            m_Obstacles.Enqueue(door);
                        }

                        if (!canDestroyObstacles)
                        {
                            break;
                        }
                    }
                    else if (canDestroyObstacles && item.Movable && item.ItemData.Impassable &&
                             item.Z + item.ItemData.Height > m_Mobile.Z && m_Mobile.Z + 16 > item.Z)
                    {
                        if (!m_Mobile.InRange(item.GetWorldLocation(), 1))
                        {
                            continue;
                        }

                        m_Obstacles.Enqueue(item);
                        ++destroyables;
                    }
                }

                eable.Free();

                if (destroyables > 0)
                {
                    Effects.PlaySound(new Point3D(x, y, m_Mobile.Z), m_Mobile.Map, 0x3B3);
                }

                if (m_Obstacles.Count > 0)
                {
                    blocked = false; // retry movement
                }

                while (m_Obstacles.Count > 0)
                {
                    var item = m_Obstacles.Dequeue();

                    if (item is BaseDoor door)
                    {
                        if (m_Mobile.Debug)
                        {
                            m_Mobile.DebugSay(
                                "Little do they expect, I've learned how to open doors. Didn't they read the script??"
                            );
                        }

                        if (m_Mobile.Debug)
                        {
                            m_Mobile.DebugSay("*twist*");
                        }

                        door.Use(m_Mobile);
                    }
                    else
                    {
                        if (m_Mobile.Debug)
                        {
                            m_Mobile.DebugSay(
                                $"Ugabooga. I'm so big and tough I can destroy it: {item.GetType().Name}"
                            );
                        }

                        if (item is Container cont)
                        {
                            for (var i = 0; i < cont.Items.Count; ++i)
                            {
                                var check = cont.Items[i];

                                if (check.Movable && check.ItemData.Impassable &&
                                    cont.Z + check.ItemData.Height > m_Mobile.Z)
                                {
                                    m_Obstacles.Enqueue(check);
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
                    blocked = !m_Mobile.Move(d);
                }
            }
        }

        if (blocked)
        {
            var offset = Utility.RandomDouble() < 0.4 ? 1 : -1;

            for (var i = 0; i < 2; ++i)
            {
                m_Mobile.TurnInternal(offset);

                if (m_Mobile.Move(m_Mobile.Direction))
                {
                    MoveImpl.IgnoreMovableImpassables = false;
                    return MoveResult.SuccessAutoTurn;
                }
            }

            MoveImpl.IgnoreMovableImpassables = false;
            return wasPushing ? MoveResult.BadState : MoveResult.Blocked;
        }

        MoveImpl.IgnoreMovableImpassables = false;
        return MoveResult.Success;
    }

    public virtual void WalkRandomInHome(int iChanceToNotMove, int iChanceToDir, int iSteps)
    {
        if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves)
        {
            return;
        }

        if (m_Mobile.Home == Point3D.Zero)
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
                else
                {
                    if (region.GoLocation != Point3D.Zero && Utility.Random(10) > 5)
                    {
                        DoMove(m_Mobile.GetDirectionTo(region.GoLocation));
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
                if (m_Mobile.RangeHome != 0)
                {
                    var iCurrDist = (int)m_Mobile.GetDistanceToSqrt(m_Mobile.Home);

                    if (iCurrDist < m_Mobile.RangeHome * 2 / 3)
                    {
                        WalkRandom(iChanceToNotMove, iChanceToDir, 1);
                    }
                    else if (iCurrDist > m_Mobile.RangeHome)
                    {
                        DoMove(m_Mobile.GetDirectionTo(m_Mobile.Home));
                    }
                    else
                    {
                        if (Utility.Random(10) > 5)
                        {
                            DoMove(m_Mobile.GetDirectionTo(m_Mobile.Home));
                        }
                        else
                        {
                            WalkRandom(iChanceToNotMove, iChanceToDir, 1);
                        }
                    }
                }
                else
                {
                    if (m_Mobile.Location != m_Mobile.Home)
                    {
                        DoMove(m_Mobile.GetDirectionTo(m_Mobile.Home));
                    }
                }
            }
        }
    }

    public virtual bool CheckFlee()
    {
        if (m_Mobile.CheckFlee())
        {
            var combatant = m_Mobile.Combatant;

            if (combatant == null)
            {
                WalkRandom(1, 2, 1);
            }
            else
            {
                var d = combatant.GetDirectionTo(m_Mobile);

                d = (Direction)((int)d + Utility.RandomMinMax(-1, +1));

                m_Mobile.Direction = d;
                m_Mobile.Move(d);
            }

            return true;
        }

        return false;
    }

    public virtual void OnTeleported()
    {
        if (m_Path != null)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("Teleported; repathing");
            }

            m_Path.ForceRepath();
        }
    }

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

        if (m_Path?.Goal == m)
        {
            if (m_Path.Follow(run, 1))
            {
                m_Path = null;
                return true;
            }
        }
        else if (!DoMove(m_Mobile.GetDirectionTo(m, run), true))
        {
            m_Path = new PathFollower(m_Mobile, m) { Mover = DoMoveImpl };

            if (m_Path.Follow(run, 1))
            {
                m_Path = null;
                return true;
            }
        }
        else
        {
            m_Path = null;
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
    public virtual bool WalkMobileRange(Mobile m, int iSteps, bool bRun, int iWantDistMin, int iWantDistMax)
    {
        if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves)
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
            var iCurrDist = (int)m_Mobile.GetDistanceToSqrt(m);

            if (iCurrDist < iWantDistMin || iCurrDist > iWantDistMax)
            {
                var needCloser = iCurrDist > iWantDistMax;
                var needFurther = !needCloser;

                if (needCloser && m_Path != null && m_Path.Goal == m)
                {
                    if (m_Path.Follow(bRun, 1))
                    {
                        m_Path = null;
                    }
                }
                else
                {
                    var dirTo = iCurrDist > iWantDistMax ?
                        m_Mobile.GetDirectionTo(m, bRun) : m.GetDirectionTo(m_Mobile, bRun);

                    if (!DoMove(dirTo, true) && needCloser)
                    {
                        m_Path = new PathFollower(m_Mobile, m) { Mover = DoMoveImpl };

                        if (m_Path.Follow(bRun, 1))
                        {
                            m_Path = null;
                        }
                    }
                    else
                    {
                        m_Path = null;
                    }
                }
            }
            else
            {
                return true;
            }
        }

        // Get the current distance
        var iNewDist = (int)m_Mobile.GetDistanceToSqrt(m);

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
        if (m_Mobile.Deleted)
        {
            return false;
        }

        if (m_Mobile.BardProvoked)
        {
            if (m_Mobile.BardTarget?.Deleted != false)
            {
                m_Mobile.FocusMob = null;
                return false;
            }

            m_Mobile.FocusMob = m_Mobile.BardTarget;
            return m_Mobile.FocusMob != null;
        }

        if (m_Mobile.Controlled)
        {
            if (m_Mobile.ControlTarget?.Deleted != false || m_Mobile.ControlTarget.Hidden ||
                !m_Mobile.ControlTarget.Alive || m_Mobile.ControlTarget.IsDeadBondedPet ||
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
            return m_Mobile.FocusMob != null;
        }

        if (m_Mobile.ConstantFocus != null)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("Acquired my constant focus");
            }

            m_Mobile.FocusMob = m_Mobile.ConstantFocus;
            return true;
        }

        if (acqType == FightMode.None)
        {
            m_Mobile.FocusMob = null;
            return false;
        }

        if (acqType == FightMode.Aggressor && m_Mobile.Aggressors.Count == 0 && m_Mobile.Aggressed.Count == 0 &&
            m_Mobile.FactionAllegiance == null && m_Mobile.EthicAllegiance == null)
        {
            m_Mobile.FocusMob = null;
            return false;
        }

        if (Core.TickCount - m_Mobile.NextReacquireTime < 0)
        {
            m_Mobile.FocusMob = null;
            return false;
        }

        m_Mobile.NextReacquireTime = Core.TickCount + (int)m_Mobile.ReacquireDelay.TotalMilliseconds;

        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("Acquiring...");
        }

        var map = m_Mobile.Map;

        if (map == null)
        {
            // TODO: Is this correct? Maybe it should return false?
            return m_Mobile.FocusMob != null;
        }

        Mobile newFocusMob = null;
        var val = double.MinValue;
        Mobile enemySummonMob = null;
        var enemySummonVal = double.MinValue;

        var eable = map.GetMobilesInRange(m_Mobile.Location, iRange);

        foreach (var m in eable)
        {
            if (m.Deleted || m.Blessed)
            {
                continue;
            }

            // Let's not target ourselves...
            if (m == m_Mobile || m is BaseFamiliar)
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
            if (!m_Mobile.CanSee(m))
            {
                continue;
            }

            var bc = m as BaseCreature;
            var pm = m as PlayerMobile;

            // Monster don't attack it's own summon or the summon of another monster
            if (Core.AOS && bc != null && bc.Summoned && (bc.SummonMaster == m_Mobile || (!bc.SummonMaster.Player && IsHostile(bc.SummonMaster))))
            {
                continue;
            }

            if (m_Mobile.Summoned && m_Mobile.SummonMaster != null)
            {
                // If this is a summon, it can't target its controller.
                if (m == m_Mobile.SummonMaster)
                {
                    continue;
                }

                // It also must abide by harmful spell rules.
                if (!SpellHelper.ValidIndirectTarget(m_Mobile.SummonMaster, m))
                {
                    continue;
                }

                // Animated creatures cannot attack players directly.
                if (pm != null && m_Mobile.IsAnimatedDead)
                {
                    continue;
                }

                // Animated creatures cannot attack other animated creatures
                if (m_Mobile.IsAnimatedDead && bc?.IsAnimatedDead == true)
                {
                    continue;
                }

                // Animated creatures cannot attack pets of other players
                if (m_Mobile.IsAnimatedDead && bc?.Controlled == true)
                {
                    continue;
                }
            }

            // If we only want faction friends, make sure it's one.
            if (bFacFriend && !m_Mobile.IsFriend(m))
            {
                continue;
            }

            // Ignore anyone under EtherealVoyage
            if (TransformationSpellHelper.UnderTransformation(m, typeof(EtherealVoyageSpell)))
            {
                continue;
            }

            // Ignore players with activated honor
            if (pm?.HonorActive == true && m_Mobile.Combatant != m)
            {
                continue;
            }

            if (acqType is FightMode.Aggressor or FightMode.Evil)
            {
                var bValid = IsHostile(m);

                if (!bValid)
                {
                    bValid = m_Mobile.GetFactionAllegiance(m) == BaseCreature.Allegiance.Enemy ||
                             m_Mobile.GetEthicAllegiance(m) == BaseCreature.Allegiance.Enemy;
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
                if (bFacFoe && !m_Mobile.IsEnemy(m))
                {
                    continue;
                }

                // If it's an enemy factioned mobile, make sure we can be harmful to it.
                if (bFacFoe && !bFacFriend && !m_Mobile.CanBeHarmful(m, false))
                {
                    continue;
                }
            }

            var theirVal = m_Mobile.GetFightModeRanking(m, acqType, bPlayerOnly);
            if (theirVal > val && m_Mobile.InLOS(m))
            {
                newFocusMob = m;
                val = theirVal;
            }
            // The summon is targeted when nothing else around. Otherwise this monster enters idle mode.
            // Do a check for this edge case so players cannot abuse by casting EVs offscreen to kill an idle monster.
            else if (Core.AOS && theirVal > enemySummonVal && m_Mobile.InLOS(m) && bc?.Summoned == true && bc?.Controlled != true)
            {
                enemySummonMob = m;
                enemySummonVal = theirVal;
            }
        }

        eable.Free();

        m_Mobile.FocusMob = newFocusMob ?? enemySummonMob;
        return m_Mobile.FocusMob != null;
    }

    private bool IsHostile(Mobile from)
    {
        if (m_Mobile.Combatant == from || from.Combatant == m_Mobile)
        {
            return true;
        }

        var count = Math.Max(m_Mobile.Aggressors.Count, m_Mobile.Aggressed.Count);

        for (var a = 0; a < count; ++a)
        {
            if (a < m_Mobile.Aggressed.Count && m_Mobile.Aggressed[a].Attacker == from)
            {
                return true;
            }

            if (a < m_Mobile.Aggressors.Count && m_Mobile.Aggressors[a].Defender == from)
            {
                return true;
            }
        }

        return false;
    }

    public virtual void DetectHidden()
    {
        if (m_Mobile.Deleted || m_Mobile.Map == null)
        {
            return;
        }

        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("Checking for hidden players");
        }

        var srcSkill = m_Mobile.Skills.DetectHidden.Value;

        if (srcSkill <= 0)
        {
            return;
        }

        var eable = m_Mobile.GetMobilesInRange(m_Mobile.RangePerception);

        foreach (var trg in eable)
        {
            if (trg != m_Mobile && trg.Player && trg.Alive && trg.Hidden && trg.AccessLevel == AccessLevel.Player &&
                m_Mobile.InLOS(trg))
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay($"Trying to detect {trg.Name}");
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

        eable.Free();
    }

    public virtual void Deactivate()
    {
        if (m_Mobile.PlayerRangeSensitive)
        {
            m_Timer.Stop();

            var spawner = m_Mobile.Spawner;

            if (spawner?.ReturnOnDeactivate == true && !m_Mobile.Controlled && (
                    spawner.HomeLocation == Point3D.Zero && !m_Mobile.Region.AcceptsSpawnsFrom(spawner.Region) ||
                    !m_Mobile.InRange(spawner.HomeLocation, spawner.HomeRange)
                ))
            {
                Timer.StartTimer(ReturnToHome);
            }
        }
    }

    private void ReturnToHome()
    {
        if (m_Mobile.Spawner != null)
        {
            var loc = m_Mobile.Spawner.GetSpawnPosition(m_Mobile, m_Mobile.Spawner.Map);

            if (loc != Point3D.Zero)
            {
                m_Mobile.MoveToWorld(loc, m_Mobile.Spawner.Map);
            }
        }
    }

    public virtual void Activate()
    {
        if (!m_Timer.Running)
        {
            // We want to randomize the time at which the AI activates.
            // This triggers when a mob is first created since it moves from the internal map to it's added location
            // If we spawn lots of mobs, we don't want their AI synchronized exactly.
            m_Timer.Delay = TimeSpan.FromMilliseconds(Utility.Random(48) * 8);
            m_Timer.Start();
        }
    }

    /*
     *  The mobile changed speeds, we must adjust the timer
     */
    public virtual void OnCurrentSpeedChanged()
    {
        m_Timer.Interval = TimeSpan.FromSeconds(Math.Max(0.008, m_Mobile.CurrentSpeed));
    }

    private class InternalEntry : ContextMenuEntry
    {
        private readonly BaseAI m_AI;
        private readonly Mobile m_From;
        private readonly BaseCreature m_Mobile;
        private readonly OrderType m_Order;

        public InternalEntry(Mobile from, int number, int range, BaseCreature mobile, BaseAI ai, OrderType order)
            : base(number, range)
        {
            m_From = from;
            m_Mobile = mobile;
            m_AI = ai;
            m_Order = order;

            if (mobile.IsDeadPet && order is OrderType.Guard or OrderType.Attack or OrderType.Transfer or OrderType.Drop)
            {
                Enabled = false;
            }
        }

        public override void OnClick()
        {
            if (m_Mobile.Deleted || !m_Mobile.Controlled || !m_From.CheckAlive())
            {
                return;
            }

            if (m_Mobile.IsDeadPet && m_Order is OrderType.Guard or OrderType.Attack or OrderType.Transfer or OrderType.Drop)
            {
                return;
            }

            var isOwner = m_From == m_Mobile.ControlMaster;
            var isFriend = !isOwner && m_Mobile.IsPetFriend(m_From);

            if (!isOwner && !isFriend)
            {
                return;
            }

            if (isFriend && m_Order != OrderType.Follow && m_Order != OrderType.Stay && m_Order != OrderType.Stop)
            {
                return;
            }

            switch (m_Order)
            {
                case OrderType.Follow:
                case OrderType.Attack:
                case OrderType.Transfer:
                case OrderType.Friend:
                case OrderType.Unfriend:
                    {
                        if (m_Order == OrderType.Transfer && m_From.HasTrade)
                        {
                            m_From.SendLocalizedMessage(1010507); // You cannot transfer a pet with a trade pending
                        }
                        else if (m_Order == OrderType.Friend && m_From.HasTrade)
                        {
                            m_From.SendLocalizedMessage(1070947); // You cannot friend a pet with a trade pending
                        }
                        else
                        {
                            m_AI.BeginPickTarget(m_From, m_Order);
                        }

                        break;
                    }
                case OrderType.Release:
                    {
                        if (m_Mobile.Summoned)
                        {
                            goto default;
                        }

                        m_From.SendGump(new ConfirmReleaseGump(m_From, m_Mobile));

                        break;
                    }
                default:
                    {
                        if (m_Mobile.CheckControlChance(m_From))
                        {
                            m_Mobile.ControlOrder = m_Order;
                        }

                        break;
                    }
            }
        }
    }

    private class TransferItem : Item
    {
        private readonly BaseCreature m_Creature;

        public TransferItem(BaseCreature creature)
            : base(ShrinkTable.Lookup(creature))
        {
            m_Creature = creature;

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
            list.Add(1041601, m_Creature.Name); // Pet Name: ~1_val~

            if (m_Creature.ControlMaster != null)
            {
                list.Add(1041602, m_Creature.ControlMaster.Name); // Owner: ~1_val~
            }
        }

        public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
            if (!base.AllowSecureTrade(from, to, newOwner, accepted))
            {
                return false;
            }

            if (Deleted || m_Creature?.Deleted != false || m_Creature.ControlMaster != from ||
                !from.CheckAlive() || !to.CheckAlive())
            {
                return false;
            }

            if (from.Map != m_Creature.Map || !from.InRange(m_Creature, 14))
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
            else if (accepted && !m_Creature.CanBeControlledBy(to))
            {
                var args = $"{to.Name}\t{from.Name}\t ";

                // The pet refuses to be transferred because it will not obey ~1_NAME~.~3_BLANK~
                from.SendLocalizedMessage(1043248, args);
                // The pet will not accept you as a master because it does not trust you.~3_BLANK~
                to.SendLocalizedMessage(1043249, args);

                return false;
            }
            else if (accepted && !m_Creature.CanBeControlledBy(from))
            {
                var args = $"{to.Name}\t{from.Name}\t ";

                // The pet refuses to be transferred because it will not obey you sufficiently.~3_BLANK~
                from.SendLocalizedMessage(1043250, args);
                // The pet will not accept you as a master because it does not trust ~2_NAME~.~3_BLANK~
                to.SendLocalizedMessage(1043251, args);
            }
            else if (accepted && to.Followers + m_Creature.ControlSlots > to.FollowersMax)
            {
                to.SendLocalizedMessage(1049607); // You have too many followers to control that creature.

                return false;
            }
            else if (accepted && IsInCombat(m_Creature))
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

            if (m_Creature?.Deleted != false || m_Creature.ControlMaster != from || !from.CheckAlive() ||
                !to.CheckAlive())
            {
                return;
            }

            if (from.Map != m_Creature.Map || !from.InRange(m_Creature, 14))
            {
                return;
            }

            if (accepted && m_Creature.SetControlMaster(to))
            {
                if (m_Creature.Summoned)
                {
                    m_Creature.SummonMaster = to;
                }

                m_Creature.ControlTarget = to;
                m_Creature.ControlOrder = OrderType.Follow;

                m_Creature.BondingBegin = DateTime.MinValue;
                m_Creature.OwnerAbandonTime = DateTime.MinValue;
                m_Creature.IsBonded = false;

                m_Creature.PlaySound(m_Creature.GetIdleSound());

                var args = $"{from.Name}\t{m_Creature.Name}\t{to.Name}";

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
        private readonly BaseAI m_Owner;

        public AITimer(BaseAI owner) : base(
            TimeSpan.FromMilliseconds(Utility.Random(96) * 8),
            TimeSpan.FromSeconds(Math.Max(0.0, owner.m_Mobile.CurrentSpeed))
        )
        {
            m_Owner = owner;
            m_Owner.m_NextDetectHidden = Core.TickCount;
        }

        protected override void OnTick()
        {
            if (m_Owner.m_Mobile.Deleted)
            {
                Stop();
                return;
            }

            if (m_Owner.m_Mobile.Map == null || m_Owner.m_Mobile.Map == Map.Internal)
            {
                m_Owner.Deactivate();
                return;
            }

            if (m_Owner.m_Mobile.PlayerRangeSensitive) // have to check this in the timer....
            {
                var sect = m_Owner.m_Mobile.Map.GetSector(m_Owner.m_Mobile.Location);
                if (!sect.Active)
                {
                    m_Owner.Deactivate();
                    return;
                }
            }

            m_Owner.m_Mobile.OnThink();

            if (m_Owner.m_Mobile.Deleted)
            {
                Stop();
                return;
            }

            if (m_Owner.m_Mobile.Map == null || m_Owner.m_Mobile.Map == Map.Internal)
            {
                m_Owner.Deactivate();
                return;
            }

            if (m_Owner.m_Mobile.BardPacified)
            {
                m_Owner.DoBardPacified();
            }
            else if (m_Owner.m_Mobile.BardProvoked)
            {
                m_Owner.DoBardProvoked();
            }
            else if (!m_Owner.m_Mobile.Controlled)
            {
                if (!m_Owner.Think())
                {
                    Stop();
                    return;
                }
            }
            else if (!m_Owner.Obey())
            {
                Stop();
                return;
            }

            if (m_Owner.CanDetectHidden && Core.TickCount - m_Owner.m_NextDetectHidden >= 0)
            {
                m_Owner.DetectHidden();

                // Not exactly OSI style, approximation.
                var delay = Math.Min(15000 / m_Owner.m_Mobile.Int, 60);

                var min = delay * 900; // 13s at 1000 int, 33s at 400 int, 54s at <250 int
                var max = delay * 1100; // 16s at 1000 int, 41s at 400 int, 66s at <250 int

                m_Owner.m_NextDetectHidden = Core.TickCount + Utility.RandomMinMax(min, max);
            }
        }
    }
}
