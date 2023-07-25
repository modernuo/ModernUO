using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Server
{
    public static partial class DataModel
    {
        public class RegionModel
        {
            [JsonIgnore]
            public Region RegistredRegion { get; set; }

            [CommandProperty(AccessLevel.GameMaster)]
            public bool Active { get; set; } = false;

            [CommandProperty(AccessLevel.GameMaster, readOnly: true)]
            public string Name { get; set; }
            [CommandProperty(AccessLevel.GameMaster)]
            public int Priority { get; set; } = 50;

            [CommandProperty(AccessLevel.GameMaster)]
            public bool Guarded { get; set; }

            [CommandProperty(AccessLevel.GameMaster)]
            public bool MountsAllowed { get; set; } = true;

            [CommandProperty(AccessLevel.GameMaster)]
            public bool ResurrectionAllowed { get; set; } = true;

            [CommandProperty(AccessLevel.GameMaster)]
            public bool LogoutAllowed { get; set; } = true;

            [CommandProperty(AccessLevel.GameMaster)]
            public bool Housellowed { get; set; } = true;

            [CommandProperty(AccessLevel.GameMaster)]
            public string EnterMessage { get; set; }

            [CommandProperty(AccessLevel.GameMaster)]
            public string OutMessage { get; set; }

            [CommandProperty(AccessLevel.GameMaster)]
            public Rectangle2D MapBounds { get; set; }

            [CommandProperty(AccessLevel.GameMaster)]
            public Map MapControl { get; set; } = Map.Felucca;
            [CommandProperty(AccessLevel.GameMaster)]
            public bool CanMark { get; set; } = true;

            [CommandProperty(AccessLevel.GameMaster)]
            public bool TravelTo { get; set; } = true;

            [CommandProperty(AccessLevel.GameMaster)]
            public bool TravelFrom { get; set; } = true;

            [CommandProperty(AccessLevel.GameMaster)]
            public bool AttackAllowed { get; set; } = true;
            [CommandProperty(AccessLevel.GameMaster)]
            public bool CastAllowed { get; set; } = true;
            [JsonIgnore]
            private string[] _excludeSpell = new string[0];

            [CommandProperty(AccessLevel.GameMaster)]
            public string ExcludeSpell { get; set; }
           
            public string[] ExcludedSpell()
            {
                if (ExcludeSpell?.Length > 0)
                {
                    var buff = ExcludeSpell.Split(',');
                    for (int i = 0; i < buff.Length; i++)
                    {
                        buff[i] = buff[i].TrimStart();
                    }
                    return buff;
                }
                return new string[0];
            }
           
        }
    }
}
