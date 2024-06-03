/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ClearCommand.cs                                                 *
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
using System;
using System.Collections.Generic;

namespace Server.Commands;

public static class ClearCommands
{
    public static void Configure()
    {
        CommandSystem.Register("ClearFacet", AccessLevel.Owner, ClearFacet_OnCommand);
        CommandSystem.Register("ClearAll", AccessLevel.Owner, ClearAll_OnCommand);
    }

    [Usage("ClearFacet")]
    [Description("Deletes all items and mobiles in your facet. Players and their inventory will not be deleted.")]
    public static void ClearFacet_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var map = from.Map;
        
        FinalStage(GetObjects(entity => entity.Map == map), from, $"the {map} facet");
    }

    [Usage( "ClearAll" )]
    [Description( "Deletes all items and mobiles in all facets. Players and their inventory will not be deleted." )]
    public static void ClearAll_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var map = from.Map;

        FinalStage(GetObjects(), from, "all facets");
    }

// COMMON METHODS //////////////////////////////////////////////////////////////

    private static List<IEntity> GetObjects(Predicate<IEntity> predicate = null)
    {
        var list = new List<IEntity>();

        foreach (var item in World.Items.Values)
        {
            if (item.Parent == null && predicate?.Invoke(item) != false)
            {
                list.Add(item);
            }
        }

        foreach (var mob in World.Mobiles.Values)
        {
            if (!mob.Player && predicate?.Invoke(mob) != false)
            {
                list.Add(mob);
            }
        }

        return list;
    }

    private static void FinalStage(List<IEntity> list, Mobile from, string facets)
    {
        if (list.Count <= 0)
        {
            from.SendMessage("There were no objects found to delete.");
            return;
        }

        CommandLogging.WriteLine(
            from,
            $"{from.AccessLevel} {CommandLogging.Format(from)} started cleaning {facets} ({list.Count} object{(list.Count == 1 ? "" : "s")})"
        );

        from.SendGump(
            new DeleteObjectsNoticeGump(
                list.Count,
                facets,
                okay => DeleteList_Callback(from, okay, list)
            )
        );
    }

    private class DeleteObjectsNoticeGump : StaticWarningGump<DeleteObjectsNoticeGump>
    {
        public override int Width => 360;
        public override int Height => 260;
        public override string Content { get; }

        public DeleteObjectsNoticeGump(int count, string facets, Action<bool> callback) : base(callback) =>
            Content = $"You are about to delete {count} object{(count == 1 ? "" : "s")} from {facets}. Do you really wish to continue?";
    }

    public static void DeleteList_Callback(Mobile from, bool okay, List<IEntity> list)
    {
        if (!okay)
        {
            from.SendMessage("You have chosen not to delete those objects.");
            return;
        }

        CommandLogging.WriteLine(
            from,
            $"{from.AccessLevel} {CommandLogging.Format(from)} deleting {list.Count} object{(list.Count == 1 ? "" : "s")}"
        );

        NetState.FlushAll();

        foreach (var entity in list)
        {
            entity.Delete();
        }

        from.SendMessage($"You have deleted {list.Count} object{(list.Count == 1 ? "" : "s")}.");
    }
}

