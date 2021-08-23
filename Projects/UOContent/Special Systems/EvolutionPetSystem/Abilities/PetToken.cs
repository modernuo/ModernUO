using EvolutionPetSystem;
using Server.Gumps;
using Server.Targeting;

namespace Server.Items
{

    public class PetToken : Item
    {
        
        private BaseEvo m_BoundEvoPet;

        [Constructible]
        public PetToken() : base(0x2AAA)
        {
            LootType = LootType.Blessed;
            Weight = 1.0;
        }

        public PetToken(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a pet token";

        [CommandProperty(AccessLevel.Administrator)]
        public BaseEvo BoundEvoPet { get => m_BoundEvoPet; set => m_BoundEvoPet = value; }

        public override void OnDoubleClick(Mobile from) 
        {
            if (!IsChildOf(from.Backpack)) // Make sure its in their pack
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                from.RevealingAction();

                if (m_BoundEvoPet == null)
                {
                    from.SendMessage("Target the pet you want to bind!");
                    from.Target = new PetTokenTarget(this); 
                                        
                    
                }
                else
                {
                    from.SendGump(new PetTokenGump(from, m_BoundEvoPet));
                }

            }
        }


        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            writer.Write(m_BoundEvoPet);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            m_BoundEvoPet = reader.ReadEntity<BaseEvo>();
        }


    }

    public class PetTokenTarget : Target // Create our targeting class (which we derive from the base target class)
    {
        private PetToken m_PetToken;

        public PetTokenTarget(PetToken pettoken) : base(1, false, TargetFlags.None) => m_PetToken = pettoken;

        public BaseEvo TargetEvo { get; set; }

        protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
        {
            if (m_PetToken.Deleted || m_PetToken.RootParent != from)
            {
                return;
            }

            if (target is BaseEvo pet)
            {
                if (pet.ControlMaster == from)
                {
                    m_PetToken.BoundEvoPet = pet;
                    from.SendMessage($"Your pet token was successfully bound to \"{pet.Name}\"");
                }
            }


        }
    }
}
