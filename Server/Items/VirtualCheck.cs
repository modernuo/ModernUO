#region Header

// **********
// ServUO - VirtualCheck.cs
// **********

#endregion

#region References

using System.Drawing;
using Server.Gumps;
using Server.Items;
using Server.Network;

#endregion

namespace Server
{
  public sealed class VirtualCheck : Item
  {
    public static bool UseEditGump = false;

    private int _Gold;

    private int _Plat;

    public VirtualCheck(int plat = 0, int gold = 0)
      : base(0x14F0)
    {
      Plat = plat;
      Gold = gold;

      Movable = false;
    }

    public VirtualCheck(Serial serial)
      : base(serial)
    {
    }

    public override bool IsVirtualItem => true;

    public override bool DisplayWeight => false;
    public override bool DisplayLootType => false;

    public override double DefaultWeight => 0;

    public override string DefaultName => "Offer Of Currency";

    public EditGump Editor{ get; private set; }

    [CommandProperty(AccessLevel.Administrator)]
    public int Plat
    {
      get => _Plat;
      set
      {
        _Plat = value;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.Administrator)]
    public int Gold
    {
      get => _Gold;
      set
      {
        _Gold = value;
        InvalidateProperties();
      }
    }

    public override bool IsAccessibleTo(Mobile check)
    {
      SecureTradeContainer c = GetSecureTradeCont();

      if (check == null || c == null) return base.IsAccessibleTo(check);

      return c.RootParent == check && IsChildOf(c);
    }

    public override void OnDoubleClickSecureTrade(Mobile from)
    {
      if (UseEditGump && IsAccessibleTo(from))
      {
        if (Editor?.Check?.Deleted != false)
        {
          Editor = new EditGump(from, this);
          Editor.Send();
        }
        else
        {
          Editor.Refresh(true);
        }
      }
      else
      {
        if (Editor != null)
        {
          Editor.Close();
          Editor = null;
        }

        base.OnDoubleClickSecureTrade(from);
      }
    }

    public override void OnSingleClick(Mobile from)
    {
      LabelTo(from, "Offer: {0:#,0} platinum, {1:#,0} gold", Plat, Gold);
    }

    public override void GetProperties(ObjectPropertyList list)
    {
      base.GetProperties(list);

      list.Add(1060738, $"{Plat:#,0} platinum, {Gold:#,0} gold"); // value: ~1_val~
    }

    public void UpdateTrade(Mobile user)
    {
      SecureTradeContainer c = GetSecureTradeCont();

      if (c?.Trade == null) return;

      if (user == c.Trade.From.Mobile)
        c.Trade.UpdateFromCurrency();
      else if (user == c.Trade.To.Mobile) c.Trade.UpdateToCurrency();

      c.ClearChecks();
    }

    public override void OnAfterDelete()
    {
      base.OnAfterDelete();

      if (Editor != null)
      {
        Editor.Close();
        Editor = null;
      }
    }

    public override void Serialize(GenericWriter writer)
    {
    }

    public override void Deserialize(GenericReader reader)
    {
      Delete();
    }

    public class EditGump : Gump
    {
      public enum Buttons
      {
        Close,
        Clear,
        Accept,
        AllPlat,
        AllGold
      }

      private int _Plat, _Gold;

      public EditGump(Mobile user, VirtualCheck check)
        : base(50, 50)
      {
        User = user;
        Check = check;

        _Plat = Check.Plat;
        _Gold = Check.Gold;

        Closable = true;
        Disposable = true;
        Draggable = true;
        Resizable = false;

        User.CloseGump<EditGump>();

        CompileLayout();
      }

      public Mobile User{ get; }
      public VirtualCheck Check{ get; private set; }

      public override void OnServerClose(NetState owner)
      {
        base.OnServerClose(owner);

        if (Check?.Deleted == false)
          Check.UpdateTrade(User);
      }

      public void Close()
      {
        User.CloseGump<EditGump>();

        if (Check?.Deleted == false)
          Check.UpdateTrade(User);
        else
          Check = null;
      }

      public void Send()
      {
        if (Check?.Deleted == false)
          User.SendGump(this);
        else
          Close();
      }

      public void Refresh(bool recompile)
      {
        if (Check?.Deleted != false)
        {
          Close();
          return;
        }

        if (recompile)
          CompileLayout();

        Close();
        Send();
      }

