using System.Reflection;
using Server;
using Server.Misc;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class ClientVerificationMessageTests
{
    [Fact]
    public void AllowedClientsMessage_ListsOnlyAllowedTypes()
    {
        var method = typeof(ClientVerification).GetMethod(
            "GetAllowedClientsString",
            BindingFlags.Static | BindingFlags.NonPublic
        )!;

        var message = (string)method.Invoke(null, [ClientType.Classic | ClientType.SA])!;

        Assert.Equal("Please connect with a classic or UO:SA", message);
    }
}
