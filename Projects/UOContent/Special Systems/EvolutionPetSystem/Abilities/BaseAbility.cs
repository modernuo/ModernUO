using Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolutionPetSystem.Abilities
{
    public enum AbilityType
    {
        Passive,
        Active
    }
    public abstract class BaseAbility
    {
        private string m_Name;
        private byte m_Icon;
        private AbilityType m_AbilityType;
        private int m_Stage;
        private int m_XP;

        public byte Icon { get => m_Icon; set => m_Icon = value; }
        public AbilityType AbilityType { get => m_AbilityType; set => m_AbilityType = value; }
        public int Stage { get => m_Stage; set => m_Stage = value; }
        public int XP { get => m_XP; set => m_XP = value; }
        public string Name { get => m_Name; set => m_Name = value; }

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
            writer.Write(m_Name);
            writer.Write(m_Icon);
            writer.Write((int)m_AbilityType);
            writer.Write(m_XP);
            writer.Write(m_Stage);

        }
        public void Deserialize(IGenericReader reader)
        {
            int version = reader.ReadInt();
            m_Name = reader.ReadString();
            m_Icon = reader.ReadByte();
            m_AbilityType = (AbilityType)reader.ReadInt();
            m_XP = reader.ReadInt();
            m_Stage = reader.ReadInt();

            

        }



    }
}
