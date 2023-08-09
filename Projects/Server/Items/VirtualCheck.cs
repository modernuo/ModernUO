/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: VirtualCheck.cs                                                 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Gumps;
using Server.Network;

namespace Server.Items;

public sealed class VirtualCheck : Item
{
    public static bool UseEditGump { get; private set; }

    public static void Configure()
    {
        UseEditGump = ServerConfiguration.GetSetting("virtualChecks.useEditGump", Core.TOL);
    }

    private int m_Gold;

    private int m_Plat;

    public VirtualCheck(int plat = 0, int gold = 0) : base(0x14F0)
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

    public EditGump Editor { get; private set; }

    [CommandProperty(AccessLevel.Administrator)]
    public int Plat
    {
        get => m_Plat;
        set
        {
            m_Plat = value;
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.Administrator)]
    public int Gold
    {
        get => m_Gold;
        set
        {
            m_Gold = value;
            InvalidateProperties();
        }
    }

    public override bool IsAccessibleTo(Mobile check)
    {
        var c = GetSecureTradeCont();

        if (check == null || c == null)
        {
            return base.IsAccessibleTo(check);
        }

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
        LabelTo(from, $"Offer: {Plat:#,0} platinum, {Gold:#,0} gold");
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1060738, $"{Plat:#,0} platinum, {Gold:#,0} gold"); // value: ~1_val~
    }

    public void UpdateTrade(Mobile user)
    {
        var c = GetSecureTradeCont();

        if (c?.Trade == null)
        {
            return;
        }

        if (user == c.Trade.From.Mobile)
        {
            c.Trade.UpdateFromCurrency();
        }
        else if (user == c.Trade.To.Mobile)
        {
            c.Trade.UpdateToCurrency();
        }

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

    public override void Serialize(IGenericWriter writer)
    {
    }

    public override void Deserialize(IGenericReader reader)
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

        private int m_Plat, m_Gold;

        public EditGump(Mobile user, VirtualCheck check) : base(50, 50)
        {
            User = user;
            Check = check;

            m_Plat = Check.Plat;
            m_Gold = Check.Gold;

            Closable = true;
            Disposable = true;
            Draggable = true;
            Resizable = false;

            User.CloseGump<EditGump>();

            CompileLayout();
        }

        public Mobile User { get; }
        public VirtualCheck Check { get; private set; }

        public override void OnServerClose(NetState owner)
        {
            base.OnServerClose(owner);

            if (Check?.Deleted == false)
            {
                Check.UpdateTrade(User);
            }
        }

        public void Close()
        {
            User.CloseGump<EditGump>();

            if (Check?.Deleted == false)
            {
                Check.UpdateTrade(User);
            }
            else
            {
                Check = null;
            }
        }

        public void Send()
        {
            if (Check?.Deleted == false)
            {
                User.SendGump(this);
            }
            else
            {
                Close();
            }
        }

        public void Refresh(bool recompile)
        {
            if (Check?.Deleted != false)
            {
                Close();
                return;
            }

            if (recompile)
            {
                CompileLayout();
            }

            Close();
            Send();
        }

        private void CompileLayout()
        {
            if (Check?.Deleted != false)
            {
                return;
            }

            Entries.ForEach(e => e.Parent = null);
            Entries.Clear();

            AddPage(0);

            AddBackground(0, 0, 400, 160, 3500);

            // Title
            AddImageTiled(25, 35, 350, 3, 96);
            AddImage(10, 8, 113);
            AddImage(360, 8, 113);

            var title =
                $"<BASEFONT COLOR=#FF2F4F4F><CENTER>BANK OF {User.RawName.ToUpper()}</CENTER>";

            AddHtml(40, 15, 320, 20, title);

            // Platinum Row
            AddBackground(15, 60, 175, 20, 9300);
            AddBackground(20, 45, 165, 30, 9350);
            AddItem(20, 45, 3826); // Plat
            AddLabel(60, 50, 0, User.Account.TotalPlat.ToString("#,0"));

            AddButton(195, 50, 95, 95, (int)Buttons.AllPlat); // ->

            AddBackground(210, 60, 175, 20, 9300);
            AddBackground(215, 45, 165, 30, 9350);
            AddTextEntry(225, 50, 145, 20, 0, 0, m_Plat.ToString(), User.Account.TotalPlat.ToString().Length);

            // Gold Row
            AddBackground(15, 100, 175, 20, 9300);
            AddBackground(20, 85, 165, 30, 9350);
            AddItem(20, 85, 3823); // Gold
            AddLabel(60, 90, 0, User.Account.TotalGold.ToString("#,0"));

            AddButton(195, 90, 95, 95, (int)Buttons.AllGold); // ->

            AddBackground(210, 100, 175, 20, 9300);
            AddBackground(215, 85, 165, 30, 9350);
            AddTextEntry(225, 90, 145, 20, 0, 1, m_Gold.ToString(), User.Account.TotalGold.ToString().Length);

            // Buttons
            AddButton(20, 128, 12006, 12007, (int)Buttons.Close);
            AddButton(215, 128, 12003, 12004, (int)Buttons.Clear);
            AddButton(305, 128, 12000, 12002, (int)Buttons.Accept);
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
                        m_Plat = m_Gold = 0;
                        refresh = true;
                    }
                    break;
                case Buttons.Accept:
                    {
                        var platText = info.GetTextEntry(0).Text;
                        var goldText = info.GetTextEntry(1).Text;

                        if (!int.TryParse(platText, out m_Plat))
                        {
                            User.SendMessage("That is not a valid amount of platinum.");
                            refresh = true;
                        }
                        else if (!int.TryParse(goldText, out m_Gold))
                        {
                            User.SendMessage("That is not a valid amount of gold.");
                            refresh = true;
                        }
                        else
                        {
                            var totalPlat = User.Account.TotalPlat;
                            var totalGold = User.Account.TotalGold;

                            if (totalPlat < m_Plat || totalGold < m_Gold)
                            {
                                m_Plat = User.Account.TotalPlat;
                                m_Gold = User.Account.TotalGold;
                                User.SendMessage("You do not have that much currency.");
                                refresh = true;
                            }
                            else
                            {
                                Check.Plat = m_Plat;
                                Check.Gold = m_Gold;
                                updated = true;
                            }
                        }
                    }
                    break;
                case Buttons.AllPlat:
                    {
                        m_Plat = User.Account.TotalPlat;
                        refresh = true;
                    }
                    break;
                case Buttons.AllGold:
                    {
                        m_Gold = User.Account.TotalGold;
                        refresh = true;
                    }
                    break;
            }

            if (updated)
            {
                User.SendMessage("Your offer has been updated.");
            }

            if (refresh && Check?.Deleted == false)
            {
                Refresh(true);
                return;
            }

            Close();
        }
    }
}
