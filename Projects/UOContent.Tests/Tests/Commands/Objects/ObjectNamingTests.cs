using System;
using Server.Commands;
using Xunit;

namespace UOContent.Tests.Commands.Objects;

public class ObjectNamingTests
{
    [Theory]
    [InlineData("Items.Skill Items.Magical", "items.skill-items.magical")]
    [InlineData("Items.Weapons.Swords", "items.weapons.swords")]
    [InlineData("Mobiles.Uncategorized", "mobiles.uncategorized")]
    public void ChunkKey_lowercases_and_replaces_spaces(string category, string expected)
    {
        Assert.Equal(expected, ObjectNaming.ChunkKey(category));
    }

    [Theory]
    [InlineData(typeof(int), "int")]
    [InlineData(typeof(bool), "bool")]
    [InlineData(typeof(string), "string")]
    [InlineData(typeof(double), "double")]
    [InlineData(typeof(int?), "int?")]
    [InlineData(typeof(Server.Items.WeaponQuality), "WeaponQuality")]
    public void FriendlyTypeName_maps_primitives_and_keeps_enum_names(Type t, string expected)
    {
        Assert.Equal(expected, ObjectNaming.FriendlyTypeName(t));
    }
}
