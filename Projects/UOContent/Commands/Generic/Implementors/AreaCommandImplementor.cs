using System;
using System.Collections.Generic;

namespace Server.Commands.Generic
{
    public class AreaCommandImplementor : BaseCommandImplementor
    {
        public AreaCommandImplementor()
        {
            Accessors = new[] { "Area", "Group" };
            SupportRequirement = CommandSupport.Area;
            SupportsConditionals = true;
            AccessLevel = AccessLevel.GameMaster;
            Usage = "Area <command> [condition]";
            Description =
                "Invokes the command on all appropriate objects in a targeted area. Optional condition arguments can further restrict the set of objects.";

            Instance = this;
        }

        public static AreaCommandImplementor Instance { get; private set; }

        public override void Process(Mobile from, BaseCommand command, string[] args)
        {
            BoundingBoxPicker.Begin(from, (map, start, end) => OnTarget(from, map, start, end, command, args));
        }

        public void OnTarget(Mobile from, Map map, Point3D start, Point3D end, BaseCommand command, string[] args)
        {
            try
            {
                var rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);

                var ext = Extensions.Parse(from, ref args);

                if (!CheckObjectTypes(from, command, ext, out var items, out var mobiles))
                {
                    return;
                }

                if (!(items || mobiles))
                {
                    return;
                }

                var eable = map.GetObjectsInBounds(rect);

                var objs = new List<object>();

                foreach (var obj in eable)
                {
                    if (!mobiles && obj is Mobile || !items && obj is Item)
                    {
                        continue;
                    }

                    if (BaseCommand.IsAccessible(from, obj) && ext.IsValid(obj))
                    {
                        objs.Add(obj);
                    }
                }
                ext.Filter(objs);

                RunCommand(from, objs, command, args);
            }
            catch (Exception ex)
            {
                from.SendMessage(ex.Message);
            }
        }
    }
}
