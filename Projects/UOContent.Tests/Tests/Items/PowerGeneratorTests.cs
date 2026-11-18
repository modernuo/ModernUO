using System;
using System.Reflection;
using Server;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class PowerGeneratorTests
{
    [Fact]
    public void NewControlPanel_HasAPathForTheGump()
    {
        var panel = new ControlPanel(4);

        Assert.NotEmpty(panel.Path);
    }

    [Fact]
    public void ControlPanelSavedWithoutAPath_GetsOneOnLoad()
    {
        // A panel saved before its constructor built the path has no path in the save.
        var panel = new ControlPanel(4);
        typeof(ControlPanel).GetField("_path", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(panel, null);

        var writer = new BufferWriter(true);
        panel.Serialize(writer);
        var buffer = writer.Buffer.AsSpan(0, (int)writer.Position).ToArray();

        var copy = new ControlPanel(World.NewItem);
        copy.Deserialize(new BufferReader(buffer));

        Assert.NotEmpty(copy.Path);

        panel.Delete();
        copy.Delete();
    }
}
