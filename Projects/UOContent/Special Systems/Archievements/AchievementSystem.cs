using Server;
using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Items;
using Server.Misc;
using Scripts.Systems.Achievements.Gumps;

namespace Scripts.Systems.Achievements
{
    public class AchievementSystem
    {
        public static List<BaseAchievement> Achievements = new List<BaseAchievement>();
        public static List<AchievementCategory> Categories = new List<AchievementCategory>();

        private static Dictionary<Serial, Dictionary<int, AchieveData>> m_featData =
            new Dictionary<Serial, Dictionary<int, AchieveData>>();

        private static Dictionary<Serial, int> m_pointsTotal = new Dictionary<Serial, int>();

        private static int GetPlayerPointsTotal(PlayerMobile m)
        {
            if (!m_pointsTotal.ContainsKey(m.Serial))
                m_pointsTotal.Add(m.Serial, 0);
            return m_pointsTotal[m.Serial];
        }

        private static void AddPoints(PlayerMobile m, int points)
        {
            if (!m_pointsTotal.ContainsKey(m.Serial))
                m_pointsTotal.Add(m.Serial, 0);
            m_pointsTotal[m.Serial] += points;
        }

        public static void Configure()
        {
            GenericPersistence.Register("AchievementSystem", Serialize, Deserialize);
        }

        public static void Serialize(IGenericWriter writer)
        {
            // Do serialization here
            writer.WriteEncodedInt(0); // version

            writer.Write(m_pointsTotal.Count);
            foreach (var kv in m_pointsTotal)
            {
                writer.Write(kv.Key);
                writer.Write(kv.Value);
            }

            writer.Write(m_featData.Count);
            foreach (var kv in m_featData)
            {
                writer.Write(kv.Key);

                writer.Write(kv.Value.Count);

                foreach (var ckv in kv.Value)
                {
                    writer.Write(ckv.Key);
                    ckv.Value.Serialize(writer);
                }
            }
        }


        public static void Deserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();
            switch (version)
            {
                case 0:
                    {
                        int pointCount = reader.ReadInt();
                        for (int i = 0; i < pointCount; ++i)
                        {
                            m_pointsTotal.Add(reader.ReadSerial(), reader.ReadInt());
                        }

                        int featCount = reader.ReadInt();
                        for (int i = 0; i < featCount; ++i)
                        {
                            var id = reader.ReadSerial();
                            int iCount = reader.ReadInt();


                            var dict = new Dictionary<int, AchieveData>();

                            if (iCount > 0)
                            {
                                for (int x = 0; x < iCount; x++)
                                {
                                    dict.Add(reader.ReadInt(), new AchieveData(reader));
                                }
                            }

                            m_featData.Add(id, dict);
                        }

                        System.Console.WriteLine("Loaded Achievements store: " + m_featData.Count);
                        break;
                    }
            }
        }


