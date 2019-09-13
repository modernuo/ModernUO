/***************************************************************************
 *                               SecureTrade.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using Server.Items;
using Server.Network;

namespace Server
{
  public class SecureTrade
  {
    public SecureTrade(Mobile from, Mobile to)
    {
      Valid = true;

      From = new SecureTradeInfo(this, from, new SecureTradeContainer(this));
      To = new SecureTradeInfo(this, to, new SecureTradeContainer(this));

      NetState fromNS = from.NetState;
      NetState toNS = to.NetState;

      bool from704565 = fromNS?.NewSecureTrading == true;
      bool to704565 = toNS?.NewSecureTrading == true;

      Packets.SendMobileStatus(fromNS, from, to);
      Packets.SendUpdateSecureTrade(fromNS, From.Container.Serial, false, false);
      Packets.SendSecureTradeEquip(fromNS, To.Container, to.Serial);
      Packets.SendUpdateSecureTrade(fromNS, From.Container.Serial, false, false);
      Packets.SendSecureTradeEquip(fromNS, To.Container, from.Serial);
      Packets.SendDisplaySecureTrade(fromNS, to.Serial, From.Container.Serial, To.Container.Serial, to.Name);
      Packets.SendUpdateSecureTrade(fromNS, From.Container.Serial, false, false);
      if (from.Account != null && from704565)
        Packets.SendUpdateSecureTrade(fromNS, From.Container.Serial, TradeFlag.UpdateLedger,from.Account.TotalGold,
          from.Account.TotalPlat);

      Packets.SendMobileStatus(toNS, to, from);
      Packets.SendUpdateSecureTrade(toNS, To.Container.Serial, false, false);
      Packets.SendSecureTradeEquip(toNS, From.Container, from.Serial);
      Packets.SendUpdateSecureTrade(toNS, To.Container.Serial, false, false);
      Packets.SendSecureTradeEquip(toNS, From.Container, to.Serial);
      Packets.SendDisplaySecureTrade(toNS, from.Serial, To.Container.Serial, From.Container.Serial, from.Name);
      Packets.SendUpdateSecureTrade(toNS, To.Container.Serial, false, false);
      if (to.Account != null && to704565)
        Packets.SendUpdateSecureTrade(toNS, To.Container.Serial, TradeFlag.UpdateLedger,to.Account.TotalGold,
          to.Account.TotalPlat);
    }

    public SecureTradeInfo From{ get; }

    public SecureTradeInfo To{ get; }

    public bool Valid{ get; private set; }

    public void Cancel()
    {
      if (!Valid) return;

      List<Item> list = From.Container.Items;

      for (int i = list.Count - 1; i >= 0; --i)
        if (i < list.Count)
        {
          Item item = list[i];

          if (item == From.VirtualCheck) continue;

          item.OnSecureTrade(From.Mobile, To.Mobile, From.Mobile, false);

          if (!item.Deleted)
            From.Mobile.AddToBackpack(item);
        }

      list = To.Container.Items;

      for (int i = list.Count - 1; i >= 0; --i)
        if (i < list.Count)
        {
          Item item = list[i];

          if (item == To.VirtualCheck) continue;

          item.OnSecureTrade(To.Mobile, From.Mobile, To.Mobile, false);

          if (!item.Deleted) To.Mobile.AddToBackpack(item);
        }

      Close();
    }

    public void Close()
    {
      if (!Valid) return;

      Valid = false;

      NetState ns = From.Mobile.NetState;
      Packets.SendCloseSecureTrade(ns, From.Container.Serial);

      ns?.RemoveTrade(this);

      ns = To.Mobile.NetState;
      Packets.SendCloseSecureTrade(ns, To.Container.Serial);

      ns?.RemoveTrade(this);

      Timer.DelayCall(From.Dispose);
      Timer.DelayCall(To.Dispose);
    }

    public void UpdateFromCurrency()
    {
      UpdateCurrency(From, To);
    }

    public void UpdateToCurrency()
    {
      UpdateCurrency(To, From);
    }

    private static void UpdateCurrency(SecureTradeInfo left, SecureTradeInfo right)
    {
      Mobile from = left.Mobile;
      Mobile to = right.Mobile;

      if (from.NetState?.NewSecureTrading == true)
        Packets.SendUpdateSecureTrade(from.NetState, left.Container.Serial, TradeFlag.UpdateLedger,
          from.Account.TotalPlat, from.Account.TotalGold);

      if (to.NetState?.NewSecureTrading == true)
        Packets.SendUpdateSecureTrade(to.NetState, right.Container.Serial, TradeFlag.UpdateLedger,
          left.Plat, left.Gold);
    }

    public void Update()
    {
      if (!Valid) return;

      if (!From.IsDisposed && From.Accepted && !To.IsDisposed && To.Accepted)
      {
        List<Item> list = From.Container.Items;

        bool allowed = true;

        for (int i = list.Count - 1; allowed && i >= 0; --i)
          if (i < list.Count)
          {
            Item item = list[i];

            if (item == From.VirtualCheck) continue;

            if (!item.AllowSecureTrade(From.Mobile, To.Mobile, To.Mobile, true)) allowed = false;
          }

        list = To.Container.Items;

        for (int i = list.Count - 1; allowed && i >= 0; --i)
          if (i < list.Count)
          {
            Item item = list[i];

            if (item == To.VirtualCheck) continue;

            if (!item.AllowSecureTrade(To.Mobile, From.Mobile, From.Mobile, true)) allowed = false;
          }

        if (AccountGold.Enabled)
        {
          if (From.Mobile.Account != null)
          {
            int totalPlat = From.Mobile.Account.TotalPlat;
            int totalGold = From.Mobile.Account.TotalGold;

            if (totalPlat < From.Plat || totalGold < From.Gold)
            {
              allowed = false;
              From.Mobile.SendMessage("You do not have enough currency to complete this trade.");
            }
          }

          if (To.Mobile.Account != null)
          {
            int totalPlat = To.Mobile.Account.TotalPlat;
            int totalGold = To.Mobile.Account.TotalGold;

            if (totalPlat < To.Plat || totalGold < To.Gold)
            {
              allowed = false;
              To.Mobile.SendMessage("You do not have enough currency to complete this trade.");
            }
          }
        }

        if (!allowed)
        {
          From.Accepted = false;
          To.Accepted = false;

          Packets.SendUpdateSecureTrade(From.Mobile.NetState, From.Container.Serial, From.Accepted, To.Accepted);
          Packets.SendUpdateSecureTrade(To.Mobile.NetState, To.Container.Serial, To.Accepted, From.Accepted);

          return;
        }

        if (AccountGold.Enabled && From.Mobile.Account != null && To.Mobile.Account != null)
          HandleAccountGoldTrade();

        list = From.Container.Items;

        for (int i = list.Count - 1; i >= 0; --i)
          if (i < list.Count)
          {
            Item item = list[i];

            if (item == From.VirtualCheck) continue;

            item.OnSecureTrade(From.Mobile, To.Mobile, To.Mobile, true);

            if (!item.Deleted) To.Mobile.AddToBackpack(item);
          }

        list = To.Container.Items;

        for (int i = list.Count - 1; i >= 0; --i)
          if (i < list.Count)
          {
            Item item = list[i];

            if (item == To.VirtualCheck) continue;

            item.OnSecureTrade(To.Mobile, From.Mobile, From.Mobile, true);

            if (!item.Deleted) From.Mobile.AddToBackpack(item);
          }

        Close();
      }
      else if (!From.IsDisposed && !To.IsDisposed)
      {
        Packets.SendUpdateSecureTrade(From.Mobile.NetState, From.Container.Serial, From.Accepted, To.Accepted);
        Packets.SendUpdateSecureTrade(To.Mobile.NetState, To.Container.Serial, To.Accepted, From.Accepted);
      }
    }

    private void HandleAccountGoldTrade()
    {
      int fromPlatSend = 0, fromGoldSend = 0, fromPlatRecv = 0, fromGoldRecv = 0;
      int toPlatSend = 0, toGoldSend = 0, toPlatRecv = 0, toGoldRecv = 0;

      if ((From.Plat > 0) & From.Mobile.Account.WithdrawPlat(From.Plat))
      {
        fromPlatSend = From.Plat;

        if (To.Mobile.Account.DepositPlat(From.Plat)) toPlatRecv = fromPlatSend;
      }

      if ((From.Gold > 0) & From.Mobile.Account.WithdrawGold(From.Gold))
      {
        fromGoldSend = From.Gold;

        if (To.Mobile.Account.DepositGold(From.Gold)) toGoldRecv = fromGoldSend;
      }

      if ((To.Plat > 0) & To.Mobile.Account.WithdrawPlat(To.Plat))
      {
        toPlatSend = To.Plat;

        if (From.Mobile.Account.DepositPlat(To.Plat)) fromPlatRecv = toPlatSend;
      }

      if ((To.Gold > 0) & To.Mobile.Account.WithdrawGold(To.Gold))
      {
        toGoldSend = To.Gold;

        if (From.Mobile.Account.DepositGold(To.Gold)) fromGoldRecv = toGoldSend;
      }

      HandleAccountGoldTrade(From.Mobile, To.Mobile, fromPlatSend, fromGoldSend, fromPlatRecv, fromGoldRecv);
      HandleAccountGoldTrade(To.Mobile, From.Mobile, toPlatSend, toGoldSend, toPlatRecv, toGoldRecv);
    }

    private static void HandleAccountGoldTrade(
      Mobile left,
      Mobile right,
      int platSend,
      int goldSend,
      int platRecv,
      int goldRecv)
    {
      if (platSend > 0 || goldSend > 0)
      {
        if (platSend > 0 && goldSend > 0)
          left.SendMessage("You traded {0:#,0} platinum and {1:#,0} gold to {2}.", platSend, goldSend,
            right.RawName);
        else if (platSend > 0)
          left.SendMessage("You traded {0:#,0} platinum to {1}.", platSend, right.RawName);
        else if (goldSend > 0) left.SendMessage("You traded {0:#,0} gold to {1}.", goldSend, right.RawName);
      }

      if (platRecv > 0 || goldRecv > 0)
      {
        if (platRecv > 0 && goldRecv > 0)
          left.SendMessage("You received {0:#,0} platinum and {1:#,0} gold from {2}.", platRecv, goldRecv,
            right.RawName);
        else if (platRecv > 0)
          left.SendMessage("You received {0:#,0} platinum from {1}.", platRecv, right.RawName);
        else if (goldRecv > 0) left.SendMessage("You received {0:#,0} gold from {1}.", goldRecv, right.RawName);
      }
    }
  }

  public class SecureTradeInfo : IDisposable
  {
    public SecureTradeInfo(SecureTrade owner, Mobile m, SecureTradeContainer c)
    {
      Owner = owner;
      Mobile = m;
      Container = c;

      Mobile.AddItem(Container);

      VirtualCheck = new VirtualCheck();
      Container.DropItem(VirtualCheck);
    }

    public SecureTrade Owner{ get; private set; }
    public Mobile Mobile{ get; private set; }
    public SecureTradeContainer Container{ get; private set; }
    public VirtualCheck VirtualCheck{ get; private set; }

    public int Gold
    {
      get => VirtualCheck.Gold;
      set => VirtualCheck.Gold = value;
    }

    public int Plat
    {
      get => VirtualCheck.Plat;
      set => VirtualCheck.Plat = value;
    }

    public bool Accepted{ get; set; }

    public bool IsDisposed{ get; private set; }

    public void Dispose()
    {
      VirtualCheck.Delete();
      VirtualCheck = null;

      Container.Delete();
      Container = null;

      Mobile = null;
      Owner = null;

      IsDisposed = true;
    }
  }
}
