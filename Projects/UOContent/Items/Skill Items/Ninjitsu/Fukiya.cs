using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Mobiles;

namespace Server.Items
{
    [Flippable(0x27AA, 0x27F5)]
    public class Fukiya : Item, INinjaWeapon
    {
        private Poison m_Poison;
        private int m_PoisonCharges;

        private int m_UsesRemaining;

        [Constructible]
        public Fukiya() : base(0x27AA)
        {
            Weight = 4.0;
            Layer = Layer.OneHanded;
        }

        public Fukiya(Serial serial) : base(serial)
        {
        }

        public virtual int WrongAmmoMessage => 1063329;    // You can only load fukiya darts
        public virtual int NoFreeHandMessage => 1063327;   // You must have a free hand to use a fukiya.
        public virtual int EmptyWeaponMessage => 1063325;  // You have no fukiya darts!
        public virtual int RecentlyUsedMessage => 1063326; // You are already using that fukiya.
        public virtual int FullWeaponMessage => 1063330;   // You can only load fukiya darts

        public virtual int WeaponMinRange => 0;
        public virtual int WeaponMaxRange => 6;

        public virtual int WeaponDamage => Utility.RandomMinMax(4, 6);

        public Type AmmoType => typeof(FukiyaDarts);

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get => m_UsesRemaining;
            set
            {
                m_UsesRemaining = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Poison Poison
        {
            get => m_Poison;
            set
            {
                m_Poison = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PoisonCharges
        {
            get => m_PoisonCharges;
            set
            {
                m_PoisonCharges = value;
                InvalidateProperties();
            }
        }

        bool IUsesRemaining.ShowUsesRemaining
        {
            get => true;
            set { }
        }

        public void AttackAnimation(Mobile from, Mobile to)
        {
            if (from.Body.IsHuman && !from.Mounted)
            {
                from.Animate(33, 2, 1, true, true, 0);
            }

            from.PlaySound(0x223);
            from.MovingEffect(to, 0x2804, 5, 0, false, false);
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060584, m_UsesRemaining); // uses remaining: ~1_val~

            if (m_Poison != null && m_PoisonCharges > 0)
            {
                list.Add(1062412 + m_Poison.Level, m_PoisonCharges);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            NinjaWeapon.AttemptShoot((PlayerMobile)from, this);
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (IsChildOf(from))
            {
                list.Add(new NinjaWeapon.LoadEntry(this, 6224));
                list.Add(new NinjaWeapon.UnloadEntry(this, 6225));
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);

            writer.Write(m_UsesRemaining);

            writer.Write(m_Poison);
            writer.Write(m_PoisonCharges);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_UsesRemaining = reader.ReadInt();

                        m_Poison = reader.ReadPoison();
                        m_PoisonCharges = reader.ReadInt();

                        break;
                    }
            }
        }
    }
}
