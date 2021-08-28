using System;

namespace Server.Items
{
    [Serializable(0)]
    public partial class GateHide : BaseStaffHide
    {
        [SerializableField(1)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private bool _redGate;

        [SerializableField(2)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _gateHue;

        [SerializableField(3)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _gateSound;

        public override string DefaultName => "Gate Hide";

        public override bool CastArea => true;

        public override void HideEffects(Mobile from)
        {
            var worldLocation = new WorldLocation(from.Location, from.Map);

            var redGate = _redGate ? 0x1AE5 : 0x1AF3;
            var gateHue = _gateHue > 0 ? _gateHue - 1 : 0;
            Effects.SendLocationParticles(from, redGate, 8, 26, gateHue, 0, 0, 0);
            Effects.PlaySound(from, _gateSound);
            Timer.StartTimer(TimeSpan.FromSeconds(1.25), () => PlaceGate(from, worldLocation));
        }

        private void PlaceGate(Mobile from, WorldLocation worldLocation)
        {
            var itemId = _redGate ? 0xDDA : 0xF6C;
            var hue = _gateHue > 0 ? _gateHue : 0;

            var moongate = new StaffHideMoongate(itemId, hue, worldLocation.Location, worldLocation.Map);

            Timer.StartTimer(TimeSpan.FromSeconds(1.0) , () => from.Hidden = !from.Hidden);
            Timer.StartTimer(TimeSpan.FromSeconds(3.0), () => KillGate(moongate));
        }

        private void KillGate(Moongate moongate)
        {
            if (moongate is { Deleted: false })
            {
                var effectItem = EffectItem.Create(moongate.Location, moongate.Map, EffectItem.DefaultDuration);
                Effects.SendLocationParticles(effectItem, 0x376A, 9, 20, 5042);
                Effects.PlaySound(moongate.Location, moongate.Map, 0x201);
                moongate.Delete();
            }
        }

        [Constructible]
        public GateHide() : base(1154)
        {
            _gateSound = 496;
            _gateHue = 0;
        }

        [Serializable(0)]
        private partial class StaffHideMoongate : Moongate
        {
            public StaffHideMoongate(int itemId, int hue, Point3D loc, Map map) : base(loc, map, false)
            {
                ItemID = itemId;
                Hue = hue;
                TargetMap = Map.Internal;
            }

            [AfterDeserialization(false)]
            private void AfterDeserialization()
            {
                Delete();
            }
        }
    }
}
