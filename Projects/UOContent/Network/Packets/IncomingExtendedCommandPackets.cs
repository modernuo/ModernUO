/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IncomingExtendedCommandPackets.cs                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Buffers;
using System.Runtime.CompilerServices;
using ModernUO.CodeGeneratedEvents;
using Server.Items;
using Server.Mobiles;

namespace Server.Network;

public static partial class IncomingExtendedCommandPackets
{
    private static readonly PacketHandler[] _extendedHandlers = new PacketHandler[0x100];

    // TODO: Change to outside configuration
    public static int[] ValidAnimations { get; } =
    {
        6, 21, 32, 33,
        100, 101, 102, 103,
        104, 105, 106, 107,
        108, 109, 110, 111,
        112, 113, 114, 115,
        116, 117, 118, 119,
        120, 121, 123, 124,
        125, 126, 127, 128
    };

    public static unsafe void Configure()
    {
        IncomingPackets.Register(0xBF, 0, true, &ExtendedCommand);

        RegisterExtended(0x05, false, &ScreenSize);
        RegisterExtended(0x06, true, &PartyMessage);
        RegisterExtended(0x09, true, &DisarmRequest);
        RegisterExtended(0x0A, true, &StunRequest);
        RegisterExtended(0x0B, false, &Language);
        RegisterExtended(0x0C, true, &CloseStatus);
        RegisterExtended(0x0E, true, &Animate);
        RegisterExtended(0x0F, false, &Empty); // What's this?
        RegisterExtended(0x10, true, &QueryProperties);
        RegisterExtended(0x1A, true, &StatLockChange);
        RegisterExtended(0x1C, true, &CastSpell);
        RegisterExtended(0x24, false, &UnhandledBF);
        RegisterExtended(0x2C, true, &BandageTarget);
        RegisterExtended(0x2D, true, &TargetedSpell);
        RegisterExtended(0x2E, true, &TargetedSkillUse);
        RegisterExtended(0x30, true, &TargetByResourceMacro);
        RegisterExtended(0x32, true, &ToggleFlying);
    }

    private static void UnhandledBF(NetState state, SpanReader reader)
    {
    }

    public static void Empty(NetState state, SpanReader reader)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void RegisterExtended(
        int packetID, bool ingame, delegate*<NetState, SpanReader, void> onReceive
    ) => RegisterExtended(packetID, ingame, false, onReceive);

    public static unsafe void RegisterExtended(
        int packetID, bool ingame, bool outgame, delegate*<NetState, SpanReader, void> onReceive
    )
    {
        if (packetID is >= 0 and < 0x100)
        {
            _extendedHandlers[packetID] = new PacketHandler(packetID, onReceive, inGameOnly: ingame, outGameOnly: outgame);
        }
    }

    public static PacketHandler GetExtendedHandler(int packetID) =>
        packetID is >= 0 and < 0x100 ? _extendedHandlers[packetID] : null;

    public static void RemoveExtendedHandler(int packetID)
    {
        if (packetID is >= 0 and < 0x100)
        {
            _extendedHandlers[packetID] = null;
        }
    }

    public static unsafe void ExtendedCommand(NetState state, SpanReader reader)
    {
        int packetId = reader.ReadUInt16();

        var ph = GetExtendedHandler(packetId);

        if (ph == null)
        {
            state.Trace(reader.Buffer);
            return;
        }

        var from = state.Mobile;

        if (ph.InGameOnly)
        {
            if (from == null)
            {
                state.Disconnect($"Received packet 0x{packetId:X2} before having been attached to a mobile.");
                return;
            }

            if (from.Deleted)
            {
                state.Disconnect($"Received packet 0x{packetId:X2} after having been attached to a deleted mobile.");
                return;
            }
        }

        if (ph.OutOfGameOnly && from?.Deleted == false)
        {
            state.Disconnect($"Received packet 0x{packetId:X2} after having been attached to a mobile.");
            return;
        }

        ph.OnReceive(state, reader);
    }

    public static void ScreenSize(NetState state, SpanReader reader)
    {
        var width = reader.ReadInt32();
        var unk = reader.ReadInt32();
    }

    public static void PartyMessage(NetState state, SpanReader reader)
    {
        if (state.Mobile == null)
        {
            return;
        }

        switch (reader.ReadByte())
        {
            case 0x01:
                {
                    PartyMessage_AddMember(state, reader);
                    break;
                }
            case 0x02:
                {
                    PartyMessage_RemoveMember(state, reader);
                    break;
                }
            case 0x03:
                {
                    PartyMessage_PrivateMessage(state, reader);
                    break;
                }
            case 0x04:
                {
                    PartyMessage_PublicMessage(state, reader);
                    break;
                }
            case 0x06:
                {
                    PartyMessage_SetCanLoot(state, reader);
                    break;
                }
            case 0x08:
                {
                    PartyMessage_Accept(state, reader);
                    break;
                }
            case 0x09:
                {
                    PartyMessage_Decline(state, reader);
                    break;
                }
            default:
                {
                    state.Trace(reader.Buffer);
                    break;
                }
        }
    }

    public static void PartyMessage_AddMember(NetState state, SpanReader reader)
    {
        PartyCommands.Handler?.OnAdd(state.Mobile);
    }

    public static void PartyMessage_RemoveMember(NetState state, SpanReader reader)
    {
        PartyCommands.Handler?.OnRemove(state.Mobile, World.FindMobile((Serial)reader.ReadUInt32()));
    }

