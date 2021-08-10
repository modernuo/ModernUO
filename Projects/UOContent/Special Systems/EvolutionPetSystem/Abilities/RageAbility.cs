using Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolutionPetSystem.Abilities
{
    public class RageAbility : BaseAbility
    {
        private int m_MinXP;
        private int m_MaxXP;
        private BaseEvo m_Evo;
        private bool m_IsRunning;



        public int MinXP { get => m_MinXP; set => m_MinXP = value; }
        public int MaxXP { get => m_MaxXP; set => m_MaxXP = value; }
        public BaseEvo Evo { get => m_Evo; set => m_Evo = value; }
        public bool IsRunning { get => m_IsRunning; set => m_IsRunning = value; }

        public RageAbility(BaseEvo evo)
        {
            Icon = 39852;
            AbilityType = AbilityType.Rage;
            MinXP = 0;
            MaxXP = 3000000;
            Evo = evo;
            Stage = SetStage();

            // Events
            Evo.OnAlterMeleeDamageToEvent += Evo_OnAlterMeleeDamageToEvent;
        }



        public RageAbility(IGenericReader reader) : base(reader)
        {
        }


        public override void Init()
        {

            base.Init();
        }

        public TimeSpan Duration()
        {
            switch (Stage)
            {
                case 0: return TimeSpan.FromSeconds(5); break;
                case 1: return TimeSpan.FromSeconds(7); break;
                case 2: return TimeSpan.FromSeconds(9); break;
                case 3: return TimeSpan.FromSeconds(11); break;
                case 4: return TimeSpan.FromSeconds(13); break;
                case 5: return TimeSpan.FromSeconds(15); break;
                case 6: return TimeSpan.FromSeconds(17); break;
                case 7: return TimeSpan.FromSeconds(19); break;
                case 8: return TimeSpan.FromSeconds(21); break;
                default: return TimeSpan.FromSeconds(5); break;

            }

        }

        public int SetStage()
        {
            switch (XP)
            {
                case int when (XP >= 0 && XP < 375000): return 0; break;
                case int when (XP >= 375000 && XP < 750000): return 1; break;
                case int when (XP >= 750000 && XP < 1125000): return 2; break;
                case int when (XP >= 1125000 && XP < 1500000): return 3; break;
                case int when (XP >= 1500000 && XP < 1875000): return 4; break;
                case int when (XP >= 1875000 && XP < 2250000): return 5; break;
                case int when (XP >= 2250000 && XP < 2625000): return 6; break;
                case int when (XP >= 2625000 && XP < 3000000): return 7; break;
                case int when (XP >= 3000000): return 8; break;
                default: return 0; break;

            }
        }

        public void ProcessAbility()
        {

            var oldDex = Evo.Dex;
            var oldHue = Evo.Hue;
            Evo.SetDex(3000);
            Evo.Hue = 1161;
            Timer.DelayCall(Duration(), () =>
            {
                Evo.SetDex(oldDex);
                Evo.Hue = oldHue;
                IsRunning = false;
            });


        }

        private void Evo_OnAlterMeleeDamageToEvent(Mobile to, int damage)
        {
            if (!IsRunning)
            {
                IsRunning = true;
                ProcessAbility();
                Timer.DelayCall(TimeSpan.FromMinutes(3), () => IsRunning = false);
            }
           
        }


    }


}
