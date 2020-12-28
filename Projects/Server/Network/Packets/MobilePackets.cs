/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MobilePackets.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Threading;

namespace Server.Network
{
    public sealed class MobileUpdate : Packet
    {
        public MobileUpdate(Mobile m, bool stygianAbyss) : base(0x20, 19)
        {
            var hue = m.Hue;

            if (m.SolidHueOverride >= 0)
            {
                hue = m.SolidHueOverride;
            }

            Stream.Write(m.Serial);
            Stream.Write((short)m.Body);
            Stream.Write((byte)0);
            Stream.Write((short)hue);
            Stream.Write((byte)m.GetPacketFlags(stygianAbyss));
            Stream.Write((short)m.X);
            Stream.Write((short)m.Y);
            Stream.Write((short)0);
            Stream.Write((byte)m.Direction);
            Stream.Write((sbyte)m.Z);
        }
    }

    public sealed class MobileIncoming : Packet
    {
        private static readonly ThreadLocal<int[]> m_DupedLayersTL = new(() => new int[256]);
        private static readonly ThreadLocal<int> m_VersionTL = new();

        public MobileIncoming(NetState ns, Mobile beholder, Mobile beheld) : base(0x78)
        {
            var sa = ns.StygianAbyss;
            var newPacket = ns.NewMobileIncoming;
            var itemIdMask = newPacket ? 0xFFFF : 0x7FFF;

            var m_Version = ++m_VersionTL.Value;
            var m_DupedLayers = m_DupedLayersTL.Value;

            var eq = beheld.Items;
            var count = eq.Count;

            if (beheld.HairItemID > 0)
            {
                count++;
            }

            if (beheld.FacialHairItemID > 0)
            {
                count++;
            }

            EnsureCapacity(23 + count * 9);

            var hue = beheld.Hue;

            if (beheld.SolidHueOverride >= 0)
            {
                hue = beheld.SolidHueOverride;
            }

            Stream.Write(beheld.Serial);
            Stream.Write((short)beheld.Body);
            Stream.Write((short)beheld.X);
            Stream.Write((short)beheld.Y);
            Stream.Write((sbyte)beheld.Z);
            Stream.Write((byte)beheld.Direction);
            Stream.Write((short)hue);
            Stream.Write((byte)beheld.GetPacketFlags(sa));
            Stream.Write((byte)Notoriety.Compute(beholder, beheld));

            for (var i = 0; i < eq.Count; ++i)
            {
                var item = eq[i];

                var layer = (byte)item.Layer;

                if (!item.Deleted && beholder.CanSee(item) && m_DupedLayers[layer] != m_Version)
                {
                    m_DupedLayers[layer] = m_Version;

                    hue = item.Hue;

                    if (beheld.SolidHueOverride >= 0)
                    {
                        hue = beheld.SolidHueOverride;
                    }

                    var itemID = item.ItemID & itemIdMask;
                    var writeHue = newPacket || hue != 0;

                    if (!newPacket)
                    {
                        itemID |= 0x8000;
                    }

                    Stream.Write(item.Serial);
                    Stream.Write((ushort)itemID);
                    Stream.Write(layer);

                    if (writeHue)
                    {
                        Stream.Write((short)hue);
                    }
                }
            }

            if (beheld.HairItemID > 0)
            {
                if (m_DupedLayers[(int)Layer.Hair] != m_Version)
                {
                    m_DupedLayers[(int)Layer.Hair] = m_Version;
                    hue = beheld.HairHue;

                    if (beheld.SolidHueOverride >= 0)
                    {
                        hue = beheld.SolidHueOverride;
                    }

                    var itemID = beheld.HairItemID & itemIdMask;
                    var writeHue = newPacket || hue != 0;

                    if (!newPacket)
                    {
                        itemID |= 0x8000;
                    }

                    Stream.Write(HairInfo.FakeSerial(beheld));
                    Stream.Write((ushort)itemID);
                    Stream.Write((byte)Layer.Hair);

                    if (writeHue)
                    {
                        Stream.Write((short)hue);
                    }
                }
            }

            if (beheld.FacialHairItemID > 0)
            {
                if (m_DupedLayers[(int)Layer.FacialHair] != m_Version)
                {
                    m_DupedLayers[(int)Layer.FacialHair] = m_Version;
                    hue = beheld.FacialHairHue;

                    if (beheld.SolidHueOverride >= 0)
                    {
                        hue = beheld.SolidHueOverride;
                    }

                    var itemID = beheld.FacialHairItemID & itemIdMask;
                    var writeHue = newPacket || hue != 0;

                    if (!newPacket)
                    {
                        itemID |= 0x8000;
                    }

                    Stream.Write(FacialHairInfo.FakeSerial(beheld));
                    Stream.Write((ushort)itemID);
                    Stream.Write((byte)Layer.FacialHair);

                    if (writeHue)
                    {
                        Stream.Write((short)hue);
                    }
                }
            }

            Stream.Write(0); // terminate
        }
    }
}
