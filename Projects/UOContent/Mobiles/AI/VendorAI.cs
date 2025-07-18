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
        DebugSay("I'm fine");

        if (_mobile.Combatant != null)
        {
            DebugSay($"{_mobile.Combatant.Name} is attacking me");

            _mobile.Say(GetRandomGuardMessage());
            Action = ActionType.Flee;
        }
        else if (_mobile.FocusMob != null)
        {
            DebugSay($"{_mobile.FocusMob.Name} has talked to me");

            Action = ActionType.Interact;
        }
        else
        {
            _mobile.Warmode = false;

            base.DoActionWander();
        }

        return true;
    }

    public override bool DoActionInteract()
    {
        var customer = _mobile.FocusMob;

        if (_mobile.Combatant != null)
        {
            DebugSay($"{_mobile.Combatant.Name} is attacking me");

            _mobile.Say(GetRandomGuardMessage());

            Action = ActionType.Flee;

            return true;
        }

        if (customer?.Deleted != false || customer.Map != _mobile.Map)
        {
            DebugSay("My customer has disappeared");

            _mobile.FocusMob = null;

            Action = ActionType.Wander;
        }
        else if (customer.InRange(_mobile, _mobile.RangeFight))
        {
            DebugSay($"I am with {customer.Name}");

            _mobile.Direction = _mobile.GetDirectionTo(customer);
        }
        else
        {
            DebugSay($"{customer.Name} is gone");

            _mobile.FocusMob = null;
            Action = ActionType.Wander;
        }

        return true;
    }

    public override bool DoActionGuard()
    {
        _mobile.FocusMob = _mobile.Combatant;
        return base.DoActionGuard();
    }

    public override bool HandlesOnSpeech(Mobile from)
    {
        if (from.InRange(_mobile, 4))
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

        if (_mobile is BaseVendor vendor && from.InRange(_mobile, Core.AOS ? 1 : 4) && !e.Handled)
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
