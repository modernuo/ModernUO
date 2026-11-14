using System;
using System.Reflection;
using Server;
using Server.Commands;
using Xunit;

namespace UOContent.Tests.Commands;

[Collection("Sequential UOContent Tests")]
public class CommandPropertyGuardTests
{
    private class Inner
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public int Value { get; set; }
    }

    private class Outer
    {
        public int Plain { get; set; }

        [CommandProperty(AccessLevel.Administrator)]
        public int Secret { get; set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int Guarded { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WriteOnly { set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int NoSetter => 1;

        [CommandProperty(AccessLevel.GameMaster, readOnly: true)]
        public int FlaggedReadOnly { get; set; }

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public int Modifiable => 1;

        [CommandProperty(AccessLevel.GameMaster, readOnly: true)]
        public Inner Inner { get; } = new();

        [CommandProperty(AccessLevel.GameMaster)]
        public static int Shared { get; set; }

        [CommandProperty(AccessLevel.Administrator)]
        public Inner Restricted { get; } = new();
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

    private static PropertyInfo Prop(string name) => typeof(Outer).GetProperty(name);

    [Fact]
    public void Check_NoAttribute_NotFound() =>
        Assert.Equal("Property 'Plain' not found.", CommandPropertyGuard.Check(Prop("Plain"), AccessLevel.Owner, PropertyAccess.Read));

    [Fact]
    public void Check_BelowReadLevel_ReportsGet() =>
        Assert.Equal(
            $"You must be at least {Mobile.GetAccessLevelName(AccessLevel.Administrator)} to get the property 'Secret'.",
            CommandPropertyGuard.Check(Prop("Secret"), AccessLevel.GameMaster, PropertyAccess.Read)
        );

    [Fact]
    public void Check_BelowWriteLevel_ReportsSet()
    {
        Assert.Null(CommandPropertyGuard.Check(Prop("Guarded"), AccessLevel.GameMaster, PropertyAccess.Read));
        Assert.Equal(
            $"You must be at least {Mobile.GetAccessLevelName(AccessLevel.Administrator)} to set the property 'Guarded'.",
            CommandPropertyGuard.Check(Prop("Guarded"), AccessLevel.GameMaster, PropertyAccess.Write)
        );
    }

    [Fact]
    public void Check_WriteOnly() =>
        Assert.Equal(
            "Property 'WriteOnly' is write only.",
            CommandPropertyGuard.Check(Prop("WriteOnly"), AccessLevel.Owner, PropertyAccess.Read)
        );

    [Fact]
    public void Check_ReadOnly()
    {
        Assert.Equal(
            "Property 'NoSetter' is read only.",
            CommandPropertyGuard.Check(Prop("NoSetter"), AccessLevel.Owner, PropertyAccess.Write)
        );
        Assert.Equal(
            "Property 'FlaggedReadOnly' is read only.",
            CommandPropertyGuard.Check(Prop("FlaggedReadOnly"), AccessLevel.Owner, PropertyAccess.Write)
        );
    }

    [Fact]
    public void Check_CanModifyWithoutSetter_AllowsWrite() =>
        Assert.Null(CommandPropertyGuard.Check(Prop("Modifiable"), AccessLevel.GameMaster, PropertyAccess.Write));

    [Fact]
    public void Chain_IntermediateNeedsReadOnly()
    {
        var chain = CommandPropertyGuard.ResolveChain(
            typeof(Outer), "Inner.Value", AccessLevel.GameMaster, PropertyAccess.Write, true, out _
        );

        Assert.NotNull(chain);
        Assert.Equal(2, chain.Length);
    }

    [Fact]
    public void Chain_IntermediateBelowReadLevel_Rejected()
    {
        var chain = CommandPropertyGuard.ResolveChain(
            typeof(Outer), "Restricted.Value", AccessLevel.GameMaster, PropertyAccess.Write, true, out var reason
        );

        Assert.Null(chain);
        Assert.Equal(
            $"You must be at least {Mobile.GetAccessLevelName(AccessLevel.Administrator)} to get the property 'Restricted'.",
            reason
        );
    }

    [Fact]
    public void Chain_FinalLinkReadOnly_RejectedForWrite()
    {
        var chain = CommandPropertyGuard.ResolveChain(
            typeof(Outer), "Inner", AccessLevel.Owner, PropertyAccess.Write, true, out var reason
        );

        Assert.Null(chain);
        Assert.Equal("Property 'Inner' is read only.", reason);
    }

    [Fact]
    public void Chain_UnknownLink_NotFound()
    {
        var chain = CommandPropertyGuard.ResolveChain(
            typeof(Outer), "Inner.Nope", AccessLevel.GameMaster, PropertyAccess.Read, true, out var reason
        );

        Assert.Null(chain);
        Assert.Equal("Property 'Nope' not found.", reason);
    }

    [Fact]
    public void HiddenProperty_ResolvesMostDerived()
    {
        var chain = CommandPropertyGuard.ResolveChain(
            typeof(Derived), "Name", AccessLevel.GameMaster, PropertyAccess.Read, false, out _
        );

        Assert.NotNull(chain);
        Assert.Equal(typeof(Derived), chain[0].DeclaringType);
        Assert.Equal(typeof(string), chain[0].PropertyType);
    }

    [Fact]
    public void StaticProperty_OnlyWhenAllowed()
    {
        Assert.NotNull(
            CommandPropertyGuard.ResolveChain(typeof(Outer), "Shared", AccessLevel.GameMaster, PropertyAccess.Read, true, out _)
        );
        Assert.Null(
            CommandPropertyGuard.ResolveChain(typeof(Outer), "Shared", AccessLevel.GameMaster, PropertyAccess.Read, false, out _)
        );
    }

    [Fact]
    public void CaseInsensitiveName()
    {
        var chain = CommandPropertyGuard.ResolveChain(
            typeof(Inner), "value", AccessLevel.GameMaster, PropertyAccess.Read, true, out _
        );

        Assert.Equal("Value", chain[0].Name);
    }

    [Fact]
    public void MobileWrapper_MatchesAccessLevelOverload()
    {
        var mobile = new Mobile { AccessLevel = AccessLevel.GameMaster };

        try
        {
            string[] bindings = ["Inner.Value", "Secret"];

            for (var i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                var a = Properties.GetPropertyInfoChain(mobile, typeof(Outer), binding, PropertyAccess.Read, out var reasonA);
                var b = Properties.GetPropertyInfoChain(
                    mobile.AccessLevel, typeof(Outer), binding, PropertyAccess.Read, out var reasonB
                );

                Assert.Equal(a, b);
                Assert.Equal(reasonA, reasonB);
            }
        }
        finally
        {
            mobile.Delete();
        }
    }
}
