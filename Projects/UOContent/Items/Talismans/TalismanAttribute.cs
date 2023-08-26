using System;
using ModernUO.Serialization;

namespace Server.Items;

[PropertyObject]
[SerializationGenerator(1, false)]
public partial class TalismanAttribute
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Type _type;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TextDefinition _name;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _amount;

    public TalismanAttribute() : this(null, null)
    {
    }

    public TalismanAttribute(TalismanAttribute copy)
    {
        if (copy != null)
        {
            _type = copy.Type;
            _name = copy.Name;
            _amount = copy.Amount;
        }
    }

    public TalismanAttribute(Type type, TextDefinition name, int amount = 0)
    {
        _type = type;
        _name = name;
        _amount = amount;
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        var flags = reader.ReadEncodedInt();

        if ((flags & 0x1) != 0)
        {
            _type = AssemblyHandler.FindTypeByFullName(reader.ReadString());
        }

        if ((flags & 0x2) != 0)
        {
            _name = reader.ReadTextDefinition();
        }

        if ((flags & 0x4) != 0)
        {
            _amount = reader.ReadEncodedInt();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsEmpty => _type == null;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsItem => _type.IsAssignableTo(typeof(Item));

    public override string ToString() => _type?.Name ?? "None";

    public int DamageBonus(Mobile to) => to?.GetType() == _type ? _amount : 0;
}
