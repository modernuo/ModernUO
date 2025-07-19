/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OnSpeech.cs                                                     *
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
using Server.Items;
using Server.Gumps;

namespace Server.Mobiles;

public abstract partial class BaseAI
{
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
        if (WasNamed(e.Speech) && e.Mobile.Alive &&
            e.Mobile.InRange(Mobile.Location, 3) && Mobile.IsHumanInTown())
        {
            if (HandleMoveCommand(e) || HandleTimeCommand(e) || HandleTrainCommand(e))
            {
                return;
            }
        }

        if (Mobile.Controlled && Mobile.Commandable)
        {
            AllOnSpeechPet(e);
            NamedOnSpeechPet(e);
            return;
        }

        if (e.Mobile.AccessLevel >= AccessLevel.GameMaster)
        {
            HandleGMCommands(e);
        }
    }

    private bool HandleMoveCommand(SpeechEventArgs e)
    {
        if (!e.HasKeyword(0x9D)) // *move*
        {
            return false;
        }

        if ((Core.Now - _lastOrder).TotalSeconds < 5)
        {
            return true;
        }

        _lastOrder = Core.Now;

        var map = Mobile.Map;
        var currentLoc = Mobile.Location;

        var newX = currentLoc.X + Utility.RandomMinMax(-1, 1);
        var newY = currentLoc.Y + Utility.RandomMinMax(-1, 1);
        var newZ = currentLoc.Z;

        if (map != null && map.CanFit(newX, newY, newZ, 16, false, false))
        {
            Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501516); // Excuse me?
            Mobile.Location = new Point3D(newX, newY, newZ);
        }
        else
        {
            Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501487);
            // You're standing too close, go away.
        }

        return true;
    }

    private bool HandleTimeCommand(SpeechEventArgs e)
    {
        if (!e.HasKeyword(0x9E)) // *time*
        {
            return false;
        }

        if ((Core.Now - _lastOrder).TotalSeconds < 5)
        {
            return true;
        }

        _lastOrder = Core.Now;

        Clock.GetTime(Mobile, out var generalNumber, out _);
        Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, generalNumber);

        return true;
    }

    private bool HandleTrainCommand(SpeechEventArgs e)
    {
        if (!e.HasKeyword(0x6C)) // *train*
        {
            return false;
        }

        HandleTraining(e.Mobile);
        return true;
    }

    public virtual void AllOnSpeechPet(SpeechEventArgs e)
    {
        if (!e.Mobile.InRange(Mobile.Location, 14))
        {
            return;
        }

        var isOwner = e.Mobile == Mobile.ControlMaster;
        var isPetFriend = !isOwner && Mobile.IsPetFriend(e.Mobile);

        if (!isOwner && !isPetFriend)
        {
            return;
        }

        var keyword = e.GetFirstKeyword(
            0x164, // all come
            0x165, // all follow
            0x166, // all guard
            0x167, // all stop
            0x168, // all kill
            0x169, // all attack
            0x16B, // all guard me
            0x16C, // all follow me
            0x170  // all stay
        );

        switch (keyword)
        {
            case 0x164: // all come
                {
                    HandleComeCommand(e.Mobile, true);
                    break;
                }
            case 0x165: // all follow
                {
                    BeginPickTarget(e.Mobile, OrderType.Follow);
                    break;
                }
            case 0x166: // all guard
            case 0x16B: // all guard me
                {
                    HandleGuardCommand(e.Mobile, true);
                    break;
                }
            case 0x167: // all stop
                {
                    HandleStayStopFollowCommand(e.Mobile, OrderType.Stop);
                    break;
                }
            case 0x168: // all kill
            case 0x169: // all attack
                {
                    HandleAttackCommand(e.Mobile, true);
                    break;
                }
            case 0x16C: // all follow me
                {
                    HandleStayStopFollowCommand(e.Mobile, OrderType.Follow, e.Mobile);
                    break;
                }
            case 0x170: // all stay
                {
                    HandleStayStopFollowCommand(e.Mobile, OrderType.Stay);
                    break;
                }
        }
    }

    public virtual void NamedOnSpeechPet(SpeechEventArgs e)
    {
        if (!e.Mobile.InRange(Mobile.Location, 14))
        {
            return;
        }

        var isOwner = e.Mobile == Mobile.ControlMaster;
        var isPetFriend = !isOwner && Mobile.IsPetFriend(e.Mobile);

        if (!isOwner && !isPetFriend)
        {
            return;
        }

        var keyword = e.GetFirstKeyword(
            0x155, // *come
            0x156, // *drop
            0x15A, // *follow
            0x15B, // *friend
            0x15C, // *guard
            0x15D, // *kill
            0x15E, // *attack
            0x161, // *stop
            0x163, // *follow me
            0x16D, // *release
            0x16E, // *transfer
            0x16F  // *stay
        );

        switch (keyword)
        {
            case 0x155: // *come
                {
                    HandleComeCommand(e.Mobile, true);
                    break;
                }
            case 0x156: // *drop
                {
                    HandleDropCommand(e.Mobile, true, e.Speech);
                    break;
                }
            case 0x15A: // *follow
                {
                    BeginPickTarget(e.Mobile, OrderType.Follow);
                    break;
                }
            case 0x15B: // *friend
                {
                    HandleFriendCommand(e.Mobile, true, e.Speech);
                    break;
                }
            case 0x15C: // *guard
                {
                    HandleGuardCommand(e.Mobile, true);
                    break;
                }
            case 0x15D: // *kill
            case 0x15E: // *attack
                {
                    HandleAttackCommand(e.Mobile, true);
                    break;
                }
            case 0x161: // *stop
                {
                    HandleStayStopFollowCommand(e.Mobile, OrderType.Stop);
                    break;
                }
            case 0x163: // *follow me
                {
                    HandleStayStopFollowCommand(e.Mobile, OrderType.Follow, e.Mobile);
                    break;
                }
            case 0x16D: // *release
                {
                    HandleReleaseCommand(e.Mobile, true, e.Speech);
                    break;
                }
            case 0x16E: // *transfer
                {
                    HandleTransferCommand(e.Mobile, true, e.Speech);
                    break;
                }
            case 0x16F: // *stay
                {
                    HandleStayStopFollowCommand(e.Mobile, OrderType.Stay);
                    break;
                }
        }
    }

    private void HandleTraining(Mobile from)
    {
        var foundSomething = false;

        foreach (var skill in Mobile.Skills)
        {
            if (skill.Base < 60.0 || !Mobile.CheckTeach(skill.SkillName, from))
            {
                continue;
            }

            var toTeach = Math.Min(skill.Base / 3.0, 42.0);

            if (toTeach <= from.Skills[skill.SkillName].Base)
            {
                continue;
            }

            var number = 1043059 + (int)skill.SkillName; // alchemy
            if (number > 1043107)                        // disarming traps
            {
                continue;
            }

            if (!foundSomething)
            {
                Mobile.Say(1043058); // I can train the following:
                foundSomething = true;
            }

            Mobile.Say(number);
        }

        if (!foundSomething)
        {
            Mobile.Say(501505); // Alas, I cannot teach thee anything.
        }
    }

    private void HandleComeCommand(Mobile from, bool isOwner)
    {
        if (isOwner && Mobile.CheckControlChance(from))
        {
            _commandIssuer = from;
            Mobile.ControlTarget = null;
            Mobile.ControlOrder = OrderType.Come;
        }
    }

    private void HandleGuardCommand(Mobile from, bool isOwner)
    {
        if (isOwner && Mobile.CheckControlChance(from))
        {
            _commandIssuer = from;
            Mobile.ControlTarget = null;
            Mobile.ControlOrder = OrderType.Guard;
        }
    }

    private void HandleStayStopFollowCommand(Mobile from, OrderType order, Mobile target = null)
    {
        if (Mobile.CheckControlChance(from))
        {
            _commandIssuer = from;
            Mobile.ControlTarget = target;
            Mobile.ControlOrder = order;
        }
    }

    private void HandleAttackCommand(Mobile from, bool isOwner)
    {
        if (isOwner)
        {
            _commandIssuer = from;
            BeginPickTarget(from, OrderType.Attack);
        }
    }

    private void HandleDropCommand(Mobile from, bool isOwner, string speech)
    {
        if (isOwner && !Mobile.IsDeadPet && !Mobile.Summoned && WasNamed(speech)
            && Mobile.CheckControlChance(from))
        {
            _commandIssuer = from;
            Mobile.ControlTarget = null;
            Mobile.ControlOrder = OrderType.Drop;
        }
    }

    private void HandleFriendCommand(Mobile from, bool isOwner, string speech)
    {
        if (isOwner && WasNamed(speech) && Mobile.CheckControlChance(from))
        {
            if (Mobile.Summoned || Mobile is GrizzledMare)
            {
                from.SendLocalizedMessage(1005481);
                // Summoned creatures are loyal only to their summoners.
                return;
            }

            if (from.HasTrade)
            {
                from.SendLocalizedMessage(1070947);
                // You cannot friend a pet with a trade pending
                return;
            }

            BeginPickTarget(from, OrderType.Friend);
        }
    }

    private void HandleReleaseCommand(Mobile from, bool isOwner, string speech)
    {
        if (!isOwner)
        {
            return;
        }

        if (WasNamed(speech) && Mobile.CheckControlChance(from))
        {
            if (!Mobile.Summoned)
            {
                from.SendGump(new ConfirmReleaseGump(from, Mobile));
            }
            else
            {
                Mobile.ControlOrder = OrderType.Release;
            }
        }
    }

    private void HandleTransferCommand(Mobile from, bool isOwner, string speech)
    {
        if (isOwner && !Mobile.IsDeadPet && WasNamed(speech) && Mobile.CheckControlChance(from))
        {
            if (Mobile.Summoned || Mobile is GrizzledMare)
            {
                from.SendLocalizedMessage(1005487);
                // You cannot transfer ownership of a summoned creature.
                return;
            }

            if (from.HasTrade)
            {
                from.SendLocalizedMessage(1010507);
                // You cannot transfer a pet with a trade pending
                return;
            }

            BeginPickTarget(from, OrderType.Transfer);
        }
    }

    private void HandleGMCommands(SpeechEventArgs e)
    {
        this.DebugSayFormatted($"Command is from GM: {e.Mobile.Name}, Target: {Mobile.ControlTarget?.Name ?? "None or Unknown"}");

        if (Mobile.FindMyName(e.Speech, true) && e.Speech.InsensitiveContains("obey"))
        {
            Mobile.SetControlMaster(e.Mobile);

            if (Mobile.Summoned)
            {
                Mobile.SummonMaster = e.Mobile;
            }
        }
    }
}
