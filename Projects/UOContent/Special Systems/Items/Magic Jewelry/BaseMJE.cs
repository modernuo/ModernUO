#region copyright
//Copyright (C) 2021  3HMonkey

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//any later version.
//
//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.
//
//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion


using Server.Network;
using Server.Spells;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
    public enum MJEEffect
    {
        NightSight,
        Agility,
        Cunning,
        Strength,
        Bless,
        Teleport,
        Invisibility
    }

    public abstract class BaseMJE : Item
    {
        private MJEEffect m_MJEEffect;
        public int m_Charges;
        public int fakecharges;
        public int itemid;

        public virtual TimeSpan GetUseDelay { get { return TimeSpan.FromSeconds(0.2); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public MJEEffect Effect
        {
            get { return m_MJEEffect; }
            set { m_MJEEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges
        {
            get { return m_Charges; }
            set { m_Charges = value; InvalidateProperties(); }
        }

        public BaseMJE(MJEEffect effect, int minCharges, int maxCharges) : base(Utility.RandomList(0x1087, 0x1F07))
        {
            Weight = 1.0;
            Effect = effect;
            Layer = Layer.Earrings;
            Charges = Utility.RandomMinMax(minCharges, maxCharges);
            Stackable = false;
        }

        public void ConsumeCharge(Mobile from)
        {
            --Charges;
            if (Charges == 0)
            {
                from.SendMessage("After you used up the last charge the magic earrings became highly unstable. They will vanish in some time.");
            }
            ApplyDelayTo(from);
        }

        public BaseMJE(Serial serial)
            : base(serial)
        {
        }

        public virtual void ApplyDelayTo(Mobile from)
        {
            from.BeginAction(typeof(BaseMJE));
            //Timer.DelayCall(GetUseDelay, new TimerStateCallback<Mobile>(ReleaseMJELock_Callback), from);
            Timer.DelayCall(GetUseDelay, () => ReleaseMJELock_Callback(from));
        }

        public virtual void ReleaseMJELock_Callback(object state)
        {
            ((Mobile)state).EndAction(typeof(BaseMJE));
        }

        public override void OnSingleClick(Mobile from)
        {
            ArrayList attrs = new ArrayList();

            if (DisplayLootType)
            {
                if (LootType == LootType.Blessed)
                    attrs.Add(new EquipInfoAttribute(1038021)); // blessed
                else if (LootType == LootType.Cursed)
                    attrs.Add(new EquipInfoAttribute(1049643)); // cursed
            }

            int num = 0;

            switch (m_MJEEffect)
            {
                case MJEEffect.Agility: num = 3002019; break;
                case MJEEffect.Cunning: num = 3002020; break;
                case MJEEffect.Strength: num = 3002026; break;
                case MJEEffect.Bless: num = 3002027; break;
                case MJEEffect.Teleport: num = 3002032; break;
                case MJEEffect.Invisibility: num = 3002054; break;
            }

            if (num > 0)
                attrs.Add(new EquipInfoAttribute(num, m_Charges));

            int number;

            if (Name == null)
            {
                number = 1017095; // magic earrings
            }
            else
            {
                this.LabelTo(from, Name);
                number = 1041000; // no name
            }

            if (attrs.Count == 0 && Name != null)
                return;

            List<EquipInfoAttribute> list = new List<EquipInfoAttribute>(attrs.Count);
            foreach (EquipInfoAttribute instance in attrs)
            {
                list.Add(instance);
            }

            from.NetState?.SendDisplayEquipmentInfo(Serial, number, null, false, list);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.CanBeginAction(typeof(BaseMJE)))
                return;

            if (Parent == from)
            {
                if (Charges > 0)
                    OnJBUse(from);
                else
                    from.SendLocalizedMessage(1019073); // This item is out of charges.
            }
            else
            {
                from.SendLocalizedMessage(502641); // You must equip this item to use it.
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
            writer.Write((int)m_MJEEffect);
            writer.Write((int)m_Charges);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_MJEEffect = (MJEEffect)reader.ReadInt();
            m_Charges = (int)reader.ReadInt();
            if (m_Charges < 1) this.Delete(); //deletes items with zero charges left on server restart
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            switch (m_MJEEffect)
            {
                case MJEEffect.Agility: list.Add(1017331, m_Charges.ToString()); break; // Agility charges: ~1_val~
                case MJEEffect.Cunning: list.Add(1017332, m_Charges.ToString()); break; // Cunning: ~1_val~
                case MJEEffect.Strength: list.Add(1017333, m_Charges.ToString()); break; // Strength charges: ~1_val~
                case MJEEffect.Bless: list.Add(1017336, m_Charges.ToString()); break; // Bless charges: ~1_val~
                case MJEEffect.Teleport: list.Add(1017337, m_Charges.ToString()); break; // Teleport charges: ~1_val~
                case MJEEffect.Invisibility: list.Add(1017347, m_Charges.ToString()); break; // Invisibility charges: ~1_val~
            }
        }

        public void Cast(Spell spell)
        {
            spell.Cast();
        }

        public virtual void OnFinish(Mobile from)
        {
            ConsumeCharge(from);
        }

        public virtual void OnJBUse(Mobile from)
        {
        }
    }
}
