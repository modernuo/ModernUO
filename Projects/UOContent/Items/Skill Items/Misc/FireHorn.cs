using System;
using Server.Collections;
using Server.Network;
using Server.Spells;
using Server.Targeting;

namespace Server.Items
{
    public class FireHorn : Item
    {
        [Constructible]
        public FireHorn() : base(0xFC7)
        {
            Hue = 0x466;
            Weight = 1.0;
        }

        public FireHorn(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1060456; // fire horn

        private bool CheckUse(Mobile from)
        {
            if (!IsAccessibleTo(from))
            {
                return false;
            }

            if (from.Map != Map || !from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return false;
            }

            if (!from.CanBeginAction<FireHorn>())
            {
                from.SendLocalizedMessage(1049615); // You must take a moment to catch your breath.
                return false;
            }

            if (from.Backpack?.GetAmount(typeof(SulfurousAsh)) >= (Core.AOS ? 4 : 15))
            {
                return true;
            }

            from.SendLocalizedMessage(1049617); // You do not have enough sulfurous ash.
            return false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (CheckUse(from))
            {
                from.SendLocalizedMessage(1049620); // Select an area to incinerate.
                from.Target = new InternalTarget(this);
            }
        }

        public void Use(Mobile from, Point3D loc)
        {
            if (!CheckUse(from))
            {
                return;
            }

            from.BeginAction<FireHorn>();
            Timer.StartTimer(Core.AOS ? TimeSpan.FromSeconds(6.0) : TimeSpan.FromSeconds(12.0),
                () =>
                {
                    from.EndAction<FireHorn>();
                    from.SendLocalizedMessage(1049621); // You catch your breath.
                }
            );

            var music = from.Skills.Musicianship.Fixed;

            var sucChance = 500 + (music - 775) * 2;
            var dSucChance = sucChance / 1000.0;

            if (!from.CheckSkill(SkillName.Musicianship, dSucChance))
            {
                from.SendLocalizedMessage(1049618); // The horn emits a pathetic squeak.
                from.PlaySound(0x18A);
                return;
            }

            var sulfAsh = Core.AOS ? 4 : 15;
            from.Backpack.ConsumeUpTo(typeof(SulfurousAsh), sulfAsh);

            from.PlaySound(0x15F);
            Effects.SendMovingEffect(
                from,
                loc,
                0x36D4,
                5,
                0,
                false,
                true
            );

            var playerVsPlayer = false;
            var eable = from.Map.GetMobilesInRange(loc, 2);

            using var targets = PooledRefQueue<Mobile>.Create();
            foreach (var m in eable)
            {
                if (from != m && SpellHelper.ValidIndirectTarget(from, m) && from.CanBeHarmful(m, false) &&
                    (!Core.AOS || from.InLOS(m)))
                {
                    targets.Enqueue(m);

                    if (m.Player)
                    {
                        playerVsPlayer = true;
                    }
                }
            }

            eable.Free();

            if (targets.Count > 0)
            {
                var prov = from.Skills.Provocation.Fixed;
                var disc = from.Skills.Discordance.Fixed;
                var peace = from.Skills.Peacemaking.Fixed;

                int minDamage, maxDamage;

                if (Core.AOS)
                {
                    var musicScaled = music + Math.Max(0, music - 900) * 2;
                    var provScaled = prov + Math.Max(0, prov - 900) * 2;
                    var discScaled = disc + Math.Max(0, disc - 900) * 2;
                    var peaceScaled = peace + Math.Max(0, peace - 900) * 2;

                    var weightAvg = (musicScaled + provScaled * 3 + discScaled * 3 + peaceScaled) / 80;

                    int avgDamage;
                    if (playerVsPlayer)
                    {
                        avgDamage = weightAvg / 3;
                    }
                    else
                    {
                        avgDamage = weightAvg / 2;
                    }

                    minDamage = avgDamage * 9 / 10;
                    maxDamage = avgDamage * 10 / 9;
                }
                else
                {
                    var total = prov + disc / 5 + peace / 5;

                    if (playerVsPlayer)
                    {
                        total /= 3;
                    }

                    maxDamage = total * 2 / 30;
                    minDamage = maxDamage * 7 / 10;
                }

                double damage = Utility.RandomMinMax(minDamage, maxDamage);

                if (Core.AOS && targets.Count > 1)
                {
                    damage = damage * 2 / targets.Count;
                }
                else if (!Core.AOS)
                {
                    damage /= targets.Count;
                }

                while (targets.Count > 0)
                {
                    var m = targets.Dequeue();

                    var toDeal = damage;

                    if (!Core.AOS && m.CheckSkill(SkillName.MagicResist, 0.0, 120.0))
                    {
                        toDeal *= 0.5;
                        m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                    }

                    from.DoHarmful(m);
                    SpellHelper.Damage(TimeSpan.Zero, m, from, toDeal, 0, 100, 0, 0, 0);

                    Effects.SendTargetEffect(m, 0x3709, 10, 30);
                }
            }

            var breakChance = Core.AOS ? 0.01 : 0.16;
            if (Utility.RandomDouble() < breakChance)
            {
                from.SendLocalizedMessage(1049619); // The fire horn crumbles in your hands.
                Delete();
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }

        private class InternalTarget : Target
        {
            private readonly FireHorn m_Horn;

            public InternalTarget(FireHorn horn) : base(Core.AOS ? 3 : 2, true, TargetFlags.Harmful) => m_Horn = horn;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Horn.Deleted)
                {
                    return;
                }

                Point3D loc;
                if (targeted is Item item)
                {
                    loc = item.GetWorldLocation();
                }
                else
                {
                    loc = new Point3D(targeted as IPoint3D);
                }

                m_Horn.Use(from, loc);
            }
        }
    }
}
