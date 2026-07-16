using System;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class GenericEntityPersistenceTypeTableTests
{
    private class TypeTablePersistence : GenericEntityPersistence<RoundTripEntity>
    {
        public TypeTablePersistence() : base("TypeTable", 3000, 1, 0x7FFFFFFF)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
        }

        public override void Deserialize(IGenericReader reader)
        {
        }
    }

    [Fact]
    public void RegisterTypeAssignsStableInsertionOrderedIndexes()
    {
        var persistence = new TypeTablePersistence();

        try
        {
            persistence.RegisterType(typeof(RoundTripEntity));
            persistence.RegisterType(typeof(string));
            persistence.RegisterType(typeof(RoundTripEntity)); // duplicate is a no-op

            Assert.Equal(2, persistence.TypeTable.Count);
            Assert.Same(typeof(RoundTripEntity), persistence.TypeTable[0]);
            Assert.Same(typeof(string), persistence.TypeTable[1]);

            Assert.True(persistence.TryGetTypeIndex(typeof(RoundTripEntity), out var first));
            Assert.Equal(0, first);
            Assert.True(persistence.TryGetTypeIndex(typeof(string), out var second));
            Assert.Equal(1, second);
            Assert.False(persistence.TryGetTypeIndex(typeof(int), out _));
        }
        finally
        {
            persistence.Unregister();
        }
    }
}
