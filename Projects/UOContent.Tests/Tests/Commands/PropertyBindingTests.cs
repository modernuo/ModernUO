using System;
using Server;
using Server.Commands;
using Server.Gumps;
using Xunit;

namespace UOContent.Tests.Commands;

[Collection("Sequential UOContent Tests")]
public class PropertyBindingTests
{
    private class Inner
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public int Value { get; set; }
    }

    private class Subject
    {
        public int Plain { get; set; }

        [CommandProperty(AccessLevel.Administrator)]
        public int Secret { get; set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int Guarded { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WriteOnlyProp { set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int NoSetter => 1;

        [CommandProperty(AccessLevel.GameMaster, readOnly: true)]
        public Inner Inner { get; } = new();
    }

    private class Base
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public int Name { get; set; }
    }

    private class Derived : Base
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public new string Name { get; set; }
    }

    private static void Bind(string binding, PropertyAccess access, AccessLevel level) =>
        new Property(binding).BindTo(typeof(Subject), access, level);

    [Fact]
    public void BindTo_Unknown_ThrowsUnknownProperty() =>
        Assert.Throws<UnknownPropertyException>(() => Bind("Missing", PropertyAccess.Read, AccessLevel.Owner));

    [Fact]
    public void BindTo_NoAttribute_ThrowsInternalAccess() =>
        Assert.Throws<InternalAccessException>(() => Bind("Plain", PropertyAccess.Read, AccessLevel.Owner));

    [Fact]
    public void BindTo_BelowReadLevel_ThrowsReadAccess()
    {
        var ex = Assert.Throws<ReadAccessException>(() => Bind("Secret", PropertyAccess.Read, AccessLevel.GameMaster));
        Assert.Equal(AccessLevel.Administrator, ex.NeededAccess);
    }

    [Fact]
    public void BindTo_WriteAccess_ReportsWriteLevel()
    {
        var ex = Assert.Throws<WriteAccessException>(() => Bind("Guarded", PropertyAccess.Write, AccessLevel.GameMaster));
        Assert.Equal(AccessLevel.Administrator, ex.NeededAccess);
    }

    [Fact]
    public void BindTo_WriteOnly_Throws() =>
        Assert.Throws<WriteOnlyException>(() => Bind("WriteOnlyProp", PropertyAccess.Read, AccessLevel.Owner));

    [Fact]
    public void BindTo_ReadOnly_Throws() =>
        Assert.Throws<ReadOnlyException>(() => Bind("NoSetter", PropertyAccess.Write, AccessLevel.Owner));

    [Fact]
    public void BindTo_HiddenProperty_DoesNotThrow()
    {
        var prop = new Property("Name");
        prop.BindTo(typeof(Derived), PropertyAccess.Read, AccessLevel.GameMaster);
        Assert.Equal(typeof(string), prop.Type);
    }

    [Fact]
    public void BindTo_IntermediateReadOnly_ReadChainBinds()
    {
        var prop = new Property("Inner.Value");
        prop.BindTo(typeof(Subject), PropertyAccess.Read, AccessLevel.GameMaster);
        Assert.Equal(2, prop.Chain.Length);
    }
}
