﻿using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MrPlainsCookies : Food
{
    [Constructible]
    public MrPlainsCookies() : base(0x160C)
    {
        FillFactor = 4;
        Hue = 0xF4;
    }

    public override double DefaultWeight => 1.0;

    public override string DefaultName => "Mr Plain's Cookies";
}
