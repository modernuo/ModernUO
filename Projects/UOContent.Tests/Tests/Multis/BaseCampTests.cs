using Server;
using Server.Multis;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class BaseCampTests
{
    private class CountingCamp : BaseCamp
    {
        public int Built;

        public CountingCamp() : base(0x10EE)
        {
        }

        public override void AddComponents() => Built++;
    }

    [Fact]
    public void CheckAddComponents_SkipsACampRestoredWithItsComponents()
    {
        var fresh = new CountingCamp();
        var restored = new CountingCamp();
        var item = new Item(0xDE3);
        restored.AddToItems(item);

        fresh.CheckAddComponents();
        restored.CheckAddComponents();

        Assert.Equal(1, fresh.Built);
        Assert.Equal(0, restored.Built);

        item.Delete();
        fresh.Delete();
        restored.Delete();
    }
}
