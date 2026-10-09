using System;
using System.Collections.Generic;
using System.IO;
using Server.Logging;
using Server.Mobiles;

namespace Server.Engines.Avatar;

/// <summary>
/// A summary of one Avatar life, written to its own file when the Avatar dies.
/// </summary>
public class DeathContext
{
    private const string DeathsDirectory = "Saves/Player/AvatarDeaths";

    private static readonly ILogger logger = LogFactory.GetLogger(typeof(DeathContext));

    public DeathContext()
    {
    }

    public DeathContext(IGenericReader reader)
    {
        reader.ReadInt(); // version

        DeathNumber = reader.ReadInt();
        GameTime = reader.ReadTimeSpan();
        LifetimeCreatureKills = reader.ReadInt();
        LifetimeEnemyFactionKills = reader.ReadInt();
        PlayerName = reader.ReadString();
        PointsFarmed = reader.ReadInt();
        RivalBonusPoints = reader.ReadInt();
        RivalFactionName = reader.ReadString();
    }

    public int DeathNumber { get; set; }
    public TimeSpan GameTime { get; set; }
    public int LifetimeCreatureKills { get; set; }
    public int LifetimeEnemyFactionKills { get; set; }
    public string PlayerName { get; set; }
    public int PointsFarmed { get; set; }
    public int RivalBonusPoints { get; set; }
    public string RivalFactionName { get; set; }

    public void Serialize(IGenericWriter writer)
    {
        writer.Write(0); // version

        writer.Write(DeathNumber);
        writer.Write(GameTime);
        writer.Write(LifetimeCreatureKills);
        writer.Write(LifetimeEnemyFactionKills);
        writer.Write(PlayerName);
        writer.Write(PointsFarmed);
        writer.Write(RivalBonusPoints);
        writer.Write(RivalFactionName);
    }

    public static List<DeathContext> Load()
    {
        var records = new List<DeathContext>();
        var directory = PathUtility.GetFullPath(DeathsDirectory, Core.BaseDirectory);

        if (!Directory.Exists(directory))
        {
            return records;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.bin"))
        {
            try
            {
                AdhocPersistence.Deserialize(file, reader => records.Add(new DeathContext(reader)));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading Avatar death record {File}", file);
            }
        }

        return records;
    }

    public static void TrySave(PlayerMobile player, PlayerContext context)
    {
        try
        {
            var deathContext = new DeathContext
            {
                PlayerName = player.Name,
                DeathNumber = context.LifetimeDeaths + 1,
                GameTime = context.GetRunGameTime(player),
                PointsFarmed = context.PointsFarmed,
                LifetimeCreatureKills = context.LifetimeCreatureKills,
                LifetimeEnemyFactionKills = context.LifetimeEnemyFactionKills,
                RivalBonusPoints = context.RivalBonusPoints,
                RivalFactionName = context.RivalFactionName
            };

            var filePath = Path.Combine(DeathsDirectory, $"{player.Serial.Value}_{deathContext.DeathNumber}.bin");
            AdhocPersistence.SerializeAndSnapshot(filePath, deathContext.Serialize, 4096);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error recording Avatar death for {Player}", player);
        }
    }
}
