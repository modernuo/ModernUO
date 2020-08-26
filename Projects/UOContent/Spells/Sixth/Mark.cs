using Server.Items;
using Server.Network;
using Server.Targeting;

namespace Server.Spells.Sixth
{
  public class MarkSpell : MagerySpell, ISpellTargetingItem
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Mark", "Kal Por Ylem",
      218,
      9002,
      Reagent.BlackPearl,
      Reagent.Bloodmoss,
      Reagent.MandrakeRoot);

    public MarkSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Sixth;

    public override void OnCast()
    {
      Caster.Target = new SpellTargetItem(this, TargetFlags.None, Core.ML ? 10 : 12);
    }

    public override bool CheckCast() => base.CheckCast() && SpellHelper.CheckTravel(Caster, TravelCheckType.Mark);

    public void Target(Item item)
    {
      if (!(item is RecallRune rune))
        Caster.Send(new MessageLocalized(Caster.Serial, Caster.Body, MessageType.Regular, 0x3B2, 3, 501797, Caster.Name,
          "")); // I cannot mark that object.
      else if (!Caster.CanSee(rune))
        Caster.SendLocalizedMessage(500237); // Target can not be seen.
      else if (!SpellHelper.CheckTravel(Caster, TravelCheckType.Mark))
      {
      }
      else if (SpellHelper.CheckMulti(Caster.Location, Caster.Map, !Core.AOS))
        Caster.SendLocalizedMessage(501942); // That location is blocked.
      else if (!rune.IsChildOf(Caster.Backpack))
        Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2,
          1062422); // You must have this rune in your backpack in order to mark it.
      else if (CheckSequence())
      {
        rune.Mark(Caster);

        Caster.PlaySound(0x1FA);
        Effects.SendLocationEffect(Caster, Caster.Map, 14201, 16);
      }

      FinishSequence();
    }
  }
}
