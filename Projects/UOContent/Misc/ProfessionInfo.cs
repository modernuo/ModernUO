using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server
{
    public class ProfessionInfo
    {
        public static ProfessionInfo[] Professions { get; }

        static ProfessionInfo()
        {
            var profs = new List<ProfessionInfo>
            {
                new()
                {
                    ID = 0, // Custom
                    Name = "Advanced",
                    TopLevel = false,
                    GumpID = 5571
                }
            };

            var file = Core.FindDataFile("prof.txt");
            if (!File.Exists(file))
            {
                var parent = Path.Combine(Core.BaseDirectory, "Data/Professions");
                file = Path.Combine(ExpansionInfo.GetEraFolder(parent), "prof.txt");
            }

            var maxProf = 0;

            if (File.Exists(file))
            {
                using var s = File.OpenText(file);
                string[] cols;

                while (!s.EndOfStream)
                {
                    var line = s.ReadLine();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    line = line.Trim();

                    if (!line.InsensitiveStartsWith("Begin"))
                    {
                        continue;
                    }

                    var prof = new ProfessionInfo();

                    int stats;
                    int valid;
                    var skills = stats = valid = 0;

                    while (!s.EndOfStream)
                    {
                        line = s.ReadLine();

                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        line = line.Trim();

                        if (line.InsensitiveStartsWith("End"))
                        {
                            if (valid >= 4)
                            {
                                profs.Add(prof);
                            }

                            break;
                        }

                        cols = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);

                        switch (cols[0].ToLowerInvariant())
                        {
                            case "truename":
                                {
                                    prof.Name = cols[1].Trim('"');

                                    ++valid;
                                }
                                break;
                            case "nameid":
                                prof.NameID = Utility.ToInt32(cols[1]);
                                break;
                            case "descid":
                                prof.DescID = Utility.ToInt32(cols[1]);
                                break;
                            case "desc":
                                {
                                    prof.ID = int.Parse(cols[1]);
                                    if (prof.ID > maxProf)
                                    {
                                        maxProf = prof.ID;
                                    }

                                    ++valid;
                                }
                                break;
                            case "toplevel":
                                prof.TopLevel = Utility.ToBoolean(cols[1]);
                                break;
                            case "gump":
                                prof.GumpID = Utility.ToInt32(cols[1]);
                                break;
                            case "skill":
                                {
                                    if (!Enum.TryParse(cols[1], out SkillName skill))
                                    {
                                        var info = SkillInfo.Table.FirstOrDefault(o => o.Name.InsensitiveContains(cols[1]));

                                        if (info == null)
                                        {
                                            break;
                                        }

                                        skill = (SkillName)info.SkillID;
                                    }

                                    prof.Skills[skills++] = new SkillNameValue(skill, Utility.ToInt32(cols[2]));

                                    if (skills == prof.Skills.Length)
                                    {
                                        ++valid;
                                    }
                                }
                                break;
                            case "stat":
                                {
                                    StatType stat;
                                    if (!Enum.TryParse(cols[1], out stat))
                                    {
                                        break;
                                    }

                                    prof.Stats[stats++] = new StatNameValue(stat, Utility.ToInt32(cols[2]));

                                    if (stats == prof.Stats.Length)
                                    {
                                        ++valid;
                                    }
                                }
                                break;
                        }
                    }
                }
            }

            Professions = new ProfessionInfo[1 + maxProf];

            foreach (var p in profs)
            {
                Professions[p.ID] = p;
            }

            profs.Clear();
            profs.TrimExcess();
        }

        private ProfessionInfo()
        {
            Name = string.Empty;

            Skills = new SkillNameValue[4];
            Stats = new StatNameValue[3];
        }

        public int ID { get; private set; }

        public string Name { get; private set; }

        public int NameID { get; private set; }
        public int DescID { get; private set; }

        public bool TopLevel { get; private set; }

        public int GumpID { get; private set; }

        public SkillNameValue[] Skills { get; }
        public StatNameValue[] Stats { get; }
    }
}
