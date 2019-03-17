using System;
using Server.Items;
using Server.Mobiles;

namespace Server.SkillHandlers
{
  public class Stealth
  {
    public static double HidingRequirement => Core.ML ? 30.0 : Core.SE ? 50.0 : 80.0;

    public static int[,] ArmorTable{ get; } =
    {
      //	Gorget	Gloves	Helmet	Arms	Legs	Chest	Shield
      /* Cloth	*/ { 0, 0, 0, 0, 0, 0, 0 },
      /* Leather	*/ { 0, 0, 0, 0, 0, 0, 0 },
      /* Studded	*/ { 2, 2, 0, 4, 6, 10, 0 },
      /* Bone		*/ { 0, 5, 10, 10, 15, 25, 0 },
      /* Spined	*/ { 0, 0, 0, 0, 0, 0, 0 },
      /* Horned	*/ { 0, 0, 0, 0, 0, 0, 0 },
      /* Barbed	*/ { 0, 0, 0, 0, 0, 0, 0 },
      /* Ring		*/ { 0, 5, 0, 10, 15, 25, 0 },
      /* Chain	*/ { 0, 0, 10, 0, 15, 25, 0 },
      /* Plate	*/ { 5, 5, 10, 10, 15, 25, 0 },
      /* Dragon	*/ { 0, 5, 10, 10, 15, 25, 0 }
    };

    public static void Initialize()
    {
      SkillInfo.Table[(int)SkillName.Stealth].Callback = OnUse;
    }

    public static int GetArmorRating(Mobile m)
    {
      if (!Core.AOS)
        return (int)m.ArmorRating;

      int ar = 0;

      for (int i = 0; i < m.Items.Count; i++)
      {
        if (!(m.Items[i] is BaseArmor armor))
          continue;

        int materialType = (int)armor.MaterialType;
        int bodyPosition = (int)armor.BodyPosition;

        if (materialType >= ArmorTable.GetLength(0) || bodyPosition >= ArmorTable.GetLength(1))
          continue;

        if (armor.ArmorAttributes.MageArmor == 0)
          ar += ArmorTable[materialType, bodyPosition];
      }

      return ar;
    }

    public static TimeSpan OnUse(Mobile m)
    {
      if (!m.Hidden)
      {
        m.SendLocalizedMessage(502725); // You must hide first
      }
      else if (m.Skills.Hiding.Base < HidingRequirement)
      {
        m.SendLocalizedMessage(502726); // You are not hidden well enough.  Become better at hiding.
        m.RevealingAction();
      }
      else if (!m.CanBeginAction<Stealth>())
      {
        m.SendLocalizedMessage(1063086); // You cannot use this skill right now.
        m.RevealingAction();
      }
      else
      {
        int armorRating = GetArmorRating(m);

        if (armorRating >= (Core.AOS ? 42 : 26)) //I have a hunch '42' was chosen cause someone is a fan of DNA
        {
          m.SendLocalizedMessage(502727); // You could not hope to move quietly wearing this much armor.
          m.RevealingAction();
        }
        else if (m.CheckSkill(SkillName.Stealth, -200 + armorRating * 20,
          (Core.AOS ? 600 : 800) + armorRating * 20))
        {
          int steps = m.Skills.Stealth.Fixed / (Core.AOS ? 50 : 100);

          if (steps < 1)
            steps = 1;

          m.AllowedStealthSteps = steps;

          if (m is PlayerMobile pm)
            pm.IsStealthing = true;

          m.SendLocalizedMessage(502730); // You begin to move quietly.

          return TimeSpan.FromSeconds(10.0);
        }
        else
        {
          m.SendLocalizedMessage(502731); // You fail in your attempt to move unnoticed.
          m.RevealingAction();
        }
      }

      return TimeSpan.FromSeconds(10.0);
    }
  }
}