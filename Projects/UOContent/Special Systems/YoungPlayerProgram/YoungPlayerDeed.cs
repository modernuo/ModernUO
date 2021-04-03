using System;
using System.IO;
using System.Text;
using System.Threading;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Accounting;
using Server.Commands;


namespace Server.Special_Systems.YoungPlayerProgram
{
    public class YoungPlayerDeed : Item
    {
        private bool m_TutorialIsActive;
        private bool m_TutorialRowAccepted;
        private bool m_TutorialRowStarted;
        private bool m_TutorialStepDone_1;
        private bool m_TutorialStepDone_2;

        [Constructible]
        public YoungPlayerDeed() : base(0x14EF)
        {
            Name = "A mysterious piece of paper";
            Weight = 1.0;
            LootType = LootType.Blessed;
            Movable = false;

            m_TutorialIsActive = true;
            m_TutorialRowAccepted = false;
            m_TutorialRowStarted = false;
            m_TutorialStepDone_1 = false;
            m_TutorialStepDone_2 = false;

            EventSink.Login += OnLogin;

            
        }

       

        public YoungPlayerDeed(Serial serial) : base(serial)
        {
            
        }

        
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add("Young"); 
        }

        public override bool OnDroppedToWorld(Mobile from, Point3D p)
        {
            return base.OnDroppedToWorld(from, p);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool TutorialStepDone_2 { get => m_TutorialStepDone_2; set => m_TutorialStepDone_2 = value; }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool TutorialStepDone_1 { get => m_TutorialStepDone_1; set => m_TutorialStepDone_1 = value; }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool TutorialRowStarted { get => m_TutorialRowStarted; set => m_TutorialRowStarted = value; }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool TutorialIsActive { get => m_TutorialIsActive; set => m_TutorialIsActive = value; }

        public void ResetYoungPlayerProgramm()
        {
            m_TutorialIsActive = true;
            m_TutorialRowAccepted = false;
            m_TutorialRowStarted = false;
            m_TutorialStepDone_1 = false;
            m_TutorialStepDone_2 = false;
            
        }

        private void OnLogin(Mobile m)
        {
            
            if (m_TutorialIsActive && !m_TutorialRowAccepted)
            {
               StarterGump.DisplayTo(m);
            }

            
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
            writer.Write(Owner);
            writer.Write(1);
            writer.Write(m_TutorialIsActive);
            writer.Write(m_TutorialRowAccepted);
            writer.Write(m_TutorialRowStarted);
            writer.Write(m_TutorialStepDone_1);
            writer.Write(m_TutorialStepDone_2);

        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Owner = reader.ReadEntity<Mobile>();
                        break;
                    }
                case 1:
                    {
                        m_TutorialIsActive = reader.ReadBool();
                        m_TutorialRowAccepted = reader.ReadBool();
                        m_TutorialRowStarted = reader.ReadBool();
                        m_TutorialStepDone_1 = reader.ReadBool();
                        m_TutorialStepDone_2 = reader.ReadBool();
                        break;
                    }
            }

            
        }
    }
}
