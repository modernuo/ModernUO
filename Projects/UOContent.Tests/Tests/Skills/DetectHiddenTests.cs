using Server;
using Server.Mobiles;
using Server.SkillHandlers;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class DetectHiddenTests
{
    // All coordinates use Y=500 to stay well within Felucca's 7168x4096 boundary.
    // X values are spread 200 tiles apart to avoid overlap with each other and
    // with the Tracking tests that use coordinates around (1000-4000, 1000-4000).

    /// <summary>
    /// A stealther with very low Hiding that steps within 4 tiles of a player
    /// with max Detect Hidden should always be revealed.
    /// Skill bounds make this deterministic:
    ///   ss ∈ [90, 110], ts ∈ [-10, 10]  →  ss >= ts always.
    /// </summary>
    [Fact]
    public void PassiveDetect_RevealsStealther_WhenWithinRangeAndHighSkillDifferential()
    {
        var map = Map.Felucca;
        var detector = CreatePlayerMobile(map, new Point3D(1000, 500, 0));
        var stealther = CreatePlayerMobile(map, new Point3D(1001, 500, 0)); // 1 tile away

        try
        {
            detector.Skills.DetectHidden.BaseFixedPoint = 1000; // 100.0
            stealther.Skills.Hiding.BaseFixedPoint = 0;         // 0.0
            stealther.Hidden = true;

            DetectHidden.PassiveDetect(stealther);

            Assert.False(stealther.Hidden);
        }
        finally
        {
            detector.Delete();
            stealther.Delete();
        }
    }

    /// <summary>
    /// A stealther 5 tiles away is outside the passive detection radius of 4 tiles
    /// and must not be revealed regardless of skill.
    /// </summary>
    [Fact]
    public void PassiveDetect_DoesNotReveal_WhenOutsidePassiveRange()
    {
        var map = Map.Felucca;
        var detector = CreatePlayerMobile(map, new Point3D(1200, 500, 0));
        var stealther = CreatePlayerMobile(map, new Point3D(1205, 500, 0)); // 5 tiles away

        try
        {
            detector.Skills.DetectHidden.BaseFixedPoint = 1000;
            stealther.Skills.Hiding.BaseFixedPoint = 0;
            stealther.Hidden = true;

            DetectHidden.PassiveDetect(stealther);

            Assert.True(stealther.Hidden);
        }
        finally
        {
            detector.Delete();
            stealther.Delete();
        }
    }

    /// <summary>
    /// A player with zero Detect Hidden skill should never passively detect
    /// any hidden mobile.
    /// </summary>
    [Fact]
    public void PassiveDetect_DoesNotReveal_WhenDetectorHasNoSkill()
    {
        var map = Map.Felucca;
        var detector = CreatePlayerMobile(map, new Point3D(1400, 500, 0));
        var stealther = CreatePlayerMobile(map, new Point3D(1401, 500, 0));

        try
        {
            detector.Skills.DetectHidden.BaseFixedPoint = 0;
            stealther.Skills.Hiding.BaseFixedPoint = 0;
            stealther.Hidden = true;

            DetectHidden.PassiveDetect(stealther);

            Assert.True(stealther.Hidden);
        }
        finally
        {
            detector.Delete();
            stealther.Delete();
        }
    }

    /// <summary>
    /// A stealther with very high Hiding skill that faces a detector with zero
    /// Detect Hidden should never be revealed.
    /// Skill bounds make this deterministic:
    ///   ss ∈ [-10, 10], ts ∈ [90, 110]  →  ss >= ts never.
    /// </summary>
    [Fact]
    public void PassiveDetect_DoesNotReveal_WhenStealtherHasMuchHigherHiding()
    {
        var map = Map.Felucca;
        var detector = CreatePlayerMobile(map, new Point3D(1600, 500, 0));
        var stealther = CreatePlayerMobile(map, new Point3D(1601, 500, 0));

        try
        {
            detector.Skills.DetectHidden.BaseFixedPoint = 0;    // 0.0
            stealther.Skills.Hiding.BaseFixedPoint = 1000;      // 100.0
            stealther.Hidden = true;

            DetectHidden.PassiveDetect(stealther);

            Assert.True(stealther.Hidden);
        }
        finally
        {
            detector.Delete();
            stealther.Delete();
        }
    }

    /// <summary>
    /// PassiveDetect is a no-op when the target is not actually hidden.
    /// </summary>
    [Fact]
    public void PassiveDetect_DoesNothing_WhenStealtherIsNotHidden()
    {
        var map = Map.Felucca;
        var detector = CreatePlayerMobile(map, new Point3D(1800, 500, 0));
        var stealther = CreatePlayerMobile(map, new Point3D(1801, 500, 0));

        try
        {
            detector.Skills.DetectHidden.BaseFixedPoint = 1000;
            stealther.Hidden = false;

            DetectHidden.PassiveDetect(stealther);

            Assert.False(stealther.Hidden);
        }
        finally
        {
            detector.Delete();
            stealther.Delete();
        }
    }

    /// <summary>
    /// A stealther exactly 4 tiles away is at the edge of the passive detection
    /// radius and can still be revealed by a skilled detector.
    /// </summary>
    [Fact]
    public void PassiveDetect_RevealsStealther_AtEdgeOfRange()
    {
        var map = Map.Felucca;
        var detector = CreatePlayerMobile(map, new Point3D(2000, 500, 0));
        var stealther = CreatePlayerMobile(map, new Point3D(2004, 500, 0)); // exactly 4 tiles

        try
        {
            detector.Skills.DetectHidden.BaseFixedPoint = 1000;
            stealther.Skills.Hiding.BaseFixedPoint = 0;
            stealther.Hidden = true;

            DetectHidden.PassiveDetect(stealther);

            Assert.False(stealther.Hidden);
        }
        finally
        {
            detector.Delete();
            stealther.Delete();
        }
    }

    /// <summary>
    /// When there is no detector on the map near the stealther, PassiveDetect
    /// should be a no-op and leave the stealther hidden.
    /// </summary>
    [Fact]
    public void PassiveDetect_DoesNotReveal_WhenNoDetectorsNearby()
    {
        var map = Map.Felucca;
        var stealther = CreatePlayerMobile(map, new Point3D(2200, 500, 0));

        try
        {
            stealther.Skills.Hiding.BaseFixedPoint = 0;
            stealther.Hidden = true;

            DetectHidden.PassiveDetect(stealther);

            Assert.True(stealther.Hidden);
        }
        finally
        {
            stealther.Delete();
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
