using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class BaseAltar : Item
    {
        public enum Stage
        {

            One,
            Two,
            Three,
            Four,
            Five
        }


        private int m_AmountLoaded;
        private int m_MaxAmount;
        private Stage m_CurrentStage;




        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int AmountLoaded { get => m_AmountLoaded; set => m_AmountLoaded = value; }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int MaxAmount { get => m_MaxAmount; set => m_MaxAmount = value; }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual Stage CurrentStage { get => m_CurrentStage; set { m_CurrentStage = value; UpdateStage(); } }

        [Constructible]
        public BaseAltar() : base(13902)
        {

            Movable = false;
            Hue = 0;
            m_AmountLoaded = 0;
            m_MaxAmount = 200;
            Name = "An Altar of Downfall";



        }

        public BaseAltar(Serial serial) : base(serial)
        {
        }



        public override void OnSingleClick(Mobile from)
        {
            if (Deleted || !from.CanSee(this))
            {
                return;
            }
            LabelTo(from, Name);
            LabelTo(from, GetLabelName(), 1161);

        }


        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(this.GetWorldLocation(), 1))
            {

                this.SendLocalizedMessageTo(from, 1010086);
                from.Target = new AltarTarget(this);

            }
        }



        public void UpdateStage()
        {
            Point3D location = Location;
            location.Z += 10;
            Effects.SendLocationEffect(location, Map, 14170, 100, 10, 0, 0);
            Effects.PlaySound(location, Map, 0x32E);

            if (m_AmountLoaded == m_MaxAmount)
            {
                Stage_5();
            }
            else if (m_AmountLoaded > m_MaxAmount / 5 * 4)
            {
                Stage_4();
            }
            else if (m_AmountLoaded > m_MaxAmount / 5 * 3)
            {
                Stage_3();
            }
            else if (m_AmountLoaded > m_MaxAmount / 5 * 2)
            {
                Stage_2();
            }
            else if (m_AmountLoaded > m_MaxAmount / 5)
            {
                Stage_1();
            }


        }


        public void Stage_1()
        {
            m_CurrentStage = Stage.One;
            ItemID = 13902;
            Hue = 0;


        }
        public void Stage_2()
        {
            m_CurrentStage = Stage.Two;
            ItemID = 13903;
            Hue = 0;

        }
        public void Stage_3()
        {
            m_CurrentStage = Stage.Three;
            ItemID = 13904;
            Hue = 0;


        }
        public void Stage_4()
        {
            m_CurrentStage = Stage.Four;
            ItemID = 13904;
            Hue = 0;

        }
        public void Stage_5()
        {
            m_CurrentStage = Stage.Five;
            ItemID = 13904;
            Hue = 1161;

        }



        public virtual string GetLabelName()
        {
            return $"{m_AmountLoaded}/{m_MaxAmount}";
        }


        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
            writer.Write(m_AmountLoaded);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            m_AmountLoaded = reader.ReadInt();
        }
    }
    public class AltarTarget : Target
    {

        private BaseAltar m_BaseAltar;

        public AltarTarget(BaseAltar balt) : base(10, false, TargetFlags.None)
        {
            m_BaseAltar = balt;
        }

        protected override void OnTarget(Mobile from, object target)
        {

            if (target == from)
            {
                from.SendMessage("You cant do that.");
            }
            else if (target is Item)
            {
                Item itm = (Item)target;
                if (m_BaseAltar.AmountLoaded + itm.Amount <= m_BaseAltar.MaxAmount)
                {
                    m_BaseAltar.AmountLoaded += itm.Amount;
                    itm.Delete();
                    m_BaseAltar.UpdateStage();
                }


            }
            else
            {
                from.SendMessage("You cant do that.");
            }
        }
    }
}

