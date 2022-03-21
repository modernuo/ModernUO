using System;
using System.Collections.Generic;
using Server.Accounting;
using Server.Engines.Help;
using Server.Factions;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Spells;

namespace Server.Commands.Generic
{
    public static class TargetCommands
    {
        public static List<BaseCommand> AllCommands { get; } = new();

        public static void Initialize()
        {
            Register(new KillCommand(true));
            Register(new KillCommand(false));
            Register(new HideCommand(true));
            Register(new HideCommand(false));
            Register(new KickCommand(true));
            Register(new KickCommand(false));
            Register(new FirewallCommand());
            Register(new TeleCommand());
            Register(new SetCommand());
            Register(new AliasedSetCommand(AccessLevel.GameMaster, "Immortal", "blessed", "true", ObjectTypes.Mobiles));
            Register(new AliasedSetCommand(AccessLevel.GameMaster, "Invul", "blessed", "true", ObjectTypes.Mobiles));
            Register(new AliasedSetCommand(AccessLevel.GameMaster, "Mortal", "blessed", "false", ObjectTypes.Mobiles));
            Register(new AliasedSetCommand(AccessLevel.GameMaster, "NoInvul", "blessed", "false", ObjectTypes.Mobiles));
            Register(new AliasedSetCommand(AccessLevel.GameMaster, "Squelch", "squelched", "true", ObjectTypes.Mobiles));
            Register(new AliasedSetCommand(AccessLevel.GameMaster, "Unsquelch", "squelched", "false", ObjectTypes.Mobiles));

            Register(new AliasedSetCommand(AccessLevel.GameMaster, "ShaveHair", "HairItemID", "0", ObjectTypes.Mobiles));
            Register(
                new AliasedSetCommand(
                    AccessLevel.GameMaster,
                    "ShaveBeard",
                    "FacialHairItemID",
                    "0",
                    ObjectTypes.Mobiles
                )
            );

            Register(new GetCommand());
            Register(new GetTypeCommand());
            Register(new DeleteCommand());
            Register(new RestockCommand());
            Register(new DismountCommand());
            Register(new AddCommand());
            Register(new AddToPackCommand());
            Register(new TellCommand(true));
            Register(new TellCommand(false));
            Register(new PrivSoundCommand());
            Register(new IncreaseCommand());
            Register(new OpenBrowserCommand());
            Register(new CountCommand());
            Register(new InterfaceCommand());
            Register(new RefreshHouseCommand());
            Register(new ConditionCommand());
            Register(new FactionKickCommand(FactionKickType.Kick));
            Register(new FactionKickCommand(FactionKickType.Ban));
            Register(new FactionKickCommand(FactionKickType.Unban));
            Register(new BringToPackCommand());
            Register(new TraceLockdownCommand());
            Register(new LocationCommand());
        }

        public static void Register(BaseCommand command)
        {
            AllCommands.Add(command);

            var impls = BaseCommandImplementor.Implementors;

            for (var i = 0; i < impls.Count; ++i)
            {
                var impl = impls[i];

                if ((command.Supports & impl.SupportRequirement) != 0)
                {
                    impl.Register(command);
                }
            }
        }
    }

    public class ConditionCommand : BaseCommand
    {
        public ConditionCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.Simple | CommandSupport.Complex | CommandSupport.Self;
            Commands = new[] { "Condition" };
            ObjectTypes = ObjectTypes.All;
            Usage = "Condition <condition>";
            Description = "Checks that the given condition matches a targeted object.";
            ListOptimized = true;
        }

