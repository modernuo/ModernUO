/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EventSink.cs                                                    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using Server.Network;

namespace Server;

public static partial class EventSink
{
    public static event Action<Mobile> OpenDoorMacroUsed;
    public static void InvokeOpenDoorMacroUsed(Mobile m) => OpenDoorMacroUsed?.Invoke(m);

    public static event Action<Mobile> Login;
    public static void InvokeLogin(Mobile m) => Login?.Invoke(m);

    public static event Action<Mobile, int> HungerChanged;
    public static void InvokeHungerChanged(Mobile mobile, int oldValue) => HungerChanged?.Invoke(mobile, oldValue);

    public static event Action Shutdown;
    public static void InvokeShutdown() => Shutdown?.Invoke();

    public static event Action<Mobile> HelpRequest;
    public static void InvokeHelpRequest(Mobile m) => HelpRequest?.Invoke(m);

    public static event Action<Mobile> DisarmRequest;
    public static void InvokeDisarmRequest(Mobile m) => DisarmRequest?.Invoke(m);

    public static event Action<Mobile> StunRequest;
    public static void InvokeStunRequest(Mobile m) => StunRequest?.Invoke(m);

    public static event Action<Mobile, int> OpenSpellbookRequest;
    public static void InvokeOpenSpellbookRequest(Mobile m, int type) => OpenSpellbookRequest?.Invoke(m, type);

    public static event Action<Mobile, int, Item> CastSpellRequest;

    public static void InvokeCastSpellRequest(Mobile m, int spellID, Item book) =>
        CastSpellRequest?.Invoke(m, spellID, book);

    public static event Action<Mobile, Item, Mobile> BandageTargetRequest;

    public static void InvokeBandageTargetRequest(Mobile m, Item bandage, Mobile target) =>
        BandageTargetRequest?.Invoke(m, bandage, target);

    public static event Action<Mobile, string> AnimateRequest;
    public static void InvokeAnimateRequest(Mobile m, string action) => AnimateRequest?.Invoke(m, action);

    public static event Action<Mobile> Logout;
    public static void InvokeLogout(Mobile m) => Logout?.Invoke(m);

    public static event Action<Mobile> Connected;
    public static void InvokeConnected(Mobile m) => Connected?.Invoke(m);

    public static event Action<Mobile> BeforeDisconnected;
    public static void InvokeBeforeDisconnected(Mobile m) => BeforeDisconnected?.Invoke(m);

    public static event Action<Mobile> Disconnected;
    public static void InvokeDisconnected(Mobile m) => Disconnected?.Invoke(m);

    public static event Action<Mobile, Mobile, string> RenameRequest;

    public static void InvokeRenameRequest(Mobile from, Mobile target, string name) =>
        RenameRequest?.Invoke(from, target, name);

    public static event Action<Mobile> PlayerDeath;
    public static void InvokePlayerDeath(Mobile m) => PlayerDeath?.Invoke(m);

    public static event Action<Mobile, Mobile> VirtueGumpRequest;

    public static void InvokeVirtueGumpRequest(Mobile beholder, Mobile beheld) =>
        VirtueGumpRequest?.Invoke(beholder, beheld);

    public static event Action<Mobile, Mobile, int> VirtueItemRequest;

    public static void InvokeVirtueItemRequest(Mobile beholder, Mobile beheld, int gumpID) =>
        VirtueItemRequest?.Invoke(beholder, beheld, gumpID);

    public static event Action<Mobile, int> VirtueMacroRequest;

    public static void InvokeVirtueMacroRequest(Mobile mobile, int virtueID) =>
        VirtueMacroRequest?.Invoke(mobile, virtueID);

    public static event Action<Mobile, Mobile> PaperdollRequest;

    public static void InvokePaperdollRequest(Mobile beholder, Mobile beheld) =>
        PaperdollRequest?.Invoke(beholder, beheld);

    public static event Action<Mobile, Mobile> ProfileRequest;
    public static void InvokeProfileRequest(Mobile beholder, Mobile beheld) => ProfileRequest?.Invoke(beholder, beheld);

    public static event Action<Mobile, Mobile, string> ChangeProfileRequest;

    public static void InvokeChangeProfileRequest(Mobile beholder, Mobile beheld, string text) =>
        ChangeProfileRequest?.Invoke(beholder, beheld, text);

    public static event Action<NetState, int> DeleteRequest;
    public static void InvokeDeleteRequest(NetState state, int index) => DeleteRequest?.Invoke(state, index);

    public static event Action ServerStarted;
    public static void InvokeServerStarted() => ServerStarted?.Invoke();

    public static event Action<Mobile> GuildGumpRequest;
    public static void InvokeGuildGumpRequest(Mobile m) => GuildGumpRequest?.Invoke(m);

    public static event Action<Mobile> QuestGumpRequest;
    public static void InvokeQuestGumpRequest(Mobile m) => QuestGumpRequest?.Invoke(m);

    public static event Action<NetState, ClientVersion> ClientVersionReceived;

    public static void InvokeClientVersionReceived(NetState state, ClientVersion cv) =>
        ClientVersionReceived?.Invoke(state, cv);

    public static event Action<Mobile, List<Serial>> EquipMacro;

    public static void InvokeEquipMacro(Mobile m, List<Serial> list)
    {
        if (list?.Count > 0)
        {
            EquipMacro?.Invoke(m, list);
        }
    }

    public static event Action<Mobile, List<Layer>> UnequipMacro;

    public static void InvokeUnequipMacro(Mobile m, List<Layer> layers)
    {
        if (layers?.Count > 0)
        {
            UnequipMacro?.Invoke(m, layers);
        }
    }

    public static event Action<Mobile, IEntity, int> TargetedSpell;

    public static void InvokeTargetedSpell(Mobile m, IEntity target, int spellId) =>
        TargetedSpell?.Invoke(m, target, spellId);

    public static event Action<Mobile, IEntity, int> TargetedSkillUse;

    public static void InvokeTargetedSkillUse(Mobile m, IEntity target, int skillId) =>
        TargetedSkillUse?.Invoke(m, target, skillId);

    public static event Action<Mobile, Item, short> TargetByResourceMacro;

    public static void InvokeTargetByResourceMacro(Mobile m, Item item, short resourceType) =>
        TargetByResourceMacro?.Invoke(m, item, resourceType);
}
