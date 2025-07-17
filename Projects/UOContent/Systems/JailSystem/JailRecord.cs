using System;
using ModernUO.Serialization;

namespace Server.Systems.JailSystem;

[SerializationGenerator(0)]
public partial class JailRecord
{
    [SerializableField(0)]
    private int _jailCount;

    [SerializableField(1)]
    private DateTime _lastJailed;

    [SerializableField(2)]
    private DateTime _jailEndTime;

    [SerializableField(3)]
    private string _lastJailReason;

    [SerializableField(4)]
    private Mobile _jailedBy;

    public bool IsCurrentlyJailed => JailEndTime > Core.Now;
}
