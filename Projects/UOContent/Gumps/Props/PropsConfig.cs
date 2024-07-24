namespace Server.Gumps;

public static class PropsConfig
{
    public const bool OldStyle = false;

    public const int GumpOffsetX = 30;
    public const int GumpOffsetY = 30;

    public const int TextHue = 0;
    public const int TextOffsetX = 2;

    public const int OffsetGumpID = 0x0A40; // Pure black

    // Light off-white, textured : Dark navy blue, textured
    public const int HeaderGumpID = OldStyle ? 0x0BBC : 0x0E14;

    public const int EntryGumpID = 0x0BBC;                   // Light offwhite, textured
    public const int BackGumpID = 0x13BE;                    // Gray slate/stoney
    public const int SetGumpID = OldStyle ? 0x0000 : 0x0E14; // Empty : Dark navy blue, textured

    public const int SetWidth = 20;
    public const int SetOffsetX = OldStyle ? 4 : 2, SetOffsetY = 2;
    public const int SetButtonID1 = 0x15E1; // Arrow pointing right
    public const int SetButtonID2 = 0x15E5; // " pressed

    public const int PrevWidth = 20;
    public const int PrevOffsetX = 2, PrevOffsetY = 2;
    public const int PrevButtonID1 = 0x15E3; // Arrow pointing left
    public const int PrevButtonID2 = 0x15E7; // " pressed

    public const int NextWidth = 20;
    public const int NextOffsetX = 2, NextOffsetY = 2;
    public const int NextButtonID1 = 0x15E1; // Arrow pointing right
    public const int NextButtonID2 = 0x15E5; // " pressed

    public const int OffsetSize = 1;

    public const int EntryHeight = 20;
    public const int BorderSize = 10;
    public const int ApplySize = 30;

    public const bool PrevLabel = false;
    public const bool NextLabel = false;
}
