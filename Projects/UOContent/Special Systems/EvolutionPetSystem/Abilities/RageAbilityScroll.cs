using EvolutionPetSystem;
using EvolutionPetSystem.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Special_Systems.EvolutionPetSystem.Abilities
{
    public class RageAbilityScroll : Item
    {
        [Constructible]
        public RageAbilityScroll() : base(8827)
        {
            LootType = LootType.Regular;
            Weight = 1.0;
            Name = "a rage ability scroll";
        }

        public RageAbilityScroll(Serial serial) : base(serial)
        {
        }

        public override bool OnDroppedToMobile(Mobile from, Mobile target)
        {
            if (target is BaseEvo pet)
            {
                
                if (pet.Abilities.Find(x => (x.AbilityType == AbilityType.Rage)) == null)
                {
                    pet.Abilities.Add(new RageAbility(pet));
                    this.Delete();
                    from.SendMessage("You successfully teached your pet.");
                }
                else
                {
                    from.SendMessage("Your pet already learned everything about this.");
                }
                
            }
            return base.OnDroppedToMobile(from, target);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
    
}
