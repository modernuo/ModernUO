using System;
using System.IO;
using Server;
using Server.Engines.Avatar;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class AvatarSystemTests
{
    private static PlayerMobile CreatePlayer()
    {
        var m = new PlayerMobile(World.NewMobile);
        m.DefaultMobileInit();
        m.MoveToWorld(new Point3D(1500, 1500, 0), Map.Felucca);
        return m;
    }

    private static void Cleanup(PlayerMobile player)
    {
        AvatarEngine.OnPlayerDeleted(player);
        player.Delete();
    }

    [Fact]
    public void PlayerContextRoundTripsThroughSerialization()
    {
        var player = CreatePlayer();
        var context = AvatarEngine.GetOrCreateContext(player);

        context.PointsSaved = 12345;
        context.StatCapLevel = 7;
        context.RivalSlayerName = SlayerName.Fey;
        context.LifetimeGameTime = TimeSpan.FromHours(3);
        context.Skills[SkillName.Magery] = 850;
        context.SetDraftModeEnabled(player, true);
        context.AddDraftedSkill(SkillName.Swords);
        context.AddDraftBannedSkill(SkillName.Begging);

        var writer = new BufferWriter(true);
        context.Serialize(writer);
        var buffer = writer.Buffer.AsSpan(0, (int)writer.Position).ToArray();
        writer.Close();

        var copy = new PlayerContext(player);
        copy.Deserialize(new BufferReader(buffer));

        Assert.Equal(12345, copy.PointsSaved);
        Assert.Equal(7, copy.StatCapLevel);
        Assert.Equal(SlayerName.Fey, copy.RivalSlayerName);
        Assert.Equal(TimeSpan.FromHours(3), copy.LifetimeGameTime);
        Assert.Equal(850, copy.Skills[SkillName.Magery]);
        Assert.True(copy.DraftModeEnabled);
        Assert.True(copy.IsSkillDrafted(SkillName.Swords));
        Assert.False(copy.IsSkillDrafted(SkillName.Magery));
        Assert.Contains(SkillName.Begging, copy.DraftBannedSkills);

        Cleanup(player);
    }

    [Fact]
    public void DraftLevelMathMatchesMemento()
    {
        var player = CreatePlayer();
        var context = AvatarEngine.GetOrCreateContext(player);

        Assert.Equal(1, context.DraftLevel);
        Assert.Equal(Constants.DRAFT_START_PICK_AMOUNT, context.DraftPicksAvailable);

        // Level 1 needs 500, level 2 needs 540, level 3 needs 580
        context.LifetimePointsGained = 500 + 540 + 580;
        Assert.Equal(4, context.DraftLevel);
        Assert.Equal(Constants.DRAFT_START_PICK_AMOUNT + 1, context.DraftPicksAvailable);

        Cleanup(player);
    }

    [Fact]
    public void StrongerCreaturesAreWorthMoreCoins()
    {
        var rat = new Rat();
        var dragon = new Dragon();

        var ratValue = CoinRewardCalculator.GetKillCoinValue(rat);
        var dragonValue = CoinRewardCalculator.GetKillCoinValue(dragon);

        Assert.True(ratValue > 0);
        Assert.True(dragonValue > ratValue);

        rat.Delete();
        dragon.Delete();
    }

    [Fact]
    public void ResetInPlaceKeepsMobileAndSafetyDepositBox()
    {
        var player = CreatePlayer();
        var serial = player.Serial;
        var context = AvatarEngine.GetOrCreateContext(player);
        context.StatCapLevel = 5;

        var box = context.GetOrCreateSafetyDepositBox(player);
        var kept = new Gold(100);
        box.DropItem(kept);

        player.AddItem(new Backpack());
        var lost = new Gold(50);
        player.AddToBackpack(lost);
        player.BankBox.DropItem(new Gold(25));
        player.Skills[SkillName.Swords].Base = 80.0;

        AvatarEngine.ResetInPlace(player);

        Assert.Equal(serial, player.Serial);
        Assert.False(player.Deleted);
        Assert.True(lost.Deleted);
        Assert.False(kept.Deleted);
        Assert.Same(box, player.BankBox.FindItemByType<SafetyDepositBox>());
        Assert.Single(player.BankBox.Items);
        Assert.NotNull(player.Backpack.FindItemByType<AvatarBook>());
        Assert.Equal(0, player.Skills[SkillName.Swords].BaseFixedPoint);
        Assert.Equal(Constants.BASE_STAT_CAP + 5, player.StatCap);
        Assert.Equal(AvatarSanctuary.RespawnLocation, player.Location);

        Cleanup(player);
    }

    [Fact]
    public void UndraftedSkillsCannotGain()
    {
        var player = CreatePlayer();
        var context = AvatarEngine.GetOrCreateContext(player);
        context.SetDraftModeEnabled(player, true);
        context.AddDraftedSkill(SkillName.Swords);

        Assert.True(AvatarEngine.CanGainSkill(player, player.Skills[SkillName.Swords]));
        Assert.False(AvatarEngine.CanGainSkill(player, player.Skills[SkillName.Magery]));

        Cleanup(player);
    }
}
