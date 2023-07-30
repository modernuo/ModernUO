using System;

namespace Server.Maps;

[Flags]
public enum MapSelectionFlags
{
    Felucca = 0x00000001,
    Trammel = 0x00000002,
    Ilshenar = 0x00000004,
    Malas = 0x00000008,
    Tokuno = 0x00000010,
    TerMur = 0x00000020
}
