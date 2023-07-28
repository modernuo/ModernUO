using System;
using ModernUO.Serialization;

namespace Server.Engines.CannedEvil;

[SerializationGenerator(0)]
public partial class ChampionTitle
{
    [EncodedInt]
    [SerializableField(0)]
    private int _value;

    // TODO: Change to delta time
    [SerializableField(1)]
    private DateTime _lastDecay;

    public bool Atrophy(int value)
    {
        var before = Value;

        Value -= Math.Min(value, before);

        if (before != Value)
        {
            LastDecay = Core.Now;
        }

        return Value > 0;
    }
}
