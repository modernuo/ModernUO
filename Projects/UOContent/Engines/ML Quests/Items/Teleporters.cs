using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Items;

[SerializationGenerator(1, false)]
public partial class MLQuestTeleporter : Teleporter
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Type _questType;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TextDefinition _message;

    [Constructible]
    public MLQuestTeleporter() : this(Point3D.Zero)
    {
    }

    [Constructible]
    public MLQuestTeleporter(
        Point3D pointDest, Map mapDest = null, Type questType = null, TextDefinition message = null
    ) : base(pointDest, mapDest)
    {
        _questType = questType;
        Message = message;
    }

    public override bool CanTeleport(Mobile m)
    {
        if (!base.CanTeleport(m))
        {
            return false;
        }

        if (_questType == null)
        {
            return true;
        }

        if (m is not PlayerMobile pm)
        {
            return false;
        }

        var context = MLQuestSystem.GetContext(pm);

        if (context?.IsDoingQuest(_questType) == true || context?.HasDoneQuest(_questType) == true)
        {
            return true;
        }

        Message.SendMessageTo(m);
        return false;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_questType != null)
        {
            list.Add($"Required quest: {_questType.Name}");
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        var typeName = reader.ReadString();

        if (typeName != null)
        {
            _questType = AssemblyHandler.FindTypeByFullName(typeName);
        }

        _message = reader.ReadTextDefinition();
    }
}

public interface ITicket
{
    void OnTicketUsed(Mobile from);
}

[SerializationGenerator(1, false)]
public partial class TicketTeleporter : Teleporter
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Type _ticketType;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TextDefinition _message;

    [Constructible]
    public TicketTeleporter() : this(Point3D.Zero)
    {
    }

    [Constructible]
    public TicketTeleporter(
        Point3D pointDest, Map mapDest = null, Type ticketType = null, TextDefinition message = null
    ) : base(pointDest, mapDest)
    {
        _ticketType = ticketType;
        Message = message;
    }

    public override bool CanTeleport(Mobile m)
    {
        if (!base.CanTeleport(m))
        {
            return false;
        }

        if (_ticketType == null)
        {
            return true;
        }

        var pack = m.Backpack;
        var ticket = pack?.FindItemByType(_ticketType, false) ??
                     m.Items.Find(item => _ticketType.IsInstanceOfType(item));

        if (ticket == null)
        {
            Message.SendMessageTo(m);
            return false;
        }

        (ticket as ITicket)?.OnTicketUsed(m);

        return true;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_ticketType != null)
        {
            list.Add($"Required ticket: {_ticketType.Name}");
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        var typeName = reader.ReadString();

        if (typeName != null)
        {
            _ticketType = AssemblyHandler.FindTypeByFullName(typeName);
        }

        _message = reader.ReadTextDefinition();
    }
}
