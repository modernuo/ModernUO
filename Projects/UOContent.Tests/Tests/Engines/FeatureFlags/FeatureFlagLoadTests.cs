using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Server;
using Server.Systems.FeatureFlags;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class FeatureFlagLoadTests
{
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;

    private const string MageryBlock = """[{"Skill":"Magery","Reason":"admin block","Active":true}]""";

    [Fact]
    public void CorruptFlagsFileKeepsAdminBlocks()
    {
        WithFlagFolder(dir =>
        {
            File.WriteAllText(Path.Combine(dir, "flags.json"), "{ not json");
            File.WriteAllText(Path.Combine(dir, "skill-blocks.json"), MageryBlock);

            FeatureFlagManager.Initialize();

            Assert.True(FeatureFlagManager.IsSkillBlocked(SkillName.Magery, out var reason));
            Assert.Equal("admin block", reason);
            Assert.Contains("admin block", File.ReadAllText(Path.Combine(dir, "skill-blocks.json")));
        });
    }

    [Fact]
    public void CorruptBlockFileIsCopiedAside()
    {
        WithFlagFolder(dir =>
        {
            File.WriteAllText(Path.Combine(dir, "skill-blocks.json"), "{ not json");

            FeatureFlagManager.Initialize();

            Assert.Equal("{ not json", File.ReadAllText(Path.Combine(dir, "skill-blocks.json.corrupt")));
        });
    }

    // Runs the body against an empty feature-flag folder, then restores the folder and the loaded flags and skill blocks.
    private static void WithFlagFolder(Action<string> body)
    {
        var manager = typeof(FeatureFlagManager);
        var dir = FeatureFlagSettings.SavePath;
        var backup = $"{dir}.bak-{Guid.NewGuid():N}";
        var hadDir = Directory.Exists(dir);
        if (hadDir)
        {
            Directory.Move(dir, backup);
        }

        var initialized = manager.GetField("_initialized", Static)!;
        var wasInitialized = initialized.GetValue(null);
        var skillBlocks = (Array)manager.GetField("_skillBlocks", Static)!.GetValue(null)!;
        var saved = (Array)skillBlocks.Clone();
        var flags = (IDictionary)manager.GetField("_flags", Static)!.GetValue(null)!;
        var savedFlags = flags.Keys.Cast<object>().ToDictionary(k => k, k => flags[k]);
        var hasActiveSkillBlocks = manager.GetField("_hasActiveSkillBlocks", Static)!;
        var hadActiveSkillBlocks = hasActiveSkillBlocks.GetValue(null);

        try
        {
            Directory.CreateDirectory(dir);
            Array.Clear(skillBlocks);
            flags.Clear();
            initialized.SetValue(null, false);
            body(dir);
        }
        finally
        {
            Array.Copy(saved, skillBlocks, saved.Length);
            flags.Clear();
            foreach (var (key, value) in savedFlags)
            {
                flags[key] = value;
            }

            hasActiveSkillBlocks.SetValue(null, hadActiveSkillBlocks);
            initialized.SetValue(null, wasInitialized);
            Directory.Delete(dir, true);
            if (hadDir)
            {
                Directory.Move(backup, dir);
            }
        }
    }
}