        public override void ExecuteList(CommandEventArgs e, List<object> list)
        {
            try
            {
                var args = e.Arguments;
                var condition = ObjectConditional.Parse(e.Mobile, ref args);

                for (var i = 0; i < list.Count; ++i)
                {
                    if (condition.CheckCondition(list[i]))
                    {
                        AddResponse("True - that object matches the condition.");
                    }
                    else
                    {
                        AddResponse("False - that object does not match the condition.");
                    }
                }
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage(ex.Message);
            }
        }
    }

    public class BringToPackCommand : BaseCommand
    {
        public BringToPackCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.AllItems;
            Commands = new[] { "BringToPack" };
            ObjectTypes = ObjectTypes.Items;
            Usage = "BringToPack";
            Description = "Brings a targeted item to your backpack.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            if (obj is Item item)
            {
                if (e.Mobile.PlaceInBackpack(item))
                {
                    AddResponse("The item has been placed in your backpack.");
                }
                else
                {
                    AddResponse("Your backpack could not hold the item.");
                }
            }
        }
    }

    public class RefreshHouseCommand : BaseCommand
    {
        public RefreshHouseCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.Simple;
            Commands = new[] { "RefreshHouse" };
            ObjectTypes = ObjectTypes.Items;
            Usage = "RefreshHouse";
            Description = "Refreshes a targeted house sign.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            if (obj is HouseSign sign)
            {
                var house = sign.Owner;

                if (house == null)
                {
                    LogFailure("That sign has no house attached.");
                }
                else
                {
                    house.RefreshDecay();
                    AddResponse("The house has been refreshed.");
                }
            }
            else
            {
                LogFailure("That is not a house sign.");
            }
        }
    }

    public class CountCommand : BaseCommand
    {
        public CountCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.Complex;
            Commands = new[] { "Count" };
            ObjectTypes = ObjectTypes.All;
            Usage = "Count";
            Description =
                "Counts the number of objects that a command modifier would use. Generally used with condition arguments.";
            ListOptimized = true;
        }

        public override void ExecuteList(CommandEventArgs e, List<object> list)
        {
            if (list.Count == 1)
            {
                AddResponse("There is one matching object.");
            }
            else
            {
                AddResponse($"There are {list.Count} matching objects.");
            }
        }
    }

    public class OpenBrowserCommand : BaseCommand
    {
        public OpenBrowserCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.AllMobiles;
            Commands = new[] { "OpenBrowser", "OB" };
            ObjectTypes = ObjectTypes.Mobiles;
            Usage = "OpenBrowser <url>";
            Description = "Opens the web browser of a targeted player to a specified url.";
        }

        public static void OpenBrowser_Callback(Mobile from, bool okay, Mobile gm, string url, bool echo)
        {
            if (okay)
            {
                if (echo)
                {
                    gm.SendMessage("{0} : has opened their web browser to : {1}", from.Name, url);
                }

                from.LaunchBrowser(url);
            }
            else
            {
                if (echo)
                {
                    gm.SendMessage("{0} : has chosen not to open their web browser to : {1}", from.Name, url);
                }

                from.SendMessage("You have chosen not to open your web browser.");
            }
        }

        public void Execute(CommandEventArgs e, object obj, bool echo)
        {
            if (e.Length == 1)
            {
                var mob = (Mobile)obj;
                var from = e.Mobile;

                if (mob.Player)
                {
                    var ns = mob.NetState;

                    if (ns == null)
                    {
                        LogFailure("That player is not online.");
                    }
                    else
                    {
                        var url = e.GetString(0);

                        CommandLogging.WriteLine(
                            from,
                            "{0} {1} requesting to open web browser of {2} to {3}",
                            from.AccessLevel,
                            CommandLogging.Format(from),
                            CommandLogging.Format(mob),
                            url
                        );

                        if (echo)
                        {
                            AddResponse("Awaiting user confirmation...");
                        }
                        else
                        {
                            AddResponse("Open web browser request sent.");
                        }

                        mob.SendGump(
                            new WarningGump(
                                1060637,
                                30720,
                                $"A game master is requesting to open your web browser to the following URL:<br>{url}",
                                0xFFC000,
                                320,
                                240,
                                okay => OpenBrowser_Callback(mob, okay, from, url, echo)
                            )
                        );
                    }
                }
                else
                {
                    LogFailure("That is not a player.");
                }
            }
            else
            {
                LogFailure("Format: OpenBrowser <url>");
            }
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            Execute(e, obj, true);
        }

        public override void ExecuteList(CommandEventArgs e, List<object> list)
        {
            for (var i = 0; i < list.Count; ++i)
            {
                Execute(e, list[i], false);
            }
        }
    }

    public class IncreaseCommand : BaseCommand
    {
        public IncreaseCommand()
        {
            AccessLevel = AccessLevel.Counselor;
            Supports = CommandSupport.All;
            Commands = new[] { "Increase", "Inc" };
            ObjectTypes = ObjectTypes.Both;
            Usage = "Increase {<propertyName> <offset> ...}";
            Description = "Increases the value of a specified property by the specified offset.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            if (obj is BaseMulti)
            {
                LogFailure("This command does not work on multis.");
            }
            else if (e.Length >= 2)
            {
                var result = Properties.IncreaseValue(e.Mobile, obj, e.Arguments);

                if (result is "The property has been increased." or "The properties have been increased." or "The property has been decreased." or "The properties have been decreased." or "The properties have been changed.")
                {
                    AddResponse(result);
                }
                else
                {
                    LogFailure(result);
                }
            }
            else
            {
                LogFailure("Format: Increase {<propertyName> <offset> ...}");
            }
        }
    }

    public class PrivSoundCommand : BaseCommand
    {
        public PrivSoundCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.AllMobiles;
            Commands = new[] { "PrivSound" };
            ObjectTypes = ObjectTypes.Mobiles;
            Usage = "PrivSound <index>";
            Description = "Plays a sound to a given target.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            var from = e.Mobile;

            if (e.Length == 1)
            {
                var index = e.GetInt32(0);
                var mob = (Mobile)obj;

                CommandLogging.WriteLine(
                    from,
                    "{0} {1} playing sound {2} for {3}",
                    from.AccessLevel,
                    CommandLogging.Format(from),
                    index,
                    CommandLogging.Format(mob)
                );
                mob.SendSound(index);
            }
            else
            {
                from.SendMessage("Format: PrivSound <index>");
            }
        }
    }

    public class TellCommand : BaseCommand
    {
        private readonly bool m_InGump;

        public TellCommand(bool inGump)
        {
            m_InGump = inGump;

            AccessLevel = AccessLevel.Counselor;
            Supports = CommandSupport.AllMobiles;
            ObjectTypes = ObjectTypes.Mobiles;

            if (inGump)
            {
                Commands = new[] { "Message", "Msg" };
                Usage = "Message \"text\"";
                Description = "Sends a message to a targeted player.";
            }
            else
            {
                Commands = new[] { "Tell" };
                Usage = "Tell \"text\"";
                Description = "Sends a system message to a targeted player.";
            }
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            var mob = (Mobile)obj;
            var from = e.Mobile;

            CommandLogging.WriteLine(
                from,
                "{0} {1} {2} {3} \"{4}\"",
                from.AccessLevel,
                CommandLogging.Format(from),
                m_InGump ? "messaging" : "telling",
                CommandLogging.Format(mob),
                e.ArgString
            );

            if (m_InGump)
            {
                mob.SendGump(new MessageSentGump(mob, from.Name, e.ArgString));
            }
            else
            {
                mob.SendMessage(e.ArgString);
            }
        }
    }

    public class AddToPackCommand : BaseCommand
    {
        public AddToPackCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.All;
            Commands = new[] { "AddToPack", "AddToCont" };
            ObjectTypes = ObjectTypes.Both;
            ListOptimized = true;
            Usage = "AddToPack <name> [params] [set {<propertyName> <value> ...}]";
            Description =
                "Adds an item by name to the backpack of a targeted player or npc, or a targeted container. Optional constructor parameters. Optional set property list.";
        }

        public override void ExecuteList(CommandEventArgs e, List<object> list)
        {
            if (e.Arguments.Length == 0)
            {
                return;
            }

            var packs = new List<Container>(list.Count);

            for (var i = 0; i < list.Count; ++i)
            {
                var obj = list[i];
                Container cont = null;

                if (obj is Mobile mobile)
                {
                    cont = mobile.Backpack;
                }
                else if (obj is Container container)
                {
                    cont = container;
                }

                if (cont != null)
                {
                    packs.Add(cont);
                }
                else
                {
                    LogFailure("That is not a container.");
                }
            }

            Add.Invoke(e.Mobile, e.Mobile.Location, e.Mobile.Location, e.Arguments, packs);
        }
    }

    public class AddCommand : BaseCommand
    {
        public AddCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.Simple | CommandSupport.Self;
            Commands = new[] { "Add" };
            ObjectTypes = ObjectTypes.All;
            Usage = "Add [<name> [params] [set {<propertyName> <value> ...}]]";
            Description =
                "Adds an item or npc by name to a targeted location. Optional constructor parameters. Optional set property list. If no arguments are specified, this brings up a categorized add menu.";
        }

        public override bool ValidateArgs(BaseCommandImplementor impl, CommandEventArgs e)
        {
            if (e.Length >= 1)
            {
                var t = AssemblyHandler.FindTypeByName(e.GetString(0));

                if (t == null)
                {
                    e.Mobile.SendMessage("No type with that name was found.");

                    var match = e.GetString(0).Trim();

                    if (match.Length < 3)
                    {
                        e.Mobile.SendMessage("Invalid search string.");
                        e.Mobile.SendGump(new AddGump(e.Mobile, match, 0, Type.EmptyTypes, false));
                    }
                    else
                    {
                        e.Mobile.SendGump(new AddGump(e.Mobile, match, 0, AddGump.Match(match), true));
                    }
                }
                else
                {
                    return true;
                }
            }
            else
            {
                e.Mobile.SendGump(new CategorizedAddGump(e.Mobile));
            }

            return false;
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            if (obj is not IPoint3D ip)
            {
                return;
            }

            Point3D p = ip switch
            {
                Item item => item.GetWorldTop(),
                Mobile m  => m.Location,
                _         => new Point3D(ip)
            };

            Add.Invoke(e.Mobile, p, p, e.Arguments);
        }
    }

    public class TeleCommand : BaseCommand
    {
        public TeleCommand()
        {
            AccessLevel = AccessLevel.Counselor;
            Supports = CommandSupport.Simple;
            Commands = new[] { "Teleport", "Tele" };
            ObjectTypes = ObjectTypes.All;
            Usage = "Teleport";
            Description = "Teleports your character to a targeted location.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            if (obj is not IPoint3D p)
            {
                return;
            }

            var from = e.Mobile;

            SpellHelper.GetSurfaceTop(ref p);

            // CommandLogging.WriteLine( from, "{0} {1} teleporting to {2}", from.AccessLevel, CommandLogging.Format( from ), new Point3D( p ) );

            var fromLoc = from.Location;
            var toLoc = new Point3D(p);

            from.Location = toLoc;
            from.ProcessDelta();

            if (!from.Hidden)
            {
                Effects.SendLocationParticles(
                    EffectItem.Create(fromLoc, from.Map, EffectItem.DefaultDuration),
                    0x3728,
                    10,
                    10,
                    2023
                );
                Effects.SendLocationParticles(
                    EffectItem.Create(toLoc, from.Map, EffectItem.DefaultDuration),
                    0x3728,
                    10,
                    10,
                    5023
                );

                from.PlaySound(0x1FE);
            }
        }
    }

    public class DismountCommand : BaseCommand
    {
        public DismountCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.AllMobiles;
            Commands = new[] { "Dismount" };
            ObjectTypes = ObjectTypes.Mobiles;
            Usage = "Dismount";
            Description = "Forcefully dismounts a given target.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            var from = e.Mobile;
            var mob = (Mobile)obj;

            CommandLogging.WriteLine(
                from,
                "{0} {1} dismounting {2}",
                from.AccessLevel,
                CommandLogging.Format(from),
                CommandLogging.Format(mob)
            );

            var takenAction = false;

            for (var i = 0; i < mob.Items.Count; ++i)
            {
                var item = mob.Items[i];

                if (item is IMountItem mountItem)
                {
                    var mount = mountItem.Mount;

                    if (mount != null)
                    {
                        mount.Rider = null;
                        takenAction = true;
                    }

                    if (mob.Items.IndexOf(item) == -1)
                    {
                        --i;
                    }
                }
            }

            for (var i = 0; i < mob.Items.Count; ++i)
            {
                var item = mob.Items[i];

                if (item.Layer == Layer.Mount)
                {
                    takenAction = true;
                    item.Delete();
                    --i;
                }
            }

            if (takenAction)
            {
                AddResponse("They have been dismounted.");
            }
            else
            {
                LogFailure("They were not mounted.");
            }
        }
    }

    public class RestockCommand : BaseCommand
    {
        public RestockCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.AllNPCs;
            Commands = new[] { "Restock" };
            ObjectTypes = ObjectTypes.Mobiles;
            Usage = "Restock";
            Description =
                "Manually restocks a targeted vendor, refreshing the quantity of every item the vendor sells to the maximum. This also invokes the maximum quantity adjustment algorithms.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            if (obj is BaseVendor vendor)
            {
                CommandLogging.WriteLine(
                    e.Mobile,
                    "{0} {1} restocking {2}",
                    e.Mobile.AccessLevel,
                    CommandLogging.Format(e.Mobile),
                    CommandLogging.Format(vendor)
                );

                vendor.Restock();
                AddResponse("The vendor has been restocked.");
            }
            else
            {
                AddResponse("That is not a vendor.");
            }
        }
    }

    public class GetTypeCommand : BaseCommand
    {
        public GetTypeCommand()
        {
            AccessLevel = AccessLevel.Counselor;
            Supports = CommandSupport.All;
            Commands = new[] { "GetType" };
            ObjectTypes = ObjectTypes.All;
            Usage = "GetType";
            Description = "Gets the type name of a targeted object.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            if (obj == null)
            {
                AddResponse("The object is null.");
            }
            else
            {
                var type = obj.GetType();

                if (type.DeclaringType == null)
                {
                    AddResponse($"The type of that object is {type.Name}.");
                }
                else
                {
                    AddResponse($"The type of that object is {type.FullName}.");
                }
            }
        }
    }

    public class GetCommand : BaseCommand
    {
        public GetCommand()
        {
            AccessLevel = AccessLevel.Counselor;
            Supports = CommandSupport.All;
            Commands = new[] { "Get" };
            ObjectTypes = ObjectTypes.All;
            Usage = "Get <propertyName>";
            Description = "Gets one or more property values by name of a targeted object.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            if (e.Length >= 1)
            {
                for (var i = 0; i < e.Length; ++i)
                {
                    var result = Properties.GetValue(e.Mobile, obj, e.GetString(i));

                    if (result is "Property not found." or "Property is write only." || result.StartsWithOrdinal("Getting this property"))
                    {
                        LogFailure(result);
                    }
                    else
                    {
                        AddResponse(result);
                    }
                }
            }
            else
            {
                LogFailure("Format: Get <propertyName>");
            }
        }
    }

    public class AliasedSetCommand : BaseCommand
    {
        private readonly string m_Name;
        private readonly string m_Value;

        public AliasedSetCommand(AccessLevel level, string command, string name, string value, ObjectTypes objects)
        {
            m_Name = name;
            m_Value = value;

            AccessLevel = level;

            if (objects == ObjectTypes.Items)
            {
                Supports = CommandSupport.AllItems;
            }
            else if (objects == ObjectTypes.Mobiles)
            {
                Supports = CommandSupport.AllMobiles;
            }
            else
            {
                Supports = CommandSupport.All;
            }

            Commands = new[] { command };
            ObjectTypes = objects;
            Usage = command;
            Description = $"Sets the {name} property to {value}.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            var result = Properties.SetValue(e.Mobile, obj, m_Name, m_Value);

            if (result == "Property has been set.")
            {
                AddResponse(result);
            }
            else
            {
                LogFailure(result);
            }
        }
    }

    public class SetCommand : BaseCommand
    {
        public SetCommand()
        {
            AccessLevel = AccessLevel.Counselor;
            Supports = CommandSupport.All;
            Commands = new[] { "Set" };
            ObjectTypes = ObjectTypes.Both;
            Usage = "Set <propertyName> <value> [...]";
            Description = "Sets one or more property values by name of a targeted object.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            if (e.Length >= 2)
            {
                for (var i = 0; i + 1 < e.Length; i += 2)
                {
                    var result = Properties.SetValue(e.Mobile, obj, e.GetString(i), e.GetString(i + 1));

                    if (result == "Property has been set.")
                    {
                        AddResponse(result);
                    }
                    else
                    {
                        LogFailure(result);
                    }
                }
            }
            else
            {
                LogFailure("Format: Set <propertyName> <value>");
            }
        }
    }

    public class DeleteCommand : BaseCommand
    {
        public DeleteCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.AllNPCs | CommandSupport.AllItems;
            Commands = new[] { "Delete", "Remove", "Rm" };
            ObjectTypes = ObjectTypes.Both;
            Usage = "Delete";
            Description = "Deletes a targeted item or mobile. Does not delete players.";
        }

        private void OnConfirmCallback(Mobile from, bool okay, CommandEventArgs e, List<object> list)
        {
            var flushToLog = false;

            if (okay)
            {
                AddResponse("Delete command confirmed.");

                if (list.Count > 20)
                {
                    CommandLogging.Enabled = false;
                    NetState.FlushAll();
                }

                base.ExecuteList(e, list);

                if (list.Count > 20)
                {
                    flushToLog = true;
                    CommandLogging.Enabled = true;
                }
            }
            else
            {
                AddResponse("Delete command aborted.");
            }

            Flush(from, flushToLog);
        }

        public override void ExecuteList(CommandEventArgs e, List<object> list)
        {
            if (list.Count > 1)
            {
                var from = e.Mobile;
                from.SendGump(
                    new WarningGump(
                        1060637,
                        30720,
                        $"You are about to delete {list.Count} objects. This cannot be undone without a full server revert.<br><br>Continue?",
                        0xFFC000,
                        420,
                        280,
                        okay => OnConfirmCallback(from, okay, e, list)
                    )
                );
                AddResponse("Awaiting confirmation...");
            }
            else
            {
                base.ExecuteList(e, list);
            }
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            if (obj is Item item)
            {
                CommandLogging.WriteLine(
                    e.Mobile,
                    "{0} {1} deleting {2}",
                    e.Mobile.AccessLevel,
                    CommandLogging.Format(e.Mobile),
                    CommandLogging.Format(item)
                );
                item.Delete();
                AddResponse("The item has been deleted.");
            }
            else if (obj is Mobile mobile && !mobile.Player)
            {
                CommandLogging.WriteLine(
                    e.Mobile,
                    "{0} {1} deleting {2}",
                    e.Mobile.AccessLevel,
                    CommandLogging.Format(e.Mobile),
                    CommandLogging.Format(mobile)
                );
                mobile.Delete();
                AddResponse("The mobile has been deleted.");
            }
            else
            {
                LogFailure("That cannot be deleted.");
            }
        }
    }

    public class KillCommand : BaseCommand
    {
        private readonly bool m_Value;

        public KillCommand(bool value)
        {
            m_Value = value;

            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.AllMobiles;
            Commands = value ? new[] { "Kill" } : new[] { "Resurrect", "Res" };
            ObjectTypes = ObjectTypes.Mobiles;

            if (value)
            {
                Usage = "Kill";
                Description = "Kills a targeted player or npc.";
            }
            else
            {
                Usage = "Resurrect";
                Description = "Resurrects a targeted ghost.";
            }
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            var mob = (Mobile)obj;
            var from = e.Mobile;

            if (m_Value)
            {
                if (!mob.Alive)
                {
                    LogFailure("They are already dead.");
                }
                else if (!mob.CanBeDamaged())
                {
                    LogFailure("They cannot be harmed.");
                }
                else
                {
                    CommandLogging.WriteLine(
                        from,
                        "{0} {1} killing {2}",
                        from.AccessLevel,
                        CommandLogging.Format(from),
                        CommandLogging.Format(mob)
                    );
                    mob.Kill();

                    AddResponse("They have been killed.");
                }
            }
            else
            {
                if (mob.IsDeadBondedPet)
                {
                    if (mob is BaseCreature bc)
                    {
                        CommandLogging.WriteLine(
                            from,
                            "{0} {1} resurrecting {2}",
                            from.AccessLevel,
                            CommandLogging.Format(from),
                            CommandLogging.Format(mob)
                        );

                        bc.PlaySound(0x214);
                        bc.FixedEffect(0x376A, 10, 16);

                        bc.ResurrectPet();

                        AddResponse("It has been resurrected.");
                    }
                }
                else if (!mob.Alive)
                {
                    CommandLogging.WriteLine(
                        from,
                        "{0} {1} resurrecting {2}",
                        from.AccessLevel,
                        CommandLogging.Format(from),
                        CommandLogging.Format(mob)
                    );

                    mob.PlaySound(0x214);
                    mob.FixedEffect(0x376A, 10, 16);

                    mob.Resurrect();

                    AddResponse("They have been resurrected.");
                }
                else
                {
                    LogFailure("They are not dead.");
                }
            }
        }
    }

    public class HideCommand : BaseCommand
    {
        private readonly bool m_Value;

        public HideCommand(bool value)
        {
            m_Value = value;

            AccessLevel = AccessLevel.Counselor;
            Supports = CommandSupport.AllMobiles;
            Commands = new[] { value ? "Hide" : "Unhide" };
            ObjectTypes = ObjectTypes.Mobiles;

            if (value)
            {
                Usage = "Hide";
                Description = "Makes a targeted mobile disappear in a puff of smoke.";
            }
            else
            {
                Usage = "Unhide";
                Description = "Makes a targeted mobile appear in a puff of smoke.";
            }
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            var m = (Mobile)obj;

            CommandLogging.WriteLine(
                e.Mobile,
                "{0} {1} {2} {3}",
                e.Mobile.AccessLevel,
                CommandLogging.Format(e.Mobile),
                m_Value ? "hiding" : "unhiding",
                CommandLogging.Format(m)
            );

            Effects.SendLocationEffect(new Point3D(m.X + 1, m.Y, m.Z + 4), m.Map, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(m.X + 1, m.Y, m.Z), m.Map, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(m.X + 1, m.Y, m.Z - 4), m.Map, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(m.X, m.Y + 1, m.Z + 4), m.Map, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(m.X, m.Y + 1, m.Z), m.Map, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(m.X, m.Y + 1, m.Z - 4), m.Map, 0x3728, 13);

            Effects.SendLocationEffect(new Point3D(m.X + 1, m.Y + 1, m.Z + 11), m.Map, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(m.X + 1, m.Y + 1, m.Z + 7), m.Map, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(m.X + 1, m.Y + 1, m.Z + 3), m.Map, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(m.X + 1, m.Y + 1, m.Z - 1), m.Map, 0x3728, 13);

            m.PlaySound(0x228);
            m.Hidden = m_Value;

            if (m_Value)
            {
                AddResponse("They have been hidden.");
            }
            else
            {
                AddResponse("They have been revealed.");
            }
        }
    }

    public class FirewallCommand : BaseCommand
    {
        public FirewallCommand()
        {
            AccessLevel = AccessLevel.Administrator;
            Supports = CommandSupport.AllMobiles;
            Commands = new[] { "Firewall" };
            ObjectTypes = ObjectTypes.Mobiles;
            Usage = "Firewall";
            Description =
                "Adds a targeted player to the firewall (list of blocked IP addresses). This command does not ban or kick.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            var from = e.Mobile;
            var targ = (Mobile)obj;
            var state = targ.NetState;

            if (state != null)
            {
                CommandLogging.WriteLine(
                    from,
                    "{0} {1} firewalling {2}",
                    from.AccessLevel,
                    CommandLogging.Format(from),
                    CommandLogging.Format(targ)
                );

                try
                {
                    Firewall.Add(state.Address);
                    AddResponse("They have been firewalled.");
                }
                catch (Exception ex)
                {
                    LogFailure(ex.Message);
                }
            }
            else
            {
                LogFailure("They are not online.");
            }
        }
    }

    public class KickCommand : BaseCommand
    {
        private readonly bool m_Ban;

        public KickCommand(bool ban)
        {
            m_Ban = ban;

            AccessLevel = ban ? AccessLevel.Administrator : AccessLevel.GameMaster;
            Supports = CommandSupport.AllMobiles;
            Commands = new[] { ban ? "Ban" : "Kick" };
            ObjectTypes = ObjectTypes.Mobiles;

            if (ban)
            {
                Usage = "Ban";
                Description = "Bans the account of a targeted player.";
            }
            else
            {
                Usage = "Kick";
                Description = "Disconnects a targeted player.";
            }
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            var from = e.Mobile;
            var targ = (Mobile)obj;

            if (from.AccessLevel > targ.AccessLevel)
            {
                NetState fromState = from.NetState, targState = targ.NetState;

                if (fromState != null && targState != null)
                {
                    if (fromState.Account is Account && targState.Account is Account targAccount)
                    {
                        CommandLogging.WriteLine(
                            from,
                            "{0} {1} {2} {3}",
                            from.AccessLevel,
                            CommandLogging.Format(from),
                            m_Ban ? "banning" : "kicking",
                            CommandLogging.Format(targ)
                        );

                        targ.Say("I've been {0}!", m_Ban ? "banned" : "kicked");

                        AddResponse($"They have been {(m_Ban ? "banned" : "kicked")}.");

                        targState.Disconnect($"Banned by {from}.");

                        if (m_Ban)
                        {
                            targAccount.Banned = true;
                            targAccount.SetUnspecifiedBan(from);
                            from.SendGump(new BanDurationGump(targAccount));
                        }
                    }
                }
                else if (targState == null)
                {
                    LogFailure("They are not online.");
                }
            }
            else
            {
                LogFailure("You do not have the required access level to do this.");
            }
        }
    }

    public class TraceLockdownCommand : BaseCommand
    {
        public TraceLockdownCommand()
        {
            AccessLevel = AccessLevel.Administrator;
            Supports = CommandSupport.Simple;
            Commands = new[] { "TraceLockdown" };
            ObjectTypes = ObjectTypes.Items;
            Usage = "TraceLockdown";
            Description = "Finds the BaseHouse for which a targeted item is locked down or secured.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            if (obj is not Item item)
            {
                return;
            }

            if (!item.IsLockedDown && !item.IsSecure)
            {
                LogFailure("That is not locked down.");
                return;
            }

            foreach (var house in BaseHouse.AllHouses)
            {
                if (house.HasSecureItem(item) || house.HasLockedDownItem(item))
                {
                    e.Mobile.SendGump(new PropertiesGump(e.Mobile, house));
                    return;
                }
            }

            LogFailure("No house was found.");
        }
    }
}
