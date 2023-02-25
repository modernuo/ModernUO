using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Hag
{
    public class Blackheart : BaseQuester
    {
        [Constructible]
        public Blackheart() : base("the Drunken Pirate")
        {
        }

        public Blackheart(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "Captain Blackheart";

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            Hue = 0x83EF;

            Female = false;
            Body = 0x190;
        }

        public override void InitOutfit()
        {
            AddItem(new FancyShirt());
            AddItem(new LongPants(0x66D));
            AddItem(new ThighBoots());
            AddItem(new TricorneHat(0x1));
            AddItem(new BodySash(0x66D));

            var gloves = new LeatherGloves();
            gloves.Hue = 0x66D;
            AddItem(gloves);

            FacialHairItemID = 0x203E; // Long Beard
            FacialHairHue = 0x455;

            Item sword = new Cutlass();
            sword.Movable = false;
            AddItem(sword);
        }

        public override void OnTalk(PlayerMobile player, bool contextMenu)
        {
            Direction = GetDirectionTo(player);
            Animate(33, 20, 1, true, false, 0);

            var qs = player.Quest;

            if (qs is WitchApprenticeQuest)
            {
                var obj = qs.FindObjective<FindIngredientObjective>();
                if (obj?.Completed == false && obj.Ingredient == Ingredient.Whiskey)
                {
                    PlaySound(Utility.RandomBool() ? 0x42E : 0x43F);

                    var tricorne = player.FindItemOnLayer<TricorneHat>(Layer.Helm) != null;

                    if (tricorne && player.BAC >= 20)
                    {
                        obj.Complete();

                        qs.AddConversation(new BlackheartPirateConversation(!obj.BlackheartMet));
                    }
                    else if (!obj.BlackheartMet)
                    {
                        obj.Complete();

                        qs.AddConversation(new BlackheartFirstConversation());
                    }
                    else
                    {
                        qs.AddConversation(new BlackheartNoPirateConversation(tricorne, player.BAC > 0));
                    }

                    return;
                }
            }

            PlaySound(0x42C);
            SayTo(player, 1055041); // The drunken pirate shakes his fist at you and goes back to drinking.
        }

        private void Heave()
        {
            PublicOverheadMessage(MessageType.Regular, 0x3B2, 500849); // *hic*

            Timer.StartTimer(TimeSpan.FromSeconds(Utility.RandomMinMax(60, 180)), Heave);
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

            Heave();
        }
    }
}
