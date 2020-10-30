/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Packets.SecureTrade.cs                                          *
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

namespace Server.Network
{
    public static partial class Packets
    {
        public static void SecureTrade(this NetState state, CircularBufferReader reader)
        {
            switch (reader.ReadByte())
            {
                case 1: // Cancel
                    {
                        Serial serial = reader.ReadUInt32();

                        if (World.FindItem(serial) is SecureTradeContainer cont && cont.Trade != null &&
                            (cont.Trade.From.Mobile == state.Mobile || cont.Trade.To.Mobile == state.Mobile))
                        {
                            cont.Trade.Cancel();
                        }

                        break;
                    }
                case 2: // Check
                    {
                        Serial serial = reader.ReadUInt32();

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
                        Serial serial = reader.ReadUInt32();

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
}
