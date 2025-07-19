using System.Runtime.CompilerServices;

namespace Server.Mobiles;

public class VendorAI : BaseAI
{
    // Guards! A villan attacks me!
    // Guards! Help!
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetRandomGuardMessage() => Utility.RandomBool() ? 1005305 : 501603;

    public VendorAI(BaseCreature m) : base(m)
    {
    }

    public override bool DoActionWander()
    {
        if (Mobile.Debug)
        {
            Mobile.DebugSay("I'm fine");
        }

        if (Mobile.Combatant != null)
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay($"{Mobile.Combatant.Name} is attacking me");
            }

            Mobile.Say(GetRandomGuardMessage());
            Action = ActionType.Flee;
        }
        else
        {
            if (Mobile.FocusMob != null)
            {
                if (Mobile.Debug)
                {
                    Mobile.DebugSay($"{Mobile.FocusMob.Name} has talked to me");
                }

                Action = ActionType.Interact;
            }
            else
            {
                Mobile.Warmode = false;

                base.DoActionWander();
            }
        }

        return true;
    }

    public override bool DoActionInteract()
    {
        var customer = Mobile.FocusMob;

        if (Mobile.Combatant != null)
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay($"{Mobile.Combatant.Name} is attacking me");
            }

            Mobile.Say(GetRandomGuardMessage());

            Action = ActionType.Flee;

            return true;
        }

        if (customer?.Deleted != false || customer.Map != Mobile.Map)
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay("My customer have disapeared");
            }

            Mobile.FocusMob = null;

            Action = ActionType.Wander;
        }
        else if (customer.InRange(Mobile, Mobile.RangeFight))
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay($"I am with {customer.Name}");
            }

            Mobile.Direction = Mobile.GetDirectionTo(customer);
        }
        else
        {
            if (Mobile.Debug)
            {
                Mobile.DebugSay($"{customer.Name} is gone");
            }

            Mobile.FocusMob = null;
            Action = ActionType.Wander;
        }

        return true;
    }

    public override bool DoActionGuard()
    {
        Mobile.FocusMob = Mobile.Combatant;
        return base.DoActionGuard();
    }

    public override bool HandlesOnSpeech(Mobile from)
    {
        if (from.InRange(Mobile, 4))
        {
            return true;
        }

        return base.HandlesOnSpeech(from);
    }

    // Temporary
    public override void OnSpeech(SpeechEventArgs e)
    {
        base.OnSpeech(e);

        var from = e.Mobile;

        if (Mobile is BaseVendor vendor && from.InRange(Mobile, Core.AOS ? 1 : 4) && !e.Handled)
        {
            if (e.HasKeyword(0x14D)) // *vendor sell*
            {
                e.Handled = true;

                vendor.VendorSell(from);
                vendor.FocusMob = from;
            }
            else if (e.HasKeyword(0x3C)) // *vendor buy*
            {
                e.Handled = true;

                vendor.VendorBuy(from);
                vendor.FocusMob = from;
            }
            else if (WasNamed(e.Speech))
            {
                if (e.HasKeyword(0x177)) // *sell*
                {
                    e.Handled = true;

                    vendor.VendorSell(from);
                }
                else if (e.HasKeyword(0x171)) // *buy*
                {
                    e.Handled = true;

                    vendor.VendorBuy(from);
                }

                vendor.FocusMob = from;
            }
        }
    }
}
