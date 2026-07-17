using System;
using System.Collections.Generic;
using System.IO;
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

    [Fact]
    public void UnresolvedTableEntrySkipsItsRecordsAfterConfirmation()
    {
        var previousAssemblies = AssemblyHandler.Assemblies;
        AssemblyHandler.Assemblies = [.. previousAssemblies ?? [], typeof(RoundTripEntity).Assembly];

        var dir = Path.Combine(Path.GetTempPath(), $"muo-typetable-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(dir, "TypeTable"));

        TypeTablePersistence loaded = null;
        var previousIn = Console.In;

        try
        {
            // Hand-write a v4 idx: two table entries (one bogus), one record per type.
            using (var idx = new FileBufferWriter(Path.Combine(dir, "TypeTable", "TypeTable.idx")))
            {
                idx.Write(4);                                       // version
                idx.Write(2);                                       // type table count
                idx.WriteRaw(typeof(RoundTripEntity).FullName);     // index 0: resolvable
                idx.WriteRaw("Server.Tests.DoesNotExistAnymore");   // index 1: bogus
                idx.Write(2);                                       // record count
                idx.Write((ushort)0);                               // record 1: real type
                idx.Write(1u);                                      // serial
                idx.Write(DateTime.UtcNow.Ticks);
                idx.Write(0L);                                      // position
                idx.Write(4);                                       // length
                idx.Write((ushort)1);                               // record 2: bogus type
                idx.Write(2u);
                idx.Write(DateTime.UtcNow.Ticks);
                idx.Write(4L);
                idx.Write(4);
            }

            // GetConstructorFor prompts on the console; answer "y" (delete those types).
            Console.SetIn(new StringReader("y\n"));

            loaded = new TypeTablePersistence();
            loaded.DeserializeIndexes(dir, null);

            Assert.Single(loaded.EntitiesBySerial);
            Assert.True(loaded.EntitiesBySerial.ContainsKey((Serial)1u));
            Assert.True(loaded.TryGetTypeIndex(typeof(RoundTripEntity), out _));
        }
        finally
        {
            Console.SetIn(previousIn);
            loaded?.Unregister();
            AssemblyHandler.Assemblies = previousAssemblies;
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void LegacyV3IndexesStillLoadAndHydrateTheTypeTable()
    {
        var previousAssemblies = AssemblyHandler.Assemblies;
        AssemblyHandler.Assemblies = [.. previousAssemblies ?? [], typeof(RoundTripEntity).Assembly];

        var dir = Path.Combine(Path.GetTempPath(), $"muo-legacyidx-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(dir, "TypeTable"));

        TypeTablePersistence loaded = null;

        try
        {
            var hash = AssemblyHandler.GetTypeHash(typeof(RoundTripEntity));

            // Hand-write a v3 idx: records carry flag + 8-byte hash, resolved via typesDb.
            using (var idx = new FileBufferWriter(Path.Combine(dir, "TypeTable", "TypeTable.idx")))
            {
                idx.Write(3);                        // version
                idx.Write(1);                        // record count
                idx.Write((byte)2);                  // xxHash3 flag
                idx.Write(hash);
                idx.Write(1u);                       // serial
                idx.Write(DateTime.UtcNow.Ticks);
                idx.Write(0L);                       // position
                idx.Write(4);                        // length
            }

            var typesDb = new Dictionary<ulong, string> { [hash] = typeof(RoundTripEntity).FullName };

            loaded = new TypeTablePersistence();
            loaded.DeserializeIndexes(dir, typesDb);

            Assert.Single(loaded.EntitiesBySerial);
            Assert.True(loaded.EntitiesBySerial.ContainsKey((Serial)1u));

            // Legacy loads must hydrate the table so the NEXT save can write v4 indexes.
            Assert.True(loaded.TryGetTypeIndex(typeof(RoundTripEntity), out _));
        }
        finally
        {
            loaded?.Unregister();
            AssemblyHandler.Assemblies = previousAssemblies;
            Directory.Delete(dir, true);
        }
    }
}
