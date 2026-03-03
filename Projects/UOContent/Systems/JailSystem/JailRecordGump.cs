/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: JailRecordGump.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Systems.JailSystem;

public class JailRecordGump : StaticGump<JailRecordGump>
{
    private readonly PlayerMobile _player;
    private readonly JailRecord _record;

    public JailRecordGump(PlayerMobile player, JailRecord record) : base(0, 0)
    {
        _player = player;
        _record = record;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.SetNoResize();

        builder.AddPage();
        builder.AddBackground(0, 0, 330, 300, 9200);
        builder.AddAlphaRegion(10, 10, 310, 280);

        builder.AddHtml(20, 20, 360, 30, "<BASEFONT size=6 color=#FF6600>JAIL RECORD</BASEFONT>");
        builder.AddHtmlPlaceholder(20, 60, 360, 25, "playerName");
        builder.AddHtmlPlaceholder(20, 80, 360, 25, "jailCount");
        builder.AddHtmlPlaceholder(20, 100, 360, 25, "lastJailed");
        builder.AddHtmlPlaceholder(20, 120, 290, 25, "jailReason");

        builder.AddHtmlPlaceholder(20, 150, 360, 25, "jailStatus");
        builder.AddHtmlPlaceholder(20, 170, 360, 25, "jailTime");

        builder.AddHtml(20, 200, 360, 25, "<BASEFONT size=4 color=#CCCCCC>If you believe you were jailed in error,</BASEFONT>");
        builder.AddHtml(20, 220, 360, 25, "<BASEFONT size=4 color=#CCCCCC>please contact staff through normal channels.</BASEFONT>");
        builder.AddHtml(20, 240, 360, 25, "<BASEFONT size=4 color=#CCCCCC>Each time you are jailed, the wait increases.</BASEFONT>");
        builder.AddHtml(20, 260, 360, 25, "<BASEFONT size=4 color=#CCCCCC>Follow all shard rules to avoid future jail time.</BASEFONT>");
    }

    protected override void BuildStrings(ref GumpStringsBuilder builder)
    {
        builder.SetHtmlText("playerName", $"Player: {_player.Name}", "#FFFF00", 5);
        builder.SetHtmlText("jailCount", $"Jail Count: {_record.JailCount}", "#FFFFFF", 5);

        if (_record.LastJailed > DateTime.MinValue)
        {
            builder.SetHtmlText("lastJailed", $"Last Jailed: {_record.LastJailed:yyyy/MM/dd HH:mm}", "#FFFFFF", 5);
        }
        else
        {
            builder.SetHtmlText("lastJailed", "Last Jailed: Never", "#FFFFFF", 5);
        }

        if (string.IsNullOrWhiteSpace(_record.LastJailReason))
        {
            builder.SetHtmlText("jailReason", "Jail Reason: None given.", "#FFFFFF", 4);
        }
        else
        {
            builder.SetHtmlText("jailReason", $"Jail Reason: {_record.LastJailReason}", "#FFFFFF", 4);
        }

        if (_record.IsCurrentlyJailed)
        {
            builder.SetHtmlText("jailStatus", "Status: Currently Jailed", "#FF6666", 5);
            builder.SetHtmlText("jailTime", $"Jail Time: {(_record.JailEndTime - Core.Now).FormatTimeCompact()}", "#FF6666", 5);
        }
        else
        {
            builder.SetHtmlText("jailStatus", "Status: Not Jailed", "#00FF00", 5);
            builder.SetStringSlot("jailTime", "");
        }
    }
}
