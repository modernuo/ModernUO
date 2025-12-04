/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: VirtualCheckGump.cs                                             *
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
using Server.Network;

namespace Server.Gumps;

public sealed class VirtualCheckGump : Gump, IVirtualCheckGump
{
    public static unsafe void Configure()
    {
        VirtualCheck.GumpActivator = &Activator;
    }

    private static VirtualCheckGump Activator(Mobile from, VirtualCheck check) => new(from, check);

    public enum Buttons
    {
        Close,
        Clear,
        Accept,
        AllPlat,
        AllGold
    }

    private int _plat, _gold;

    public override bool Singleton => true;

    public VirtualCheckGump(Mobile user, VirtualCheck check) : base(50, 50)
    {
        User = user;
        Check = check;

        _plat = Check.Plat;
        _gold = Check.Gold;

        Closable = true;
        Disposable = true;
        Draggable = true;
        Resizable = false;

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
        User.CloseGump<VirtualCheckGump>();

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

        AddHtml(40, 15, 320, 20, Html.Center($"BANK OF {User.RawName.ToUpper()}", 0x2F4F4F));

        // Platinum Row
        AddBackground(15, 60, 175, 20, 9300);
        AddBackground(20, 45, 165, 30, 9350);
        AddItem(20, 45, 3826); // Plat
        AddLabel(60, 50, 0, User.Account.TotalPlat.ToString("#,0"));

        AddButton(195, 50, 95, 95, (int)Buttons.AllPlat); // ->

        AddBackground(210, 60, 175, 20, 9300);
        AddBackground(215, 45, 165, 30, 9350);
        AddTextEntry(225, 50, 145, 20, 0, 0, _plat.ToString(), User.Account.TotalPlat.ToString().Length);

        // Gold Row
        AddBackground(15, 100, 175, 20, 9300);
        AddBackground(20, 85, 165, 30, 9350);
        AddItem(20, 85, 3823); // Gold
        AddLabel(60, 90, 0, User.Account.TotalGold.ToString("#,0"));

        AddButton(195, 90, 95, 95, (int)Buttons.AllGold); // ->

        AddBackground(210, 100, 175, 20, 9300);
        AddBackground(215, 85, 165, 30, 9350);
        AddTextEntry(225, 90, 145, 20, 0, 1, _gold.ToString(), User.Account.TotalGold.ToString().Length);

        // Buttons
        AddButton(20, 128, 12006, 12007, (int)Buttons.Close);
        AddButton(215, 128, 12003, 12004, (int)Buttons.Clear);
        AddButton(305, 128, 12000, 12002, (int)Buttons.Accept);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (Check?.Deleted != false || sender.Mobile != User)
        {
            Close();
            return;
        }

        var refresh = false;
        var updated = false;

        switch ((Buttons)info.ButtonID)
        {
            case Buttons.Clear:
                {
                    _plat = _gold = 0;
                    refresh = true;
                    break;
                }
            case Buttons.Accept:
                {
                    var platText = info.GetTextEntry(0);
                    var goldText = info.GetTextEntry(1);

                    if (!int.TryParse(platText, out _plat))
                    {
                        User.SendMessage("That is not a valid amount of platinum.");
                        refresh = true;
                    }
                    else if (!int.TryParse(goldText, out _gold))
                    {
                        User.SendMessage("That is not a valid amount of gold.");
                        refresh = true;
                    }
                    else
                    {
                        var totalPlat = User.Account.TotalPlat;
                        var totalGold = User.Account.TotalGold;

                        if (totalPlat < _plat || totalGold < _gold)
                        {
                            _plat = User.Account.TotalPlat;
                            _gold = User.Account.TotalGold;
                            User.SendMessage("You do not have that much currency.");
                            refresh = true;
                        }
                        else
                        {
                            Check.Plat = _plat;
                            Check.Gold = _gold;
                            updated = true;
                        }
                    }
                    break;
                }
            case Buttons.AllPlat:
                {
                    _plat = User.Account.TotalPlat;
                    refresh = true;
                    break;
                }
            case Buttons.AllGold:
                {
                    _gold = User.Account.TotalGold;
                    refresh = true;
                    break;
                }
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
