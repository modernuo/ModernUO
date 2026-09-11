using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Commands.Generic;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Commands;

// `where` constants resolve through Types.TryParse like every other command. Type- and
// entity-valued properties have no static Parse, so the old local parser threw on them.
[Collection("Sequential UOContent Tests")]
public class WhereConstantParsingTests : IDisposable
{
    private readonly List<IEntity> _entities = [];

    public void Dispose()
    {
        for (var i = 0; i < _entities.Count; i++)
        {
            _entities[i].Delete();
        }

        _entities.Clear();
    }

    public class Subject
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public Type Kind { get; set; } = typeof(Static);

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Thing { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Label { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextDefinition Message { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map Facet { get; set; } = Map.Felucca;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Count { get; set; } = 0x1F13;

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName Craft { get; set; } = SkillName.Magery;
    }

    private static bool Check(object target, string binding, string value,
        ComparisonOperator op = ComparisonOperator.Equal)
    {
        var prop = new Property(binding);
        prop.BindTo(typeof(Subject), PropertyAccess.Read);

        var compiled = ConditionalCompiler.Compile(
            typeof(Subject),
            [TypeCondition.Default, new ComparisonCondition(prop, false, op, value)]
        );

        return compiled.Verify(target);
    }

    private T Track<T>(T entity) where T : IEntity
    {
        _entities.Add(entity);
        return entity;
    }

    [Fact]
    public void TypeValuedPropertiesResolveByName()
    {
        var s = new Subject();

        Assert.True(Check(s, "Kind", "Static"));
        Assert.False(Check(s, "Kind", "Container"));
    }

    [Fact]
    public void EntityValuedPropertiesResolveBySerial()
    {
        var mob = Track(new Mobile());
        var item = Track(new Static(0x1F13));
        var s = new Subject { Owner = mob, Thing = item };

        Assert.True(Check(s, "Owner", mob.Serial.ToString()));
        Assert.True(Check(s, "Thing", item.Serial.ToString()));
        Assert.False(Check(s, "Thing", (item.Serial + 1).ToString()));
    }

    // `where` spells null as a bare `null`, not [set's (-null-); every existing clause relies on it.
    [Fact]
    public void BareNullStillMeansNull()
    {
        var s = new Subject();

        Assert.True(Check(s, "Label", "null"));
        Assert.True(Check(s, "Owner", "null"));

        s.Label = "set";
        s.Owner = Track(new Mobile());

        Assert.False(Check(s, "Label", "null"));
        Assert.False(Check(s, "Owner", "null"));
    }

    [Fact]
    public void QuotedNullStillMeansTheLiteralString()
    {
        var s = new Subject { Label = "null" };

        Assert.True(Check(s, "Label", @"@""null"""));
        Assert.False(Check(s, "Label", "null"));
    }

    [Theory]
    [InlineData("Facet", "Felucca", true)]
    [InlineData("Facet", "Trammel", false)]
    [InlineData("Count", "0x1F13", true)]
    [InlineData("Count", "7955", true)]
    [InlineData("Count", "0x1F14", false)]
    [InlineData("Craft", "Magery", true)]
    [InlineData("Craft", "Anatomy", false)]
    public void ExistingConstantFormsKeepWorking(string binding, string value, bool expected)
    {
        Assert.Equal(expected, Check(new Subject(), binding, value));
    }

    [Theory]
    [InlineData("#1060847", true)]
    [InlineData("#1060848", false)]
    public void TextDefinitionMarkersReachTheWhereClause(string value, bool expected)
    {
        var s = new Subject { Message = TextDefinition.Of(1060847) };

        Assert.Equal(expected, Check(s, "Message", value));
    }

    [Fact]
    public void QuotedLiteralsReachTextDefinitionsInWhereToo()
    {
        var s = new Subject { Message = TextDefinition.Of("1060847") };

        Assert.True(Check(s, "Message", @"@""1060847"""));
        Assert.False(Check(s, "Message", "1060847"));
    }

    [Fact]
    public void UnresolvableConstantsStillReportAnError()
    {
        Assert.Throws<InvalidOperationException>(
            () => Check(new Subject(), "Kind", "NoSuchTypeAnywhere")
        );
    }
}
