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
        // Stands in for the npc-speeds table (unconfigured in the test fixture).
        public double TableActiveMove;
        public double TablePassiveMove;

        public SpeedStub() : base(AIType.AI_Animal) => Body = 0xC9;

        public SpeedStub(Serial serial) : base(serial) => Body = 0xC9;

        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.3;
            passiveSpeed = 0.6;
        }

        public override void GetMoveSpeeds(out double activeMoveSpeed, out double passiveMoveSpeed)
        {
            activeMoveSpeed = TableActiveMove;
            passiveMoveSpeed = TablePassiveMove;
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

    [Fact]
    public void ScaleMoveSpeed_ScalesOverrides_LeavesInheritAlone()
    {
        var bc = NewCreature();
        bc.ActiveMoveSpeed = 0.6; // passive left inheriting

        bc.ScaleMoveSpeed(1.0 / 1.2);

        Assert.Equal(0.5, bc.ActiveMoveSpeed);
        Assert.Equal(bc.PassiveSpeed, bc.PassiveMoveSpeed); // still inheriting, not 0 * scalar
    }

    [Fact]
    public void Herding_DrivesMoveClock_ThinkUntouched()
    {
        var bc = NewCreature(); // think 0.3/0.6, passive
        bc.SetMoveSpeed(0.45, 1.05);

        bc.TargetLocation = new Point2D(10, 10);

        Assert.Equal(0.6, bc.CurrentSpeed);     // think clock unaffected by herding
        Assert.Equal(0.3, bc.CurrentMoveSpeed); // fixed herding pace, not 1.05

        bc.TargetLocation = null;
        Assert.Equal(1.05, bc.CurrentMoveSpeed);
    }

    [Fact]
    public void SnapSpeedsToTable_UndoesScalingDrift_KeepsTunedValues()
    {
        var bc = NewCreature();
        bc.TableActiveMove = 0.45;
        bc.TablePassiveMove = 0.9;
        bc.SetMoveSpeed(0.45, 0.9);

        // 0.45 and 0.9 do not survive /1.2 then *1.2 bit-exactly.
        bc.ScaleMoveSpeed(1.0 / 1.2);
        bc.ScaleMoveSpeed(1.2);
        Assert.NotEqual(0.45, bc.ActiveMoveSpeed);

        bc.SnapSpeedsToTable();
        Assert.Equal(0.45, bc.ActiveMoveSpeed);
        Assert.Equal(0.9, bc.PassiveMoveSpeed);

        // A hand-tuned value is nowhere near the epsilon and must keep.
        bc.SetMoveSpeed(0.7, 0.9);
        bc.SnapSpeedsToTable();
        Assert.Equal(0.7, bc.ActiveMoveSpeed);
    }

    [Fact]
    public void Migration_MatchingThinkSpeeds_AdoptTableMoveValues()
    {
        var bc = NewCreature(); // think 0.3/0.6, matching its table entry
        bc.TableActiveMove = 0.45;
        bc.TablePassiveMove = 0.9;

        bc.MigrateMoveSpeeds();

        Assert.Equal(0.45, bc.ActiveMoveSpeed);
        Assert.Equal(0.9, bc.PassiveMoveSpeed);
    }

    [Fact]
    public void Migration_TunedThinkSpeeds_KeepInheriting()
    {
        var bc = NewCreature();
        bc.SetSpeed(0.35, 0.6); // hand-tuned: no longer matches the table entry
        bc.TableActiveMove = 0.45;
        bc.TablePassiveMove = 0.9;

        bc.MigrateMoveSpeeds();

        Assert.Equal(0.35, bc.ActiveMoveSpeed);
        Assert.Equal(0.6, bc.PassiveMoveSpeed);
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
