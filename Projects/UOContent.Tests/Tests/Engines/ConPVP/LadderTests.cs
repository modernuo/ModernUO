using Xunit;

namespace Server.Engines.ConPVP.Tests;

[Collection("Sequential UOContent Tests")]
public class LadderTests
{
    [Fact]
    public void TestFindCreatesOnceThenReturnsSameEntry()
    {
        var m = new Mobile((Serial)0x1024u);
        m.DefaultMobileInit();

        try
        {
            var ladder = new Ladder();
            var a = ladder.Find(m);
            var b = ladder.Find(m);

            Assert.NotNull(a);
            Assert.Same(a, b);
            Assert.Single(ladder.Entries);
        }
        finally
        {
            m.Delete();
        }
    }
}
