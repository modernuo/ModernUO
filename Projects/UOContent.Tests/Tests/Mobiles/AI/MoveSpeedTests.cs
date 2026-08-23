using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// Pins the CurrentMoveSpeed classification (verbatim active/passive maps to the matching
// move value; bespoke stays fused), SetSpeed's one-clock guarantee, and the v22 tail.
[Collection("Sequential UOContent Tests")]
public class MoveSpeedTests : IDisposable
{
    // Delete spawned stubs so they don't linger in the shared static World.
    private readonly List<Mobile> _created = new();

    public void Dispose()
    {
        for (var i = 0; i < _created.Count; i++)
        {
            _created[i].Delete();
        }
    }

    private sealed class SpeedStub : BaseCreature
    {
        public SpeedStub() : base(AIType.AI_Animal) => Body = 0xC9;

        public SpeedStub(Serial serial) : base(serial) => Body = 0xC9;

        // NPCSpeeds isn't configured in the test fixture; provide fixed think speeds.
        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.3;
            passiveSpeed = 0.6;
        }
    }

    private SpeedStub NewCreature()
    {
        var bc = new SpeedStub();
        _created.Add(bc);
        return bc;
    }

    [Fact]
    public void MoveSpeeds_InheritThinkValues_ByDefault()
    {
        var bc = NewCreature();

        Assert.Equal(0.3, bc.ActiveMoveSpeed);
        Assert.Equal(0.6, bc.PassiveMoveSpeed);
        Assert.Equal(bc.CurrentSpeed, bc.CurrentMoveSpeed);
    }

    [Fact]
    public void CurrentMoveSpeed_ResolvesPerMode_WhenOverridden()
    {
        var bc = NewCreature();
        bc.SetMoveSpeed(0.45, 0.9);

        // SetSpeed left the creature passive; the think clock is untouched.
        Assert.Equal(0.6, bc.CurrentSpeed);
        Assert.Equal(0.9, bc.CurrentMoveSpeed);

        bc.SetCurrentSpeedToActive();
        Assert.Equal(0.3, bc.CurrentSpeed);
        Assert.Equal(0.45, bc.CurrentMoveSpeed);
    }

    [Fact]
    public void CurrentMoveSpeed_BespokePace_StaysFused()
    {
        var bc = NewCreature();
        bc.SetMoveSpeed(0.45, 0.9);

        // Neither think value verbatim, so both clocks run it.
        bc.CurrentSpeed = 0.11;
        Assert.Equal(0.11, bc.CurrentMoveSpeed);
    }

    [Fact]
    public void SetSpeed_ClearsMoveOverrides()
    {
        var bc = NewCreature();
        bc.SetMoveSpeed(0.45, 0.9);

        bc.SetSpeed(0.2, 0.4);

        Assert.Equal(0.2, bc.ActiveMoveSpeed);
        Assert.Equal(0.4, bc.PassiveMoveSpeed);
    }

    [Fact]
    public void NonPositiveMoveSpeed_ClearsThatOverride()
    {
        var bc = NewCreature();
        bc.SetMoveSpeed(0.45, 0.9);

        bc.ActiveMoveSpeed = 0;

        Assert.Equal(0.3, bc.ActiveMoveSpeed);  // inheriting again
        Assert.Equal(0.9, bc.PassiveMoveSpeed); // other override untouched
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MoveSpeedOverrides_SurviveSerialization(bool overridden)
    {
        var bc = NewCreature();
        if (overridden)
        {
            bc.SetMoveSpeed(0.45, 0.9);
        }

        var writer = new BufferWriter(true);
        bc.Serialize(writer);

        var buffer = new byte[writer.Position];
        writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

        var copy = new SpeedStub(World.NewMobile);
        _created.Add(copy);
        var reader = new BufferReader(buffer);
        copy.Deserialize(reader);

        // The v22 tail is the last block; exact consumption catches any offset mistake.
        Assert.Equal(buffer.Length, reader.Position);
        Assert.Equal(overridden ? 0.45 : 0.3, copy.ActiveMoveSpeed);
        Assert.Equal(overridden ? 0.9 : 0.6, copy.PassiveMoveSpeed);
    }
}