        public static void Initialize()
        {
            Categories.Add(new AchievementCategory(1, 0, "Exploration"));
            Categories.Add(new AchievementCategory(2, 1, "Towns"));
            Categories.Add(new AchievementCategory(3, 1, "Dungeons"));
            Categories.Add(new AchievementCategory(4, 1, "Points of Interest"));
            Categories.Add(new AchievementCategory(1000, 0, "Crafting"));
            Categories.Add(new AchievementCategory(1001, 1000, "Alch"));
            Categories.Add(new AchievementCategory(1002, 1000, "Smithy"));
            Categories.Add(new AchievementCategory(1003, 1000, "Tink"));
            Categories.Add(new AchievementCategory(2000, 0, "Resource Gathering"));
            Categories.Add(new AchievementCategory(3000, 0, "Hunting"));
            Categories.Add(new AchievementCategory(4000, 0, "Character Development"));
            Categories.Add(new AchievementCategory(5000, 0, "Other"));
            Achievements.Add(
                new DiscoveryAchievement(8888, 1, 0x14EB, false, null, "General expo!", "General expo!", 5, "Green Acres")
            );

            Achievements.Add(
                new DiscoveryAchievement(0, 2, 0x14EB, false, null, "Cove!", "Discover the Cove Township", 5, "Cove")
            );

            Achievements.Add(
                new DiscoveryAchievement(1, 2, 0x14EB, false, null, "Britain!", "Discover the City Britain", 5, "Britain")
            );

            Achievements.Add(
                new DiscoveryAchievement(2, 2, 0x14EB, false, null, "Minoc!", "Discover the Minoc Township", 5, "Minoc")
            );

            Achievements.Add(
                new DiscoveryAchievement(3, 2, 0x14EB, false, null, "Ocllo!", "Discover the Ocllo Township", 5, "Ocllo")
            );

            Achievements.Add(
                new DiscoveryAchievement(4, 2, 0x14EB, false, null, "Trinsic!", "Discover the City of Trinsic", 5, "Trinsic")
            );

            Achievements.Add(
                new DiscoveryAchievement(5, 2, 0x14EB, false, null, "Vesper!", "Discover the City of Vesper", 5, "Vesper")
            );

            Achievements.Add(
                new DiscoveryAchievement(6, 2, 0x14EB, false, null, "Yew!", "Discover the Yew Township", 5, "Yew")
            );

            Achievements.Add(
                new DiscoveryAchievement(7, 2, 0x14EB, false, null, "Wind!", "Discover the City of Wind", 5, "Wind")
            );

            Achievements.Add(
                new DiscoveryAchievement(
                    8,
                    2,
                    0x14EB,
                    false,
                    null,
                    "Serpent's Hold!",
                    "Discover the City of Serpent's Hold",
                    5,
                    "Serpent's Hold"
                )
            );

            Achievements.Add(
                new DiscoveryAchievement(
                    9,
                    2,
                    0x14EB,
                    false,
                    null,
                    "Skara Brae!",
                    "Discover the Island of Skara Brae",
                    5,
                    "Skara Brae"
                )
            );

            Achievements.Add(
                new DiscoveryAchievement(
                    10,
                    2,
                    0x14EB,
                    false,
                    null,
                    "Nujel'm!",
                    "Discover the Island of Nujel'm",
                    5,
                    "Nujel'm"
                )
            );

            Achievements.Add(
                new DiscoveryAchievement(
                    11,
                    2,
                    0x14EB,
                    false,
                    null,
                    "Moonglow!",
                    "Discover the City of Moonglow",
                    5,
                    "Moonglow"
                )
            );

            Achievements.Add(
                new DiscoveryAchievement(
                    12,
                    2,
                    0x14EB,
                    false,
                    null,
                    "Magincia!",
                    "Discover the City of Magincia",
                    5,
                    "Magincia"
                )
            );

            Achievements.Add(
                new DiscoveryAchievement(
                    13,
                    2,
                    0x14EB,
                    false,
                    null,
                    "Buccaneer's Den!",
                    "Discover the Secrets of Buccaneer's Den",
                    5,
                    "Buccaneer's Den"
                )
            );

            Achievements.Add(
                new DiscoveryAchievement(
                    25,
                    3,
                    0x14EB,
                    false,
                    null,
                    "Covetous!",
                    "Discover the dungeon of Covetous",
                    10,
                    "Covetous"
                )
            );

            Achievements.Add(
                new DiscoveryAchievement(
                    26,
                    3,
                    0x14EB,
                    false,
                    null,
                    "Deceit!",
                    "Discover the dungeon of Deceit",
                    10,
                    "Deceit"
                )
            );

            Achievements.Add(
                new DiscoveryAchievement(
                    27,
                    3,
                    0x14EB,
                    false,
                    null,
                    "Despise!",
                    "Discover the dungeon of Despise",
                    10,
                    "Despise"
                )
            );

            Achievements.Add(
                new DiscoveryAchievement(
                    28,
                    3,
                    0x14EB,
                    false,
                    null,
                    "Destard!",
                    "Discover the dungeon of Destard",
                    10,
                    "Destard"
                )
            );

            Achievements.Add(
                new DiscoveryAchievement(
                    29,
                    3,
                    0x14EB,
                    false,
                    null,
                    "Hythloth!",
                    "Discover the dungeon of Hythloth",
                    10,
                    "Hythloth"
                )
            );

            Achievements.Add(
                new DiscoveryAchievement(30, 3, 0x14EB, false, null, "Wrong!", "Discover the dungeon of Wrong", 10, "Wrong")
            );

            Achievements.Add(
                new DiscoveryAchievement(31, 3, 0x14EB, false, null, "Shame!", "Discover the dungeon of Shame", 10, "Shame")
            );

            Achievements.Add(
                new DiscoveryAchievement(
                    100,
                    4,
                    0x14EB,
                    false,
                    null,
                    "Cotton!",
                    "Discover A Cotton Field in Moonglow",
                    5,
                    "A Cotton Field in Moonglow"
                )
            );

            Achievements.Add(
                new DiscoveryAchievement(
                    101,
                    4,
                    0x14EB,
                    false,
                    null,
                    "Carrots!",
                    "Discover A Carrot Field in Skara Brae",
                    5,
                    "A Carrot Field in Skara Brae"
                )
            );

            //these two show examples of adding a reward or multiple rewards
            var achieve = new HarvestAchievement(
                500,
                2000,
                0x0E85,
                false,
                null,
                500,
                "500 Iron Ore",
                "Mine 500 Iron Ore",
                5,
                typeof(IronOre),
                typeof(AncientSmithyHammer)
            );

            Achievements.Add(achieve);
            Achievements.Add(
                new HarvestAchievement(
                    501,
                    2000,
                    0x0E85,
                    false,
                    achieve,
                    5000,
                    "5000 Iron Ore",
                    "Mine 5000 Iron Ore",
                    5,
                    typeof(IronOre),
                    typeof(AncientSmithyHammer),
                    typeof(TinkerTools),
                    typeof(HatOfTheMagi)
                )
            );

            var slay5dog = new HunterAchievement(
                1000,
                3000,
                0x25D1,
                false,
                null,
                5,
                "Dog Slayer",
                "Slay 5 Dogs",
                5,
                typeof(Dog)
            );

            var slay50dragon = new HunterAchievement(
                1001,
                3000,
                0x25D1,
                false,
                null,
                50,
                "Dragon Slayer",
                "Slay 50 Dragon",
                50,
                typeof(Dragon)
            );

            var slay150dragon = new HunterAchievement(
                1002,
                3000,
                0x25D1,
                false,
                slay50dragon,
                150,
                "Master Dragon Slayer",
                "Slay 150 Dragon",
                60,
                typeof(Dragon)
            );

            var slay400dragon = new HunterAchievement(
                1003,
                3000,
                0x25D1,
                false,
                slay150dragon,
                400,
                "Dragon Exterminator",
                "Slay 400 Dragon",
                180,
                typeof(Dragon)
            );

            Achievements.Add(slay5dog);
            Achievements.Add(slay50dragon);
            Achievements.Add(slay150dragon);
            Achievements.Add(slay400dragon);
            CommandSystem.Register("feats", AccessLevel.Player, new CommandEventHandler(OpenGumpCommand));
            
        }

