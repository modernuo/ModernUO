using Server.Commands;
using Server.Items;
using Xunit;

namespace UOContent.Tests.Commands.Objects;

[Collection("Sequential UOContent Tests")]
public class ObjectIntrospectionOplTests
{
    [Fact]
    public void ExtractOpl_captures_the_runebook_name_cliloc()
    {
        // Runebook.LabelNumber is 1041267 ("runebook") — it appears as an OPL line.
        var opl = ObjectIntrospection.ExtractOpl(typeof(Runebook));
        Assert.Contains(opl, line => line.Cliloc == 1041267);
    }
}
