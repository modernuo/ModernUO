using System.Collections.Generic;
using System.Text;
using Server.Commands.Generic;
using Server.Engines.Help;
using Server.Gumps;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Menus.Questions;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Spells;
using Server.Targeting;
using Server.Targets;

namespace Server.Commands
{
  public class CommandHandlers
  {
    public static void Initialize()
    {
      CommandSystem.Prefix = "[";

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

      Register("Save", AccessLevel.Administrator, Save_OnCommand);
      Register("BackgroundSave", AccessLevel.Administrator, BackgroundSave_OnCommand);
      Register("BGSave", AccessLevel.Administrator, BackgroundSave_OnCommand);
      Register("SaveBG", AccessLevel.Administrator, BackgroundSave_OnCommand);

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
      Mobile from = e.Mobile;

      if (e.Length <= 1)
      {
        if (e.Length == 1 && !e.GetBoolean(0))
        {
          from.Send(SpeedControl.Disable);
          from.SendMessage("Speed boost has been disabled.");
        }
        else
        {
          from.Send(SpeedControl.MountSpeed);
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
      Mobile from = e.Mobile;
      Map map = from.Map;

      from.SendMessage("You are at {0} {1} {2} in {3}.", from.X, from.Y, from.Z, map);

      if (map != null)
      {
        Region reg = from.Region;

        if (!reg.IsDefault)
        {
          StringBuilder builder = new StringBuilder();

          builder.Append(reg);
          reg = reg.Parent;

          while (reg != null)
          {
            builder.Append(" <- " + reg);
            reg = reg.Parent;
          }

          from.SendMessage("Your region is {0}.", builder.ToString());
        }
      }
    }

    [Usage("DropHolding")]
    [Description(
      "Drops the item, if any, that a targeted player is holding. The item is placed into their backpack, or if that's full, at their feet.")]
    public static void DropHolding_OnCommand(CommandEventArgs e)
    {
      e.Mobile.BeginTarget(-1, false, TargetFlags.None, DropHolding_OnTarget);
      e.Mobile.SendMessage("Target the player to drop what they are holding.");
    }

    public static void DropHolding_OnTarget(Mobile from, object obj)
    {
      if (obj is Mobile targ && targ.Player)
      {
        Item held = targ.Holding;

        if (held == null)
        {
          from.SendMessage("They are not holding anything.");
        }
        else
        {
          if (from.AccessLevel == AccessLevel.Counselor)
          {
            PageEntry pe = PageQueue.GetEntry(targ);

            if (pe?.Handler == from)
              from.SendMessage("You may only use this command if you are handling their help page.");
            else
              from.SendMessage("You may only use this command on someone who has paged you.");

            return;
          }

          if (targ.AddToBackpack(held))
            from.SendMessage("The item they were holding has been placed into their backpack.");
          else
            from.SendMessage("The item they were holding has been placed at their feet.");

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
        CommandLogging.WriteLine(from, "{0} {1} deleting {2} object{3}", from.AccessLevel,
          CommandLogging.Format(from), list.Count, list.Count == 1 ? "" : "s");

        NetState.Pause();

        for (int i = 0; i < list.Count; ++i)
          list[i].Delete();

        NetState.Resume();

        from.SendMessage("You have deleted {0} object{1}.", list.Count, list.Count == 1 ? "" : "s");
      }
      else
      {
        from.SendMessage("You have chosen not to delete those objects.");
      }
    }

    [Usage("ClearFacet")]
    [Description("Deletes all items and mobiles in your facet. Players and their inventory will not be deleted.")]
    public static void ClearFacet_OnCommand(CommandEventArgs e)
    {
      Mobile from = e.Mobile;
      Map map = from.Map;

      if (map == null || map == Map.Internal)
      {
        from.SendMessage("You may not run that command here.");
        return;
      }

      List<IEntity> list = new List<IEntity>();

      foreach (Item item in World.Items.Values)
        if (item.Map == map && item.Parent == null)
          list.Add(item);

      foreach (Mobile m in World.Mobiles.Values)
        if (m.Map == map && !m.Player)
          list.Add(m);

      if (list.Count > 0)
      {
        CommandLogging.WriteLine(from, "{0} {1} starting facet clear of {2} ({3} object{4})",
          from.AccessLevel, CommandLogging.Format(from), map, list.Count, list.Count == 1 ? "" : "s");

        from.SendGump(
          new WarningGump(1060635, 30720,
            $"You are about to delete {list.Count} object{(list.Count == 1 ? "" : "s")} from this facet.  Do you really wish to continue?",
            0xFFC000, 360, 260, okay => DeleteList_Callback(from, okay, list)));
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
        List<Mobile> pets = pm.AllFollowers;

        if (pets.Count > 0)
        {
          CommandLogging.WriteLine(from, "{0} {1} getting all followers of {2}", from.AccessLevel,
            CommandLogging.Format(from), CommandLogging.Format(pm));

          from.SendMessage("That player has {0} pet{1}.", pets.Count, pets.Count != 1 ? "s" : "");

          for (int i = 0; i < pets.Count; ++i)
          {
            Mobile pet = pets[i];

            if (pet is IMount mount)
              mount.Rider = null; // make sure it's dismounted

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
        List<BaseCreature> pets = new List<BaseCreature>();

        foreach (Mobile m in World.Mobiles.Values)
          if (m is BaseCreature bc)
            if (bc.Controlled && bc.ControlMaster == master || bc.Summoned && bc.SummonMaster == master)
              pets.Add(bc);

        if (pets.Count > 0)
        {
          CommandLogging.WriteLine(from, "{0} {1} getting all followers of {2}", from.AccessLevel,
            CommandLogging.Format(from), CommandLogging.Format(master));

          from.SendMessage("That player has {0} pet{1}.", pets.Count, pets.Count != 1 ? "s" : "");

          for (int i = 0; i < pets.Count; ++i)
          {
            Mobile pet = pets[i];

            if (pet is IMount mount)
              mount.Rider = null; // make sure it's dismounted

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

    [Usage("ViewEquip")]
    [Description("Lists equipment of a targeted mobile. From the list you can move, delete, or open props.")]
    public static void ViewEquip_OnCommand(CommandEventArgs e)
    {
      e.Mobile.Target = new ViewEqTarget();
    }

    [Usage("Sound <index> [toAll=true]")]
    [Description(
      "Plays a sound to players within 12 tiles of you. The (toAll) argument specifies to everyone, or just those who can see you.")]
    public static void Sound_OnCommand(CommandEventArgs e)
    {
      if (e.Length == 1)
        PlaySound(e.Mobile, e.GetInt32(0), true);
      else if (e.Length == 2)
        PlaySound(e.Mobile, e.GetInt32(0), e.GetBoolean(1));
      else
        e.Mobile.SendMessage("Format: Sound <index> [toAll]");
    }

    private static void PlaySound(Mobile m, int index, bool toAll)
    {
      Map map = m.Map;

      if (map == null)
        return;

      CommandLogging.WriteLine(m, "{0} {1} playing sound {2} (toAll={3})", m.AccessLevel, CommandLogging.Format(m),
        index, toAll);

      Packet p = new PlaySound(index, m.Location);

      p.Acquire();

      foreach (NetState state in m.GetClientsInRange(12))
        if (toAll || state.Mobile.CanSee(m))
          state.Send(p);

      p.Release();
    }

    [Usage("Echo <text>")]
    [Description("Relays (text) as a system message.")]
    public static void Echo_OnCommand(CommandEventArgs e)
    {
      string toEcho = e.ArgString.Trim();

      if (toEcho.Length > 0)
        e.Mobile.SendMessage(toEcho);
      else
        e.Mobile.SendMessage("Format: Echo \"<text>\"");
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

    [Usage("Save")]
    [Description("Saves the world.")]
    private static void Save_OnCommand(CommandEventArgs e)
    {
      AutoSave.Save();
    }

    [Usage("BackgroundSave")]
    [Aliases("BGSave", "SaveBG")]
    [Description("Saves the world, writing to the disk in the background")]
    private static void BackgroundSave_OnCommand(CommandEventArgs e)
    {
      AutoSave.Save(true);
    }

    private static bool FixMap(ref Map map, ref Point3D loc, Item item)
    {
      return map != null && map != Map.Internal || item.RootParent is Mobile m && FixMap(ref map, ref loc, m);
    }

    private static bool FixMap(ref Map map, ref Point3D loc, Mobile m)
    {
      bool validMap = map != null && map != Map.Internal;

      if (!validMap)
      {
        map = m.LogoutMap;
        loc = m.LogoutLocation;
      }

      return validMap;
    }

    [Usage("Go [name | serial | (x y [z]) | (deg min (N | S) deg min (E | W))]")]
    [Description(
      "With no arguments, this command brings up the go menu. With one argument, (name), you are moved to that regions \"go location.\" Or, if a numerical value is specified for one argument, (serial), you are moved to that object. Two or three arguments, (x y [z]), will move your character to that location. When six arguments are specified, (deg min (N | S) deg min (E | W)), your character will go to an approximate of those sextant coordinates.")]
    private static void Go_OnCommand(CommandEventArgs e)
    {
      Mobile from = e.Mobile;

      if (e.Length == 0)
      {
        GoGump.DisplayTo(from);
        return;
      }

      if (e.Length == 1)
      {
        try
        {
          uint ser = e.GetUInt32(0);

          IEntity ent = World.FindEntity(ser);

          if (ent is Item item)
          {
            Map map = item.Map;
            Point3D loc = item.GetWorldLocation();

            Mobile owner = item.RootParent as Mobile;

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
            Map map = m.Map;
            Point3D loc = m.Location;

            Mobile owner = m;

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
            string name = e.GetString(0);
            Map map;

            for (int i = 0; i < Map.AllMaps.Count; ++i)
            {
              map = Map.AllMaps[i];

              if (map.MapIndex == 0x7F || map.MapIndex == 0xFF)
                continue;

              if (Insensitive.Equals(name, map.Name))
              {
                from.Map = map;
                return;
              }
            }

            Dictionary<string, Region> list = from.Map.Regions;

            foreach (KeyValuePair<string, Region> kvp in list)
            {
              Region r = kvp.Value;

              if (Insensitive.Equals(r.Name, name))
              {
                from.Location = new Point3D(r.GoLocation);
                return;
              }
            }

            for (int i = 0; i < Map.AllMaps.Count; ++i)
            {
              map = Map.AllMaps[i];

              if (map.MapIndex == 0x7F || map.MapIndex == 0xFF || from.Map == map)
                continue;

              foreach (Region r in map.Regions.Values)
                if (Insensitive.Equals(r.Name, name))
                {
                  from.MoveToWorld(r.GoLocation, map);
                  return;
                }
            }

            if (ser != 0)
              from.SendMessage("No object with that serial was found.");
            else
              from.SendMessage("No region with that name was found.");

            return;
          }
        }
        catch
        {
          // ignored
        }

        from.SendMessage("Region name not found");
      }
      else if (e.Length == 2 || e.Length == 3)
      {
        Map map = from.Map;

        if (map != null)
          try
          {
            /*
             * This to avoid being teleported to (0,0) if trying to teleport
             * to a region with spaces in its name.
             */
            int x = int.Parse(e.GetString(0));
            int y = int.Parse(e.GetString(1));
            int z = e.Length == 3 ? int.Parse(e.GetString(2)) : map.GetAverageZ(x, y);

            from.Location = new Point3D(x, y, z);
          }
          catch
          {
            from.SendMessage("Region name not found.");
          }
      }
      else if (e.Length == 6)
      {
        Map map = from.Map;

        if (map != null)
        {
          Point3D p = Sextant.ReverseLookup(map, e.GetInt32(3), e.GetInt32(0), e.GetInt32(4), e.GetInt32(1),
            Insensitive.Equals(e.GetString(5), "E"), Insensitive.Equals(e.GetString(2), "S"));

          if (p != Point3D.Zero)
            from.Location = p;
          else
            from.SendMessage("Sextant reverse lookup failed.");
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
      Mobile m = e.Mobile;

      List<CommandEntry> list = new List<CommandEntry>();

      foreach (CommandEntry entry in CommandSystem.Entries.Values)
        if (m.AccessLevel >= entry.AccessLevel)
          list.Add(entry);

      list.Sort();

      StringBuilder sb = new StringBuilder();

      if (list.Count > 0)
        sb.Append(list[0].Command);

      for (int i = 1; i < list.Count; ++i)
      {
        string v = list[i].Command;

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
        m.SendAsciiMessage(0x482, sb.ToString());
    }

    [Usage("SMsg <text>")]
    [Aliases("S", "SM")]
    [Description("Broadcasts a message to all online staff.")]
    public static void StaffMessage_OnCommand(CommandEventArgs e)
    {
      BroadcastMessage(AccessLevel.Counselor, e.Mobile.SpeechHue, $"[{e.Mobile.Name}] {e.ArgString}");
    }

    [Usage("BCast <text>")]
    [Aliases("B", "BC")]
    [Description("Broadcasts a message to everyone online.")]
    public static void BroadcastMessage_OnCommand(CommandEventArgs e)
    {
      BroadcastMessage(AccessLevel.Player, 0x482, $"Staff message from {e.Mobile.Name}:");
      BroadcastMessage(AccessLevel.Player, 0x482, e.ArgString);
    }

    public static void BroadcastMessage(AccessLevel ac, int hue, string message)
    {
      foreach (NetState state in NetState.Instances)
      {
        Mobile m = state.Mobile;

        if (m?.AccessLevel >= ac)
          m.SendMessage(hue, message);
      }
    }

    [Usage("AutoPageNotify")]
    [Aliases("APN")]
    [Description("Toggles your auto-page-notify status.")]
    public static void APN_OnCommand(CommandEventArgs e)
    {
      Mobile m = e.Mobile;

      m.AutoPageNotify = !m.AutoPageNotify;

      m.SendMessage("Your auto-page-notify has been turned {0}.", m.AutoPageNotify ? "on" : "off");
    }

    [Usage("Animate <action> <frameCount> <repeatCount> <forward> <repeat> <delay>")]
    [Description("Makes your character do a specified animation.")]
    public static void Animate_OnCommand(CommandEventArgs e)
    {
      if (e.Length == 6)
        e.Mobile.Animate(e.GetInt32(0), e.GetInt32(1), e.GetInt32(2), e.GetBoolean(3), e.GetBoolean(4),
          e.GetInt32(5));
      else
        e.Mobile.SendMessage("Format: Animate <action> <frameCount> <repeatCount> <forward> <repeat> <delay>");
    }

    [Usage("Cast <name>")]
    [Description("Casts a spell by name.")]
    public static void Cast_OnCommand(CommandEventArgs e)
    {
      if (e.Length == 1)
      {
        if (!DesignContext.Check(e.Mobile))
          return; // They are customizing

        Spell spell = SpellRegistry.NewSpell(e.GetString(0), e.Mobile, null);

        if (spell != null)
          spell.Cast();
        else
          e.Mobile.SendMessage("That spell was not found.");
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
      e.Mobile.SendMessage("Open Connections: {0}", NetState.Instances.Count);
      e.Mobile.SendMessage("Mobiles: {0}", World.Mobiles.Count);
      e.Mobile.SendMessage("Items: {0}", World.Items.Count);
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
          from.SendMenu(new EquipMenu(from, mobile, GetEquip(mobile)));
      }

      private static ItemListEntry[] GetEquip(Mobile m)
      {
        ItemListEntry[] entries = new ItemListEntry[m.Items.Count];

        for (int i = 0; i < m.Items.Count; ++i)
        {
          Item item = m.Items[i];

          entries[i] = new ItemListEntry($"{item.Layer}: {item.GetType().Name}", item.ItemID, item.Hue);
        }

        return entries;
      }

      private class EquipMenu : ItemListMenu
      {
        private Mobile m_Mobile;

        public EquipMenu(Mobile from, Mobile m, ItemListEntry[] entries) : base("Equipment", entries)
        {
          m_Mobile = m;

          CommandLogging.WriteLine(from, "{0} {1} viewing equipment of {2}", from.AccessLevel,
            CommandLogging.Format(from), CommandLogging.Format(m));
        }

        public override void OnResponse(NetState state, int index)
        {
          if (index >= 0 && index < m_Mobile.Items.Count)
          {
            Item item = m_Mobile.Items[index];

            state.Mobile.SendMenu(new EquipDetailsMenu(m_Mobile, item));
          }
        }

        private class EquipDetailsMenu : QuestionMenu
        {
          private Item m_Item;
          private Mobile m_Mobile;

          public EquipDetailsMenu(Mobile m, Item item) : base($"{item.Layer}: {item.GetType().Name}",
            new[] { "Move", "Delete", "Props" })
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
              CommandLogging.WriteLine(state.Mobile, "{0} {1} moving equipment item {2} of {3}",
                state.Mobile.AccessLevel, CommandLogging.Format(state.Mobile), CommandLogging.Format(m_Item),
                CommandLogging.Format(m_Mobile));
              state.Mobile.Target = new MoveTarget(m_Item);
            }
            else if (index == 1)
            {
              CommandLogging.WriteLine(state.Mobile, "{0} {1} deleting equipment item {2} of {3}",
                state.Mobile.AccessLevel, CommandLogging.Format(state.Mobile), CommandLogging.Format(m_Item),
                CommandLogging.Format(m_Mobile));
              m_Item.Delete();
            }
            else if (index == 2)
            {
              CommandLogging.WriteLine(state.Mobile,
                "{0} {1} opening properties for equipment item {2} of {3}", state.Mobile.AccessLevel,
                CommandLogging.Format(state.Mobile), CommandLogging.Format(m_Item),
                CommandLogging.Format(m_Mobile));
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
          BankBox box = m.Player ? m.BankBox : m.FindBankNoCreate();

          if (box != null)
          {
            CommandLogging.WriteLine(from, "{0} {1} opening bank box of {2}", from.AccessLevel,
              CommandLogging.Format(from), CommandLogging.Format(m));

            if (from == m)
              box.Open();
            else
              box.DisplayTo(from);
          }
          else
          {
            from.SendMessage("They have no bank box.");
          }
        }
      }
    }

    private class DismountTarget : Target
    {
      public DismountTarget() : base(-1, false, TargetFlags.None)
      {
      }

      protected override void OnTarget(Mobile from, object targeted)
      {
        if (targeted is Mobile targ)
        {
          CommandLogging.WriteLine(from, "{0} {1} dismounting {2}", from.AccessLevel, CommandLogging.Format(from),
            CommandLogging.Format(targ));

          for (int i = 0; i < targ.Items.Count; ++i)
          {
            Item item = targ.Items[i];

            if (item is IMountItem mountItem)
            {
              IMount mount = mountItem.Mount;

              if (mount != null)
                mount.Rider = null;

              if (targ.Items.IndexOf(item) == -1)
                --i;
            }
          }

          for (int i = 0; i < targ.Items.Count; ++i)
          {
            Item item = targ.Items[i];

            if (item.Layer == Layer.Mount)
            {
              item.Delete();
              --i;
            }
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
          CommandLogging.WriteLine(from, "{0} {1} opening client menu of {2}", from.AccessLevel,
            CommandLogging.Format(from), CommandLogging.Format(targ));
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
            from.SendMessage("You can't do that to someone with higher Accesslevel than you!");
          else
            from.SendGump(new StuckMenu(from, mobile, false));
        }
      }
    }
  }
}
