using Server.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Zulu
{
    public class ZuluDamageSystem
    {
        private const int CombatCount = 25;

        public ZuluDamageSystem(Serial owner)
        {
            _combatList = new FixedLengthList<ZuluDamageCombat> (CombatCount);
            _owner = owner;
        }
        private Serial _owner;

        private FixedLengthList<ZuluDamageCombat> _combatList;

        public FixedLengthList<ZuluDamageCombat> CombatList
        {
            get
            {
                if (_combatList is null)
                    _combatList = new FixedLengthList<ZuluDamageCombat>(CombatCount);
                return _combatList;
            }
        }

        public void AddDamageLog(ZuluDamageLog damageLog, bool attacker)
        {
            Serial serialFind = attacker ? damageLog.defender.Serial : damageLog.attacker.Serial;

            ZuluDamageCombat combat = CombatList.FirstOrDefault(p => p?.combatent?.Serial == serialFind);

            if (combat == null) {
                combat = new ZuluDamageCombat()
                {
                    combatent = attacker ? damageLog.defender : damageLog.attacker
                };
                CombatList.Add(combat);
            }

            if(attacker)
            {
                combat.totalDamageDealt += damageLog.finalDamage;

                combat.damageDealt[damageLog.Type] += damageLog.finalDamage;

            }
            else
            {
                combat.totalDamageTaken += damageLog.finalDamage;
                combat.totalDamageAbsorbed += damageLog.armorMod;

                combat.damageTaken[damageLog.Type] += damageLog.finalDamage;
            }

            combat.DamageLogList.Add(damageLog);
        }
    }

    public class ZuluDamageCombat
    {
        private const int DamageCount = 150;

        public Mobile combatent { get; set; }

        public double totalDamageTaken { get; set; }

        public double totalDamageDealt { get; set; }
        public double totalDamageAbsorbed { get; set; }

        public Dictionary<ZuluDamageType, double> damageTaken {  get; set; }

        public Dictionary<ZuluDamageType, double> damageDealt { get; set; }

        public Dictionary<ZuluDamageType, double> damageAbsorbed { get; set; }

        private FixedLengthList<ZuluDamageLog> _damageLogList;

        public FixedLengthList<ZuluDamageLog> DamageLogList
        {
            get
            {
                if (_damageLogList is null)
                    _damageLogList = new FixedLengthList<ZuluDamageLog>(DamageCount);
                return _damageLogList;
            }
        }

        public ZuluDamageCombat()
        {
            damageTaken = new Dictionary<ZuluDamageType, double>();
            damageDealt = new Dictionary<ZuluDamageType, double>();
            damageAbsorbed = new Dictionary<ZuluDamageType, double>();

            foreach (var item in Enum.GetValues<ZuluDamageType>())
            {
                damageTaken.Add(item, 0);
                damageDealt.Add(item, 0);
                damageAbsorbed.Add(item, 0);
            }
        }

    }
    public class ZuluDamageLog
    {
        public ZuluDamageType Type { get; set; }
        public ZuluDamageMod Mod { get; set; }
        public Mobile attacker { get; set; }
        public Mobile defender { get; set; }
        public double rawDamage { get; set; }
        public double skillAttackMod { get; set; }
        public double strMod { get; set; }
        public double dexMod { get; set; }
        public double durabilityMod { get; set; }
        public double parryMod { get; set; }
        public bool isParryied { get; set; }
        public bool isHitKill { get; set; }
        public double finalDamage { get; set; }
        public double attacckerClassMod { get; set; }
        public double defenderClassMod { get; set; }
        public double armorMod { get; set; }
        public double parryChance { get; set; }
        public TimeSpan date { get; set; }

        public ZuluDamageLog()
        {
            isHitKill = false;
            isParryied = false;
            rawDamage = 0;
            skillAttackMod = 0;
            strMod = 0;
            dexMod = 0;
            durabilityMod = 0;
            parryMod = 0;
            finalDamage = 0;
            attacckerClassMod = 0;
            defenderClassMod = 0;
            armorMod = 0;
            parryChance = 0;    
        }

    }
}
