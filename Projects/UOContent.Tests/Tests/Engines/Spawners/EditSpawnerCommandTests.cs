using Server.Engines.Spawners;
using Xunit;

namespace UOContent.Tests.Engines.Spawners;

public class EditSpawnerCommandTests
{
    [Theory]
    [InlineData("Rabbit set Hits 5", null, "Hits 5", null)]
    [InlineData("Rabbit 5 set Hits 5", "5", "Hits 5", null)]
    [InlineData("Rabbit set Hits 5 where Str 10", null, "Hits 5", "Str 10")]
    [InlineData("Rabbit 5 set Hits 5 where Str 10", "5", "Hits 5", "Str 10")]
    [InlineData("Rabbit where Str 10", null, null, "Str 10")]
    [InlineData("Rabbit 5 where Str 10", "5", null, "Str 10")]
    [InlineData("Rabbit where Name set ", null, null, "Name set")]
    [InlineData("Rabbit 5", "5", null, null)]
    [InlineData("Rabbit", null, null, null)]
    public void ParseArguments_SplitsArgumentsSetAndWhere(
        string argString, string expectedArguments, string expectedProperties, string expectedWhere
    )
    {
        EditSpawnCommand.ParseArguments(argString, "Rabbit", out var arguments, out var properties, out var where);

        Assert.Equal(expectedArguments, arguments);
        Assert.Equal(expectedProperties, properties);
        Assert.Equal(expectedWhere, where);
    }
}
