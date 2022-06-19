using System;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Definitions
{
    public class UnfadingMemoriesPartOne : MLQuest
    {
        public UnfadingMemoriesPartOne()
        {
            Activated = true;
            Title = 1075355; // Unfading Memories
            // Aargh! It's just not right! It doesn't capture the unique color of her hair at all! If only I had some Prismatic Amber. That would be perfect. They used to mine it in Malas, but alas, those veins ran dry some time ago. I hear it may have been found in the Prism of Light. Oh, if only there were a bold adventurer within earshot who would go to the Prism of Light and retrieve some for me!
            Description = 1075356;
            RefusalMessage = 1075358; // Is there no one who can help a humble artist pursue his Muse?
            // You can find Prismatic Amber in the Prism of Light, located just north of the city of Nujel'm.
            InProgressMessage = 1075359;
            // I knew it! See, it's just the color I needed! Look how it brings out the highlights of her wheaten tresses!
            CompletionMessage = 1075360;
            CompletionNotice = CompletionNoticeShort;

            Objectives.Add(new CollectObjective(1, typeof(PrismaticAmber), "Prismatic Amber"));

            // The joy of contributing to a noble artistic effort, however paltry the end product.
            Rewards.Add(new DummyReward(1075357));
        }

        public override Type NextQuest => typeof(UnfadingMemoriesPartTwo);
    }

    public class UnfadingMemoriesPartTwo : MLQuest
    {
        public UnfadingMemoriesPartTwo()
        {
            Activated = true;
            Title = 1075367; // Unfading Memories
            // Finished! With the pigment I was able to create from the Prismatic Amber you brought me, I was able to complete my humble work. I should explain. Once, I loved a noble lady of gentleness and refinement, who possessed such beauty that I have found myself unable to love another to this day. But it was from afar that I admired her, for it is not for one so lowly as I to pay court to the likes of her. You have heard of the fair Thalia, Lady of Nujel'm? No? Well, she was my Muse, my inspiration, and when I heard she was to be married, I lost whatever pitiful talent I possessed. I felt I must compose a portrait of her, my masterpiece, or I would never be able to paint again. You, my friend, have helped me complete my work. Now I ask another favor of you. Will you take it to her as a wedding gift? She will probably reject it, but I must make the offer.
            Description = 1075368;
            // Alright then, you have already helped me more than I deserved. I shall find someone else to undertake this task.
            RefusalMessage = 1075370;
            // The wedding is taking place in the palace in Nujel'm. You will likely find her there.
            InProgressMessage = 1075371;
            // I'm sorry, I'm getting ready to be married. I don't have time to . . . what's that you say?
            CompletionMessage = 1075372;
            CompletionNotice = CompletionNoticeShort;

            Objectives.Add(new DeliverObjective(typeof(PortraitOfTheBride), 1, "Portrait of the Bride", typeof(Thalia)));

            Rewards.Add(new DummyReward(1075369)); // The Artist's gratitude.
        }

        public override Type NextQuest => typeof(UnfadingMemoriesPartThree);
        public override bool IsChainTriggered => true;
    }

    public class UnfadingMemoriesPartThree : MLQuest
    {
        public UnfadingMemoriesPartThree()
        {
            Activated = true;
            OneTimeOnly = true;
            Title = 1075373; // Unfading Memories
            // Emilio painted this? It is absolutely wonderful! I used to love looking at his paintings, but I don't remember him creating anything like this before. Would you be so kind as to carry a letter to him? Fate may have it that I am to marry another, yet I am compelled to reveal to him that his love was not entirely unrequited.
            Description = 1075374;
            RefusalMessage = 1075376; // Very well, then. If you will excuse me, I need to get ready.
            // Take the letter back to the Artist's Guild in Britain, if you would do me this kindness.
            InProgressMessage = 1075377;
            // She said what? She thinks what of me? I . . . I can't believe it! All this time, I never knew how she truly felt. Thank you, my friend. I believe now I will be able to paint once again. Here, take this bleach. I was going to use it to destroy all of my works. Perhaps you can find a better use for it now.
            CompletionMessage = 1075378;
            CompletionNotice = CompletionNoticeShort;

            Objectives.Add(new DeliverObjective(typeof(BridesLetter), 1, "Bride's Letter", typeof(Emilio)));

            Rewards.Add(new ItemReward(1075375, typeof(Bleach))); // Bleach
        }

        public override bool IsChainTriggered => true;
    }

    [QuesterName("Emilio (Britain)")] // OSI's description is "Artist", not very helpful
    public class Emilio : BaseCreature
    {
        [Constructible]
        public Emilio() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Tortured Artist";
            Race = Race.Human;
            Body = 0x190;
            Female = false;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            AddItem(new Backpack());
            AddItem(new Sandals(0x72B));
            AddItem(new LongPants(0x525));
            AddItem(new FancyShirt(0x53F));
            AddItem(new FloppyHat(0x58C));
            AddItem(new BodySash(0x1C));
        }

        public Emilio(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Emilio";
        public override bool IsInvulnerable => true;

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

    [QuesterName("Thalia (Nujel'm)")] // OSI's description is "Bride", not very helpful
    public class Thalia : BaseCreature
    {
        [Constructible]
        public Thalia() : base(AIType.AI_Vendor, FightMode.None, 2)
        {
            Title = "the Bride";
            Race = Race.Human;
            Body = 0x191;
            Female = true;
            Hue = Race.RandomSkinHue();

            SetSpeed(0.5, 2.0);
            InitStats(100, 100, 25);

            Utility.AssignRandomHair(this);

            AddItem(new Backpack());
            AddItem(new Sandals(0x8FD));
            AddItem(new FancyDress(0x8FD));
        }

        public Thalia(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Thalia";
        public override bool IsInvulnerable => true;

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
