using System;
using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class RejuvinationAddonComponent : AddonComponent
    {
        public RejuvinationAddonComponent(int itemID) : base(itemID)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.BeginAction<RejuvinationAddonComponent>())
            {
                from.FixedEffect(0x373A, 1, 16);

                var random = Utility.Random(1, 4);

                if (random is 1 or 4)
                {
                    from.Hits = from.HitsMax;
                    SendLocalizedMessageTo(from, 500801); // A sense of warmth fills your body!
                }
                else if (random is 2 or 4)
                {
                    from.Mana = from.ManaMax;
                    SendLocalizedMessageTo(from, 500802); // A feeling of power surges through your veins!
                }
                else if (random is 3 or 4)
                {
                    from.Stam = from.StamMax;
                    SendLocalizedMessageTo(from, 500803); // You feel as though you've slept for days!
                }

                Timer.StartTimer(TimeSpan.FromHours(2.0), () => ReleaseUseLock_Callback(from, random));
            }
        }

        public virtual void ReleaseUseLock_Callback(Mobile from, int random)
        {
            from.EndAction<RejuvinationAddonComponent>();

            if (random == 4)
            {
                from.Hits = from.HitsMax;
                from.Mana = from.ManaMax;
                from.Stam = from.StamMax;
                SendLocalizedMessageTo(from, 500807); // You feel completely rejuvinated!
            }
        }
    }

    [SerializationGenerator(0, false)]
    public abstract partial class BaseRejuvinationAnkh : BaseAddon
    {
        private DateTime m_NextMessage;

        public BaseRejuvinationAnkh()
        {
        }

        public override bool HandlesOnMovement => true;

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);

            if (m.Player && Utility.InRange(Location, m.Location, 3) &&
                !Utility.InRange(Location, oldLocation, 3) && Core.Now >= m_NextMessage)
            {
                if (Components.Count > 0)
                {
                    Components[0].SendLocalizedMessageTo(m, 1010061); // An overwhelming sense of peace fills you.
                }

                m_NextMessage = Core.Now + TimeSpan.FromSeconds(25.0);
            }
        }
    }

    [SerializationGenerator(0, false)]
    public partial class RejuvinationAnkhWest : BaseRejuvinationAnkh
    {
        [Constructible]
        public RejuvinationAnkhWest()
        {
            AddComponent(new RejuvinationAddonComponent(0x3), 0, 0, 0);
            AddComponent(new RejuvinationAddonComponent(0x2), 0, 1, 0);
        }
    }

    [SerializationGenerator(0, false)]
    public partial class RejuvinationAnkhNorth : BaseRejuvinationAnkh
    {
        [Constructible]
        public RejuvinationAnkhNorth()
        {
            AddComponent(new RejuvinationAddonComponent(0x4), 0, 0, 0);
            AddComponent(new RejuvinationAddonComponent(0x5), 1, 0, 0);
        }
    }
}
