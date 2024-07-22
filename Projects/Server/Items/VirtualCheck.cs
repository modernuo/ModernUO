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

using ModernUO.Serialization;
using Server.Gumps;
using System;

namespace Server.Items;

[SerializationGenerator(0, false)]
public sealed partial class VirtualCheck : Item
{
    public static bool UseEditGump { get; private set; }
    public static unsafe delegate*<Mobile, VirtualCheck, IVirtualCheckGump> GumpActivator { get; set; }

    public static unsafe void Configure()
    {
        UseEditGump = ServerConfiguration.GetSetting("virtualChecks.useEditGump", Core.TOL);

        if (UseEditGump && GumpActivator is null)
        {
            throw new NullReferenceException(nameof(GumpActivator));
        }
    }

    private int _gold;

    private int _plat;

    public VirtualCheck(int plat = 0, int gold = 0) : base(0x14F0)
    {
        Plat = plat;
        Gold = gold;

        Movable = false;
    }

    public override bool IsVirtualItem => true;
    public override bool DisplayWeight => false;
    public override bool DisplayLootType => false;
    public override double DefaultWeight => 0;
    public override string DefaultName => "Offer Of Currency";

    public IVirtualCheckGump Editor { get; private set; }

    [CommandProperty(AccessLevel.Administrator)]
    public int Plat
    {
        get => _plat;
        set
        {
            _plat = value;
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.Administrator)]
    public int Gold
    {
        get => _gold;
        set
        {
            _gold = value;
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

    public override unsafe void OnDoubleClickSecureTrade(Mobile from)
    {
        if (UseEditGump && IsAccessibleTo(from))
        {
            if (Editor?.Check?.Deleted != false)
            {
                Editor = GumpActivator(from, this);
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

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        Delete();
    }
}