        public static void OpenGump(Mobile from, Mobile target)
        {
            if (from == null || target == null)
                return;
            if (target as PlayerMobile != null)
            {
                var player = target as PlayerMobile;
                if (!m_featData.ContainsKey(player.Serial))
                    m_featData.Add(player.Serial, new Dictionary<int, AchieveData>());
                var achieves = m_featData[player.Serial];
                var total = GetPlayerPointsTotal(player);
                from.SendGump(new AchievementGump(achieves, total));
            }
        }

        [Usage("achievements"), Aliases("ach", "achievement", "achs", "achievements")]
        [Description("Opens the Achievements gump")]
        private static void OpenGumpCommand(CommandEventArgs e)
        {
            OpenGump(e.Mobile, e.Mobile);
        }

        internal static int GetArchievementPoints(PlayerMobile player, BaseAchievement ach)
        {
            var achieves = m_featData[player.Serial];

            if (achieves.ContainsKey(ach.ID))
            {
                return achieves[ach.ID].Progress;

            }

            return 0;
        }

        internal static void SetAchievementStatus(PlayerMobile player, BaseAchievement ach, int progress)
        {
            if (!m_featData.ContainsKey(player.Serial))
                m_featData.Add(player.Serial, new Dictionary<int, AchieveData>());
            var achieves = m_featData[player.Serial];


            if (achieves.ContainsKey(ach.ID))
            {
                if (achieves[ach.ID].Progress >= ach.CompletionTotal)
                    return;
                achieves[ach.ID].Progress += progress;

            }
            else
            {
                achieves.Add(ach.ID, new AchieveData() { Progress = progress });
            }

            if (achieves[ach.ID].Progress >= ach.CompletionTotal)
            {
                player.SendGump(new AchievementObtainedGump(ach));
                achieves[ach.ID].CompletedOn = DateTime.UtcNow;

                AddPoints(player, ach.RewardPoints);

                if (ach.RewardItems != null && ach.RewardItems.Length > 0)
                {
                    try
                    {
                        player.SendAsciiMessage("You have recieved an award for completing this achievment!");
                        var item = (Item)Activator.CreateInstance(ach.RewardItems[0]);
                        if (!WeightOverloading.IsOverloaded(player))
                            player.Backpack.DropItem(item);
                        else
                            player.BankBox.DropItem(item);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
