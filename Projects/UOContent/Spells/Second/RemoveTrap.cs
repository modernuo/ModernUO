using Server.Items;
using Server.Targeting;

namespace Server.Spells.Second
{
  public class RemoveTrapSpell : MagerySpell, ISpellTargetingItem
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Remove Trap", "An Jux",
      212,
      9001,
      Reagent.Bloodmoss,
      Reagent.SulfurousAsh);

    public RemoveTrapSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Second;

    public override void OnCast()
    {
      Caster.Target = new SpellTargetItem(this, TargetFlags.None, Core.ML ? 10 : 12);
      Caster.SendMessage("What do you wish to untrap?"); // TODO: Localization?
    }

    public void Target(Item item)
    {
      if (!(item is TrappableContainer cont))
        Caster.SendMessage("You can't disarm that"); // TODO: Localization?
      else if (!Caster.CanSee(item))
      {
        Caster.SendLocalizedMessage(500237); // Target can not be seen.
      }
      else if (cont.TrapType != TrapType.None && cont.TrapType != TrapType.MagicTrap)
      {
        DoFizzle();
      }
      else if (CheckSequence())
      {
        SpellHelper.Turn(Caster, item);

        Point3D loc = item.GetWorldLocation();

        Effects.SendLocationParticles(EffectItem.Create(loc, item.Map, EffectItem.DefaultDuration), 0x376A, 9, 32,
          5015);
        Effects.PlaySound(loc, item.Map, 0x1F0);

        cont.TrapType = TrapType.None;
        cont.TrapPower = 0;
        cont.TrapLevel = 0;
      }

      FinishSequence();
    }
  }
}
