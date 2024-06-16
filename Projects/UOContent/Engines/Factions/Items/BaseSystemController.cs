using ModernUO.Serialization;

namespace Server.Factions;

[SerializationGenerator(1, false)]
public abstract partial class BaseSystemController : Item
{
    private int _labelNumber;

    public BaseSystemController(int itemID) : base(itemID)
    {
    }

    public virtual int DefaultLabelNumber => 0;

    [SerializableProperty(0, useField: nameof(_labelNumber))]
    public override int LabelNumber => _labelNumber > 0 ? _labelNumber : DefaultLabelNumber;

    public virtual void AssignName(TextDefinition name)
    {
        if (name?.Number > 0)
        {
            _labelNumber = name.Number;
            Name = null;
        }
        else if (name?.String != null)
        {
            _labelNumber = 0;
            Name = name.String;
        }
        else
        {
            _labelNumber = 0;
            Name = null;
        }

        InvalidateProperties();
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        // Do nothing
    }
}
