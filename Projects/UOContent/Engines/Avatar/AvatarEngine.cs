using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Collections;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Avatar;

public class AvatarEngine : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AvatarEngine));

    private static readonly Dictionary<PlayerMobile, PlayerContext> _contexts = [];

    private static AvatarEngine _instance;

    // Set while a template is applied so the skill churn does not feed the archive or count as gains
    public static bool DisableSkillGains { get; set; }

    // Lets PlayerMobile.Resurrect tell the reset's own resurrection apart from a player's
    private static PlayerMobile _resetting;

    public static bool IsEnabled { get; private set; }

    public AvatarEngine() : base("Avatar", 10)
    {
    }

    public static void Configure()
    {
        IsEnabled = ServerConfiguration.GetOrUpdateSetting("avatar.enabled", true);
        _instance = new AvatarEngine();
    }

    public static void InitializePlayer(PlayerMobile player)
    {
        player.InitStats(10, 10, 10);

        if (player.Backpack == null)
        {
            player.AddItem(new Backpack { Movable = false });
        }

        if (player.Backpack.FindItemByType<AvatarBook>() == null)
        {
            player.Backpack.AddItem(new AvatarBook());
        }
    }

    public static void ApplyContext(PlayerMobile player, PlayerContext context)
    {
        if (!context.Active)
        {
            return;
        }

        player.StatCap = Constants.BASE_STAT_CAP + context.StatCapLevel * Constants.STAT_CAP_PER_LEVEL;
        player.Skills.Cap = Constants.SKILL_CAP_BASE + context.SkillCapLevel * Constants.SKILL_CAP_PER_LEVEL * 10;
    }

    public static PlayerContext GetContextOrDefault(Mobile mobile) =>
        mobile is PlayerMobile pm && _contexts.TryGetValue(pm, out var context) ? context : PlayerContext.Default;

    public static PlayerContext GetOrCreateContext(PlayerMobile player)
    {
        if (!_contexts.TryGetValue(player, out var context))
        {
            _contexts[player] = context = new PlayerContext(player);
        }

        return context;
    }

    public static bool IsResetting(PlayerMobile player) => _resetting == player;

    /// <summary>
    /// Starts a new life on the same mobile: wipes possessions, skills and stats, resurrects if needed,
    /// and returns the Avatar to the sanctuary. Houses stay owned because the mobile is unchanged.
    /// </summary>
    public static void ResetInPlace(PlayerMobile player)
    {
        var context = GetContextOrDefault(player);
        if (!context.Active)
        {
            return;
        }

        context.LifetimeGameTime += context.GetRunGameTime(player);
        context.RunStartGameTime = player.GameTime;

        if (!player.Alive)
        {
            _resetting = player;
            try
            {
                player.Resurrect();
            }
            finally
            {
                _resetting = null;
            }
        }

        WipePossessions(player, context);

        for (var i = 0; i < player.Skills.Length; i++)
        {
            var skill = player.Skills[i];
            skill.SetLockNoRelay(SkillLock.Up);
            skill.Base = 0;
        }

        if (!context.UnlockRecordRecipes)
        {
            player.ResetRecipes();
        }

        player.Fame = 0;
        player.Karma = 0;
        player.Hunger = 20;

        context.RewardCache = null;
        context.BoostedTemplateCache = null;
        context.SelectedTemplate = AvatarStarterTemplates.None;
        context.GenerateRivalry();

        AvatarSanctuary.MoveTo(player);
        InitializePlayer(player);
        ApplyContext(player, context);

        if (context.DraftModeEnabled)
        {
            context.SetDraftModeEnabled(player, true);
        }

        player.Hits = player.HitsMax;
        player.Stam = player.StamMax;
        player.Mana = player.ManaMax;
        player.NetState.SendSkillsUpdate(player.Skills);
    }

    private static void WipePossessions(PlayerMobile player, PlayerContext context)
    {
        var box = context.HasSafetyDepositBox ? context.SafetyDepositBox : null;
        var bank = player.BankBox;

        using var toDelete = PooledRefQueue<Item>.Create();

        foreach (var item in player.Items)
        {
            if (item != bank)
            {
                toDelete.Enqueue(item);
            }
        }

        foreach (var item in bank.Items)
        {
            if (item != box)
            {
                toDelete.Enqueue(item);
            }
        }

        while (toDelete.Count > 0)
        {
            toDelete.Dequeue().Delete();
        }

        if (box != null)
        {
            box.Owner = player;
        }

        if (player.AllFollowers?.Count > 0)
        {
            using var followers = PooledRefQueue<Mobile>.Create();
            foreach (var m in player.AllFollowers)
            {
                followers.Enqueue(m);
            }

            while (followers.Count > 0)
            {
                if (followers.Dequeue() is BaseCreature bc && bc.ControlMaster == player)
                {
                    bc.SetControlMaster(null);
                }
            }
        }

        if (player.Stabled?.Count > 0)
        {
            using var stabled = PooledRefQueue<Mobile>.Create();
            foreach (var m in player.Stabled)
            {
                stabled.Enqueue(m);
            }

            while (stabled.Count > 0)
            {
                stabled.Dequeue().Delete();
            }
        }
    }

    public static bool CanGainSkill(Mobile from, Skill skill)
    {
        if (DisableSkillGains)
        {
            return false;
        }

        var context = GetContextOrDefault(from);
        return !context.Active || context.IsSkillDrafted(skill.SkillName);
    }

    public static double GetSkillGainMultiplier(Mobile from)
    {
        var context = GetContextOrDefault(from);
        return context.Active && context.SkillGainRateLevel > 0
            ? 1 + Constants.SKILL_GAIN_RATE_PER_LEVEL * context.SkillGainRateLevel * 0.01
            : 1.0;
    }

    public static void OnSkillGain(Mobile from, Skill skill)
    {
        if (DisableSkillGains || from is not PlayerMobile player)
        {
            return;
        }

        var context = GetContextOrDefault(player);
        if (!context.Active)
        {
            return;
        }

        context.Skills[skill.SkillID] = Math.Max(context.Skills[skill.SkillID], skill.BaseFixedPoint);
    }

    private static int GetBonusCoinsAmount(int value, PlayerContext context) =>
        (int)(value * context.PointGainRateLevel * Constants.POINT_GAIN_RATE_PER_LEVEL * 0.01);

    private static void GrantCoins(PlayerMobile player, int value, PlayerContext context)
    {
        context.PointsFarmed += value;

        player.SendMessage($"You have gained {value} coins.");
    }

    [OnEvent(nameof(CreatureEvents.CreatureDeathEvent))]
    public static void OnCreatureDeath(BaseCreature creature)
    {
        if (!IsEnabled || _contexts.Count == 0)
        {
            return;
        }

        if (creature.AI == AIType.AI_Vendor || creature.NoKillAwards || creature.Summoned || creature.Controlled ||
            creature.IsDeadPet)
        {
            return;
        }

        var rights = BaseCreature.GetLootingRights(creature.DamageEntries, creature.HitsMax);

        var damagerCount = 0;
        for (var i = 0; i < rights.Count; i++)
        {
            if (rights[i].m_HasRight)
            {
                damagerCount++;
            }
        }

        if (damagerCount == 0)
        {
            return;
        }

        for (var i = 0; i < rights.Count; i++)
        {
            var ds = rights[i];
            if (ds.m_HasRight && ds.m_Mobile is PlayerMobile player)
            {
                OnKilledBy(creature, player, damagerCount);
            }
        }
    }

    private static void OnKilledBy(BaseCreature creature, PlayerMobile player, int damagerCount)
    {
        var context = GetContextOrDefault(player);
        if (!context.Active)
        {
            return;
        }

        var value = CoinRewardCalculator.GetKillCoinValue(creature);
        if (value < 1)
        {
            return;
        }

        if (damagerCount > 1)
        {
            value /= 2;
        }

        context.LifetimeCreatureKills += 1;

        // Apply bonus coin multiplier
        value += GetBonusCoinsAmount(value, context);

        // Apply rivalry bonus
        if (context.HasRivalFaction)
        {
            var slayer = SlayerGroup.GetEntryByName(context.RivalSlayerName);
            if (slayer?.Slays(creature) == true)
            {
                context.LifetimeEnemyFactionKills += 1;

                if (context.RivalBonusEnabled)
                {
                    var bonus = (int)(value * Constants.RIVAL_BONUS_PERCENT * 0.01);
                    if (bonus > 0)
                    {
                        context.RivalBonusPoints += bonus;
                        value += bonus;

                        if (context.RivalBonusPoints >= Constants.RIVAL_BONUS_MAX_POINTS)
                        {
                            context.RivalBonusEnabled = false;
                            player.SendMessage(
                                $"You have avenged your family by vanquishing all members of '{context.RivalFactionName}'."
                            );
                        }
                        else
                        {
                            player.SendMessage($"You have eliminated another member of '{context.RivalFactionName}'.");
                        }
                    }
                }
            }
        }

        GrantCoins(player, value, context);
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
    public static void OnPlayerDeath(PlayerMobile player)
    {
        var context = GetContextOrDefault(player);
        if (!context.Active)
        {
            return;
        }

        DeathContext.TrySave(player, context);

        context.LifetimeDeaths += 1;
        context.LifetimePointsGained += context.PointsFarmed;
        context.PointsSaved += context.PointsFarmed;
        context.PointsFarmed = 0;
        context.RivalBonusPoints = 0;
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    public static void OnPlayerDeleted(Mobile m)
    {
        if (m is PlayerMobile pm && _contexts.Remove(pm))
        {
            logger.Information("Removed Avatar context for player {Player}", pm);
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(_contexts.Count);
        foreach (var (player, context) in _contexts)
        {
            writer.Write(player);
            context.Serialize(writer);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            var player = reader.ReadEntity<PlayerMobile>();
            var context = new PlayerContext(player);
            context.Deserialize(reader);

            if (player?.Deleted == false)
            {
                _contexts[player] = context;
            }
        }

        logger.Information("Loaded Avatar data for {Count} characters", _contexts.Count);
    }
}
