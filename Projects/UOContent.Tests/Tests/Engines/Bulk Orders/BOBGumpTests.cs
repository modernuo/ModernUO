using Server.Engines.BulkOrders;
using Xunit;

namespace UOContent.Tests;

public class BOBGumpTests
{
    [Fact]
    public void TailorMaterialNameToleratesUnresolvedItemType()
    {
        // A saved entry whose item type no longer resolves loads with a null type.
        Assert.Equal(1062235, BOBGump.GetMaterialName(BulkMaterialType.None, BODType.Tailor, null).Number);
    }
}
