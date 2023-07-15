using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Maps
{
    public enum MapSelectionFlags
    {
        None = 0,
        Felucca = 1 << 0,
        Trammel = 1 << 1,
        Ilshenar = 1 << 2,
        Malas = 1 << 3,
        Tokuno = 1 << 4,
        TerMur = 1 << 5
    }
}
