using System;
using System.Collections.Generic;
using System.IO;

namespace Server
{
    public class ProfessionInfo
    {
        public static ProfessionInfo[] Professions { get; }

        private static bool TryGetSkillName(string name, out SkillName skillName)
        {
            if (Enum.TryParse(name, out skillName))
            {
                return true;
            }

            var lowerName = name?.ToLowerInvariant().Replace(" ", "");

            if (!string.IsNullOrEmpty(lowerName))
            {
                foreach (var so in SkillInfo.Table)
                {
                    if (lowerName == so.ProfessionSkillName.ToLowerInvariant())
                    {
                        skillName = (SkillName)so.SkillID;
                        return true;
                    }
                }
            }

            return false;
        }

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

            var file = Core.FindDataFile("prof.txt", false);
            if (!File.Exists(file))
            {
                var parent = Path.Combine(Core.BaseDirectory, "Data/Professions");
                file = Path.Combine(ExpansionInfo.GetEraFolder(parent), "prof.txt");
            }

            var maxProf = 0;

            if (File.Exists(file))
            {
                using var s = File.OpenText(file);

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

                        var cols = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                        var key = cols[0].ToLowerInvariant();
                        var value = cols[1].Trim('"');

                        if (key == "type" && value != "profession")
                        {
                            break;
                        }

                        switch (key)
                        {
                            case "truename":
                                {
                                    prof.Name = value;
                                    ++valid;
                                }
                                break;
                            case "nameid":
                                prof.NameID = Utility.ToInt32(value);
                                break;
                            case "descid":
                                prof.DescID = Utility.ToInt32(value);
                                break;
                            case "desc":
                                {
                                    prof.ID = Utility.ToInt32(value);
                                    if (prof.ID > maxProf)
                                    {
                                        maxProf = prof.ID;
                                    }

                                    ++valid;
                                }
                                break;
                            case "toplevel":
                                prof.TopLevel = Utility.ToBoolean(value);
                                break;
                            case "gump":
                                prof.GumpID = Utility.ToInt32(value);
                                break;
                            case "skill":
                                {
                                    if (!TryGetSkillName(value, out var skillName))
                                    {
                                        break;
                                    }

                                    prof.Skills[skills++] = new SkillNameValue(skillName, Utility.ToInt32(cols[2]));

                                    if (skills == prof.Skills.Length)
                                    {
                                        ++valid;
                                    }
                                }
                                break;
                            case "stat":
                                {
                                    if (!Enum.TryParse(value, out StatType stat))
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
