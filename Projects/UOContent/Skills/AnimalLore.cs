using System;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.SkillHandlers
{
    public static class AnimalLore
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.AnimalLore].Callback = OnUse;
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.Target = new InternalTarget();

            m.SendLocalizedMessage(500328); // What animal should I look at?

            return TimeSpan.FromSeconds(1.0);
        }

        private class InternalTarget : Target
        {
            public InternalTarget() : base(8, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!from.Alive)
                {
                    from.SendLocalizedMessage(500331); // The spirits of the dead are not the province of animal lore.
                }
                else if (targeted is BaseCreature c)
                {
                    if (!c.IsDeadPet)
                    {
                        if (c.Body.IsAnimal || c.Body.IsMonster || c.Body.IsSea)
                        {
                            if (!c.Controlled && from.Skills.AnimalLore.Value < 100.0)
                            {
                                from.SendLocalizedMessage(
                                    1049674
                                ); // At your skill level, you can only lore tamed creatures.
                            }
                            else if (!c.Controlled && !c.Tamable && from.Skills.AnimalLore.Value < 110.0)
                            {
                                from.SendLocalizedMessage(
                                    1049675
                                ); // At your skill level, you can only lore tamed or tameable creatures.
                            }
                            else if (!from.CheckTargetSkill(SkillName.AnimalLore, c, 0.0, 120.0))
                            {
                                from.SendLocalizedMessage(500334); // You can't think of anything you know offhand.
                            }
                            else
                            {
                                from.CloseGump<AnimalLoreGump>();
                                from.SendGump(new AnimalLoreGump(c));
                            }
                        }
                        else
                        {
                            from.SendLocalizedMessage(500329); // That's not an animal!
                        }
                    }
                    else
                    {
                        from.SendLocalizedMessage(500331); // The spirits of the dead are not the province of animal lore.
                    }
                }
                else
                {
                    from.SendLocalizedMessage(500329); // That's not an animal!
                }
            }
        }
    }

    public class AnimalLoreGump : Gump
    {
        private const int LabelColor = 0x24E5;

        public AnimalLoreGump(BaseCreature c) : base(250, 50)
        {
            AddPage(0);

            AddImage(100, 100, 2080);
            AddImage(118, 137, 2081);
            AddImage(118, 207, 2081);
            AddImage(118, 277, 2081);
            AddImage(118, 347, 2083);

            AddHtml(147, 108, 210, 18, $"<center><i>{c.Name}</i></center>");

            AddButton(240, 77, 2093, 2093, 2);

            AddImage(140, 138, 2091);
            AddImage(140, 335, 2091);

            var pages = Core.AOS ? 5 : 3;
            var page = 0;

            AddPage(++page);

            AddImage(128, 152, 2086);
            AddHtmlLocalized(147, 150, 160, 18, 1049593, 200); // Attributes

            AddHtmlLocalized(153, 168, 160, 18, 1049578, LabelColor); // Hits
            AddHtml(280, 168, 75, 18, FormatAttributes(c.Hits, c.HitsMax));

            AddHtmlLocalized(153, 186, 160, 18, 1049579, LabelColor); // Stamina
            AddHtml(280, 186, 75, 18, FormatAttributes(c.Stam, c.StamMax));

            AddHtmlLocalized(153, 204, 160, 18, 1049580, LabelColor); // Mana
            AddHtml(280, 204, 75, 18, FormatAttributes(c.Mana, c.ManaMax));

            AddHtmlLocalized(153, 222, 160, 18, 1028335, LabelColor); // Strength
            AddHtml(320, 222, 35, 18, FormatStat(c.Str));

            AddHtmlLocalized(153, 240, 160, 18, 3000113, LabelColor); // Dexterity
            AddHtml(320, 240, 35, 18, FormatStat(c.Dex));

            AddHtmlLocalized(153, 258, 160, 18, 3000112, LabelColor); // Intelligence
            AddHtml(320, 258, 35, 18, FormatStat(c.Int));

            if (Core.AOS)
            {
                var y = 276;

                if (Core.SE)
                {
                    var bd = BaseInstrument.GetBaseDifficulty(c);
                    if (c.Uncalmable)
                    {
                        bd = 0;
                    }

                    AddHtmlLocalized(153, 276, 160, 18, 1070793, LabelColor); // Barding Difficulty
                    AddHtml(320, y, 35, 18, FormatDouble(bd));

                    y += 18;
                }

                AddImage(128, y + 2, 2086);
                AddHtmlLocalized(147, y, 160, 18, 1049594, 200); // Loyalty Rating
                y += 18;

                AddHtmlLocalized(
                    153,
                    y,
                    160,
                    18,
                    !c.Controlled || c.Loyalty == 0 ? 1061643 : 1049595 + c.Loyalty / 10,
                    LabelColor
                );
            }
            else
            {
                AddImage(128, 278, 2086);
                AddHtmlLocalized(147, 276, 160, 18, 3001016, 200); // Miscellaneous

                AddHtmlLocalized(153, 294, 160, 18, 1049581, LabelColor); // Armor Rating
                AddHtml(320, 294, 35, 18, FormatStat(c.VirtualArmor));
            }

            AddButton(340, 358, 5601, 5605, 0, GumpButtonType.Page, page + 1);
            AddButton(317, 358, 5603, 5607, 0, GumpButtonType.Page, pages);

            if (Core.AOS)
            {
                AddPage(++page);

                AddImage(128, 152, 2086);
                AddHtmlLocalized(147, 150, 160, 18, 1061645, 200); // Resistances

                AddHtmlLocalized(153, 168, 160, 18, 1061646, LabelColor); // Physical
                AddHtml(320, 168, 35, 18, FormatElement(c.PhysicalResistance));

                AddHtmlLocalized(153, 186, 160, 18, 1061647, LabelColor); // Fire
                AddHtml(320, 186, 35, 18, FormatElement(c.FireResistance));

                AddHtmlLocalized(153, 204, 160, 18, 1061648, LabelColor); // Cold
                AddHtml(320, 204, 35, 18, FormatElement(c.ColdResistance));

                AddHtmlLocalized(153, 222, 160, 18, 1061649, LabelColor); // Poison
                AddHtml(320, 222, 35, 18, FormatElement(c.PoisonResistance));

                AddHtmlLocalized(153, 240, 160, 18, 1061650, LabelColor); // Energy
                AddHtml(320, 240, 35, 18, FormatElement(c.EnergyResistance));

                AddButton(340, 358, 5601, 5605, 0, GumpButtonType.Page, page + 1);
                AddButton(317, 358, 5603, 5607, 0, GumpButtonType.Page, page - 1);
            }

            if (Core.AOS)
            {
                AddPage(++page);

                AddImage(128, 152, 2086);
                AddHtmlLocalized(147, 150, 160, 18, 1017319, 200); // Damage

                AddHtmlLocalized(153, 168, 160, 18, 1061646, LabelColor); // Physical
                AddHtml(320, 168, 35, 18, FormatElement(c.PhysicalDamage));

                AddHtmlLocalized(153, 186, 160, 18, 1061647, LabelColor); // Fire
                AddHtml(320, 186, 35, 18, FormatElement(c.FireDamage));

                AddHtmlLocalized(153, 204, 160, 18, 1061648, LabelColor); // Cold
                AddHtml(320, 204, 35, 18, FormatElement(c.ColdDamage));

                AddHtmlLocalized(153, 222, 160, 18, 1061649, LabelColor); // Poison
                AddHtml(320, 222, 35, 18, FormatElement(c.PoisonDamage));

                AddHtmlLocalized(153, 240, 160, 18, 1061650, LabelColor); // Energy
                AddHtml(320, 240, 35, 18, FormatElement(c.EnergyDamage));

                if (Core.ML)
                {
                    AddHtmlLocalized(153, 258, 160, 18, 1076750, LabelColor); // Base Damage
                    AddHtml(300, 258, 55, 18, FormatDamage(c.DamageMin, c.DamageMax));
                }

                AddButton(340, 358, 5601, 5605, 0, GumpButtonType.Page, page + 1);
                AddButton(317, 358, 5603, 5607, 0, GumpButtonType.Page, page - 1);
            }

            AddPage(++page);

            AddImage(128, 152, 2086);
            AddHtmlLocalized(147, 150, 160, 18, 3001030, 200); // Combat Ratings

            AddHtmlLocalized(153, 168, 160, 18, 1044103, LabelColor); // Wrestling
            AddHtml(320, 168, 35, 18, FormatSkill(c, SkillName.Wrestling));

            AddHtmlLocalized(153, 186, 160, 18, 1044087, LabelColor); // Tactics
            AddHtml(320, 186, 35, 18, FormatSkill(c, SkillName.Tactics));

            AddHtmlLocalized(153, 204, 160, 18, 1044086, LabelColor); // Magic Resistance
            AddHtml(320, 204, 35, 18, FormatSkill(c, SkillName.MagicResist));

            AddHtmlLocalized(153, 222, 160, 18, 1044061, LabelColor); // Anatomy
            AddHtml(320, 222, 35, 18, FormatSkill(c, SkillName.Anatomy));

            if (c is CuSidhe)
            {
                AddHtmlLocalized(153, 240, 160, 18, 1044077, LabelColor); // Healing
                AddHtml(320, 240, 35, 18, FormatSkill(c, SkillName.Healing));
            }
            else
            {
                AddHtmlLocalized(153, 240, 160, 18, 1044090, LabelColor); // Poisoning
                AddHtml(320, 240, 35, 18, FormatSkill(c, SkillName.Poisoning));
            }

            AddImage(128, 260, 2086);
            AddHtmlLocalized(147, 258, 160, 18, 3001032, 200); // Lore & Knowledge

            AddHtmlLocalized(153, 276, 160, 18, 1044085, LabelColor); // Magery
            AddHtml(320, 276, 35, 18, FormatSkill(c, SkillName.Magery));

            AddHtmlLocalized(153, 294, 160, 18, 1044076, LabelColor); // Evaluating Intelligence
            AddHtml(320, 294, 35, 18, FormatSkill(c, SkillName.EvalInt));

            AddHtmlLocalized(153, 312, 160, 18, 1044106, LabelColor); // Meditation
            AddHtml(320, 312, 35, 18, FormatSkill(c, SkillName.Meditation));

            AddButton(340, 358, 5601, 5605, 0, GumpButtonType.Page, page + 1);
            AddButton(317, 358, 5603, 5607, 0, GumpButtonType.Page, page - 1);

            AddPage(++page);

            AddImage(128, 152, 2086);
            AddHtmlLocalized(147, 150, 160, 18, 1049563, 200); // Preferred Foods

            var foodPref = 3000340;

            if ((c.FavoriteFood & FoodType.FruitsAndVegies) != 0)
            {
                foodPref = 1049565; // Fruits and Vegetables
            }
            else if ((c.FavoriteFood & FoodType.GrainsAndHay) != 0)
            {
                foodPref = 1049566; // Grains and Hay
            }
            else if ((c.FavoriteFood & FoodType.Fish) != 0)
            {
                foodPref = 1049568; // Fish
            }
            else if ((c.FavoriteFood & FoodType.Meat) != 0)
            {
                foodPref = 1049564; // Meat
            }
            else if ((c.FavoriteFood & FoodType.Eggs) != 0)
            {
                foodPref = 1044477; // Eggs
            }

            AddHtmlLocalized(153, 168, 160, 18, foodPref, LabelColor);

            AddImage(128, 188, 2086);
            AddHtmlLocalized(147, 186, 160, 18, 1049569, 200); // Pack Instincts

            var packInstinct = 3000340;

            if ((c.PackInstinct & PackInstinct.Canine) != 0)
            {
                packInstinct = 1049570; // Canine
            }
            else if ((c.PackInstinct & PackInstinct.Ostard) != 0)
            {
                packInstinct = 1049571; // Ostard
            }
            else if ((c.PackInstinct & PackInstinct.Feline) != 0)
            {
                packInstinct = 1049572; // Feline
            }
            else if ((c.PackInstinct & PackInstinct.Arachnid) != 0)
            {
                packInstinct = 1049573; // Arachnid
            }
            else if ((c.PackInstinct & PackInstinct.Daemon) != 0)
            {
                packInstinct = 1049574; // Daemon
            }
            else if ((c.PackInstinct & PackInstinct.Bear) != 0)
            {
                packInstinct = 1049575; // Bear
            }
            else if ((c.PackInstinct & PackInstinct.Equine) != 0)
            {
                packInstinct = 1049576; // Equine
            }
            else if ((c.PackInstinct & PackInstinct.Bull) != 0)
            {
                packInstinct = 1049577; // Bull
            }

            AddHtmlLocalized(153, 204, 160, 18, packInstinct, LabelColor);

            if (!Core.AOS)
            {
                AddImage(128, 224, 2086);
                AddHtmlLocalized(147, 222, 160, 18, 1049594, 200); // Loyalty Rating

                AddHtmlLocalized(
                    153,
                    240,
                    160,
                    18,
                    !c.Controlled || c.Loyalty == 0 ? 1061643 : 1049595 + c.Loyalty / 10,
                    LabelColor
                );
            }

            AddButton(340, 358, 5601, 5605, 0, GumpButtonType.Page, 1);
            AddButton(317, 358, 5603, 5607, 0, GumpButtonType.Page, page - 1);
        }

        private static string FormatSkill(BaseCreature c, SkillName name)
        {
            var skill = c.Skills[name];

            if (skill.Base < 10.0)
            {
                return "<div align=right>---</div>";
            }

            return $"<div align=right>{skill.Value:F1}</div>";
        }

        private static string FormatAttributes(int cur, int max)
        {
            if (max == 0)
            {
                return "<div align=right>---</div>";
            }

            return $"<div align=right>{cur}/{max}</div>";
        }

        private static string FormatStat(int val)
        {
            if (val == 0)
            {
                return "<div align=right>---</div>";
            }

            return $"<div align=right>{val}</div>";
        }

        private static string FormatDouble(double val)
        {
            if (val == 0)
            {
                return "<div align=right>---</div>";
            }

            return $"<div align=right>{val:F1}</div>";
        }

        private static string FormatElement(int val)
        {
            if (val <= 0)
            {
                return "<div align=right>---</div>";
            }

            return $"<div align=right>{val}%</div>";
        }

        private static string FormatDamage(int min, int max)
        {
            if (min <= 0 || max <= 0)
            {
                return "<div align=right>---</div>";
            }

            return $"<div align=right>{min}-{max}</div>";
        }
    }
}
