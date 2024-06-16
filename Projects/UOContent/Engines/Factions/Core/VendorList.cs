using System.Collections.Generic;

namespace Server.Factions;

public class VendorList
{
    public VendorList(VendorDefinition definition)
    {
        Definition = definition;
        Vendors = [];
    }

    public VendorDefinition Definition { get; }

    public List<BaseFactionVendor> Vendors { get; }

    public BaseFactionVendor Construct(Town town, Faction faction)
    {
        try
        {
            return Definition.Type.CreateInstance<BaseFactionVendor>(town, faction);
        }
        catch
        {
            return null;
        }
    }
}