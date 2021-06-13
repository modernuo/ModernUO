using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Lootpack
{
    public static class PackWorker
    {
        public static packLoader.InitStats GetStats(string Name)
        {
            var obj = AssemblyHandler.FindTypeByName(Name);
            if (obj == null) return null;
            var mobile = Activator.CreateInstance(obj) as BaseCreature;
            return new packLoader.InitStats()
            {
                Name = Name,
                maxStr = mobile.m_Maxstr,
                minStr = mobile.m_Minstr,
                maxDex = mobile.m_Maxdex,
                minDex = mobile.m_Mindex,
                maxInt = mobile.m_Maxint,
                minInt = mobile.m_Minint,
                maxHits = mobile.m_Maxhits > 0 ? mobile.m_Maxhits : mobile.Hits,
                minHits = mobile.m_Minhits > 0 ? mobile.m_Minhits : mobile.Hits,
                FightMode = (int)mobile.FightMode,
                ActiveSpeed = mobile.ActiveSpeed,
                PassiveSpeed = mobile.PassiveSpeed,
                AgrRange = mobile.RangePerception,
                atkSkill = mobile.GetAttackValue(),
                maxDmg = mobile.DamageMax,
                minDmg = mobile.DamageMin,
                VirtualArmor = mobile.VirtualArmor,
                HitPoison = mobile.HitPoison!=null? mobile.HitPoison.Level:-1,
                HitPoisonChance = (int)(mobile.HitPoisonChance * 100),
            };
         
        
        }
       
        public static void TryToCreatePack(string Name, out LootPack _pack, out packLoader.InitStats _stat)
        {
            _stat = null;
            _pack = null;
            var fPack = packLoader.GetPackByName(Name);
            if (fPack is not null)
            {
                try
                {
                    //copy list
                    var Pack = fPack.LootItems?.ToList();
                    if (Pack != null)
                    {
                        //include another pack to stack
                        var DestPack = Pack.Where(_ => _.IsDestPack)
                              .Select(_ => packLoader.GetPackLootByName(_.Name)).SelectMany(_ => _).ToList();
                        Pack.AddRange(DestPack);
                        //create a loot pack
                        var Items = Pack.Where(_ => !_.IsDestPack).Select(_ => new LootPackEntry(_.AtSpawnTime,
                       new LootPackItem[] { new(AssemblyHandler.FindTypeByName(_.TypeName), 1) },
                       _.DropChance, _.Quantity
                       , _.maxProps, _.minIntensity,
                       _.maxIntensity)).ToArray();

                        if(Items?.Length>0)
                            _pack = new LootPack(Items);
                    }
                    if (fPack?.Stats != null)
                        _stat = fPack.Stats;
                   
                }
                catch (Exception e)
                {
                    Console.WriteLine("Fail to create a pack " + Name);
                    Console.WriteLine(e.Message);
                }
            }
           
        }
    }
}
