using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(1, false)]
public partial class TransientItem : Item
{
    private TimerExecutionToken _timerToken;

    [DeltaDateTime]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _expiration;

    [Constructible]
    public TransientItem(int itemID, TimeSpan lifeSpan) : base(itemID)
    {
        _expiration = Core.Now + lifeSpan;

        Timer.StartTimer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), CheckExpiry, out _timerToken);
    }

    public override bool Nontransferable => true;

    public virtual TextDefinition InvalidTransferMessage => TextDefinition.Empty;

    public override void HandleInvalidTransfer(Mobile from)
    {
        InvalidTransferMessage.SendMessageTo(from);

        Delete();
    }

    public virtual void Expire(Mobile parent)
    {
        parent?.SendLocalizedMessage(1072515, Name ?? $"#{LabelNumber}"); // The ~1_name~ expired...

        Effects.PlaySound(GetWorldLocation(), Map, 0x201);

        Delete();
    }

    public virtual void SendTimeRemainingMessage(Mobile to)
    {
        var remaining = Utility.Max(_expiration - Core.Now, TimeSpan.Zero);

        to.SendLocalizedMessage(
            1072516, // ~1_name~ will expire in ~2_val~ seconds!
            $"{Name ?? $"#{LabelNumber}"}\t{remaining.TotalSeconds:F0}"
        );
    }

    public override void OnDelete()
    {
        _timerToken.Cancel();
        base.OnDelete();
    }

    public virtual void CheckExpiry()
    {
        if (_expiration - Core.Now <= TimeSpan.Zero)
        {
            Expire(RootParent as Mobile);
        }
        else
        {
            InvalidateProperties();
        }
    }

    public void ResetExpiration(TimeSpan duration) => Expiration = Core.Now + duration;

    public virtual bool DisplaySeconds => true;

    public override void GetProperties( IPropertyList list )
    {
        base.GetProperties( list );

        var remaining = Utility.Max( _expiration - Core.Now, TimeSpan.Zero );

        if ( _expiration > DateTime.UtcNow )
        {
            if ( DisplaySeconds )
            {
                list.Add( 1072517, $"{remaining.TotalSeconds:F0}" ); // Lifespan: ~1_val~ seconds
            }
            else
            {
                var t = _expiration - DateTime.UtcNow;

                var weeks = t.Days / 7;
                var days = t.Days;
                var hours = t.Hours;
                var minutes = t.Minutes;

                switch ( weeks )
                {
                    case > 1:
                        list.Add( 1153092, ( t.Days / 7 ).ToString() ); // Lifespan: ~1_val~ weeks
                        break;
                    default:
                        switch ( days )
                        {
                            case > 1:
                                list.Add( 1153091, t.Days.ToString() ); // Lifespan: ~1_val~ days
                                break;
                            default:
                                {
                                    if ( hours > 1 )
                                    {
                                        list.Add( 1153090, t.Hours.ToString() ); // Lifespan: ~1_val~ hours
                                    }
                                    else if ( minutes > 1 )
                                    {
                                        list.Add( 1153089, t.Minutes.ToString() ); // Lifespan: ~1_val~ minutes
                                    }
                                    else
                                    {
                                        list.Add( 1072517, t.Seconds.ToString() ); // Lifespan: ~1_val~ seconds
                                    }

                                    break;
                                }
                        }

                        break;
                }
            }
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        var lifespan = reader.ReadTimeSpan();
        var creationTime = reader.ReadDateTime(); // CreationTime

        _expiration = creationTime + lifespan;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Timer.StartTimer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), CheckExpiry, out _timerToken);
    }
}
