using Server.Engines.Plants;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class PlantItemPropertyListTests
{
    // The getter used to re-initialize without a Reset, appending another copy of every property
    // per read. SendOPLPacketTo and SendPropertiesTo both go through it, so a pre-7.0.12 client
    // looking at a plant grew the buffer without bound.
    [Fact]
    public void OldClientPropertyList_BuildsOnceAndIsStableAcrossReads()
    {
        var plant = new PlantItem();

        try
        {
            var first = plant.OldClientPropertyList;
            var length = first.Buffer.Length;
            var hash = first.Hash;

            var second = plant.OldClientPropertyList;
            var third = plant.OldClientPropertyList;

            Assert.Same(first, second);
            Assert.Same(first, third);
            Assert.Equal(length, third.Buffer.Length);
            Assert.Equal(hash, third.Hash);
        }
        finally
        {
            plant.Delete();
        }
    }
}
