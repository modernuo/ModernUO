using Server.Factions;
using Server.Guilds;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public class Guildstone : Item, IAddon, IChoppable
    {
        private bool m_BeforeChangeover;
        private string m_GuildAbbrev;
        private string m_GuildName;

        public Guildstone(Guild g) : this(g, g.Name, g.Abbreviation)
        {
        }

        public Guildstone(Guild g, string guildName, string abbrev) : base(Guild.NewGuildSystem ? 0xED6 : 0xED4)
        {
            Guild = g;
            m_GuildName = guildName;
            m_GuildAbbrev = abbrev;

            Movable = false;
        }

        public Guildstone(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string GuildName
        {
            get => m_GuildName;
            set
            {
                m_GuildName = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string GuildAbbrev
        {
            get => m_GuildAbbrev;
            set
            {
                m_GuildAbbrev = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Guild Guild { get; private set; }

        public override int LabelNumber => 1041429; // a guildstone

        public Item Deed => new GuildstoneDeed(Guild, m_GuildName, m_GuildAbbrev);

        public bool CouldFit(IPoint3D p, Map map) => map.CanFit(p.X, p.Y, p.Z, ItemData.Height);

        public void OnChop(Mobile from)
        {
            if (!Guild.NewGuildSystem)
            {
                return;
            }

            var house = BaseHouse.FindHouseAt(this);

            var contains = false;

            if (house == null && m_BeforeChangeover ||
                house?.IsOwner(from) == true && (contains = house.Addons.Contains(this)))
            {
                Effects.PlaySound(GetWorldLocation(), Map, 0x3B3);
                from.SendLocalizedMessage(500461); // You destroy the item.

                Delete();

                if (contains)
                {
                    house.Addons.Remove(this);
                }

                var deed = Deed;

                if (deed != null)
                {
                    from.AddToBackpack(deed);
                }
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            if (Guild?.Disbanded == false)
            {
                m_GuildName = Guild.Name;
                m_GuildAbbrev = Guild.Abbreviation;
            }

            writer.Write(3); // version

            writer.Write(m_BeforeChangeover);

            writer.Write(m_GuildName);
            writer.Write(m_GuildAbbrev);

            writer.Write(Guild);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        m_BeforeChangeover = reader.ReadBool();
                        goto case 2;
                    }
                case 2:
                    {
                        m_GuildName = reader.ReadString();
                        m_GuildAbbrev = reader.ReadString();

                        goto case 1;
                    }
                case 1:
                    {
                        Guild = reader.ReadEntity<Guild>();

                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }

            if (Guild.NewGuildSystem && ItemID == 0xED4)
            {
                ItemID = 0xED6;
            }

            if (version <= 2)
            {
                m_BeforeChangeover = true;
            }

            if (Guild.NewGuildSystem && m_BeforeChangeover)
            {
                Timer.StartTimer(AddToHouse);
            }

            if (!Guild.NewGuildSystem && Guild == null)
            {
                Delete();
            }
        }

        private void AddToHouse()
        {
            var house = BaseHouse.FindHouseAt(this);

            if (Guild.NewGuildSystem && m_BeforeChangeover && house?.Addons.Contains(this) == false)
            {
                house.Addons.Add(this);
                m_BeforeChangeover = false;
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (Guild?.Disbanded == false)
            {
                string name;
                string abbr;

                if ((name = Guild.Name) == null || (name = name.Trim()).Length <= 0)
                {
                    name = "(unnamed)";
                }

                if ((abbr = Guild.Abbreviation) == null || (abbr = abbr.Trim()).Length <= 0)
                {
                    abbr = "";
                }

                // list.Add( 1060802, Utility.FixHtml( name ) ); // Guild name: ~1_val~
                list.Add(1060802, $"{Utility.FixHtml(name)} [{Utility.FixHtml(abbr)}]");
            }
            else if (m_GuildName != null && m_GuildAbbrev != null)
            {
                list.Add(1060802, $"{Utility.FixHtml(m_GuildName)} [{Utility.FixHtml(m_GuildAbbrev)}]");
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (Guild?.Disbanded == false)
            {
                string name;

                if ((name = Guild.Name) == null || (name = name.Trim()).Length <= 0)
                {
                    name = "(unnamed)";
                }

                LabelTo(from, name);
            }
            else if (m_GuildName != null)
            {
                LabelTo(from, m_GuildName);
            }
        }

        public override void OnAfterDelete()
        {
            if (!Guild.NewGuildSystem && Guild?.Disbanded == false)
            {
                Guild.Disband();
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Guild.NewGuildSystem)
            {
                return;
            }

            if (Guild?.Disbanded != false)
            {
                Delete();
            }
            else if (!from.InRange(GetWorldLocation(), 2))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
            else if (Guild.Accepted.Contains(from))
            {
                var guildState = PlayerState.Find(Guild.Leader);
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

                Guild.Accepted.Remove(from);
                Guild.AddMember(from);

                GuildGump.EnsureClosed(from);
                from.SendGump(new GuildGump(from, Guild));
            }
            else if (from.AccessLevel < AccessLevel.GameMaster && !Guild.IsMember(from))
            {
                from.NetState.SendMessageLocalized(
                    Serial,
                    ItemID,
                    MessageType.Regular,
                    0x3B2,
                    3,
                    501158
                ); // You are not a member ...
            }
            else
            {
                GuildGump.EnsureClosed(from);
                from.SendGump(new GuildGump(from, Guild));
            }
        }
    }

    [Flippable(0x14F0, 0x14EF)]
    public class GuildstoneDeed : Item
    {
        private string m_GuildAbbrev;

        private string m_GuildName;

        [Constructible]
        public GuildstoneDeed(Guild g = null, string guildName = null, string abbrev = null) : base(0x14F0)
        {
            Guild = g;
            m_GuildName = guildName;
            m_GuildAbbrev = abbrev;

            Weight = 1.0;
        }

        public GuildstoneDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041233; // deed to a guildstone

        [CommandProperty(AccessLevel.GameMaster)]
        public string GuildName
        {
            get => m_GuildName;
            set
            {
                m_GuildName = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string GuildAbbrev
        {
            get => m_GuildAbbrev;
            set
            {
                m_GuildAbbrev = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Guild Guild { get; private set; }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            if (Guild?.Disbanded == false)
            {
                m_GuildName = Guild.Name;
                m_GuildAbbrev = Guild.Abbreviation;
            }

            writer.Write(1); // version

            writer.Write(m_GuildName);
            writer.Write(m_GuildAbbrev);

            writer.Write(Guild);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_GuildName = reader.ReadString();
                        m_GuildAbbrev = reader.ReadString();

                        Guild = reader.ReadEntity<Guild>();

                        break;
                    }
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (Guild?.Disbanded == false)
            {
                string name;
                string abbr;

                if ((name = Guild.Name) == null || (name = name.Trim()).Length <= 0)
                {
                    name = "(unnamed)";
                }

                if ((abbr = Guild.Abbreviation) == null || (abbr = abbr.Trim()).Length <= 0)
                {
                    abbr = "";
                }

                // list.Add( 1060802, Utility.FixHtml( name ) ); // Guild name: ~1_val~
                list.Add(1060802, $"{Utility.FixHtml(name)} [{Utility.FixHtml(abbr)}]");
            }
            else if (m_GuildName != null && m_GuildAbbrev != null)
            {
                list.Add(1060802, $"{Utility.FixHtml(m_GuildName)} [{Utility.FixHtml(m_GuildAbbrev)}]");
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
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
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public void Placement_OnTarget(Mobile from, object targeted)
        {
            if (!(targeted is IPoint3D p) || Deleted)
            {
                return;
            }

            var loc = new Point3D(p);

            var house = BaseHouse.FindHouseAt(loc, from.Map, 16);

            if (IsChildOf(from.Backpack))
            {
                if (house?.IsOwner(from) == true)
                {
                    Item addon = new Guildstone(Guild, m_GuildName, m_GuildAbbrev);

                    addon.MoveToWorld(loc, from.Map);

                    house.Addons.Add(addon);
                    Delete();
                }
                else
                {
                    from.SendLocalizedMessage(1042036); // That location is not in your house.
                }
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }
    }
}