      private void CompileLayout()
      {
        if (Check?.Deleted != false)
          return;

        Entries.ForEach(e => e.Parent = null);
        Entries.Clear();

        AddPage(0);

        AddBackground(0, 0, 400, 160, 3500);

        // Title
        AddImageTiled(25, 35, 350, 3, 96);
        AddImage(10, 8, 113);
        AddImage(360, 8, 113);

        string title =
          $"<BASEFONT COLOR=#{Color.DarkSlateGray.ToArgb():X}><CENTER>BANK OF {User.RawName.ToUpper()}</CENTER>";

        AddHtml(40, 15, 320, 20, title, false, false);

        // Platinum Row
        AddBackground(15, 60, 175, 20, 9300);
        AddBackground(20, 45, 165, 30, 9350);
        AddItem(20, 45, 3826); // Plat
        AddLabel(60, 50, 0, User.Account.TotalPlat.ToString("#,0"));

        AddButton(195, 50, 95, 95, (int)Buttons.AllPlat, GumpButtonType.Reply, 0); // ->

        AddBackground(210, 60, 175, 20, 9300);
        AddBackground(215, 45, 165, 30, 9350);
        AddTextEntry(225, 50, 145, 20, 0, 0, _Plat.ToString(), User.Account.TotalPlat.ToString().Length);

        // Gold Row
        AddBackground(15, 100, 175, 20, 9300);
        AddBackground(20, 85, 165, 30, 9350);
        AddItem(20, 85, 3823); // Gold
        AddLabel(60, 90, 0, User.Account.TotalGold.ToString("#,0"));

        AddButton(195, 90, 95, 95, (int)Buttons.AllGold, GumpButtonType.Reply, 0); // ->

        AddBackground(210, 100, 175, 20, 9300);
        AddBackground(215, 85, 165, 30, 9350);
        AddTextEntry(225, 90, 145, 20, 0, 1, _Gold.ToString(), User.Account.TotalGold.ToString().Length);

        // Buttons
        AddButton(20, 128, 12006, 12007, (int)Buttons.Close, GumpButtonType.Reply, 0);
        AddButton(215, 128, 12003, 12004, (int)Buttons.Clear, GumpButtonType.Reply, 0);
        AddButton(305, 128, 12000, 12002, (int)Buttons.Accept, GumpButtonType.Reply, 0);
      }

      public override void OnResponse(NetState sender, RelayInfo info)
      {
        if (Check?.Deleted != false || sender.Mobile != User)
        {
          Close();
          return;
        }

        bool refresh = false, updated = false;

        switch ((Buttons)info.ButtonID)
        {
          case Buttons.Close:
            break;
          case Buttons.Clear:
          {
            _Plat = _Gold = 0;
            refresh = true;
          }
            break;
          case Buttons.Accept:
          {
            string platText = info.GetTextEntry(0).Text;
            string goldText = info.GetTextEntry(1).Text;

            if (!int.TryParse(platText, out _Plat))
            {
              User.SendMessage("That is not a valid amount of platinum.");
              refresh = true;
            }
            else if (!int.TryParse(goldText, out _Gold))
            {
              User.SendMessage("That is not a valid amount of gold.");
              refresh = true;
            }
            else
            {
              int totalPlat = User.Account.TotalPlat;
              int totalGold = User.Account.TotalGold;

              if (totalPlat < _Plat || totalGold < _Gold)
              {
                _Plat = User.Account.TotalPlat;
                _Gold = User.Account.TotalGold;
                User.SendMessage("You do not have that much currency.");
                refresh = true;
              }
              else
              {
                Check.Plat = _Plat;
                Check.Gold = _Gold;
                updated = true;
              }
            }
          }
            break;
          case Buttons.AllPlat:
          {
            _Plat = User.Account.TotalPlat;
            refresh = true;
          }
            break;
          case Buttons.AllGold:
          {
            _Gold = User.Account.TotalGold;
            refresh = true;
          }
            break;
        }

        if (updated) User.SendMessage("Your offer has been updated.");

        if (refresh && Check?.Deleted == false)
        {
          Refresh(true);
          return;
        }

        Close();
      }
    }
  }
}