    public static void PartyMessage_PrivateMessage(NetState state, SpanReader reader)
    {
        PartyCommands.Handler?.OnPrivateMessage(
            state.Mobile,
            World.FindMobile((Serial)reader.ReadUInt32()),
            reader.ReadBigUniSafe()
        );
    }

    public static void PartyMessage_PublicMessage(NetState state, SpanReader reader)
    {
        PartyCommands.Handler?.OnPublicMessage(state.Mobile, reader.ReadBigUniSafe());
    }

    public static void PartyMessage_SetCanLoot(NetState state, SpanReader reader)
    {
        PartyCommands.Handler?.OnSetCanLoot(state.Mobile, reader.ReadBoolean());
    }

    public static void PartyMessage_Accept(NetState state, SpanReader reader)
    {
        PartyCommands.Handler?.OnAccept(state.Mobile, World.FindMobile((Serial)reader.ReadUInt32()));
    }

    public static void PartyMessage_Decline(NetState state, SpanReader reader)
    {
        PartyCommands.Handler?.OnDecline(state.Mobile, World.FindMobile((Serial)reader.ReadUInt32()));
    }

    public static void Animate(NetState state, SpanReader reader)
    {
        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        var action = reader.ReadInt32();

        var ok = false;

        for (var i = 0; !ok && i < ValidAnimations.Length; ++i)
        {
            ok = action == ValidAnimations[i];
        }

        if (ok && from.Alive && from.Body.IsHuman && !from.Mounted)
        {
            from.Animate(action, 7, 1, true, false, 0);
        }
    }

    public static void CastSpell(NetState state, SpanReader reader)
    {
        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        Item spellbook = reader.ReadInt16() == 1 ? World.FindItem((Serial)reader.ReadUInt32()) : null;

        var spellID = reader.ReadInt16() - 1;
        Spellbook.CastSpellRequest(from, spellID, spellbook);
    }

    public static void ToggleFlying(NetState state, SpanReader reader)
    {
        state.Mobile?.ToggleFlying();
    }

    public static void StunRequest(NetState state, SpanReader reader)
    {
        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        Fists.StunRequest(from);
    }

    public static void DisarmRequest(NetState state, SpanReader reader)
    {
        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        Fists.DisarmRequest(from);
    }

    public static void StatLockChange(NetState state, SpanReader reader)
    {
        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        int stat = reader.ReadByte();
        int lockValue = reader.ReadByte();

        if (lockValue > 2)
        {
            lockValue = 0;
        }

        switch (stat)
        {
            case 0:
                {
                    from.StrLock = (StatLockType)lockValue;
                    break;
                }
            case 1:
                {
                    from.DexLock = (StatLockType)lockValue;
                    break;
                }
            case 2:
                {
                    from.IntLock = (StatLockType)lockValue;
                    break;
                }
        }
    }

    public static void CloseStatus(NetState state, SpanReader reader)
    {
        var serial = (Serial)reader.ReadUInt32();
    }

    public static void Language(NetState state, SpanReader reader)
    {
        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        from.Language = reader.ReadAscii(4);
    }

    public static void QueryProperties(NetState state, SpanReader reader)
    {
        if (!ObjectPropertyList.Enabled)
        {
            return;
        }

        var from = state.Mobile;

        Serial s = (Serial)reader.ReadUInt32();

        if (s.IsMobile)
        {
            var m = World.FindMobile(s);

            if (m != null && from.CanSee(m) && Utility.InUpdateRange(from.Location, m.Location))
            {
                m.SendPropertiesTo(state);
            }
        }
        else if (s.IsItem)
        {
            var item = World.FindItem(s);

            if (item?.Deleted == false && from.CanSee(item) &&
                Utility.InUpdateRange(from.Location, item.GetWorldLocation()))
            {
                item.SendPropertiesTo(state);
            }
        }
    }

    public static void BandageTarget(NetState state, SpanReader reader)
    {
        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        if (from.AccessLevel >= AccessLevel.Counselor || Core.TickCount - from.NextActionTime >= 0)
        {
            var bandage = World.FindItem((Serial)reader.ReadUInt32());

            if (bandage == null)
            {
                return;
            }

            var target = World.FindMobile((Serial)reader.ReadUInt32());

            if (target == null)
            {
                return;
            }

            Bandage.BandageTargetRequest(from, bandage, target);

            from.NextActionTime = Core.TickCount + Mobile.ActionDelay;
        }
        else
        {
            from.SendActionMessage();
        }
    }

    public static void TargetedSpell(NetState state, SpanReader reader)
    {
        var spellId = (short)(reader.ReadInt16() - 1); // zero based;

        Spellbook.TargetedSpell(state.Mobile, World.FindEntity((Serial)reader.ReadUInt32()), spellId);
    }

    public static void TargetedSkillUse(NetState state, SpanReader reader)
    {
        var skillId = reader.ReadInt16();

        PlayerMobile.TargetedSkillUse(state.Mobile, World.FindEntity((Serial)reader.ReadUInt32()), skillId);
    }

    [GeneratedEvent("TargetByResourceMacro")]
    public static partial void InvokeTargetByResourceMacro(Mobile m, Item item, short resourceType);

    public static void TargetByResourceMacro(NetState state, SpanReader reader)
    {
        var serial = (Serial)reader.ReadUInt32();

        if (serial.IsItem)
        {
            InvokeTargetByResourceMacro(state.Mobile, World.FindItem(serial), reader.ReadInt16());
        }
    }
}
