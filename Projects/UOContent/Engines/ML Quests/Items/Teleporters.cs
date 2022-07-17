using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Items
{
    public class MLQuestTeleporter : Teleporter
    {
        private Type m_QuestType;

        [Constructible]
        public MLQuestTeleporter()
            : this(Point3D.Zero)
        {
        }

        [Constructible]
        public MLQuestTeleporter(
            Point3D pointDest, Map mapDest = null, Type questType = null, TextDefinition message = null
        )
            : base(pointDest, mapDest)
        {
            m_QuestType = questType;
            Message = message;
        }

        public MLQuestTeleporter(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Type QuestType
        {
            get => m_QuestType;
            set
            {
                m_QuestType = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextDefinition Message { get; set; }

        public override bool CanTeleport(Mobile m)
        {
            if (!base.CanTeleport(m))
            {
                return false;
            }

            if (m_QuestType == null)
            {
                return true;
            }

            if (m is not PlayerMobile pm)
            {
                return false;
            }

            var context = MLQuestSystem.GetContext(pm);

            if (context?.IsDoingQuest(m_QuestType) == true || context?.HasDoneQuest(m_QuestType) == true)
            {
                return true;
            }

            TextDefinition.SendMessageTo(m, Message);
            return false;
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_QuestType != null)
            {
                list.Add($"Required quest: {m_QuestType.Name}");
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_QuestType?.FullName);
            TextDefinition.Serialize(writer, Message);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            var typeName = reader.ReadString();

            if (typeName != null)
            {
                m_QuestType = AssemblyHandler.FindTypeByFullName(typeName);
            }

            Message = TextDefinition.Deserialize(reader);
        }
    }

    public interface ITicket
    {
        void OnTicketUsed(Mobile from);
    }

    public class TicketTeleporter : Teleporter
    {
        private Type m_TicketType;

        [Constructible]
        public TicketTeleporter()
            : this(Point3D.Zero)
        {
        }

        [Constructible]
        public TicketTeleporter(
            Point3D pointDest, Map mapDest = null, Type ticketType = null, TextDefinition message = null
        )
            : base(pointDest, mapDest)
        {
            m_TicketType = ticketType;
            Message = message;
        }

        public TicketTeleporter(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Type TicketType
        {
            get => m_TicketType;
            set
            {
                m_TicketType = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextDefinition Message { get; set; }

        public override bool CanTeleport(Mobile m)
        {
            if (!base.CanTeleport(m))
            {
                return false;
            }

            if (m_TicketType == null)
            {
                return true;
            }

            var pack = m.Backpack;
            var ticket = pack?.FindItemByType(m_TicketType, false) ??
                         m.Items.Find(item => m_TicketType.IsInstanceOfType(item));

            if (ticket == null)
            {
                TextDefinition.SendMessageTo(m, Message);
                return false;
            }

            (ticket as ITicket)?.OnTicketUsed(m);

            return true;
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_TicketType != null)
            {
                list.Add($"Required ticket: {m_TicketType.Name}");
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write( m_TicketType?.FullName);
            TextDefinition.Serialize(writer, Message);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            var typeName = reader.ReadString();

            if (typeName != null)
            {
                m_TicketType = AssemblyHandler.FindTypeByFullName(typeName);
            }

            Message = TextDefinition.Deserialize(reader);
        }
    }
}
