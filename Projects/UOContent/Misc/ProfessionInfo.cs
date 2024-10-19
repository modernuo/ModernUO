using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Server;

public class ProfessionInfo
{
    private static readonly ProfessionInfo[] _professions;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool VerifyProfession(int profIndex) => profIndex > 0 && profIndex < _professions.Length;

    public static bool GetProfession(int profIndex, out ProfessionInfo profession)
    {
        if (!VerifyProfession(profIndex))
        {
            profession = null;
            return false;
        }

        return (profession = _professions[profIndex]) != null;
    }

    private static bool TryGetSkillName(string name, out SkillName skillName)
    {
        if (Enum.TryParse(name, out skillName))
        {
            return true;
        }

        var lowerName = name?.ToLowerInvariant().RemoveOrdinal(" ");

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
        var file = Core.FindDataFile("prof.txt", false);
        if (!File.Exists(file))
        {
            var parent = Path.Combine(Core.BaseDirectory, "Data/Professions");
            file = Path.Combine(ExpansionInfo.GetEraFolder(parent), "prof.txt");
        }

        if (File.Exists(file))
        {
            var maxProf = 0;
            List<ProfessionInfo> profs = [];

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

                var totalStats = 0;
                var skillIndex = 0;
                var totalSkill = 0;

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
                        if (prof.ID > 0 && totalStats >= 80 && totalSkill >= 100)
                        {
                            prof.FixSkills(); // Adjust skills array in case there are fewer skills than the default 4
                            profs.Add(prof);
                        }

                        break;
                    }

                    var cols = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                    var key = cols[0].ToLowerInvariant();
                    var value = cols[1].Trim('"');

                    if (key == "type" && !value.InsensitiveEquals("profession"))
                    {
                        break;
                    }

                    switch (key)
                    {
                        case "truename":
                            {
                                prof.Name = value;
                            }
                            break;
                        case "nameid":
                            {
                                prof.NameID = Utility.ToInt32(value);
                                break;
                            }
                        case "descid":
                            {
                                prof.DescID = Utility.ToInt32(value);
                                break;
                            }
                        case "desc":
                            {
                                prof.ID = Utility.ToInt32(value);
                                if (prof.ID > maxProf)
                                {
                                    maxProf = prof.ID;
                                }
                            }
                            break;
                        case "toplevel":
                            {
                                prof.TopLevel = Utility.ToBoolean(value);
                                break;
                            }
                        case "gump":
                            {
                                prof.GumpID = Utility.ToInt32(value);
                                break;
                            }
                        case "skill":
                            {
                                if (!TryGetSkillName(value, out var skillName))
                                {
                                    break;
                                }

                                var skillValue = byte.Parse(cols[2]);
                                prof.Skills[skillIndex++] = (skillName, skillValue);
                                totalSkill += skillValue;
                            }
                            break;
                        case "stat":
                            {
                                if (!Enum.TryParse(value, out StatType stat))
                                {
                                    break;
                                }

                                var statValue = byte.Parse(cols[2]);
                                prof.Stats[(int)stat >> 1] = statValue;
                                totalStats += statValue;
                            }
                            break;
                    }
                }
            }

            _professions = new ProfessionInfo[maxProf + 1];

            foreach (var p in profs)
            {
                _professions[p.ID] = p;
            }

            profs.Clear();
            profs.TrimExcess();
        }
        else
        {
            _professions = new ProfessionInfo[1];
        }

        _professions[0] = new ProfessionInfo
        {
            Name = "Advanced Skills"
        };
    }

    private (SkillName, byte)[] _skills;

    private ProfessionInfo()
    {
        Name = string.Empty;

        _skills = new (SkillName, byte)[4];
        Stats = new byte[3];
    }

    public int ID { get; private set; }
    public string Name { get; private set; }
    public int NameID { get; private set; }
    public int DescID { get; private set; }
    public bool TopLevel { get; private set; }
    public int GumpID { get; private set; }
    public (SkillName, byte)[] Skills => _skills;
    public byte[] Stats { get; }

    public void FixSkills()
    {
        var index = _skills.Length - 1;
        while (index >= 0)
        {
            var skill = _skills[index];
            if (skill is not (SkillName.Alchemy, 0))
            {
                break;
            }

            index--;
        }

        Array.Resize(ref _skills, index + 1);
    }
}
