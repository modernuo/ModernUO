using Server;
using Server.Mobiles;
using Server.SkillHandlers;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class DetectHiddenTests
{
    // All coordinates use Y=500 to stay well within Felucca's 7168x4096 boundary.
    // X values are spread 200 tiles apart to avoid overlap with each other and
    // with the Tracking tests that use coordinates around (1000-4000, 1000-4000).

    /// <summary>
    /// A detector with higher Detect Hidden than the stealther's Hiding should reveal them.
    /// With PredictableRandom both rolls get the same offset, so detection succeeds
    /// when detectSkill >= hiding.
    /// </summary>
    [Fact]
    public void TryDetectStealther_Reveals_WhenDetectSkillExceedsHiding()
    {
        using var rng = new PredictableRandom(10);
        DetectHidden.ClearDebounceCache();
        var map = Map.Felucca;
        var detector = CreatePlayerMobile(map, new Point3D(1000, 500, 0));
        var stealther = CreatePlayerMobile(map, new Point3D(1001, 500, 0));

        try
        {
            detector.Skills.DetectHidden.BaseFixedPoint = 600; // 60.0
            stealther.Skills.Hiding.BaseFixedPoint = 500;      // 50.0
            stealther.Hidden = true;

            var result = DetectHidden.TryDetectStealther(detector, stealther);

            Assert.True(result);
            Assert.False(stealther.Hidden);
        }
        finally
        {
            detector.Delete();
            stealther.Delete();
            DetectHidden.ClearDebounceCache();
        }
    }

    /// <summary>
    /// When detect skill exactly equals hiding, detection succeeds (ss >= ts).
    /// </summary>
    [Fact]
    public void TryDetectStealther_Reveals_WhenSkillsAreEqual()
    {
        using var rng = new PredictableRandom(10);
        DetectHidden.ClearDebounceCache();
        var map = Map.Felucca;
        var detector = CreatePlayerMobile(map, new Point3D(1200, 500, 0));
        var stealther = CreatePlayerMobile(map, new Point3D(1201, 500, 0));

        try
        {
            detector.Skills.DetectHidden.BaseFixedPoint = 500; // 50.0
            stealther.Skills.Hiding.BaseFixedPoint = 500;      // 50.0
            stealther.Hidden = true;

            var result = DetectHidden.TryDetectStealther(detector, stealther);

            Assert.True(result);
            Assert.False(stealther.Hidden);
        }
        finally
        {
            detector.Delete();
            stealther.Delete();
            DetectHidden.ClearDebounceCache();
        }
    }

    /// <summary>
    /// When hiding skill exceeds detect skill, detection fails.
    /// </summary>
    [Fact]
    public void TryDetectStealther_DoesNotReveal_WhenHidingExceedsDetectSkill()
    {
        using var rng = new PredictableRandom(10);
        DetectHidden.ClearDebounceCache();
        var map = Map.Felucca;
        var detector = CreatePlayerMobile(map, new Point3D(1400, 500, 0));
        var stealther = CreatePlayerMobile(map, new Point3D(1401, 500, 0));

        try
        {
            detector.Skills.DetectHidden.BaseFixedPoint = 500; // 50.0
            stealther.Skills.Hiding.BaseFixedPoint = 600;      // 60.0
            stealther.Hidden = true;

            var result = DetectHidden.TryDetectStealther(detector, stealther);

            Assert.False(result);
            Assert.True(stealther.Hidden);
        }
        finally
        {
            detector.Delete();
            stealther.Delete();
            DetectHidden.ClearDebounceCache();
        }
    }

    /// <summary>
    /// A detector with zero Detect Hidden skill should never passively detect anyone.
    /// Uses Elf race since Humans get a 20.0 racial bonus (Jack of All Trades).
    /// </summary>
    [Fact]
    public void TryDetectStealther_DoesNotReveal_WhenDetectorHasNoSkill()
    {
        using var rng = new PredictableRandom(10);
        DetectHidden.ClearDebounceCache();
        var map = Map.Felucca;
        var detector = CreatePlayerMobile(map, new Point3D(1600, 500, 0));
        var stealther = CreatePlayerMobile(map, new Point3D(1601, 500, 0));

        try
        {
            detector.Race = Race.Elf;
            detector.Skills.DetectHidden.BaseFixedPoint = 0; // 0.0
            stealther.Skills.Hiding.BaseFixedPoint = 0;      // 0.0
            stealther.Hidden = true;

            var result = DetectHidden.TryDetectStealther(detector, stealther);

            Assert.False(result);
            Assert.True(stealther.Hidden);
        }
        finally
        {
            detector.Delete();
            stealther.Delete();
            DetectHidden.ClearDebounceCache();
        }
    }

    /// <summary>
    /// TryDetectStealther is a no-op when the target is not actually hidden.
    /// </summary>
    [Fact]
    public void TryDetectStealther_DoesNothing_WhenStealtherIsNotHidden()
    {
        using var rng = new PredictableRandom(10);
        DetectHidden.ClearDebounceCache();
        var map = Map.Felucca;
        var detector = CreatePlayerMobile(map, new Point3D(1800, 500, 0));
        var stealther = CreatePlayerMobile(map, new Point3D(1801, 500, 0));

        try
        {
            detector.Skills.DetectHidden.BaseFixedPoint = 1000;
            stealther.Hidden = false;

            var result = DetectHidden.TryDetectStealther(detector, stealther);

            Assert.False(result);
            Assert.False(stealther.Hidden);
        }
        finally
        {
            detector.Delete();
            stealther.Delete();
            DetectHidden.ClearDebounceCache();
        }
    }

    /// <summary>
    /// Passive detection only works on Felucca. A stealther on Trammel
    /// should never be revealed by passive detection.
    /// </summary>
    [Fact]
    public void TryDetectStealther_DoesNotReveal_WhenNotOnFelucca()
    {
        using var rng = new PredictableRandom(10);
        DetectHidden.ClearDebounceCache();
        var map = Map.Trammel;
        var detector = CreatePlayerMobile(map, new Point3D(2000, 500, 0));
        var stealther = CreatePlayerMobile(map, new Point3D(2001, 500, 0));

        try
        {
            detector.Skills.DetectHidden.BaseFixedPoint = 1000; // 100.0
            stealther.Skills.Hiding.BaseFixedPoint = 0;         // 0.0
            stealther.Hidden = true;

            var result = DetectHidden.TryDetectStealther(detector, stealther);

            Assert.False(result);
            Assert.True(stealther.Hidden);
        }
        finally
        {
            detector.Delete();
            stealther.Delete();
            DetectHidden.ClearDebounceCache();
        }
    }

    private static PlayerMobile CreatePlayerMobile(Map map, Point3D location)
    {
        var mobile = new PlayerMobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.MoveToWorld(location, map);
        return mobile;
    }
}
