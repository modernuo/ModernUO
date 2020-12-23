using System;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Doom
{
    public class BellOfTheDead : Item
    {
        [Constructible]
        public BellOfTheDead() : base(0x91A)
        {
            Hue = 0x835;
            Movable = false;
        }

        public BellOfTheDead(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1050018; // bell of the dead

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public Chyloth Chyloth { get; set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public SkeletalDragon Dragon { get; set; }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool Summoning { get; set; }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(GetWorldLocation(), 2))
            {
                BeginSummon(from);
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
        }

        public virtual void BeginSummon(Mobile from)
        {
            if (Chyloth?.Deleted == false)
            {
                from.SendLocalizedMessage(
                    1050010
                ); // The ferry man has already been summoned.  There is no need to ring for him again.
            }
            else if (Dragon?.Deleted == false)
            {
                from.SendLocalizedMessage(
                    1050017
                ); // The ferryman has recently been summoned already.  You decide against ringing the bell again so soon.
            }
            else if (!Summoning)
            {
                Summoning = true;

                Effects.PlaySound(GetWorldLocation(), Map, 0x100);

                Timer.DelayCall(TimeSpan.FromSeconds(8.0), EndSummon, from);
            }
        }

        public virtual void EndSummon(Mobile from)
        {
            if (Chyloth?.Deleted == false)
            {
                from.SendLocalizedMessage(
                    1050010
                ); // The ferry man has already been summoned.  There is no need to ring for him again.
            }
            else if (Dragon?.Deleted == false)
            {
                from.SendLocalizedMessage(
                    1050017
                ); // The ferryman has recently been summoned already.  You decide against ringing the bell again so soon.
            }
            else if (Summoning)
            {
                Summoning = false;

                var loc = GetWorldLocation();

                loc.Z -= 16;

                Effects.SendLocationParticles(
                    EffectItem.Create(loc, Map, EffectItem.DefaultDuration),
                    0x3728,
                    10,
                    10,
                    0,
                    0,
                    2023,
                    0
                );
                Effects.PlaySound(loc, Map, 0x1FE);

                Chyloth = new Chyloth { Direction = (Direction)(7 & (4 + (int)from.GetDirectionTo(loc))) };

                Chyloth.MoveToWorld(loc, Map);

                Chyloth.Bell = this;
                Chyloth.AngryAt = from;
                Chyloth.BeginGiveWarning();
                Chyloth.BeginRemove(TimeSpan.FromSeconds(40.0));
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(Chyloth);
            writer.Write(Dragon);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Chyloth = reader.ReadEntity<Chyloth>();
            Dragon = reader.ReadEntity<SkeletalDragon>();

            Chyloth?.Delete();
        }
    }
}
