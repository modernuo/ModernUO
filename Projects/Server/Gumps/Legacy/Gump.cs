/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Gump.cs                                                         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;
using Server.Network;
using Server.Utilities;

namespace Server.Gumps;

public class Gump : BaseGump
{
    private int _switches;
    private int _textEntries;

    public Gump(int x, int y)
    {
        X = x;
        Y = y;

        Entries = [];
        Strings = [];
    }

    public List<string> Strings { get; }

    public List<GumpEntry> Entries { get; }

    public bool Disposable { get; set; } = true;

    public bool Resizable { get; set; } = true;

    public bool Draggable { get; set; } = true;

    public bool Closable { get; set; } = true;

    public override int Switches => _switches;

    public override int TextEntries => _textEntries;

    public void AddPage(int page)
    {
        Add(new GumpPage(page));
    }

    public void AddAlphaRegion(int x, int y, int width, int height)
    {
        Add(new GumpAlphaRegion(x, y, width, height));
    }

    public void AddBackground(int x, int y, int width, int height, int gumpID)
    {
        Add(new GumpBackground(x, y, width, height, gumpID));
    }

    public void AddButton(
        int x, int y, int normalID, int pressedID, int buttonID,
        GumpButtonType type = GumpButtonType.Reply, int param = 0
    )
    {
        Add(new GumpButton(x, y, normalID, pressedID, buttonID, type, param));
    }

    public void AddCheck(int x, int y, int inactiveID, int activeID, bool initialState, int switchID)
    {
        Add(new GumpCheck(x, y, inactiveID, activeID, initialState, switchID));
    }

    public void AddGroup(int group)
    {
        Add(new GumpGroup(group));
    }

    public void AddTooltip(int number, string args = null)
    {
        Add(new GumpTooltip(number, args));
    }

    public void AddHtml(
        int x, int y, int width, int height, string text, bool background = false, bool scrollbar = false
    )
    {
        Add(new GumpHtml(x, y, width, height, text, background, scrollbar));
    }

    public void AddHtmlLocalized(
        int x, int y, int width, int height, int number, bool background = false,
        bool scrollbar = false
    )
    {
        Add(new GumpHtmlLocalized(x, y, width, height, number, background, scrollbar));
    }

    public void AddHtmlLocalized(
        int x, int y, int width, int height, int number, int color, bool background = false,
        bool scrollbar = false
    )
    {
        Add(new GumpHtmlLocalized(x, y, width, height, number, color, background, scrollbar));
    }

    public void AddHtmlLocalized(
        int x, int y, int width, int height, int number, string args, int color,
        bool background = false, bool scrollbar = false
    )
    {
        Add(new GumpHtmlLocalized(x, y, width, height, number, args, color, background, scrollbar));
    }

    public void AddImage(int x, int y, int gumpID, int hue = 0)
    {
        Add(new GumpImage(x, y, gumpID, hue));
    }

    public void AddImageTiled(int x, int y, int width, int height, int gumpID)
    {
        Add(new GumpImageTiled(x, y, width, height, gumpID));
    }

    public void AddImageTiledButton(
        int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type,
        int param, int itemID, int hue, int width, int height
    )
    {
        Add(
            new GumpImageTileButton(
                x,
                y,
                normalID,
                pressedID,
                buttonID,
                type,
                param,
                itemID,
                hue,
                width,
                height
            )
        );
    }

    public void AddItem(int x, int y, int itemID, int hue = 0)
    {
        Add(new GumpItem(x, y, itemID, hue));
    }

    public void AddLabel(int x, int y, int hue, string text)
    {
        Add(new GumpLabel(x, y, hue, text));
    }

    public void AddLabelHtml(int x, int y, int width, int height, string text, string hue, int size = 4, bool center = true)
    {
        AddHtml(x, y, width, height, center ? text.Center(hue, size) : text.Color(hue, size));
    }

    public void AddLabelCropped(int x, int y, int width, int height, int hue, string text)
    {
        Add(new GumpLabelCropped(x, y, width, height, hue, text));
    }

    public void AddRadio(int x, int y, int inactiveID, int activeID, bool initialState, int switchID)
    {
        Add(new GumpRadio(x, y, inactiveID, activeID, initialState, switchID));
    }

    public void AddTextEntry(int x, int y, int width, int height, int hue, int entryID, string initialText)
    {
        Add(new GumpTextEntry(x, y, width, height, hue, entryID, initialText));
    }

    public void AddTextEntry(int x, int y, int width, int height, int hue, int entryID, string initialText, int size)
    {
        Add(new GumpTextEntryLimited(x, y, width, height, hue, entryID, initialText, size));
    }

    public void AddItemProperty(Serial serial)
    {
        Add(new GumpItemProperty(serial));
    }

    public void AddSpriteImage(int x, int y, int gumpID, int width, int height, int sx, int sy)
    {
        Add(new GumpSpriteImage(x, y, gumpID, width, height, sx, sy));
    }

    public void AddECHandleInput()
    {
        Add(new GumpECHandleInput());
    }

    public void AddGumpIDOverride(int gumpID)
    {
        Add(new GumpMasterGump(gumpID));
    }

    public void Add(GumpEntry g)
    {
        if (g.Parent != this)
        {
            g.Parent = this;
        }
        else if (!Entries.Contains(g))
        {
            Entries.Add(g);
        }
    }

    public void Remove(GumpEntry g)
    {
        if (g == null || !Entries.Contains(g))
        {
            return;
        }

        Entries.Remove(g);
        g.Parent = null;
    }

    public int Intern(string value)
    {
        var indexOf = Strings.IndexOf(value);

        if (indexOf >= 0)
        {
            return indexOf;
        }

        Strings.Add(value);
        return Strings.Count - 1;
    }

    public override void SendTo(NetState state)
    {
        state.AddGump(this);
        state.SendDisplayGump(this, out _switches, out _textEntries);
    }

    protected void Reset()
    {
        _switches = 0;
        _textEntries = 0;
        Entries.Clear();
        Strings.Clear();
    }
}
