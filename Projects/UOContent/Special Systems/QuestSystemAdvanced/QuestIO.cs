using Server.Json;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Server.QuestSystemAdvanced
{
    public static class QuestIO
    {
        public static List<QuestDefinition> Quests { get; set; } = new List<QuestDefinition>();

        public static void Initialize()
        {
            Quests.Clear();
            //LoadQuests(null);
            //CommandSystem.Register("LoadQuests", AccessLevel.GameMaster, GenerateQuests_OnCommand);
        }

        private static void GenerateQuests_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;
            LoadQuests(from);
        }

        private static void LoadQuests(Mobile from)
        {
            if (Quests.Count > 0)
                Quests.Clear();

            var di = new DirectoryInfo(Core.BaseDirectory);

            var files = di.GetFiles("questsystem.json", SearchOption.AllDirectories);

            if (files.Length == 0)
            {
                if (from != null)
                    from.SendMessage("QuestSystem: No files found.");
                return;

            }

            var options = JsonConfig.GetOptions(new TextDefinitionConverterFactory());

            for (var i = 0; i < files.Length; i++)
            {
                var file = files[i];
                if (from != null)
                    from.SendMessage("Generating quests from: {0}", file.Name);

                NetState.FlushAll();

                try
                {
                    var quests = JsonConfig.Deserialize<List<DynamicJson>>(file.FullName);
                    ParseQuestList(from, quests, options);
                }
                catch (JsonException)
                {
                    if (from != null)
                        from.SendMessage(
                            "QuestSystem: Exception parsing {0}, file may not be in the correct format.",
                            file.FullName
                    );
                }
            }
        }

        private static void ParseQuestList(Mobile from, List<DynamicJson> quests, JsonSerializerOptions options)
        {
            var watch = Stopwatch.StartNew();
            var failures = new List<string>();


            for (int i = 0; i < quests.Count; i++)
            {
                var json = quests[i];
                var type = AssemblyHandler.FindTypeByName(json.Type);

                if (type == null || !typeof(QuestDefinition).IsAssignableFrom(type))
                {
                    var failure = $"QuestSystem: Invalid quest type {json.Type ?? "(-null-)"} ({i})";
                    if (!failures.Contains(failure))
                    {
                        failures.Add(failure);
                        if (from != null)
                            from.SendMessage(failure);
                    }

                    continue;
                }

                json.GetProperty("ID", options, out int id);
                json.GetProperty("Title", options, out string title);
                json.GetProperty("Description", options, out string description);
                json.GetProperty("Rewards", options, out List<RewardDefinition> rewards);
                json.GetProperty("XP", options, out int xp);
                json.GetProperty("IsQuestChainElement", options, out bool isquestchainelement);
                json.GetProperty("ParentQuestID", options, out int parentquestid);
                json.GetProperty("QuestTaskList", options, out List<QuestTaskDefinition> questtasklist);

                var questdefinition = new QuestDefinition()
                {
                    ID = id,
                    Title = title,
                    Description = description,
                    Rewards = rewards,
                    XP = xp,
                    IsQuestChainElement = isquestchainelement,
                    ParentQuestID = parentquestid,
                    QuestTaskList = questtasklist
                };

                Quests.Add(questdefinition);




            }
        }
    }
}
