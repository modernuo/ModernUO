using System;
using System.IO;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class ExpansionPromptTests
{
    [Fact]
    public void GetExpansion_RejectsOutOfRangeNumber()
    {
        var oldIn = Console.In;
        var oldOut = Console.Out;
        try
        {
            Console.SetIn(new StringReader($"{ExpansionInfo.Table.Length}\n-1\n3\n"));
            Console.SetOut(TextWriter.Null);

            Assert.Equal((Expansion)3, ExpansionConfigurationPrompts.GetExpansion());
        }
        finally
        {
            Console.SetIn(oldIn);
            Console.SetOut(oldOut);
        }
    }
}
