using System;
using System.Reflection;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles;

[Collection("Sequential UOContent Tests")]
public class PlagueBeastLordTests
{
    private const BindingFlags NonPublic = BindingFlags.Instance | BindingFlags.NonPublic;

    [Fact]
    public void DecayTimerProgress_SurvivesReload()
    {
        var from = new Mobile();
        var lord = new PlagueBeastLord();
        lord.OnParalyzed(from);

        var timer = (Timer)typeof(PlagueBeastLord).GetField("m_Timer", NonPublic)!.GetValue(lord);
        var tick = typeof(Timer).GetMethod("OnTick", NonPublic)!;
        for (var i = 0; i < 5; i++)
        {
            tick.Invoke(timer, null);
        }

        var writer = new BufferWriter(true);
        lord.Serialize(writer);
        var buffer = writer.Buffer.AsSpan(0, (int)writer.Position).ToArray();

        var copy = new PlagueBeastLord(World.NewMobile);
        copy.Deserialize(new BufferReader(buffer));

        var copyTimer = (PlagueBeastLord.DecayTimer)typeof(PlagueBeastLord).GetField("m_Timer", NonPublic)!.GetValue(copy);
        Assert.NotNull(copyTimer);
        Assert.Equal(5, copyTimer.Count);
        Assert.Equal(120, copyTimer.Deadline);

        from.Delete();
        lord.Delete();
        copy.Delete();
    }
}
