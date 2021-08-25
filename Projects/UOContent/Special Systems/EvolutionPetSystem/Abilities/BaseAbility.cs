using Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolutionPetSystem.Abilities
{
    
    public class BaseAbility
    {
        private int m_Slot = 0;
        private int m_Icon = 2262;
        private AbilityType m_AbilityType = AbilityType.None;
        private int m_Stage = 0;
        private int m_XP = 0;
        private int m_MaxXP = 0;

        public delegate void OnInitHandler();
        public event OnInitHandler OnInit;

        public int Icon { get => m_Icon; set => m_Icon = value; }
        public AbilityType AbilityType { get => m_AbilityType; set => m_AbilityType = value; } 
        public int Stage { get => m_Stage; set => m_Stage = value; }
        public int XP { get => m_XP; set => m_XP = value; }
        public int Slot { get => m_Slot; set => m_Slot = value; }
        public int MaxXP { get => m_MaxXP; set => m_MaxXP = value; }



        public BaseAbility()
        {

        }
        public BaseAbility(IGenericReader reader)
        {
            Deserialize(reader);
        }
                
        public void Serialize(IGenericWriter writer)
        {

            writer.Write(0); // version
            writer.Write(m_Slot);
            writer.Write(m_Icon);
            writer.Write((int)m_AbilityType);
            writer.Write(m_XP);
            writer.Write(m_Stage);
            writer.Write(m_MaxXP);

        }
        public void Deserialize(IGenericReader reader)
        {
            int version = reader.ReadInt();
            m_Slot = reader.ReadInt();
            m_Icon = reader.ReadInt();
            m_AbilityType = (AbilityType)reader.ReadInt();
            m_XP = reader.ReadInt();
            m_Stage = reader.ReadInt();
            m_MaxXP = reader.ReadInt();

            OnInit?.Invoke();
            

        }



    }
}
