using System;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class MobileLazyModListTests
{
    private class TestMobile : Mobile
    {
    }

    [Fact]
    public void FreshMobile_HasNoSkillModList()
    {
        var m = new TestMobile();

        try
        {
            Assert.Null(m.SkillMods);
        }
        finally
        {
            m.Delete();
        }
    }

    [Fact]
    public void AddSkillMod_CreatesList_RemoveSkillMod_ReleasesIt()
    {
        var m = new TestMobile();

        try
        {
            m.AddSkillMod(new DefaultSkillMod(SkillName.Magery, "test-mod", true, 10.0));

            Assert.NotNull(m.SkillMods);
            Assert.Single(m.SkillMods);
            Assert.NotNull(m.GetSkillMod("test-mod"));

            m.RemoveSkillMod("test-mod");

            Assert.Null(m.SkillMods);
            Assert.Null(m.GetSkillMod("test-mod"));
        }
        finally
        {
            m.Delete();
        }
    }

    [Fact]
    public void StatMods_LazyThroughAddAndRemove()
    {
        var m = new TestMobile();

        try
        {
            Assert.Null(m.GetStatMod("test-stat"));

            m.AddStatMod(new StatMod(StatType.Str, "test-stat", 5, TimeSpan.Zero));
            Assert.NotNull(m.GetStatMod("test-stat"));

            m.RemoveStatMod("test-stat");
            Assert.Null(m.GetStatMod("test-stat"));
        }
        finally
        {
            m.Delete();
        }
    }
}
