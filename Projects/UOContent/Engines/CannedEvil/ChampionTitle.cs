using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.CannedEvil;

[SerializationGenerator(0)]
public partial class ChampionTitle
{
    [DirtyTrackingEntity]
    private PlayerMobile _player;

    public ChampionTitle(ChampionTitleContext context) : this(context?.Player)
    {
    }

    // The generator resolves the deserialization constructor against the owning type, but emits the
    // owner's own dirty-tracking reference (the player) at the call site, so both overloads exist.
    public ChampionTitle(PlayerMobile player) => _player = player;

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
