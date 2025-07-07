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
        CommandSystem.Register("ClearXY", AccessLevel.Owner, ClearXY_OnCommand);
    }

    [Usage("ClearFacet")]
    [Description("Deletes all items and mobiles in your facet. Players and their inventory will not be deleted.")]
    public static void ClearFacet_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var map = from.Map;
        var list = GetObjects(entity => entity.Map == map);

        DeleteObjects(list, from, map.Name);
    }

    [Usage("ClearAll")]
    [Description("Deletes all items and mobiles in all facets. Players and their inventory will not be deleted.")]
    public static void ClearAll_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var list = GetObjects();

        DeleteObjects(list, from, "globally");
    }

    [Usage("ClearXY x1 y1 x2 y2")]
    [Description("Deletes all items and mobiles in the specified rectangular area on your facet. Players and their inventory will not be deleted.")]
    public static void ClearXY_OnCommand(CommandEventArgs e)
    {
        if (e.Arguments.Length != 4
            || !int.TryParse(e.Arguments[0], out var x1)
            || !int.TryParse(e.Arguments[1], out var y1)
            || !int.TryParse(e.Arguments[2], out var x2)
            || !int.TryParse(e.Arguments[3], out var y2))
        {
            e.Mobile.SendMessage("Format: ClearXY x1 y1 x2 y2");
            return;
        }

        var from = e.Mobile;
        var map = from.Map;

        if (x2 < x1)
        {
            (x1, x2) = (x2, x1);
        }

        if (y2 < y1)
        {
            (y1, y2) = (y2, y1);
        }

        var rect = new Rectangle2D(x1, y1, x2 - x1 + 1, y2 - y1 + 1);
        List<IEntity> list = [];

        foreach (var item in map.GetItemsInBounds(rect))
        {
            list.Add(item);
        }

        foreach (var mob in map.GetMobilesInBounds(rect))
        {
            if (!mob.Player)
            {
                list.Add(mob);
            }
        }

        DeleteObjects(list, from, $"({x1}, {y1}) to ({x2}, {y2}) in {map.Name}");
    }

    private static List<IEntity> GetObjects(Predicate<IEntity> predicate = null)
    {
        List<IEntity> list = [];

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

    private static void DeleteObjects(List<IEntity> list, Mobile from, string facets)
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
            Content =
                $"You are about to delete {count} object{(count == 1 ? "" : "s")} from {facets}. Do you really wish to continue?";
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

