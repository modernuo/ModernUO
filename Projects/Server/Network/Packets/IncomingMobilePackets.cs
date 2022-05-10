/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IncomingMobilePackets.cs                                        *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Items;

namespace Server.Network;

public static class IncomingMobilePackets
{
    public static void Configure()
    {
        IncomingPackets.Register(0x75, 35, true, RenameRequest);
        IncomingPackets.Register(0x98, 0, true, MobileNameRequest);
        IncomingPackets.Register(0xB8, 0, true, ProfileReq);
        IncomingPackets.Register(0x6F, 0, true, SecureTrade);
    }

    public static void RenameRequest(NetState state, CircularBufferReader reader, int packetLength)
    {
        var from = state.Mobile;
        var targ = World.FindMobile((Serial)reader.ReadUInt32());

        if (targ != null)
        {
            EventSink.InvokeRenameRequest(from, targ, reader.ReadAsciiSafe());
        }
    }

    public static void MobileNameRequest(NetState state, CircularBufferReader reader, int packetLength)
    {
        var m = World.FindMobile((Serial)reader.ReadUInt32());

        if (m != null && Utility.InUpdateRange(state.Mobile.Location, m.Location) && state.Mobile.CanSee(m))
        {
            state.SendMobileName(m);
        }
    }

    public static void ProfileReq(NetState state, CircularBufferReader reader, int packetLength)
    {
        int type = reader.ReadByte();
        var serial = (Serial)reader.ReadUInt32();

        var beholder = state.Mobile;
        var beheld = World.FindMobile(serial);

        if (beheld == null)
        {
            return;
        }

        switch (type)
        {
            case 0x00: // display request
                {
                    EventSink.InvokeProfileRequest(beholder, beheld);

                    break;
                }
            case 0x01: // edit request
                {
                    reader.ReadInt16(); // Skip
                    int length = reader.ReadUInt16();

                    if (length > 511)
                    {
                        return;
                    }

                    var text = reader.ReadBigUni(length);

                    EventSink.InvokeChangeProfileRequest(beholder, beheld, text);

                    break;
                }
        }
    }

    public static void SecureTrade(NetState state, CircularBufferReader reader, int packetLength)
    {
        switch (reader.ReadByte())
        {
            case 1: // Cancel
                {
                    var serial = (Serial)reader.ReadUInt32();

                    if (World.FindItem(serial) is SecureTradeContainer cont && cont.Trade != null &&
                        (cont.Trade.From.Mobile == state.Mobile || cont.Trade.To.Mobile == state.Mobile))
                    {
                        cont.Trade.Cancel();
                    }

                    break;
                }
            case 2: // Check
                {
                    var serial = (Serial)reader.ReadUInt32();

                    if (World.FindItem(serial) is SecureTradeContainer cont)
                    {
                        var trade = cont.Trade;

                        var value = reader.ReadInt32() != 0;

                        if (trade != null)
                        {
                            if (trade.From.Mobile == state.Mobile)
                            {
                                trade.From.Accepted = value;
                                trade.Update();
                            }
                            else if (trade.To.Mobile == state.Mobile)
                            {
                                trade.To.Accepted = value;
                                trade.Update();
                            }
                        }
                    }

                    break;
                }
            case 3: // Update Gold
                {
                    var serial = (Serial)reader.ReadUInt32();

                    if (World.FindItem(serial) is SecureTradeContainer cont)
                    {
                        var gold = reader.ReadInt32();
                        var plat = reader.ReadInt32();

                        var trade = cont.Trade;

                        if (trade != null)
                        {
                            if (trade.From.Mobile == state.Mobile)
                            {
                                trade.From.Gold = gold;
                                trade.From.Plat = plat;
                                trade.UpdateFromCurrency();
                            }
                            else if (trade.To.Mobile == state.Mobile)
                            {
                                trade.To.Gold = gold;
                                trade.To.Plat = plat;
                                trade.UpdateToCurrency();
                            }
                        }
                    }
                }
                break;
        }
    }
}
