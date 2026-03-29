using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.PlayerMurderSystem;

[SerializationGenerator(2)]
public partial class MurderContext
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeSpan _shortTermElapse;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeSpan _longTermElapse;

    [SerializableProperty(2)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int ShortTermMurders
    {
        get => _shortTermMurders;
        set => _shortTermMurders = Math.Max(value, 0);
    }

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _pingPongs;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _bounty;

    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _lastMurderTime;

    private void MigrateFrom(V0Content content)
    {
        _shortTermElapse = content.ShortTermElapse;
        _longTermElapse = content.LongTermElapse;
        _shortTermMurders = content.ShortTermMurders;
        // Players already at >= 5 kills have crossed the threshold at least once
        _pingPongs = _player.Kills >= 5 ? 1 : 0;
    }

    private void MigrateFrom(V1Content content)
    {
        _shortTermElapse = content.ShortTermElapse;
        _longTermElapse = content.LongTermElapse;
        _shortTermMurders = content.ShortTermMurders;
        _pingPongs = content.PingPongs;
        // _bounty defaults to 0
        // Default to now so the bounty board date display is sensible for migrated contexts
        _lastMurderTime = Core.Now;
    }

    public PlayerMobile _player;

    public PlayerMobile Player => _player;

    // Wall clock time for next short or long term expiration
    internal DateTime _nextElapse;

    public MurderContext(PlayerMobile player) => _player = player;

    public void ResetKillTime()
    {
        var gameTime = _player.GameTime;

        if (ShortTermMurders > 0)
        {
            ShortTermElapse = gameTime + PlayerMurderSystem.ShortTermMurderDuration;
        }

        if (_player.Kills > 0)
        {
            LongTermElapse = gameTime + PlayerMurderSystem.LongTermMurderDuration;
        }
    }

    public void DecayKills()
    {
        var gameTime = _player.GameTime;

        if (ShortTermMurders > 0 && _shortTermElapse < gameTime)
        {
            ShortTermElapse += PlayerMurderSystem.ShortTermMurderDuration;
            --ShortTermMurders;
        }

        if (_player.Kills > 0 && _longTermElapse < gameTime)
        {
            LongTermElapse += PlayerMurderSystem.LongTermMurderDuration;
            --_player.Kills;
        }
    }

    public bool CanRemove() => _pingPongs <= 0 && _shortTermMurders <= 0 && _player.Kills <= 0;

    public bool CheckStart()
    {
        _nextElapse = DateTime.MaxValue;

        var now = Core.Now;
        var gameTime = _player.GameTime;

        if (ShortTermMurders > 0)
        {
            _nextElapse = now + (ShortTermElapse - gameTime);
        }

        if (_player.Kills > 0)
        {
            _nextElapse = Utility.Min(_nextElapse, now + (LongTermElapse - gameTime));
        }

        return _nextElapse != DateTime.MaxValue;
    }

    public class EqualityComparer : IEqualityComparer<MurderContext>
    {
        public static EqualityComparer Default { get; } = new ();

        public bool Equals(MurderContext x, MurderContext y) => x?._player == y?._player;

        public int GetHashCode(MurderContext context) => context._player?.GetHashCode() ?? 0;
    }
}
