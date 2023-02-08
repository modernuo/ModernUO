using System;
using ModernUO.Serialization;
using Server.Factions;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

public enum MoonstoneType
{
    Felucca,
    Trammel
}

[SerializationGenerator(1, false)]
public partial class Moonstone : Item
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private MoonstoneType _type;

    [Constructible]
    public Moonstone(MoonstoneType type) : base(0xF8B)
    {
        Weight = 1.0;
        _type = type;
    }

    public override int LabelNumber => 1041490 + (int)_type;

    public override void OnSingleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            Hue = Utility.RandomBirdHue();
            ProcessDelta();
            from.SendLocalizedMessage(1005398); // The stone's substance shifts as you examine it.
        }

        base.OnSingleClick(from);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else if (from.Mounted)
        {
            from.SendLocalizedMessage(1005399); // You can not bury a stone while you sit on a mount.
        }
        else if (!from.Body.IsHuman)
        {
            from.SendLocalizedMessage(1005400); // You can not bury a stone in this form.
        }
        else if (Sigil.ExistsOn(from))
        {
            from.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
        }
        else if (from.Map == GetTargetMap() || from.Map != Map.Trammel && from.Map != Map.Felucca)
        {
            from.SendLocalizedMessage(1005401); // You cannot bury the stone here.
        }
        else if (from is PlayerMobile mobile && mobile.Young)
        {
            mobile.SendLocalizedMessage(1049543); // You decide against traveling to Felucca while you are still young.
        }
        else if (from.Kills >= 5)
        {
            // The magic of the stone cannot be evoked by someone with blood on their hands.
            from.SendLocalizedMessage(1005402);
        }
        else if (from.Criminal)
        {
            from.SendLocalizedMessage(1005403); // The magic of the stone cannot be evoked by the lawless.
        }
        else if (!Region.Find(from.Location, from.Map).IsDefault ||
                 !Region.Find(from.Location, GetTargetMap()).IsDefault)
        {
            from.SendLocalizedMessage(1005401); // You cannot bury the stone here.
        }
        else if (!GetTargetMap().CanFit(from.Location, 16))
        {
            from.SendLocalizedMessage(1005408); // Something is blocking the facet gate exit.
        }
        else
        {
            Movable = false;
            MoveToWorld(from.Location, from.Map);

            from.Animate(32, 5, 1, true, false, 0);

            new SettleTimer(this, from.Location, from.Map, GetTargetMap(), from).Start();
        }
    }

    public Map GetTargetMap() => _type == MoonstoneType.Felucca ? Map.Felucca : Map.Trammel;

    private void Deserialize(IGenericReader reader, int version)
    {
        _type = (MoonstoneType)reader.ReadInt();
    }

    private class SettleTimer : Timer
    {
        private Mobile _caster;
        private Point3D _location;
        private Map _map;
        private Item _stone;
        private Map _targetMap;
        private int _count;

        public SettleTimer(Item stone, Point3D loc, Map map, Map targetMap, Mobile caster) : base(
            TimeSpan.FromSeconds(2.5),
            TimeSpan.FromSeconds(1.0)
        )
        {
            _stone = stone;
            _location = loc;
            _map = map;
            _targetMap = targetMap;
            _caster = caster;
        }

        protected override void OnTick()
        {
            ++_count;

            if (_count == 1)
            {
                _stone.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1005414); // The stone settles into the ground.
            }
            else if (_count >= 10)
            {
                _stone.Location = new Point3D(_stone.X, _stone.Y, _stone.Z - 1);

                if (_count == 16)
                {
                    if (!Region.Find(_location, _map).IsDefault || !Region.Find(_location, _targetMap).IsDefault)
                    {
                        _stone.Movable = true;
                        _caster.AddToBackpack(_stone);
                        Stop();
                        return;
                    }

                    if (!_targetMap.CanFit(_location, 16))
                    {
                        _stone.Movable = true;
                        _caster.AddToBackpack(_stone);
                        Stop();
                        return;
                    }

                    var hue = _stone.Hue;

                    if (hue == 0)
                    {
                        hue = Utility.RandomBirdHue();
                    }

                    new MoonstoneGate(_location, _targetMap, _map, _caster, hue);
                    new MoonstoneGate(_location, _map, _targetMap, _caster, hue);

                    _stone.Delete();
                    Stop();
                }
            }
        }
    }
}
