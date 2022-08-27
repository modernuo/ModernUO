using ModernUO.Serialization;
using Server.Factions;
using Server.Guilds;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(4, false)]
public partial class Guildstone : Item, IAddon, IChoppable
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private string _guildName;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private string _guildAbbrev;

    [InvalidateProperties]
    [SerializableField(2, setter: "private")]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private Guild _guild;

    public Guildstone(Guild g) : this(g, g.Name, g.Abbreviation)
    {
    }

    public Guildstone(Guild g, string guildName, string abbrev) : base(Guild.NewGuildSystem ? 0xED6 : 0xED4)
    {
        _guild = g;
        _guildName = guildName;
        _guildAbbrev = abbrev;

        Movable = false;
    }

    public override int LabelNumber => 1041429; // a guildstone

    public Item Deed => new GuildstoneDeed(Guild, _guildName, _guildAbbrev);

    public bool CouldFit(IPoint3D p, Map map) => map.CanFit(p.X, p.Y, p.Z, ItemData.Height);

    public void OnChop(Mobile from)
    {
        if (!Guild.NewGuildSystem)
        {
            return;
        }

        var house = BaseHouse.FindHouseAt(this);

        if (house?.IsOwner(from) == true && house.Addons.Contains(this))
        {
            Effects.PlaySound(GetWorldLocation(), Map, 0x3B3);
            from.SendLocalizedMessage(500461); // You destroy the item.

            Delete();

            house.Addons.Remove(this);

            var deed = Deed;

            if (deed != null)
            {
                from.AddToBackpack(deed);
            }
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        reader.ReadBool(); // Before Change Over
        _guildName = reader.ReadString();
        _guildAbbrev = reader.ReadString();
        _guild = reader.ReadEntity<Guild>();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (Guild.NewGuildSystem && ItemID == 0xED4)
        {
            ItemID = 0xED6;
        }

        if (!Guild.NewGuildSystem && Guild == null)
        {
            Delete();
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_guild?.Disbanded == false)
        {
            string name;
            string abbr;

            if ((name = _guild.Name) == null || (name = name.Trim()).Length <= 0)
            {
                name = "(unnamed)";
            }

            if ((abbr = _guild.Abbreviation) == null || (abbr = abbr.Trim()).Length <= 0)
            {
                abbr = "";
            }

            // list.Add( 1060802, Utility.FixHtml( name ) ); // Guild name: ~1_val~
            list.Add(1060802, $"{Utility.FixHtmlFormattable(name)} [{Utility.FixHtmlFormattable(abbr)}]");
        }
        else if (_guildName != null && _guildAbbrev != null)
        {
            list.Add(1060802, $"{Utility.FixHtmlFormattable(_guildName)} [{Utility.FixHtmlFormattable(_guildAbbrev)}]");
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        if (_guild?.Disbanded == false)
        {
            string name;

            if ((name = _guild.Name) == null || (name = name.Trim()).Length <= 0)
            {
                name = "(unnamed)";
            }

            LabelTo(from, name);
        }
        else if (_guildName != null)
        {
            LabelTo(from, _guildName);
        }
    }

    public override void OnAfterDelete()
    {
        if (!Guild.NewGuildSystem && _guild?.Disbanded == false)
        {
            _guild.Disband();
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (Guild.NewGuildSystem)
        {
            return;
        }

        if (_guild?.Disbanded != false)
        {
            Delete();
        }
        else if (!from.InRange(GetWorldLocation(), 2))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
        }
        else if (_guild.Accepted.Contains(from))
        {
            var guildState = PlayerState.Find(_guild.Leader);
            var targetState = PlayerState.Find(from);

            var guildFaction = guildState?.Faction;
            var targetFaction = targetState?.Faction;

            if (guildFaction != targetFaction || targetState?.IsLeaving == true)
            {
                return;
            }

            if (guildState != null && targetState != null)
            {
                targetState.Leaving = guildState.Leaving;
            }

            _guild.Remove(_guild.Accepted, from);
            _guild.AddMember(from);

            GuildGump.EnsureClosed(from);
            from.SendGump(new GuildGump(from, Guild));
        }
        else if (from.AccessLevel < AccessLevel.GameMaster && !_guild.IsMember(from))
        {
            // You are not a member ...
            from.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Regular, 0x3B2, 3, 501158);
        }
        else
        {
            GuildGump.EnsureClosed(from);
            from.SendGump(new GuildGump(from, _guild));
        }
    }
}

[Flippable(0x14F0, 0x14EF)]
[SerializationGenerator(0, false)]
public partial class GuildstoneDeed : Item
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private string _guildName;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private string _guildAbbrev;

    [InvalidateProperties]
    [SerializableField(2, setter: "private")]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private Guild _guild;

    [Constructible]
    public GuildstoneDeed(Guild g = null, string guildName = null, string abbrev = null) : base(0x14F0)
    {
        _guild = g;
        _guildName = guildName;
        _guildAbbrev = abbrev;

        Weight = 1.0;
    }

    public override int LabelNumber => 1041233; // deed to a guildstone

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_guild?.Disbanded == false)
        {
            string name;
            string abbr;

            if ((name = _guild.Name) == null || (name = name.Trim()).Length <= 0)
            {
                name = "(unnamed)";
            }

            if ((abbr = _guild.Abbreviation) == null || (abbr = abbr.Trim()).Length <= 0)
            {
                abbr = "";
            }

            // list.Add( 1060802, Utility.FixHtml( name ) ); // Guild name: ~1_val~
            list.Add(1060802, $"{Utility.FixHtmlFormattable(name)} [{Utility.FixHtmlFormattable(abbr)}]");
        }
        else if (_guildName != null && _guildAbbrev != null)
        {
            list.Add(1060802, $"{Utility.FixHtmlFormattable(_guildName)} [{Utility.FixHtmlFormattable(_guildAbbrev)}]");
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        var house = BaseHouse.FindHouseAt(from);

        if (house?.IsOwner(from) == true)
        {
            from.SendLocalizedMessage(1062838); // Where would you like to place this decoration?
            from.BeginTarget(-1, true, TargetFlags.None, Placement_OnTarget);
        }
        else
        {
            from.SendLocalizedMessage(502092); // You must be in your house to do this.
        }
    }

    public void Placement_OnTarget(Mobile from, object targeted)
    {
        if (targeted is not IPoint3D p || Deleted)
        {
            return;
        }

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        var loc = new Point3D(p);

        var house = BaseHouse.FindHouseAt(loc, from.Map, 16);
        if (house?.IsOwner(from) == true)
        {
            Item addon = new Guildstone(_guild, _guildName, _guildAbbrev);

            addon.MoveToWorld(loc, from.Map);

            house.Add(house.Addons, addon);
            Delete();
        }
        else
        {
            from.SendLocalizedMessage(1042036); // That location is not in your house.
        }
    }
}
