using System;
using Server;
using Server.Items;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class TreasureChestDecayTests
{
    public static TheoryData<Type> ChestTypes =>
    [
        typeof(TreasureChestLevel1),
        typeof(TreasureChestLevel2),
        typeof(TreasureChestLevel3),
        typeof(TreasureChestLevel4)
    ];

    // DecayScheduler reads ScheduledDecayTime repeatedly and needs the reads to agree, so the
    // random interval must be rolled once per chest rather than per read.
    [Theory]
    [MemberData(nameof(ChestTypes))]
    public void DecayTimeIsStableAcrossReads(Type chestType)
    {
        var chest = chestType.CreateInstance<LockableContainer>();

        try
        {
            var expected = chest.DecayTime;

            for (var i = 0; i < 100; i++)
            {
                Assert.Equal(expected, chest.DecayTime);
            }
        }
        finally
        {
            chest.Delete();
        }
    }

    [Theory]
    [MemberData(nameof(ChestTypes))]
    public void ScheduledDecayTimeIsStableAcrossReads(Type chestType)
    {
        var chest = chestType.CreateInstance<LockableContainer>();

        try
        {
            chest.MoveToWorld(new Point3D(150, 150, 0), Map.Felucca);

            var expected = chest.ScheduledDecayTime;

            for (var i = 0; i < 100; i++)
            {
                Assert.Equal(expected, chest.ScheduledDecayTime);
            }
        }
        finally
        {
            chest.Delete();
        }
    }
}
