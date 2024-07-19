using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;

namespace Server.Items;

[SerializationGenerator(2, false)]
public partial class HouseTeleporter : Item, ISecurable
{
    [SerializedIgnoreDupe]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Item _target;

    public HouseTeleporter(int itemID, Item target = null) : base(itemID)
    {
        Movable = false;
        _level = SecureLevel.Anyone;
        _target = target;
    }

    public bool CheckAccess(Mobile m)
    {
        var house = BaseHouse.FindHouseAt(this);

        return (house == null || house.Public && !house.IsBanned(m) || house.HasAccess(m)) &&
               house?.HasSecureAccess(m, Level) == true;
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (Target?.Deleted != false)
        {
            return true;
        }

        if (!CheckAccess(m))
        {
            m.SendLocalizedMessage(1061637); // You are not allowed to access this.
            return false;
        }

        if (!m.Hidden || m.AccessLevel == AccessLevel.Player)
        {
            new EffectTimer(Location, Map, 2023, 0x1F0, TimeSpan.FromSeconds(0.4)).Start();
        }

        new DelayTimer(this, m).Start();
        return true;
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);
        SetSecureLevelEntry.AddTo(from, this, ref list);
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        Level = (SecureLevel)reader.ReadInt();
        Target = reader.ReadEntity<Item>();
    }

    private class EffectTimer : Timer
    {
        private readonly int _effectId;
        private readonly Point3D _location;
        private readonly Map _map;
        private readonly int _soundId;

        public EffectTimer(Point3D p, Map map, int effectID, int soundID, TimeSpan delay) : base(delay)
        {
            _location = p;
            _map = map;
            _effectId = effectID;
            _soundId = soundID;
        }

        protected override void OnTick()
        {
            Effects.SendLocationParticles(
                EffectItem.Create(_location, _map, EffectItem.DefaultDuration),
                0x3728,
                10,
                10,
                _effectId,
                0
            );

            if (_soundId != -1)
            {
                Effects.PlaySound(_location, _map, _soundId);
            }
        }
    }

    private class DelayTimer : Timer
    {
        private readonly Mobile _mobile;
        private readonly HouseTeleporter _teleporter;

        public DelayTimer(HouseTeleporter tp, Mobile m) : base(TimeSpan.FromSeconds(1.0))
        {
            _teleporter = tp;
            _mobile = m;
        }

        protected override void OnTick()
        {
            var target = _teleporter.Target;

            if (target?.Deleted != false)
            {
                return;
            }

            if (_mobile.Location != _teleporter.Location || _mobile.Map != _teleporter.Map)
            {
                return;
            }

            var p = target.GetWorldTop();
            var map = target.Map;

            BaseCreature.TeleportPets(_mobile, p, map);

            _mobile.MoveToWorld(p, map);

            if (_mobile.Hidden && _mobile.AccessLevel != AccessLevel.Player)
            {
                return;
            }

            Effects.PlaySound(target.Location, target.Map, 0x1FE);

            Effects.SendLocationParticles(
                EffectItem.Create(_teleporter.Location, _teleporter.Map, EffectItem.DefaultDuration),
                0x3728,
                10,
                10,
                2023,
                0
            );
            Effects.SendLocationParticles(
                EffectItem.Create(target.Location, target.Map, EffectItem.DefaultDuration),
                0x3728,
                10,
                10,
                5023,
                0
            );

            new EffectTimer(target.Location, target.Map, 2023, -1, TimeSpan.FromSeconds(0.4)).Start();
        }
    }
}
