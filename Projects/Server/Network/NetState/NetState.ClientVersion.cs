/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: NetState.ClientVersion.cs                                       *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Runtime.CompilerServices;

namespace Server.Network;

public partial class NetState
{
    public ProtocolChanges ProtocolChanges { get; set; }
    public ClientFlags Flags { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasFlag(ClientFlags flag) => (Flags & flag) != 0;

    public ClientVersion Version
    {
        get => _version;
        set => ProtocolChanges = (_version = value)?.ProtocolChanges ?? ProtocolChanges.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasProtocolChanges(ProtocolChanges changes) => (ProtocolChanges & changes) != 0;

    public bool NewSpellbook => HasProtocolChanges(ProtocolChanges.NewSpellbook);
    public bool DamagePacket => HasProtocolChanges(ProtocolChanges.DamagePacket);
    public bool Unpack => HasProtocolChanges(ProtocolChanges.Unpack);
    public bool BuffIcon => HasProtocolChanges(ProtocolChanges.BuffIcon);
    public bool NewHaven => HasProtocolChanges(ProtocolChanges.NewHaven);
    public bool ContainerGridLines => HasProtocolChanges(ProtocolChanges.ContainerGridLines);
    public bool ExtendedSupportedFeatures => HasProtocolChanges(ProtocolChanges.ExtendedSupportedFeatures);
    public bool StygianAbyss => HasProtocolChanges(ProtocolChanges.StygianAbyss);
    public bool HighSeas => HasProtocolChanges(ProtocolChanges.HighSeas);
    public bool NewCharacterList => HasProtocolChanges(ProtocolChanges.NewCharacterList);
    public bool NewCharacterCreation => HasProtocolChanges(ProtocolChanges.NewCharacterCreation);
    public bool ExtendedStatus => HasProtocolChanges(ProtocolChanges.ExtendedStatus);
    public bool NewMobileIncoming => HasProtocolChanges(ProtocolChanges.NewMobileIncoming);
    public bool NewSecureTrading => HasProtocolChanges(ProtocolChanges.NewSecureTrading);

    public bool IsUOTDClient => HasFlag(ClientFlags.UOTD) || _version?.Type == ClientType.UOTD;
    public bool IsKRClient => _version?.Type == ClientType.KR;
    public bool IsSAClient => _version?.Type == ClientType.SA;
    public bool IsEnhancedClient => _version?.Type is ClientType.KR or ClientType.SA;

    private ExpansionInfo m_Expansion;

    public ExpansionInfo ExpansionInfo
    {
        get
        {
            if (m_Expansion == null)
            {
                for (var i = ExpansionInfo.Table.Length - 1; i >= 0; i--)
                {
                    var info = ExpansionInfo.Table[i];

                    // KR sends same client flags as EC
                    if (IsKRClient && (info.ClientFlags & ClientFlags.TerMur) != 0)
                    {
                        continue;
                    }

                    if ((info.RequiredClient == null || !IsKRClient) &&
                        Version >= info.RequiredClient || HasFlag(info.ClientFlags))
                    {
                        m_Expansion = info;
                        break;
                    }
                }

                m_Expansion ??= ExpansionInfo.GetInfo(Expansion.None);
            }

            return m_Expansion;
        }
    }

    public bool SupportsExpansion(ExpansionInfo info, bool checkCoreExpansion = true) =>
        info != null && (!checkCoreExpansion || (int)Core.Expansion >= info.Id) && ExpansionInfo.Id >= info.Id;

    public bool SupportsExpansion(Expansion ex, bool checkCoreExpansion = true) =>
        SupportsExpansion(ExpansionInfo.GetInfo(ex), checkCoreExpansion);
}
