using System;
using System.IO;
using ModernUO.Serialization;

namespace Server.Engines.BulkOrders;

[SerializationGenerator(1)]
public abstract partial class BaseBOBEntry : IBOBEntry
{
    [SerializableField(0, setter: "protected")]
    private bool _requireExceptional;

    [SerializableField(1, setter: "protected")]
    private BODType _deedType;

    [SerializableField(2, setter: "protected")]
    private BulkMaterialType _material;

    [SerializableField(3, setter: "protected")]
    private int _amountMax;

    [SerializableField(4)]
    private int _price;

    public DateTime Created { get; set; } = Core.Now;

    public DateTime LastSerialized { get; set; } = Core.Now;

    public Serial Serial { get; }

    public bool Deleted { get; private set; }

    public BaseBOBEntry()
    {
        Serial = BOBEntries.NewBOBEntry;
        BOBEntries.Add(this);
    }

    public virtual void Delete()
    {
        Deleted = true;
        BOBEntries.Remove(this);
    }

    public abstract Item Reconstruct();

    private void Deserialize(IGenericReader reader, int version)
    {
        if (version == 0)
        {
            // version 0 - This class didn't exist, so we are going to skip deserializing
            // Seek back 1 byte because encoded int of 0 is 1 byte
            reader.Seek(-1, SeekOrigin.Current);
        }
        else
        {
            throw new Exception("Unknown deserialization error in presource generated BOB Entries");
        }
    }
}
