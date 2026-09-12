using Server;
using Server.Misc;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class SkillEventsTests
{
    private sealed class Recorder
    {
        public Mobile From;
        public Skill Skill;
        public bool Success;
        public int Calls;

        public void Handle(Mobile from, Skill skill, bool success)
        {
            From = from;
            Skill = skill;
            Success = success;
            Calls++;
        }
    }

    [Fact]
    public void DirectTarget_RolledAttempt_RaisesOnceWithTheReturnedOutcome()
    {
        var from = new Mobile();
        var skill = from.Skills[SkillName.Mining];
        var recorder = new Recorder();

        SkillEvents.SkillUsed += recorder.Handle;
        try
        {
            var rolled = SkillCheck.Mobile_SkillCheckDirectTarget(from, SkillName.Mining, null, 0.5);
            Assert.Equal(1, recorder.Calls);
            Assert.Same(from, recorder.From);
            Assert.Same(skill, recorder.Skill);
            Assert.Equal(rolled, recorder.Success);

            Assert.False(SkillCheck.Mobile_SkillCheckDirectTarget(from, SkillName.Mining, null, 0.0));
            Assert.Equal(2, recorder.Calls);
            Assert.False(recorder.Success);
        }
        finally
        {
            SkillEvents.SkillUsed -= recorder.Handle;
            from.Delete();
        }
    }

    [Fact]
    public void ShortCircuits_StillRaise_WithTheHandlerOutcome()
    {
        var from = new Mobile();
        var recorder = new Recorder();

        SkillEvents.SkillUsed += recorder.Handle;
        try
        {
            Assert.True(SkillCheck.Mobile_SkillCheckDirectLocation(from, SkillName.Mining, 1.0));
            Assert.Equal(1, recorder.Calls);
            Assert.True(recorder.Success);

            Assert.False(SkillCheck.Mobile_SkillCheckDirectTarget(from, SkillName.Mining, null, -0.1));
            Assert.Equal(2, recorder.Calls);
            Assert.False(recorder.Success);

            Assert.False(SkillCheck.Mobile_SkillCheckLocation(from, SkillName.Mining, 50.0, 100.0));
            Assert.Equal(3, recorder.Calls);
            Assert.False(recorder.Success);

            Assert.True(SkillCheck.Mobile_SkillCheckTarget(from, SkillName.Mining, null, 0.0, 0.0));
            Assert.Equal(4, recorder.Calls);
            Assert.True(recorder.Success);
        }
        finally
        {
            SkillEvents.SkillUsed -= recorder.Handle;
            from.Delete();
        }
    }

    [Fact]
    public void CheckSkill_Direct_DoesNotRaise()
    {
        var from = new Mobile();
        var skill = from.Skills[SkillName.Mining];
        var recorder = new Recorder();

        SkillEvents.SkillUsed += recorder.Handle;
        try
        {
            SkillCheck.CheckSkill(from, skill, null, 1.0);
            Assert.Equal(0, recorder.Calls);
        }
        finally
        {
            SkillEvents.SkillUsed -= recorder.Handle;
            from.Delete();
        }
    }

    [Fact]
    public void NoSubscriber_DoesNotThrow()
    {
        var from = new Mobile();
        var recorder = new Recorder();

        try
        {
            SkillEvents.SkillUsed += recorder.Handle;
            SkillEvents.SkillUsed -= recorder.Handle;

            Assert.True(SkillCheck.Mobile_SkillCheckDirectLocation(from, SkillName.Mining, 1.0));
            Assert.Equal(0, recorder.Calls);
        }
        finally
        {
            from.Delete();
        }
    }
}
