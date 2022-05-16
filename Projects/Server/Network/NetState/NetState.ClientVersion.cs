/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
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

namespace Server.Network
{
    public partial class NetState
    {
        public ProtocolChanges ProtocolChanges { get; set; }
        public ClientFlags Flags { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasFlag(ClientFlags flag) => (Flags & flag) != 0;

        public ClientVersion Version
        {
            get => _version;
            set => ProtocolChanges = ProtocolChangesByVersion(_version = value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ProtocolChanges ProtocolChangesByVersion(ClientVersion version) =>
            version switch
            {
                var v when v >= ClientVersion.Version670061 => ProtocolChanges.Version70610,  //EC 4.0.61.1
                var v when v >= ClientVersion.Version670050 => ProtocolChanges.Version70500,  //EC 4.0.50.0
                var v when v >= ClientVersion.Version670045 => ProtocolChanges.Version704565, //EC 4.0.45.0
                var v when v >= ClientVersion.Version670033 => ProtocolChanges.Version70331,  //EC 4.0.33.0
                var v when v >= ClientVersion.Version670030 => ProtocolChanges.Version70300,  //EC 4.0.30.0
                var v when v >= ClientVersion.Version670016 => ProtocolChanges.Version70160,  //EC 4.0.16.0
                var v when v >= ClientVersion.Version670013 => ProtocolChanges.Version70130,  //EC 4.0.13.1
                var v when v >= ClientVersion.Version670009 => ProtocolChanges.Version7090,   //EC 4.0.9.0
                var v when v >= ClientVersion.Version670000 => ProtocolChanges.Version7000,   //EC 4.0.0.2 (First release)
                var v when v >= ClientVersion.Version665553 => ProtocolChanges.Version60142,  //KR 2.59.0.2
                var v when v >= ClientVersion.Version665539 => ProtocolChanges.Version6017,   //KR 2.45.0.4
                var v when v >= ClientVersion.Version665538 => ProtocolChanges.Version6000,   //KR 2.44.0.15 (First release)
                var v when v >= ClientVersion.Version70610  => ProtocolChanges.Version70610,
                var v when v >= ClientVersion.Version70500  => ProtocolChanges.Version70500,
                var v when v >= ClientVersion.Version704565 => ProtocolChanges.Version704565,
                var v when v >= ClientVersion.Version70331  => ProtocolChanges.Version70331,
                var v when v >= ClientVersion.Version70300  => ProtocolChanges.Version70300,
                var v when v >= ClientVersion.Version70160  => ProtocolChanges.Version70160,
                var v when v >= ClientVersion.Version70130  => ProtocolChanges.Version70130,
                var v when v >= ClientVersion.Version7090   => ProtocolChanges.Version7090,
                var v when v >= ClientVersion.Version7000   => ProtocolChanges.Version7000,
                var v when v >= ClientVersion.Version60142  => ProtocolChanges.Version60142,
                var v when v >= ClientVersion.Version6017   => ProtocolChanges.Version6017,
                var v when v >= ClientVersion.Version6000   => ProtocolChanges.Version6000,
                var v when v >= ClientVersion.Version502b   => ProtocolChanges.Version502b,
                var v when v >= ClientVersion.Version500a   => ProtocolChanges.Version500a,
                var v when v >= ClientVersion.Version407a   => ProtocolChanges.Version407a,
                var v when v >= ClientVersion.Version400a   => ProtocolChanges.Version400a,
                _                                           => ProtocolChanges.None
            };

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

        public bool IsUOTDClient =>
            (Flags & ClientFlags.UOTD) != 0 || _version?.Type == ClientType.UOTD;

        public bool IsSAClient => _version?.Type == ClientType.SA;

        public bool IsKRClient => _version?.Type == ClientType.KR;

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

                        if (info.RequiredClient != null && Version >= info.RequiredClient || (Flags & info.ClientFlags) != 0)
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
            info != null && (!checkCoreExpansion || (int)Core.Expansion >= info.ID) && ExpansionInfo.ID >= info.ID;

        public bool SupportsExpansion(Expansion ex, bool checkCoreExpansion = true) =>
            SupportsExpansion(ExpansionInfo.GetInfo(ex), checkCoreExpansion);
    }
}
