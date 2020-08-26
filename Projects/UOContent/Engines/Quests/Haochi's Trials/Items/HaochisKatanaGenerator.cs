using Server.Mobiles;

namespace Server.Engines.Quests.Samurai
{
    public class HaochisKatanaGenerator : Item
    {
        [Constructible]
        public HaochisKatanaGenerator() : base(0x1B7B)
        {
            Visible = false;
            Movable = false;
        }

        public HaochisKatanaGenerator(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "Haochi's katana generator";

        public override bool OnMoveOver(Mobile m)
        {
            if (m is PlayerMobile player)
            {
                var qs = player.Quest;

                if (qs is HaochisTrialsQuest)
                {
                    if (HaochisTrialsQuest.HasLostHaochisKatana(player))
                    {
                        Item katana = new HaochisKatana();

                        if (!player.PlaceInBackpack(katana))
                        {
                            katana.Delete();
                            player.SendLocalizedMessage(
                                1046260
                            ); // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                        }
                    }
                    else
                    {
                        QuestObjective obj = qs.FindObjective<FifthTrialIntroObjective>();

                        if (obj?.Completed == false)
                        {
                            Item katana = new HaochisKatana();

                            if (player.PlaceInBackpack(katana))
                            {
                                obj.Complete();
                            }
                            else
                            {
                                katana.Delete();
                                player.SendLocalizedMessage(
                                    1046260
                                ); // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                            }
                        }
                    }
                }
            }

            return base.OnMoveOver(m);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
