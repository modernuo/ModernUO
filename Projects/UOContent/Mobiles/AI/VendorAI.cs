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

        if (m_Mobile.Combatant != null)
        {
            DebugSay($"{m_Mobile.Combatant.Name} is attacking me");

            m_Mobile.Say(GetRandomGuardMessage());
            Action = ActionType.Flee;
        }
        else if (m_Mobile.FocusMob != null)
        {
            DebugSay($"{m_Mobile.FocusMob.Name} has talked to me");

            Action = ActionType.Interact;
        }
        else
        {
            m_Mobile.Warmode = false;

            base.DoActionWander();
        }

        return true;
    }

    public override bool DoActionInteract()
    {
        var customer = m_Mobile.FocusMob;

        if (m_Mobile.Combatant != null)
        {
            DebugSay($"{m_Mobile.Combatant.Name} is attacking me");

            m_Mobile.Say(GetRandomGuardMessage());

            Action = ActionType.Flee;

            return true;
        }

        if (customer?.Deleted != false || customer.Map != m_Mobile.Map)
        {
            DebugSay("My customer have disapeared");

            m_Mobile.FocusMob = null;

            Action = ActionType.Wander;
        }
        else if (customer.InRange(m_Mobile, m_Mobile.RangeFight))
        {
            DebugSay($"I am with {customer.Name}");

            m_Mobile.Direction = m_Mobile.GetDirectionTo(customer);
        }
        else
        {
            DebugSay($"{customer.Name} is gone");

            m_Mobile.FocusMob = null;
            Action = ActionType.Wander;
        }

        return true;
    }

    public override bool DoActionGuard()
    {
        m_Mobile.FocusMob = m_Mobile.Combatant;
        return base.DoActionGuard();
    }

    public override bool HandlesOnSpeech(Mobile from)
    {
        if (from.InRange(m_Mobile, 4))
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

        if (m_Mobile is BaseVendor vendor && from.InRange(m_Mobile, Core.AOS ? 1 : 4) && !e.Handled)
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
