using System;
using System.Collections.Generic;
using System.Text;
using Server.Commands.Generic;
using Server.Engines.Help;
using Server.Gumps;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Menus.Questions;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Spells;
using Server.Targeting;
using Server.Targets;

namespace Server.Commands
{
    public static class CommandHandlers
    {
        public static void Initialize()
        {
            CommandSystem.Prefix = ServerConfiguration.GetOrUpdateSetting("commandsystem.prefix", "[");

            Register("Go", AccessLevel.Counselor, Go_OnCommand);

            Register("DropHolding", AccessLevel.Counselor, DropHolding_OnCommand);

            Register("GetFollowers", AccessLevel.GameMaster, GetFollowers_OnCommand);

            Register("ClearFacet", AccessLevel.Administrator, ClearFacet_OnCommand);

            Register("Where", AccessLevel.Counselor, Where_OnCommand);

            Register("AutoPageNotify", AccessLevel.Counselor, APN_OnCommand);
            Register("APN", AccessLevel.Counselor, APN_OnCommand);

            Register("Animate", AccessLevel.GameMaster, Animate_OnCommand);

            Register("Cast", AccessLevel.Counselor, Cast_OnCommand);

            Register("Stuck", AccessLevel.Counselor, Stuck_OnCommand);

            Register("Help", AccessLevel.Player, Help_OnCommand);

            Register("Move", AccessLevel.GameMaster, Move_OnCommand);
            Register("Client", AccessLevel.Counselor, Client_OnCommand);

            Register("SMsg", AccessLevel.Counselor, StaffMessage_OnCommand);
            Register("SM", AccessLevel.Counselor, StaffMessage_OnCommand);
            Register("S", AccessLevel.Counselor, StaffMessage_OnCommand);

            Register("BCast", AccessLevel.GameMaster, BroadcastMessage_OnCommand);
            Register("BC", AccessLevel.GameMaster, BroadcastMessage_OnCommand);
            Register("B", AccessLevel.GameMaster, BroadcastMessage_OnCommand);

            Register("Bank", AccessLevel.GameMaster, Bank_OnCommand);

            Register("Echo", AccessLevel.Counselor, Echo_OnCommand);

            Register("Sound", AccessLevel.GameMaster, Sound_OnCommand);

            Register("ViewEquip", AccessLevel.GameMaster, ViewEquip_OnCommand);

            Register("Light", AccessLevel.Counselor, Light_OnCommand);
            Register("Stats", AccessLevel.Counselor, Stats_OnCommand);

            Register("SpeedBoost", AccessLevel.Counselor, SpeedBoost_OnCommand);
        }

        public static void Register(string command, AccessLevel access, CommandEventHandler handler)
        {
            CommandSystem.Register(command, access, handler);
        }

        [Usage("SpeedBoost [true|false]")]
        [Description("Enables a speed boost for the invoker.  Disable with parameters.")]
        private static void SpeedBoost_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;

            if (e.Length <= 1)
            {
                if (e.Length == 1 && !e.GetBoolean(0))
                {
                    from.NetState.SendSpeedControl(SpeedControlSetting.Disable);
                    from.SendMessage("Speed boost has been disabled.");
                }
                else
                {
                    from.NetState.SendSpeedControl(SpeedControlSetting.Mount);
                    from.SendMessage("Speed boost has been enabled.");
                }
            }
            else
            {
                from.SendMessage("Format: SpeedBoost [true|false]");
            }
        }

        [Usage("Where")]
        [Description("Tells the commanding player his coordinates, region, and facet.")]
        public static void Where_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;
            var map = from.Map;

            from.SendMessage($"You are at {from.X} {from.Y} {from.Z} in {map}.");

