using Server.Engines.Plants;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Naturalist
{
    public class Naturalist : BaseQuester
    {
        [Constructible]
        public Naturalist() : base("the Naturalist")
        {
        }

        public Naturalist(Serial serial) : base(serial)
        {
        }

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            Hue = Race.Human.RandomSkinHue();

            Female = false;
            Body = 0x190;
            Name = NameList.RandomName("male");
        }

        public override void InitOutfit()
        {
            AddItem(new Tunic(0x598));
            AddItem(new LongPants(0x59B));
            AddItem(new Boots());

            Utility.AssignRandomHair(this);
            Utility.AssignRandomFacialHair(this, HairHue);
        }

        public override void OnTalk(PlayerMobile player, bool contextMenu)
        {
            if (player.Quest is StudyOfSolenQuest qs && qs.Naturalist == this)
            {
                var study = qs.FindObjective<StudyNestsObjective>();
                if (study == null)
                {
                    return;
                }

                if (!study.Completed)
                {
                    PlaySound(0x41F);
                    qs.AddConversation(new NaturalistDuringStudyConversation());
                    return;
                }

                QuestObjective obj = qs.FindObjective<ReturnToNaturalistObjective>();

                if (obj?.Completed == false)
                {
                    Seed reward;

                    var type = Utility.Random(17) switch
                    {
                        0  => PlantType.CampionFlowers,
                        1  => PlantType.Poppies,
                        2  => PlantType.Snowdrops,
                        3  => PlantType.Bulrushes,
                        4  => PlantType.Lilies,
                        5  => PlantType.PampasGrass,
                        6  => PlantType.Rushes,
                        7  => PlantType.ElephantEarPlant,
                        8  => PlantType.Fern,
                        9  => PlantType.PonytailPalm,
                        10 => PlantType.SmallPalm,
                        11 => PlantType.CenturyPlant,
                        12 => PlantType.WaterPlant,
                        13 => PlantType.SnakePlant,
                        14 => PlantType.PricklyPearCactus,
                        15 => PlantType.BarrelCactus,
                        _  => PlantType.TribarrelCactus
                    };

                    if (study.StudiedSpecialNest)
                    {
                        reward = new Seed(type, PlantHue.FireRed);
                    }
                    else
                    {
                        var hue = Utility.Random(3) switch
                        {
                            0 => PlantHue.Pink,
                            1 => PlantHue.Magenta,
                            _ => PlantHue.Aqua
                        };

                        reward = new Seed(type, hue);
                    }

                    if (player.PlaceInBackpack(reward))
                    {
                        obj.Complete();

                        PlaySound(0x449);
                        PlaySound(0x41B);

                        if (study.StudiedSpecialNest)
                        {
                            qs.AddConversation(new SpecialEndConversation());
                        }
                        else
                        {
                            qs.AddConversation(new EndConversation());
                        }
                    }
                    else
                    {
                        reward.Delete();

                        qs.AddConversation(new FullBackpackConversation());
                    }
                }
            }
            else
            {
                QuestSystem newQuest = new StudyOfSolenQuest(player, this);

                if (player.Quest == null && QuestSystem.CanOfferQuest(player, typeof(StudyOfSolenQuest)))
                {
                    PlaySound(0x42F);
                    newQuest.SendOffer();
                }
                else
                {
                    PlaySound(0x448);
                    newQuest.AddConversation(new DontOfferConversation());
                }
            }
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
