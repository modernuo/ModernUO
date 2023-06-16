using System.IO;
using Server.Json;

namespace Server;

public static class SkillsInfo
{
    public static void Configure()
    {
        SkillInfo.Table = JsonConfig.Deserialize<SkillInfo[]>(Path.Combine(Core.BaseDirectory, "Data/skills.json"));

        if (Core.AOS)
        {
            AOS.DisableStatInfluences();
        }
    }
}
