using System;
using System.Collections.Generic;
using Server.Engines.MLQuests.Definitions;
using Server.Engines.MLQuests.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.MLQuests.Mobiles
{
    public abstract class DoneQuestCollector : BaseCreature, IRaceChanger
    {
        private InternalTimer m_Timer;

        public DoneQuestCollector() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            SetSpeed(0.5, 2.0);
        }

        public DoneQuestCollector(Serial serial)
            : base(serial)
        {
        }

        public override bool IsInvulnerable => true;

        public abstract TextDefinition[] Offer { get; }
        public abstract TextDefinition[] Incomplete { get; }
        public abstract TextDefinition[] Complete { get; }
        public abstract Type[] Needed { get; }

        public bool CheckComplete(PlayerMobile pm)
        {
            if (CompletedCount(pm) == Needed.Length)
            {
                return true;
            }

            pm.SendLocalizedMessage(1073644); // You must complete all the tasks before proceeding...
            return false;
        }

        public void ConsumeNeeded(PlayerMobile pm)
        {
            var context = MLQuestSystem.GetContext(pm);

            if (context != null)
            {
                foreach (var type in Needed)
                {
                    var quest = MLQuestSystem.FindQuest(type);

                    if (quest != null)
                    {
                        context.RemoveDoneQuest(quest);
                    }
                }
            }
        }

        public void OnCancel(PlayerMobile pm)
        {
            pm.SendLocalizedMessage(1073645); // You may try this again later...
        }

        public override void OnDoubleClick(Mobile from)
        {
            TryTalkTo(from, true);
        }

        public override void OnDoubleClickDead(Mobile from)
        {
            TryTalkTo(from, true);
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m.Player && InRange(m, 6) && !InRange(oldLocation, 6))
            {
                TryTalkTo(m, false);
            }

            base.OnMovement(m, oldLocation);
        }

        public void TryTalkTo(Mobile from, bool fromClick)
        {
            if (!from.Hidden && !from.HasGump<RaceChangeConfirmGump>() &&
                !RaceChangeConfirmGump.IsPending(from.NetState) && CanTalkTo(from))
            {
                TalkTo(from as PlayerMobile);
            }
            else if (fromClick)
            {
                DenyTalk(from);
            }
        }

        public virtual bool CanTalkTo(Mobile from) => true;

        public virtual void DenyTalk(Mobile from)
        {
        }

        public void TalkTo(PlayerMobile pm)
        {
            if (pm == null || m_Timer?.Running == true)
            {
                return;
            }

            var completed = CompletedCount(pm);

            if (completed == Needed.Length)
            {
                m_Timer = new InternalTimer(this, pm, Complete, true);
            }
            else if (completed == 0)
            {
                m_Timer = new InternalTimer(this, pm, Offer, false);
            }
            else
            {
                var conversation = new List<TextDefinition>();
                conversation.AddRange(Incomplete);

                var context = MLQuestSystem.GetContext(pm);

                if (context != null)
                {
                    foreach (var type in Needed)
                    {
                        var quest = MLQuestSystem.FindQuest(type);

                        if (quest == null || context.HasDoneQuest(quest))
                        {
                            continue;
                        }

                        conversation.Add(quest.Title);
                    }
                }

                m_Timer = new InternalTimer(this, pm, conversation, false);
            }

            m_Timer.Start();
        }

        private int CompletedCount(PlayerMobile pm)
        {
            var context = MLQuestSystem.GetContext(pm);

            if (context == null)
            {
                return 0;
            }

            var result = 0;

            foreach (var type in Needed)
            {
                var quest = MLQuestSystem.FindQuest(type);

                if (quest == null || context.HasDoneQuest(quest))
                {
                    ++result;
                }
            }

            return result;
        }

        public virtual void OnComplete(PlayerMobile pm)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        private class InternalTimer : Timer
        {
            private readonly IList<TextDefinition> m_Conversation;
            private readonly bool m_IsComplete;
            private readonly DoneQuestCollector m_Owner;
            private readonly PlayerMobile m_Target;
            private int m_Index;

            public InternalTimer(
                DoneQuestCollector owner, PlayerMobile target, IList<TextDefinition> conversation,
                bool isComplete
            )
                : base(TimeSpan.Zero, GetDelay())
            {
                m_Owner = owner;
                m_Target = target;
                m_Conversation = conversation;
                m_IsComplete = isComplete;
                m_Index = 0;
            }

            private static TimeSpan GetDelay() => TimeSpan.FromSeconds(Utility.RandomBool() ? 3 : 4);

            protected override void OnTick()
            {
                if (m_Owner.Deleted)
                {
                    Stop();
                    return;
                }

                if (m_Index >= m_Conversation.Count)
                {
                    if (m_IsComplete)
                    {
                        m_Owner.OnComplete(m_Target);
                    }

                    Stop();
                }
                else
                {
                    if (m_Index == 0)
                    {
                        if (m_Target.ShowFameTitle && m_Target.Fame >= 10000)
                        {
                            m_Owner.Say(true, $"{(m_Target.Female ? "Lady" : "Lord")} {m_Target.Name}");
                        }
                        else
                        {
                            m_Owner.Say(true, m_Target.Name);
                        }
                    }

                    TextDefinition.PublicOverheadMessage(m_Owner, MessageType.Regular, 0x3B2, m_Conversation[m_Index++]);
                    Interval = GetDelay();
                }
            }
        }
    }

    public class Darius : DoneQuestCollector
    {
        private static readonly TextDefinition[] m_Offer =
        {
            1073998, // Blessings of Sosaria to you and merry met, friend.
            1073999, // I am glad for your company and wonder if you seek the heritage of your people?  I sense within you an elven bloodline -- the purity of which was lost when our brothers and sisters were exiled here in the Rupture.
            1074000, // If it is your desire to reclaim your place amongst the people, you must demonstrate that you understand and embrace the responsibilities expected of you as an elf.
            1074001, // The most basic lessons of our Sosaria are taught by her humblest children.  Seek Maul, the great bear, who understands instictively the seasons.
            1074398, // Seek Strongroot, the great treefellow, whose very roots reach to the heart of the world.  Seek Enigma, whose wisdom can only be conveyed in riddles and rhymes.  Seek Bravehorn, the great hart, who exemplifies the fierce dedication of a protector of his people.
            1074399, // Seek the Huntsman, the centuar tasked with maintaining the balance.  And lastly seek Arielle, the pixie, who has perhaps the most important lesson -- not to take yourself too seriously.
            1074400 // Or do none of these things.  You must choose your own path in the world, and what use you'll make of your existence.
        };

        private static readonly TextDefinition[] m_Incomplete =
        {
            1074002, // You have begun to walk the path of reclaiming your heritage, but you have not learned all the lessons before you.
            1074003 // You yet must perform these services:
        };

        private static readonly TextDefinition[] m_Complete =
        {
            1074004, // You have carved a path in history, sought to understand the way from our sage companions.
            1074005, // And now you have returned full circle to the place of your origin within the arms of Mother Sosaria. There is but one thing left to do if you truly wish to embrace your elven heritage.
            1074006, // To be born once more an elf, you must strip of all worldly possessions. Nothing of man or beast much touch your skin.
            1074007 // Then you may step forth into history.
        };

        private static readonly Type[] m_Needed =
        {
            typeof(Seasons),
            typeof(CaretakerOfTheLand),
            typeof(WisdomOfTheSphynx),
            typeof(DefendingTheHerd),
            typeof(TheBalanceOfNature),
            typeof(TheJoysOfLife)
        };

        [Constructible]
        public Darius()
        {
            Title = "the wise";
            Race = Race.Elf;
            Hue = Race.RandomSkinHue();
            SpeechHue = Utility.RandomDyedHue();

            AddItem(new WildStaff());
            AddItem(new Sandals(0x1BB));
            AddItem(new GemmedCirclet());
            AddItem(new Tunic(Utility.RandomBrightHue()));

            Utility.AssignRandomHair(this);

            SetStr(40, 50);
            SetDex(60, 70);
            SetInt(90, 100); // Verified int
        }

        public Darius(Serial serial)
            : base(serial)
        {
        }

        public override TextDefinition[] Offer => m_Offer;
        public override TextDefinition[] Incomplete => m_Incomplete;
        public override TextDefinition[] Complete => m_Complete;
        public override Type[] Needed => m_Needed;

        public override string DefaultName => "Darius";

        public override bool CanTalkTo(Mobile from) => from.Race == Race.Human;

        public override void DenyTalk(Mobile from)
        {
            from.SendLocalizedMessage(1074017); // He's too busy right now, so he ignores you.
        }

        public override void OnComplete(PlayerMobile from)
        {
            from.SendGump(new RaceChangeConfirmGump(this, from, Race.Elf));
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Nedrick : DoneQuestCollector
    {
        private static readonly TextDefinition[] m_Offer =
        {
            1074403, // Greetings, traveler and welcome.
            1074404, // Perhaps you have heard of the service I offer?  Perhaps you wish to avail yourself of the opportunity I lay before you.
            1074405, // Elves and humans; we lived together once in peace.  Mighty relics that attest to our friendship remain, of course.  Yet, memories faded when the Gem was shattered and the world torn asunder.  Alone in The Heartwood, our elven brothers and sisters wondered what terrible evil had befallen Sosaria.
            1074406, // Violent change marked the sundering of our ties.  We are different -- elves and humans.  And yet we are much alike.  I can give an elf the chance to walk as a human upon Sosaria.  I can undertake the transformation.
            1074407, // But you must prove yourself to me.  Humans possess a strength of character and back.  Humans are quick-witted and able to pick up a smattering of nearly any talent.  Humans are tough both mentally and physically.  And of course, humans defend their own -- sometimes with their own lives.
            1074408, // Seek Sledge the Versatile and learn about human ingenuity and creativity.  Seek Patricus and demonstrate your integrity and strength.
            1074409, // Seek out a human in need and prove your worth as a defender of humanity.  Seek Belulah in Nu'Jelm and heartily challenge the elements in a display of toughness to rival any human.
            1074411 // Or turn away and embrace your heritage.  It matters not to me.
        };

        private static readonly TextDefinition[] m_Incomplete =
        {
            1074412, // You have made a good start but have more yet to do.
            1074413  // You must yet perform these deeds:
        };

        private static readonly TextDefinition[] m_Complete =
        {
            1074410, // You have proven yourself capable and commited and so I will grant you the transformation you seek.
            1074531, // The first time you were born, you entered the world bare of all possessions and concerns.  So too as you transform to your new life as a human, you must remove all worldly goods from the touch of your flesh.
            1074532 // I call upon all nearby to witness your rebirth!
        };

        private static readonly Type[] m_Needed =
        {
            typeof(Ingenuity),
            typeof(HeaveHo),
            typeof(HumanInNeed),
            typeof(AllSeasonAdventurer)
        };

        [Constructible]
        public Nedrick()
        {
            Title = "the iron worker";
            Race = Race.Human;
            Hue = Race.RandomSkinHue();
            SpeechHue = Utility.RandomDyedHue();

            AddItem(new Boots());
            AddItem(new LongPants(Utility.RandomNondyedHue()));
            AddItem(new FancyShirt(Utility.RandomNondyedHue()));

            Utility.AssignRandomHair(this);
            Utility.AssignRandomFacialHair(this, HairHue);

            SetStr(70, 80);
            SetDex(50, 60);
            SetInt(60, 70); // Verified int
        }

        public Nedrick(Serial serial)
            : base(serial)
        {
        }

        public override TextDefinition[] Offer => m_Offer;
        public override TextDefinition[] Incomplete => m_Incomplete;
        public override TextDefinition[] Complete => m_Complete;
        public override Type[] Needed => m_Needed;

        public override string DefaultName => "Nedrick";

        public override bool CanTalkTo(Mobile from) => from.Race == Race.Elf;

        public override void DenyTalk(Mobile from)
        {
            from.SendLocalizedMessage(1074017); // He's too busy right now, so he ignores you.
        }

        public override void OnComplete(PlayerMobile from)
        {
            from.SendGump(new RaceChangeConfirmGump(this, from, Race.Human));
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
