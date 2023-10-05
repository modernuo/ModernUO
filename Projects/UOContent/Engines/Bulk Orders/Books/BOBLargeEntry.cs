using ModernUO.Serialization;

namespace Server.Engines.BulkOrders;

[SerializationGenerator(1)]
public partial class BOBLargeEntry : BaseBOBEntry
{
    [SerializableField(0, setter: "private")]
    private BOBLargeSubEntry[] _entries;

    public BOBLargeEntry(LargeBOD bod)
    {
        RequireExceptional = bod.RequireExceptional;

        DeedType = bod switch
        {
            LargeTailorBOD => BODType.Tailor,
            LargeSmithBOD  => BODType.Smith,
            _              => DeedType
        };

        Material = bod.Material;
        AmountMax = bod.AmountMax;

        _entries = new BOBLargeSubEntry[bod.Entries.Length];

        for (var i = 0; i < _entries.Length; ++i)
        {
            _entries[i] = new BOBLargeSubEntry(bod.Entries[i]);
        }
    }

    public override Item Reconstruct()
    {
        LargeBOD bod = DeedType switch
        {
            BODType.Smith  => new LargeSmithBOD(AmountMax, RequireExceptional, Material, ReconstructEntries()),
            BODType.Tailor => new LargeTailorBOD(AmountMax, RequireExceptional, Material, ReconstructEntries()),
            _              => null
        };

        for (var i = 0; i < bod?.Entries.Length; ++i)
        {
            bod.Entries[i].Owner = bod;
        }

        return bod;
    }

    private LargeBulkEntry[] ReconstructEntries()
    {
        var entries = new LargeBulkEntry[Entries.Length];

        for (var i = 0; i < Entries.Length; ++i)
        {
            entries[i] = new LargeBulkEntry(
                    null,
                    new SmallBulkEntry(Entries[i].ItemType, Entries[i].Number, Entries[i].Graphic)
                )
                { Amount = Entries[i].AmountCur };
        }

        return entries;
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        RequireExceptional = reader.ReadBool();

        DeedType = (BODType)reader.ReadEncodedInt();
        Material = (BulkMaterialType)reader.ReadEncodedInt();
        AmountMax = reader.ReadEncodedInt();
        Price = reader.ReadEncodedInt();

        _entries = new BOBLargeSubEntry[reader.ReadEncodedInt()];

        for (var i = 0; i < Entries.Length; ++i)
        {
            _entries[i] = new BOBLargeSubEntry();
            _entries[i].Deserialize(reader);
        }
    }
}
