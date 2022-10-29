using System;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Network;
using Server.Spells.Ninjitsu;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Bola : Item
{
    [Constructible]
    public Bola(int amount = 1) : base(0x26AC)
    {
        Weight = 4.0;
        Stackable = true;
        Amount = amount;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1040019); // The bola must be in your pack to use it.
        }
        else if (!from.CanBeginAction<Bola>())
        {
            from.SendLocalizedMessage(1049624); // You have to wait a few moments before you can use another bola!
        }
        else if (from.Target is BolaTarget)
        {
            from.SendLocalizedMessage(1049631); // This bola is already being used.
        }
        else if (!HasFreeHands(from))
        {
            from.SendLocalizedMessage(1040015); // Your hands must be free to use this
        }
        else if (from.Mounted)
        {
            from.SendLocalizedMessage(1040016); // You cannot use this while riding a mount
        }
        else if (AnimalForm.UnderTransformation(from))
        {
            from.SendLocalizedMessage(1070902); // You can't use this while in an animal form!
        }
        else
        {
            EtherealMount.StopMounting(from);

            from.Target = new BolaTarget(this);
            from.LocalOverheadMessage(MessageType.Emote, 0x3B2, 1049632); // * You begin to swing the bola...*
            // ~1_NAME~ begins to menacingly swing a bola...
            from.NonlocalOverheadMessage(MessageType.Emote, 0x3B2, 1049633, from.Name);
        }
    }

    private static void FinishThrow(Mobile from, Mobile to)
    {
        if (Core.AOS)
        {
            new Bola().MoveToWorld(to.Location, to.Map);
        }

        if (to is ChaosDragoon or ChaosDragoonElite)
        {
            from.SendLocalizedMessage(1042047); // You fail to knock the rider from its mount.
        }

        var mt = to.Mount;
        if (mt != null && !(to is ChaosDragoon or ChaosDragoonElite))
        {
            mt.Rider = null;
        }

        if (to is PlayerMobile mobile)
        {
            if (AnimalForm.UnderTransformation(mobile))
            {
                mobile.SendLocalizedMessage(1114066, from.Name); // ~1_NAME~ knocked you out of animal form!
            }
            else if (mobile.Mounted)
            {
                mobile.SendLocalizedMessage(1040023); // You have been knocked off of your mount!
            }

            mobile.SetMountBlock(BlockMountType.Dazed, TimeSpan.FromSeconds(Core.ML ? 10 : 3), true);
        }

        /* only failsafe, attacker should already be dismounted */
        if (Core.AOS)
        {
            (from as PlayerMobile)?.SetMountBlock(
                BlockMountType.BolaRecovery,
                TimeSpan.FromSeconds(Core.ML ? 10 : 3),
                true
            );
        }

        to.Damage(1);

        Timer.StartTimer(TimeSpan.FromSeconds(2.0), from.EndAction<Bola>);
    }

    private static bool HasFreeHands(Mobile from)
    {
        var one = from.FindItemOnLayer(Layer.OneHanded);
        var two = from.FindItemOnLayer(Layer.TwoHanded);

        if (Core.SE)
        {
            var pack = from.Backpack;

            if (pack != null)
            {
                if (one?.Movable == true)
                {
                    pack.DropItem(one);
                    one = null;
                }

                if (two?.Movable == true)
                {
                    pack.DropItem(two);
                    two = null;
                }
            }
        }
        else if (Core.AOS)
        {
            if (one?.Movable == true)
            {
                from.AddToBackpack(one);
                one = null;
            }

            if (two?.Movable == true)
            {
                from.AddToBackpack(two);
                two = null;
            }
        }

        return one == null && two == null;
    }

    public class BolaTarget : Target
    {
        private readonly Bola m_Bola;

        public BolaTarget(Bola bola) : base(8, false, TargetFlags.Harmful) => m_Bola = bola;

        protected override void OnTarget(Mobile from, object obj)
        {
            if (m_Bola.Deleted)
            {
                return;
            }

            if (obj is not Mobile to)
            {
                from.SendLocalizedMessage(1049629); // You cannot throw a bola at that.
            }
            else if (!m_Bola.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1040019); // The bola must be in your pack to use it.
            }
            else if (!HasFreeHands(from))
            {
                from.SendLocalizedMessage(1040015); // Your hands must be free to use this
            }
            else if (from.Mounted)
            {
                from.SendLocalizedMessage(1040016); // You cannot use this while riding a mount
            }
            else if (AnimalForm.UnderTransformation(from))
            {
                from.SendLocalizedMessage(1070902); // You can't use this while in an animal form!
            }
            else if (!to.Mounted && !AnimalForm.UnderTransformation(to))
            {
                from.SendLocalizedMessage(1049628); // You have no reason to throw a bola at that.
            }
            else if (!from.CanBeHarmful(to))
            {
            }
            else if (from.BeginAction<Bola>())
            {
                EtherealMount.StopMounting(from);

                from.DoHarmful(to);

                m_Bola.Consume();

                from.Direction = from.GetDirectionTo(to);
                from.Animate(11, 5, 1, true, false, 0);
                from.MovingEffect(to, 0x26AC, 10, 0, false, false);

                Timer.StartTimer(TimeSpan.FromSeconds(0.5), () => FinishThrow(from, to));
            }
            else
            {
                // You have to wait a few moments before you can use another bola!
                from.SendLocalizedMessage(1049624);
            }
        }
    }
}
