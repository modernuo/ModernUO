using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// Guard-following may pathfind, so this shares the pathfinding collection.
[Collection("Sequential Pathfinding Tests")]
public class GuardFollowTests : IDisposable
{
    private readonly List<Mobile> _created = [];

    public void Dispose()
    {
        for (var i = 0; i < _created.Count; i++)
        {
            _created[i].Delete();
        }

        _created.Clear();
    }

    [Fact]
    public void GuardFollow_StepsTowardMaster_AndRegistersMoveIntent()
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);
        map.GetAverageZ(1500, 1600, out _, out var z, out _);

        // Pet 6 tiles east of the master, open terrain.
        var (master, pet) = PetTestSetup.SpawnControlledPet(
            new Point3D(1494, 1600, (sbyte)z),
            new Point3D(1500, 1600, (sbyte)z),
            map
        );
        _created.Add(master);
        _created.Add(pet);

        var ai = pet.AIObject;
        ai.AITimer?.Stop(); // drive manually
        pet.ControlOrder = OrderType.Guard;
        ai.AITimer?.Stop(); // the order change may restart the timer

        var start = pet.Location;
        ai.NextMove = 0;
        ai.Obey();

        Assert.True(pet.Location != start, "a guarding pet beyond guard range must step toward its master");
        // Without a move intent, guard-following only steps on the think grid.
        Assert.True(ai.TryGetMoveWake(out _), "guard-following must register a move intent");

        // The return is paced by FollowMoveSpeed; the think clock stays on the active value
        // and the per-step speed flip must not undo either.
        Assert.Equal(0.2, pet.CurrentSpeed);
        Assert.Equal(pet.FollowMoveSpeed, pet.CurrentMoveSpeed);
    }
}
