using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Hag
{
    public class FindApprenticeObjective : QuestObjective
    {
        private static readonly Point3D[] m_CorpseLocations =
        {
            new(778, 1158, 0),
            new(698, 1443, 0),
            new(785, 1548, 0),
            new(734, 1504, 0),
            new(819, 1266, 0)
        };

        private Point3D m_CorpseLocation;

        public FindApprenticeObjective(bool init)
        {
            if (init)
            {
                m_CorpseLocation = RandomCorpseLocation();
            }
        }

        public FindApprenticeObjective()
        {
        }

        public override object Message => 1055014;

        public Corpse Corpse { get; private set; }

        private static Point3D RandomCorpseLocation() => m_CorpseLocations.RandomElement();

        public override void CheckProgress()
        {
            var player = System.From;
            var map = player.Map;

            if (Corpse?.Deleted == false || map != Map.Trammel && map != Map.Felucca ||
                !player.InRange(m_CorpseLocation, 8))
            {
                return;
            }

            Corpse = new HagApprenticeCorpse();
            Corpse.MoveToWorld(m_CorpseLocation, map);

            Effects.SendLocationEffect(m_CorpseLocation, map, 0x3728, 10);
            Effects.PlaySound(m_CorpseLocation, map, 0x1FE);

            Mobile imp = new Zeefzorpul();
            imp.MoveToWorld(m_CorpseLocation, map);

            // * You see a strange imp stealing a scrap of paper from the bloodied corpse *
            Corpse.SendLocalizedMessageTo(player, 1055049);

            Timer.StartTimer(TimeSpan.FromSeconds(3.0), () => DeleteImp(imp));
        }

        private void DeleteImp(Mobile m)
        {
            if (m?.Deleted == false)
            {
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10);
                Effects.PlaySound(m.Location, m.Map, 0x1FE);

                m.Delete();
            }
        }

        public override void OnComplete()
        {
            System.AddConversation(new ApprenticeCorpseConversation());
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 1:
                    {
                        m_CorpseLocation = reader.ReadPoint3D();
                        goto case 0;
                    }
                case 0:
                    {
                        Corpse = (Corpse)reader.ReadEntity<Item>();
                        break;
                    }
            }

            if (version == 0)
            {
                m_CorpseLocation = RandomCorpseLocation();
            }
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            if (Corpse?.Deleted == true)
            {
                Corpse = null;
            }

            writer.WriteEncodedInt(1); // version

            writer.Write(m_CorpseLocation);
            writer.Write(Corpse);
        }
    }

    public class FindGrizeldaAboutMurderObjective : QuestObjective
    {
        public override object Message => 1055015;

        public override void OnComplete()
        {
            System.AddConversation(new MurderConversation());
        }
    }

    public class KillImpsObjective : QuestObjective
    {
        private int m_MaxProgress;

        public KillImpsObjective(bool init)
        {
            if (init)
            {
                m_MaxProgress = Utility.RandomMinMax(1, 4);
            }
        }

        public KillImpsObjective()
        {
        }

        public override object Message => 1055016;

        public override int MaxProgress => m_MaxProgress;

        public override bool IgnoreYoungProtection(Mobile from)
        {
            if (!Completed && from is Imp)
            {
                return true;
            }

            return false;
        }

        public override void OnKill(BaseCreature creature, Container corpse)
        {
            if (creature is Imp)
            {
                CurProgress++;
            }
        }

        public override void OnComplete()
        {
            var from = System.From;

            var loc = WitchApprenticeQuest.RandomZeefzorpulLocation();

            var mapItem = new MapItem();
            mapItem.SetDisplay(loc.X - 200, loc.Y - 200, loc.X + 200, loc.Y + 200, 200, 200);
            mapItem.AddWorldPin(loc.X, loc.Y);
            from.AddToBackpack(mapItem);

            from.AddToBackpack(new MagicFlute());

            from.SendLocalizedMessage(1055061); // You have received a map and a magic flute.

            System.AddConversation(new ImpDeathConversation(loc));
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_MaxProgress = reader.ReadInt();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_MaxProgress);
        }
    }

    public class FindZeefzorpulObjective : QuestObjective
    {
        public FindZeefzorpulObjective(Point3D impLocation) => ImpLocation = impLocation;

        public FindZeefzorpulObjective()
        {
        }

        public override object Message => 1055017;

        public Point3D ImpLocation { get; private set; }

        public override void OnComplete()
        {
            Mobile from = System.From;
            var map = from.Map;

            Effects.SendLocationEffect(ImpLocation, map, 0x3728, 10);
            Effects.PlaySound(ImpLocation, map, 0x1FE);

            Mobile imp = new Zeefzorpul();
            imp.MoveToWorld(ImpLocation, map);

            imp.Direction = imp.GetDirectionTo(from);

            Timer.StartTimer(TimeSpan.FromSeconds(3.0), () => DeleteImp(imp));
        }

        private void DeleteImp(object imp)
        {
            if (imp is Mobile m && !m.Deleted)
            {
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10);
                Effects.PlaySound(m.Location, m.Map, 0x1FE);

                m.Delete();
            }

            System.From.SendLocalizedMessage(1055062); // You have received the Magic Brew Recipe.

            System.AddConversation(new ZeefzorpulConversation());
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            ImpLocation = reader.ReadPoint3D();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(ImpLocation);
        }
    }

    public class ReturnRecipeObjective : QuestObjective
    {
        public override object Message => 1055018;

        public override void OnComplete()
        {
            System.AddConversation(new RecipeConversation());
        }
    }

    public class FindIngredientObjective : QuestObjective
    {
        public FindIngredientObjective(Ingredient[] oldIngredients, bool blackheartMet = false)
        {
            if (!blackheartMet)
            {
                Ingredients = new Ingredient[oldIngredients.Length + 1];

                for (var i = 0; i < oldIngredients.Length; i++)
                {
                    Ingredients[i] = oldIngredients[i];
                }

                Ingredients[^1] = IngredientInfo.RandomIngredient(oldIngredients);
            }
            else
            {
                Ingredients = new Ingredient[oldIngredients.Length];

                for (var i = 0; i < oldIngredients.Length; i++)
                {
                    Ingredients[i] = oldIngredients[i];
                }
            }

            BlackheartMet = blackheartMet;
        }

        public FindIngredientObjective()
        {
        }

        public override object Message
        {
            get
            {
                if (!BlackheartMet)
                {
                    return Step switch
                    {
                        1 =>
                            /* You must gather each ingredient on the Hag's list so that she can cook
                               * up her vile Magic Brew.  The first ingredient is :
                               */
                            1055019,
                        2 =>
                            /* You must gather each ingredient on the Hag's list so that she can cook
                               * up her vile Magic Brew.  The second ingredient is :
                               */
                            1055044,
                        _ => 1055045
                    };
                }

                /* You are still attempting to obtain a jug of Captain Blackheart's
                   * Whiskey, but the drunkard Captain refuses to share his unique brew.
                   * You must prove your worthiness as a pirate to Blackheart before he'll
                   * offer you a jug.
                   */
                return 1055055;
            }
        }

        public override int MaxProgress
        {
            get
            {
                var info = IngredientInfo.Get(Ingredient);

                return info.Quantity;
            }
        }

        public Ingredient[] Ingredients { get; private set; }

        public Ingredient Ingredient => Ingredients[^1];
        public int Step => Ingredients.Length;
        public bool BlackheartMet { get; private set; }

        public override void RenderProgress(BaseQuestGump gump)
        {
            if (!Completed)
            {
                var info = IngredientInfo.Get(Ingredient);

                gump.AddHtmlLocalized(70, 260, 270, 100, info.Name, BaseQuestGump.Blue);
                gump.AddLabel(70, 280, 0x64, CurProgress.ToString());
                gump.AddLabel(100, 280, 0x64, "/");
                gump.AddLabel(130, 280, 0x64, info.Quantity.ToString());
            }
            else
            {
                base.RenderProgress(gump);
            }
        }

        public override bool IgnoreYoungProtection(Mobile from)
        {
            if (Completed)
            {
                return false;
            }

            var info = IngredientInfo.Get(Ingredient);
            var fromType = from.GetType();

            for (var i = 0; i < info.Creatures.Length; i++)
            {
                if (fromType == info.Creatures[i])
                {
                    return true;
                }
            }

            return false;
        }

        public override void OnKill(BaseCreature creature, Container corpse)
        {
            var info = IngredientInfo.Get(Ingredient);

            for (var i = 0; i < info.Creatures.Length; i++)
            {
                var type = info.Creatures[i];

                if (creature.GetType() == type)
                {
                    // You gather a ~1_INGREDIENT_NAME~ from the corpse.
                    System.From.SendLocalizedMessage(1055043, $"#{info.Name}");

                    CurProgress++;
                    break;
                }
            }
        }

        public override void OnComplete()
        {
            if (Ingredient != Ingredient.Whiskey)
            {
                NextStep();
            }
        }

        public void NextStep()
        {
            System.From.SendLocalizedMessage(
                1055046
            ); // You have completed your current task on the Hag's Magic Brew Recipe list.

            if (Step < 3)
            {
                System.AddObjective(new FindIngredientObjective(Ingredients));
            }
            else
            {
                System.AddObjective(new ReturnIngredientsObjective());
            }
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            Ingredients = new Ingredient[reader.ReadEncodedInt()];
            for (var i = 0; i < Ingredients.Length; i++)
            {
                Ingredients[i] = (Ingredient)reader.ReadEncodedInt();
            }

            BlackheartMet = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.WriteEncodedInt(Ingredients.Length);
            for (var i = 0; i < Ingredients.Length; i++)
            {
                writer.WriteEncodedInt((int)Ingredients[i]);
            }

            writer.Write(BlackheartMet);
        }
    }

    public class ReturnIngredientsObjective : QuestObjective
    {
        public override object Message => 1055050;

        public override void OnComplete()
        {
            System.AddConversation(new EndConversation());
        }
    }
}