            if (map != null)
            {
                var reg = Region.Find(from.Location, from.Map);

                if (!reg.IsDefault)
                {
                    var builder = new StringBuilder();

                    builder.Append(reg);
                    reg = reg.Parent;

                    while (reg != null)
                    {
                        builder.Append($" <- {reg}");
                        reg = reg.Parent;
                    }

                    from.SendMessage($"Your region is {builder}.");
                }
            }
        }

        [Usage("DropHolding")]
        [Description("Drops the item, if any, that a targeted player is holding. The item is placed into their backpack, or if that's full, at their feet.")]
        public static void DropHolding_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, DropHolding_OnTarget);
            e.Mobile.SendMessage("Target the player to drop what they are holding.");
        }

        public static void DropHolding_OnTarget(Mobile from, object obj)
        {
            if (obj is Mobile targ && targ.Player)
            {
                var held = targ.Holding;

                if (held == null)
                {
                    from.SendMessage("They are not holding anything.");
                }
                else
                {
                    if (from.AccessLevel == AccessLevel.Counselor)
                    {
                        var pe = PageQueue.GetEntry(targ);

                        if (pe?.Handler == from)
                        {
                            from.SendMessage("You may only use this command if you are handling their help page.");
                        }
                        else
                        {
                            from.SendMessage("You may only use this command on someone who has paged you.");
                        }

                        return;
                    }

                    if (targ.AddToBackpack(held))
                    {
                        from.SendMessage("The item they were holding has been placed into their backpack.");
                    }
                    else
                    {
                        from.SendMessage("The item they were holding has been placed at their feet.");
                    }

                    held.ClearBounce();

                    targ.Holding = null;
                }
            }
            else
            {
                from.BeginTarget(-1, false, TargetFlags.None, DropHolding_OnTarget);
                from.SendMessage("That is not a player. Try again.");
            }
        }

        public static void DeleteList_Callback(Mobile from, bool okay, List<IEntity> list)
        {
            if (okay)
            {
                CommandLogging.WriteLine(
                    from,
                    $"{from.AccessLevel} {CommandLogging.Format(from)} deleting {list.Count} object{(list.Count == 1 ? "" : "s")}"
                );

                NetState.FlushAll();

                for (var i = 0; i < list.Count; ++i)
                {
                    list[i].Delete();
                }

                if (list.Count == 1)
                {
                    from.SendMessage($"You have deleted {list.Count} object.");
                }
                else
                {
                    from.SendMessage($"You have deleted {list.Count} objects.");
                }
            }
            else
            {
                from.SendMessage("You have chosen not to delete those objects.");
            }
        }

        [Usage("ClearFacet"),
         Description("Deletes all items and mobiles in your facet. Players and their inventory will not be deleted.")]
        public static void ClearFacet_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;
            var map = from.Map;

            if (map == null || map == Map.Internal)
            {
                from.SendMessage("You may not run that command here.");
                return;
            }

            var list = new List<IEntity>();

            foreach (var item in World.Items.Values)
            {
                if (item.Map == map && item.Parent == null)
                {
                    list.Add(item);
                }
            }

            foreach (var m in World.Mobiles.Values)
            {
                if (m.Map == map && !m.Player)
                {
                    list.Add(m);
                }
            }

            if (list.Count > 0)
            {
                CommandLogging.WriteLine(
                    from,
                    $"{from.AccessLevel} {CommandLogging.Format(from)} starting facet clear of {map} ({list.Count} object{(list.Count == 1 ? "" : "s")})"
                );

                from.SendGump(
                    new WarningGump(
                        1060635,
                        30720,
                        $"You are about to delete {list.Count} object{(list.Count == 1 ? "" : "s")} from this facet.  Do you really wish to continue?",
                        0xFFC000,
                        360,
                        260,
                        okay => DeleteList_Callback(from, okay, list)
                    )
                );
            }
            else
            {
                from.SendMessage("There were no objects found to delete.");
            }
        }

        [Usage("GetFollowers")]
        [Description("Teleports all pets of a targeted player to your location.")]
        public static void GetFollowers_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, GetFollowers_OnTarget);
            e.Mobile.SendMessage("Target a player to get their pets.");
        }

        public static void GetFollowers_OnTarget(Mobile from, object obj)
        {
            if (obj is PlayerMobile pm)
            {
                var pets = pm.AllFollowers;

                if (pets.Count > 0)
                {
                    CommandLogging.WriteLine(
                        from,
                        $"{from.AccessLevel} {CommandLogging.Format(from)} getting all followers of {CommandLogging.Format(pm)}"
                    );

                    if (pets.Count == 1)
                    {
                        from.SendMessage($"That player has {pets.Count} pet.");
                    }
                    else
                    {
                        from.SendMessage($"That player has {pets.Count} pets.");
                    }

                    for (var i = 0; i < pets.Count; ++i)
                    {
                        var pet = pets[i];

                        if (pet is IMount mount)
                        {
                            mount.Rider = null; // make sure it's dismounted
                        }

                        pet.MoveToWorld(from.Location, from.Map);
                    }
                }
                else
                {
                    from.SendMessage("There were no pets found for that player.");
                }
            }
            else if (obj is Mobile master && master.Player)
            {
                var pets = new List<BaseCreature>();

                foreach (var m in World.Mobiles.Values)
                {
                    if (m is BaseCreature bc)
                    {
                        if (bc.Controlled && bc.ControlMaster == master || bc.Summoned && bc.SummonMaster == master)
                        {
                            pets.Add(bc);
                        }
                    }
                }

                if (pets.Count > 0)
                {
                    CommandLogging.WriteLine(
                        from,
                        $"{from.AccessLevel} {CommandLogging.Format(from)} getting all followers of {CommandLogging.Format(master)}"
                    );

                    if (pets.Count == 1)
                    {
                        from.SendMessage($"That player has {pets.Count} pet.");
                    }
                    else
                    {
                        from.SendMessage($"That player has {pets.Count} pets.");
                    }

                    for (var i = 0; i < pets.Count; ++i)
                    {
                        Mobile pet = pets[i];

                        if (pet is IMount mount)
                        {
                            mount.Rider = null; // make sure it's dismounted
                        }

                        pet.MoveToWorld(from.Location, from.Map);
                    }
                }
                else
                {
                    from.SendMessage("There were no pets found for that player.");
                }
            }
            else
            {
                from.BeginTarget(-1, false, TargetFlags.None, GetFollowers_OnTarget);
                from.SendMessage("That is not a player. Try again.");
            }
        }

        [Usage("ViewEquip"),
         Description("Lists equipment of a targeted mobile. From the list you can move, delete, or open props.")]
        public static void ViewEquip_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new ViewEqTarget();
        }

        [Usage("Sound <index> [toAll=true]")]
        [Description("Plays a sound to players within 12 tiles of you. The (toAll) argument specifies to everyone, or just those who can see you.")]
        public static void Sound_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 1)
            {
                PlaySound(e.Mobile, e.GetInt32(0), true);
            }
            else if (e.Length == 2)
            {
                PlaySound(e.Mobile, e.GetInt32(0), e.GetBoolean(1));
            }
            else
            {
                e.Mobile.SendMessage("Format: Sound <index> [toAll]");
            }
        }

        private static void PlaySound(Mobile m, int index, bool toAll)
        {
            var map = m.Map;

            if (map == null)
            {
                return;
            }

            CommandLogging.WriteLine(
                m,
                $"{m.AccessLevel} {CommandLogging.Format(m)} playing sound {index} (toAll={toAll})"
            );

            Span<byte> buffer = stackalloc byte[OutgoingEffectPackets.SoundPacketLength].InitializePacket();

            foreach (var state in m.GetClientsInRange(12))
            {
                if (toAll || state.Mobile.CanSee(m))
                {
                    OutgoingEffectPackets.CreateSoundEffect(buffer, index, m);
                    state.Send(buffer);
                }
            }
        }

        [Usage("Echo <text>")]
        [Description("Relays (text) as a system message.")]
        public static void Echo_OnCommand(CommandEventArgs e)
        {
            var toEcho = e.ArgString.Trim();

            if (toEcho.Length > 0)
            {
                e.Mobile.SendMessage(toEcho);
            }
            else
            {
                e.Mobile.SendMessage("Format: Echo \"<text>\"");
            }
        }

        [Usage("Bank")]
        [Description("Opens the bank box of a given target.")]
        public static void Bank_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new BankTarget();
        }

        [Usage("Client")]
        [Description("Opens the client gump menu for a given player.")]
        private static void Client_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new ClientTarget();
        }

        [Usage("Move")]
        [Description("Repositions a targeted item or mobile.")]
        private static void Move_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new PickMoveTarget();
        }

        private static bool FixMap(ref Map map, ref Point3D loc, Item item) =>
            map != null && map != Map.Internal || item.RootParent is Mobile m &&
            FixMap(ref map, ref loc, m);

        private static bool FixMap(ref Map map, ref Point3D loc, Mobile m)
        {
            var validMap = map != null && map != Map.Internal;

            if (!validMap)
            {
                map = m.LogoutMap;
                loc = m.LogoutLocation;
            }

            return validMap;
        }

        [Usage("Go [name | serial | (x y [z]) | (deg min (N | S) deg min (E | W))]")]
        [Description("With no arguments, this command brings up the go menu. With one argument, (name), you are moved to that regions \"go location.\" Or, if a numerical value is specified for one argument, (serial), you are moved to that object. Two or three arguments, (x y [z]), will move your character to that location. When six arguments are specified, (deg min (N | S) deg min (E | W)), your character will go to an approximate of those sextant coordinates.")]
        private static void Go_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;

            if (e.Length == 0)
            {
                GoGump.DisplayTo(from);
                return;
            }

            if (e.Length == 1)
            {
                try
                {
                    var ser = (Serial)e.GetUInt32(0);

                    var ent = World.FindEntity(ser);

                    if (ent is Item item)
                    {
                        var map = item.Map;
                        var loc = item.GetWorldLocation();

                        var owner = item.RootParent as Mobile;

                        if (owner?.Map != null && owner.Map != Map.Internal &&
                            !BaseCommand.IsAccessible(from, owner) /* !from.CanSee( owner )*/)
                        {
                            from.SendMessage("You can not go to what you can not see.");
                            return;
                        }

                        if (owner != null && (owner.Map == null || owner.Map == Map.Internal) && owner.Hidden &&
                            owner.AccessLevel >= from.AccessLevel)
                        {
                            from.SendMessage("You can not go to what you can not see.");
                            return;
                        }

                        if (!FixMap(ref map, ref loc, item))
                        {
                            from.SendMessage("That is an internal item and you cannot go to it.");
                            return;
                        }

                        from.MoveToWorld(loc, map);

                        return;
                    }

                    if (ent is Mobile m)
                    {
                        var map = m.Map;
                        var loc = m.Location;

                        var owner = m;

                        if (owner.Map != null && owner.Map != Map.Internal &&
                            !BaseCommand.IsAccessible(from, owner) /* !from.CanSee( owner )*/)
                        {
                            from.SendMessage("You can not go to what you can not see.");
                            return;
                        }

                        if ((owner.Map == null || owner.Map == Map.Internal) && owner.Hidden &&
                            owner.AccessLevel >= from.AccessLevel)
                        {
                            from.SendMessage("You can not go to what you can not see.");
                            return;
                        }

                        if (!FixMap(ref map, ref loc, m))
                        {
                            from.SendMessage("That is an internal mobile and you cannot go to it.");
                            return;
                        }

                        from.MoveToWorld(loc, map);

                        return;
                    }
                    else
                    {
                        var name = e.GetString(0);
                        Map map;

                        for (var i = 0; i < Map.AllMaps.Count; ++i)
                        {
                            map = Map.AllMaps[i];

                            if (map.MapIndex is 0x7F or 0xFF)
                            {
                                continue;
                            }

                            if (name.InsensitiveEquals(map.Name))
                            {
                                from.Map = map;
                                return;
                            }
                        }

                        var list = from.Map.Regions;

                        foreach (var kvp in list)
                        {
                            var r = kvp.Value;

                            if (r.Name.InsensitiveEquals(name))
                            {
                                from.Location = new Point3D(r.GoLocation);
                                return;
                            }
                        }

                        for (var i = 0; i < Map.AllMaps.Count; ++i)
                        {
                            map = Map.AllMaps[i];

                            if (map.MapIndex is 0x7F or 0xFF || from.Map == map)
                            {
                                continue;
                            }

                            foreach (var r in map.Regions.Values)
                            {
                                if (r.Name.InsensitiveEquals(name))
                                {
                                    from.MoveToWorld(r.GoLocation, map);
                                    return;
                                }
                            }
                        }

                        if (ser != 0)
                        {
                            from.SendMessage("No object with that serial was found.");
                        }
                        else
                        {
                            from.SendMessage("No region with that name was found.");
                        }

                        return;
                    }
                }
                catch
                {
                    // ignored
                }

                from.SendMessage("Region name not found");
            }
            else if (e.Length is 2 or 3)
            {
                var map = from.Map;

                if (map != null)
                {
                    try
                    {
                        /*
                         * This to avoid being teleported to (0,0) if trying to teleport
                         * to a region with spaces in its name.
                         */
                        var x = int.Parse(e.GetString(0));
                        var y = int.Parse(e.GetString(1));
                        var z = e.Length == 3 ? int.Parse(e.GetString(2)) : map.GetAverageZ(x, y);

                        from.Location = new Point3D(x, y, z);
                    }
                    catch
                    {
                        from.SendMessage("Region name not found.");
                    }
                }
            }
            else if (e.Length == 6)
            {
                var map = from.Map;

                if (map != null)
                {
                    var p = Sextant.ReverseLookup(
                        map,
                        e.GetInt32(3),
                        e.GetInt32(0),
                        e.GetInt32(4),
                        e.GetInt32(1),
                        e.GetString(5).InsensitiveEquals("E"),
                        e.GetString(2).InsensitiveEquals("S")
                    );

                    if (p != Point3D.Zero)
                    {
                        from.Location = p;
                    }
                    else
                    {
                        from.SendMessage("Sextant reverse lookup failed.");
                    }
                }
            }
            else
            {
                from.SendMessage("Format: Go [name | serial | (x y [z]) | (deg min (N | S) deg min (E | W)]");
            }
        }

        [Usage("Help")]
        [Description("Lists all available commands.")]
        public static void Help_OnCommand(CommandEventArgs e)
        {
            var m = e.Mobile;

            var list = new List<CommandEntry>();

            foreach (var entry in CommandSystem.Entries.Values)
            {
                if (m.AccessLevel >= entry.AccessLevel)
                {
                    list.Add(entry);
                }
            }

            list.Sort();

            var sb = new StringBuilder();

            if (list.Count > 0)
            {
                sb.Append(list[0].Command);
            }

            for (var i = 1; i < list.Count; ++i)
            {
                var v = list[i].Command;

                if (sb.Length + 1 + v.Length >= 256)
                {
                    m.SendAsciiMessage(0x482, sb.ToString());
                    sb = new StringBuilder();
                    sb.Append(v);
                }
                else
                {
                    sb.Append(' ');
                    sb.Append(v);
                }
            }

            if (sb.Length > 0)
            {
                m.SendAsciiMessage(0x482, sb.ToString());
            }
        }

        [Usage("SMsg <text>"), Aliases("S", "SM")]
        [Description("Broadcasts a message to all online staff.")]
        public static void StaffMessage_OnCommand(CommandEventArgs e)
        {
            BroadcastMessage(AccessLevel.Counselor, e.Mobile.SpeechHue, $"[{e.Mobile.Name}] {e.ArgString}");
        }

        [Usage("BCast <text>"), Aliases("B", "BC")]
        [Description("Broadcasts a message to everyone online.")]
        public static void BroadcastMessage_OnCommand(CommandEventArgs e)
        {
            BroadcastMessage(AccessLevel.Player, 0x482, $"Staff message from {e.Mobile.Name}:");
            BroadcastMessage(AccessLevel.Player, 0x482, e.ArgString);
        }

        public static void BroadcastMessage(AccessLevel ac, int hue, string message)
        {
            foreach (var state in TcpServer.Instances)
            {
                var m = state.Mobile;

                if (m?.AccessLevel >= ac)
                {
                    m.SendMessage(hue, message);
                }
            }
        }

        [Usage("AutoPageNotify"), Aliases("APN")]
        [Description("Toggles your auto-page-notify status.")]
        public static void APN_OnCommand(CommandEventArgs e)
        {
            var m = e.Mobile;

            m.AutoPageNotify = !m.AutoPageNotify;

            if (m.AutoPageNotify)
            {
                m.SendMessage($"Your auto-page-notify has been turned on.");
            }
            else
            {
                m.SendMessage($"Your auto-page-notify has been turned off.");
            }
        }

        [Usage("Animate <action> <frameCount> <repeatCount> <forward> <repeat> <delay>"),
         Description("Makes your character do a specified animation.")]
        public static void Animate_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 6)
            {
                e.Mobile.Animate(
                    e.GetInt32(0),
                    e.GetInt32(1),
                    e.GetInt32(2),
                    e.GetBoolean(3),
                    e.GetBoolean(4),
                    e.GetInt32(5)
                );
            }
            else
            {
                e.Mobile.SendMessage("Format: Animate <action> <frameCount> <repeatCount> <forward> <repeat> <delay>");
            }
        }

        [Usage("Cast <name>")]
        [Description("Casts a spell by name.")]
        public static void Cast_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 1)
            {
                if (!DesignContext.Check(e.Mobile))
                {
                    return; // They are customizing
                }

                var spell = SpellRegistry.NewSpell(e.GetString(0), e.Mobile, null);

                if (spell != null)
                {
                    spell.Cast();
                }
                else
                {
                    e.Mobile.SendMessage("That spell was not found.");
                }
            }
            else
            {
                e.Mobile.SendMessage("Format: Cast <name>");
            }
        }

        [Usage("Stuck")]
        [Description("Opens a menu of towns, used for teleporting stuck mobiles.")]
        public static void Stuck_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new StuckMenuTarget();
        }

        [Usage("Light <level>")]
        [Description("Set your local lightlevel.")]
        public static void Light_OnCommand(CommandEventArgs e)
        {
            e.Mobile.LightLevel = e.GetInt32(0);
        }

        [Usage("Stats")]
        [Description("View some stats about the server.")]
        public static void Stats_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage($"Open Connections: {TcpServer.Instances.Count}");
            e.Mobile.SendMessage($"Mobiles: {World.Mobiles.Count}");
            e.Mobile.SendMessage($"Items: {World.Items.Count}");
        }

        private class ViewEqTarget : Target
        {
            public ViewEqTarget() : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!BaseCommand.IsAccessible(from, targeted))
                {
                    from.SendLocalizedMessage(500447); // That is not accessible.
                    return;
                }

                if (targeted is Mobile mobile)
                {
                    from.SendMenu(new EquipMenu(from, mobile, GetEquip(mobile)));
                }
            }

            private static ItemListEntry[] GetEquip(Mobile m)
            {
                var entries = new ItemListEntry[m.Items.Count];

                for (var i = 0; i < m.Items.Count; ++i)
                {
                    var item = m.Items[i];

                    entries[i] = new ItemListEntry($"{item.Layer}: {item.GetType().Name}", item.ItemID, item.Hue);
                }

                return entries;
            }

            private class EquipMenu : ItemListMenu
            {
                private readonly Mobile m_Mobile;

                public EquipMenu(Mobile from, Mobile m, ItemListEntry[] entries) : base("Equipment", entries)
                {
                    m_Mobile = m;

                    CommandLogging.WriteLine(
                        from,
                        $"{from.AccessLevel} {CommandLogging.Format(from)} viewing equipment of {CommandLogging.Format(m)}"
                    );
                }

                public override void OnResponse(NetState state, int index)
                {
                    if (index >= 0 && index < m_Mobile.Items.Count)
                    {
                        var item = m_Mobile.Items[index];

                        state.Mobile.SendMenu(new EquipDetailsMenu(m_Mobile, item));
                    }
                }

                private class EquipDetailsMenu : QuestionMenu
                {
                    private readonly Item m_Item;
                    private readonly Mobile m_Mobile;

                    public EquipDetailsMenu(Mobile m, Item item) : base(
                        $"{item.Layer}: {item.GetType().Name}",
                        new[] { "Move", "Delete", "Props" }
                    )
                    {
                        m_Mobile = m;
                        m_Item = item;
                    }

                    public override void OnCancel(NetState state)
                    {
                        state.Mobile.SendMenu(new EquipMenu(state.Mobile, m_Mobile, GetEquip(m_Mobile)));
                    }

                    public override void OnResponse(NetState state, int index)
                    {
                        if (index == 0)
                        {
                            CommandLogging.WriteLine(
                                state.Mobile,
                                $"{state.Mobile.AccessLevel} {CommandLogging.Format(state.Mobile)} moving equipment item {CommandLogging.Format(m_Item)} of {CommandLogging.Format(m_Mobile)}"
                            );
                            state.Mobile.Target = new MoveTarget(m_Item);
                        }
                        else if (index == 1)
                        {
                            CommandLogging.WriteLine(
                                state.Mobile,
                                $"{state.Mobile.AccessLevel} {CommandLogging.Format(state.Mobile)} deleting equipment item {CommandLogging.Format(m_Item)} of {CommandLogging.Format(m_Mobile)}"
                            );
                            m_Item.Delete();
                        }
                        else if (index == 2)
                        {
                            CommandLogging.WriteLine(
                                state.Mobile,
                                $"{state.Mobile.AccessLevel} {CommandLogging.Format(state.Mobile)} opening properties for equipment item {CommandLogging.Format(m_Item)} of {CommandLogging.Format(m_Mobile)}"
                            );
                            state.Mobile.SendGump(new PropertiesGump(state.Mobile, m_Item));
                        }
                    }
                }
            }
        }

        private class BankTarget : Target
        {
            public BankTarget() : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile m)
                {
                    var box = m.Player ? m.BankBox : m.FindBankNoCreate();

                    if (box != null)
                    {
                        CommandLogging.WriteLine(
                            from,
                            $"{from.AccessLevel} {CommandLogging.Format(from)} opening bank box of {CommandLogging.Format(m)}"
                        );

                        if (from == m)
                        {
                            box.Open();
                        }
                        else
                        {
                            box.DisplayTo(from);
                        }
                    }
                    else
                    {
                        from.SendMessage("They have no bank box.");
                    }
                }
            }
        }

        private class ClientTarget : Target
        {
            public ClientTarget() : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile targ && targ.NetState != null)
                {
                    CommandLogging.WriteLine(
                        from,
                        $"{from.AccessLevel} {CommandLogging.Format(from)} opening client menu of {CommandLogging.Format(targ)}"
                    );
                    from.SendGump(new ClientGump(from, targ.NetState));
                }
            }
        }

        private class StuckMenuTarget : Target
        {
            public StuckMenuTarget() : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile mobile)
                {
                    if (mobile.AccessLevel >= from.AccessLevel && mobile != from)
                    {
                        from.SendMessage("You can't do that to someone with higher Accesslevel than you!");
                    }
                    else
                    {
                        from.SendGump(new StuckMenu(from, mobile, false));
                    }
                }
            }
        }
    }
}
