using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// The Running bit is derived from the step pace: a step shorter than the client's walk
// interpolation (400ms on foot, 200ms mounted/flying) is flagged as a run.
[Collection("Sequential Pathfinding Tests")]
public class RunFlagTests : System.IDisposable
{
    private readonly List<Mobile> _created = new();

    private PetTestStub Spawn(double activeMove)
    {
        var pet = new PetTestStub();
        pet.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
        pet.AIObject.AITimer?.Stop();
        pet.SetMoveSpeed(activeMove, activeMove * 3);
        pet.SetCurrentSpeedToActive();
        _created.Add(pet);
        return pet;
    }

    public void Dispose()
    {
        foreach (var m in _created)
        {
            m?.Delete();
        }

        _created.Clear();
    }

    [Theory]
    [InlineData(0.3, true)]
    [InlineData(0.125, true)]
    [InlineData(0.4, false)]
    [InlineData(0.45, false)]
    [InlineData(1.05, false)]
    public void FootCreature_RunsOnlyWhenFasterThanWalk(double activeMove, bool expected)
    {
        var pet = Spawn(activeMove);

        Assert.Equal(activeMove, pet.CurrentMoveSpeed);
        Assert.Equal(expected, pet.AIObject.ShouldRun());
    }

    [Theory]
    [InlineData(0.3, false)]
    [InlineData(0.15, true)]
    public void FlyingCreature_UsesMountThresholds(double activeMove, bool expected)
    {
        var pet = Spawn(activeMove);
        pet.Flying = true;

        Assert.Equal(expected, pet.AIObject.ShouldRun());
    }

    [Fact]
    public void BadlyHurt_SlowsBelowWalk_DropsToWalk()
    {
        var pet = Spawn(0.35);
        Assert.True(pet.AIObject.ShouldRun());

        // The hurt inflation is on the observed step pace, so the flag follows it.
        pet.SetHits(100);
        pet.Hits = 5;
        pet.SetStam(100);
        pet.Stam = 5;

        Assert.False(pet.AIObject.ShouldRun());
    }

    [Theory]
    [InlineData(0.3, true)]
    [InlineData(0.45, false)]
    public void DoMove_StampsRunningBit(double activeMove, bool expected)
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);
        map.GetAverageZ(1500, 1600, out _, out var z, out _);

        var pet = Spawn(activeMove);
        pet.MoveToWorld(new Point3D(1500, 1600, (sbyte)z), map);

        var ai = pet.AIObject;
        ai.NextMove = 0;
        var start = pet.Location;

        Assert.True(ai.DoMove(Direction.West));
        Assert.NotEqual(start, pet.Location);
        Assert.Equal(expected, (pet.Direction & Direction.Running) != 0);
    }
}
