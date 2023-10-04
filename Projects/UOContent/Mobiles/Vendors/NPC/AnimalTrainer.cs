using ModernUO.Serialization;
using System.Collections.Generic;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Targeting;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class AnimalTrainer : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new();

        [Constructible]
        public AnimalTrainer() : base("the animal trainer")
        {
            SetSkill(SkillName.AnimalLore, 64.0, 100.0);
            SetSkill(SkillName.AnimalTaming, 90.0, 100.0);
            SetSkill(SkillName.Veterinary, 65.0, 88.0);
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override VendorShoeType ShoeType => Female ? VendorShoeType.ThighBoots : VendorShoeType.Boots;

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBAnimalTrainer());
        }

        public override int GetShoeHue() => 0;

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(Utility.RandomBool() ? new QuarterStaff() : new ShepherdsCrook());
        }

        public override void AddCustomContextEntries(Mobile from, List<ContextMenuEntry> list)
        {
            if (from is PlayerMobile { Alive: true } pm)
            {
                list.Add(new StableEntry(this, from));

                if (pm.Stabled?.Count > 0)
                {
                    list.Add(new ClaimAllEntry(this, from));
                }
            }

            base.AddCustomContextEntries(from, list);
        }

        public static int GetMaxStabled(Mobile from)
        {
            var taming = from.Skills.AnimalTaming.Value;
            var anlore = from.Skills.AnimalLore.Value;
            var vetern = from.Skills.Veterinary.Value;
            var sklsum = taming + anlore + vetern;

            int max;

            if (sklsum >= 240.0)
            {
                max = 5;
            }
            else if (sklsum >= 200.0)
            {
                max = 4;
            }
            else if (sklsum >= 160.0)
            {
                max = 3;
            }
            else
            {
                max = 2;
            }

            if (taming >= 100.0)
            {
                max += (int)((taming - 90.0) / 10);
            }

            if (anlore >= 100.0)
            {
                max += (int)((anlore - 90.0) / 10);
            }

            if (vetern >= 100.0)
            {
                max += (int)((vetern - 90.0) / 10);
            }

            return max;
        }

        private void CloseClaimList(Mobile from)
        {
            from.CloseGump<ClaimListGump>();
        }

        public void BeginClaimList(Mobile from)
        {
            if (Deleted || !from.CheckAlive() || from is not PlayerMobile pm)
            {
                return;
            }

            var list = new List<BaseCreature>();

            if (pm.Stabled?.Count > 0)
            {
                using var queue = PooledRefQueue<Mobile>.Create();

                foreach (var m in pm.Stabled)
                {
                    if (m is BaseCreature pet)
                    {
                        if (!pet.Deleted)
                        {
                            list.Add(pet);
                            continue;
                        }

                        pet.IsStabled = false;
                        pet.StabledBy = null;
                    }

                    queue.Enqueue(m);
                }

                while (queue.Count > 0)
                {
                    pm.RemoveStabled(queue.Dequeue());
                }
            }

            if (list.Count > 0)
            {
                from.SendGump(new ClaimListGump(this, from, list));
            }
            else
            {
                SayTo(from, 502671); // But I have no animals stabled with me at the moment!
            }
        }

        public void EndClaimList(Mobile from, BaseCreature pet)
        {
            if (pet?.Deleted != false || from.Map != Map || from is not PlayerMobile pm || pm.Stabled?.Contains(pet) != true || !from.CheckAlive())
            {
                return;
            }

            if (!from.InRange(this, 14))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            if (CanClaim(from, pet))
            {
                DoClaim(from, pet);

                pm.RemoveStabled(pet);
                pm.AutoStabled?.Remove(pet);
            }
            else
            {
                SayTo(from, 1049612, pet.Name); // ~1_NAME~ remained in the stables because you have too many followers.
            }
        }

        public void BeginStable(Mobile from)
        {
            if (Deleted || !from.CheckAlive())
            {
                return;
            }

            if (!(from.Backpack?.GetAmount(typeof(Gold)) >= 30) &&
                !(Banker.GetBalance(from) >= 30))
            {
                SayTo(from, 1042556); // Thou dost not have enough gold, not even in thy bank account.
            }
            else
            {
                /* I charge 30 gold per pet for a real week's stable time.
                 * I will withdraw it from thy bank account.
                 * Which animal wouldst thou like to stable here?
                 */
                from.SendLocalizedMessage(1042558);

                from.Target = new StableTarget(this);
            }
        }

        public void EndStable(Mobile from, BaseCreature pet)
        {
            if (Deleted || !from.CheckAlive() || from is not PlayerMobile pm)
            {
                return;
            }

            if (pet.Body.IsHuman)
            {
                SayTo(from, 502672); // HA HA HA! Sorry, I am not an inn.
            }
            else if (!pet.Controlled)
            {
                SayTo(from, 1048053); // You can't stable that!
            }
            else if (pet.ControlMaster != from)
            {
                SayTo(from, 1042562); // You do not own that pet!
            }
            else if (pet.IsDeadPet)
            {
                SayTo(from, 1049668); // Living pets only, please.
            }
            else if (pet.Summoned)
            {
                SayTo(from, 502673); // I can not stable summoned creatures.
            }
            /*
                  else if (pet.Allured)
                  {
                    SayTo( from, 1048053 ); // You can't stable that!
                  }
            */
            else if (pet is PackLlama or PackHorse or Beetle && pet.Backpack?.Items.Count > 0)
            {
                SayTo(from, 1042563); // You need to unload your pet.
            }
            else if (pet.Combatant != null && pet.InRange(pet.Combatant, 12) && pet.Map == pet.Combatant.Map)
            {
                SayTo(from, 1042564); // I'm sorry.  Your pet seems to be busy.
            }
            else if (pm.Stabled?.Count >= GetMaxStabled(from))
            {
                SayTo(from, 1042565); // You have too many pets in the stables!
            }
            else
            {
                if (from.Backpack?.ConsumeTotal(typeof(Gold), 30) == true || Banker.Withdraw(from, 30))
                {
                    pet.ControlTarget = null;
                    pet.ControlOrder = OrderType.Stay;
                    pet.Internalize();

                    pet.SetControlMaster(null);
                    pet.SummonMaster = null;

                    pet.IsStabled = true;
                    pet.StabledBy = from;

                    if (Core.SE)
                    {
                        pet.Loyalty = MaxLoyalty; // Wonderfully happy
                    }

                    pm.AddStabled(pet);

                    // [AOS: Your pet has been stabled.] Very well, thy pet is stabled. Thou mayst recover it by saying 'claim' to me. In one real world week, I shall sell it off if it is not claimed!
                    SayTo(from, Core.AOS ? 1049677 : 502679);
                }
                else
                {
                    SayTo(from, 502677); // But thou hast not the funds in thy bank account!
                }
            }
        }

        public void Claim(Mobile from, string petName = null)
        {
            if (Deleted || !from.CheckAlive() || from is not PlayerMobile pm)
            {
                return;
            }

            var claimed = false;
            var stabled = 0;

            var claimByName = petName != null;

            if (pm.Stabled?.Count > 0)
            {
                using var queue = PooledRefQueue<Mobile>.Create();

                foreach (var m in pm.Stabled)
                {
                    var pet = m as BaseCreature;

                    if (pet?.Deleted != false)
                    {
                        if (pet != null)
                        {
                            pet.IsStabled = false;
                            pet.StabledBy = null;
                        }

                        queue.Enqueue(pet);
                        continue;
                    }

                    ++stabled;

                    if (claimByName && !pet.Name.InsensitiveEquals(petName))
                    {
                        continue;
                    }

                    if (CanClaim(from, pet))
                    {
                        DoClaim(from, pet);

                        queue.Enqueue(pet);

                        claimed = true;
                        pm.AutoStabled?.Remove(pet);
                    }
                    else
                    {
                        SayTo(from, 1049612, pet.Name); // ~1_NAME~ remained in the stables because you have too many followers.
                    }
                }

                while (queue.Count > 0)
                {
                    pm.RemoveStabled(queue.Dequeue());
                }
            }

            if (claimed)
            {
                SayTo(from, 1042559); // Here you go... and good day to you!
            }
            else if (stabled == 0)
            {
                SayTo(from, 502671); // But I have no animals stabled with me at the moment!
            }
            else if (claimByName)
            {
                BeginClaimList(from);
            }
        }

        public bool CanClaim(Mobile from, BaseCreature pet) => from.Followers + pet.ControlSlots <= from.FollowersMax;

        private void DoClaim(Mobile from, BaseCreature pet)
        {
            pet.SetControlMaster(from);

            if (pet.Summoned)
            {
                pet.SummonMaster = from;
            }

            pet.ControlTarget = from;
            pet.ControlOrder = OrderType.Follow;

            pet.MoveToWorld(from.Location, from.Map);

            pet.IsStabled = false;
            pet.StabledBy = null;

            if (Core.SE)
            {
                pet.Loyalty = MaxLoyalty; // Wonderfully Happy
            }
        }

        public override bool HandlesOnSpeech(Mobile from) => true;

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!e.Handled && e.HasKeyword(0x0008)) // *stable*
            {
                e.Handled = true;

                CloseClaimList(e.Mobile);
                BeginStable(e.Mobile);
            }
            else if (!e.Handled && e.HasKeyword(0x0009)) // *claim*
            {
                e.Handled = true;

                CloseClaimList(e.Mobile);

                var index = e.Speech.IndexOfOrdinal(' ');

                if (index != -1)
                {
                    Claim(e.Mobile, e.Speech[index..].Trim());
                }
                else
                {
                    Claim(e.Mobile);
                }
            }
            else
            {
                base.OnSpeech(e);
            }
        }

        private class StableEntry : ContextMenuEntry
        {
            private readonly Mobile m_From;
            private readonly AnimalTrainer m_Trainer;

            public StableEntry(AnimalTrainer trainer, Mobile from) : base(6126, 12)
            {
                m_Trainer = trainer;
                m_From = from;
            }

            public override void OnClick()
            {
                m_Trainer.BeginStable(m_From);
            }
        }

        private class ClaimListGump : Gump
        {
            private readonly Mobile m_From;
            private readonly List<BaseCreature> m_List;
            private readonly AnimalTrainer m_Trainer;

            public ClaimListGump(AnimalTrainer trainer, Mobile from, List<BaseCreature> list) : base(50, 50)
            {
                m_Trainer = trainer;
                m_From = from;
                m_List = list;

                from.CloseGump<ClaimListGump>();

                AddPage(0);

                AddBackground(0, 0, 325, 50 + list.Count * 20, 9250);
                AddAlphaRegion(5, 5, 315, 40 + list.Count * 20);

                AddHtml(15, 15, 275, 20, "<BASEFONT COLOR=#FFFFFF>Select a pet to retrieve from the stables:</BASEFONT>");

                for (var i = 0; i < list.Count; ++i)
                {
                    var pet = list[i];

                    if (pet?.Deleted != false)
                    {
                        continue;
                    }

                    AddButton(15, 39 + i * 20, 10006, 10006, i + 1);
                    AddHtml(32, 35 + i * 20, 275, 18, $"<BASEFONT COLOR=#C0C0EE>{pet.Name}</BASEFONT>");
                }
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                var index = info.ButtonID - 1;

                if (index >= 0 && index < m_List.Count)
                {
                    m_Trainer.EndClaimList(m_From, m_List[index]);
                }
            }
        }

        private class ClaimAllEntry : ContextMenuEntry
        {
            private readonly Mobile m_From;
            private readonly AnimalTrainer m_Trainer;

            public ClaimAllEntry(AnimalTrainer trainer, Mobile from) : base(6127, 12)
            {
                m_Trainer = trainer;
                m_From = from;
            }

            public override void OnClick()
            {
                m_Trainer.Claim(m_From);
            }
        }

        private class StableTarget : Target
        {
            private readonly AnimalTrainer m_Trainer;

            public StableTarget(AnimalTrainer trainer) : base(12, false, TargetFlags.None) => m_Trainer = trainer;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is BaseCreature creature)
                {
                    m_Trainer.EndStable(from, creature);
                }
                else if (targeted == from)
                {
                    m_Trainer.SayTo(from, 502672); // HA HA HA! Sorry, I am not an inn.
                }
                else
                {
                    m_Trainer.SayTo(from, 1048053); // You can't stable that!
                }
            }
        }
    }
}
