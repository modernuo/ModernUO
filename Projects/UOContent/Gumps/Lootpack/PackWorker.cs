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

        private static List<packLoader.LootItem> getIncludedItems(List<packLoader.LootItem> packItems)
        {
            var includedItems = new List<packLoader.LootItem>();

            for (int i = 0; i < packItems.Count; i++)
            {
                var pack = packItems[i];
                if (pack.IsDestPack && packLoader.GetPackLootByName(pack.Name)
                    is List<packLoader.LootItem> itemsInPack)
                {
                    includedItems.AddRange(itemsInPack);
                }
            }
            return includedItems;
        }
        private static LootPackEntry[] createPack(List<packLoader.LootItem> packItems)
        {
            var lootPackEntry = new List<LootPackEntry>();
            for (int i = 0; i < packItems.Count; i++)
            {
                var item = packItems[i];
                if (!item.IsDestPack)
                {
                    lootPackEntry.Add(new LootPackEntry(
                        atSpawnTime: item.AtSpawnTime,
                        chance: item.DropChance,
                        quantity: item.Quantity,
                        minIntensity: item.minIntensity,
                        maxIntensity: item.maxIntensity,
                        maxProps: item.maxProps,
                        items: new LootPackItem[]
                        {
                            new(AssemblyHandler.FindTypeByName(item.TypeName), 1)
                        }));
                }
            }

            return lootPackEntry.ToArray();
        }
        public static void TryToCreatePack(string Name, out LootPack _pack, out packLoader.InitStats _stat)
        {
            _stat = null;
            _pack = null;
            if (packLoader.GetPackByName(Name) is packLoader.Pack fPack)
            {
                try
                {
                    //copy list
                    var Pack = fPack.LootItems?.ToList();

                    if (Pack != null)
                    {
                        //include items from another packs
                        Pack.AddRange(getIncludedItems(Pack));


                        //create a loot pack
                        var Items = createPack(Pack);

                        if (Items?.Length > 0)
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
