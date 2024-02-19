using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HagStew : BaseAddon
{
    [Constructible]
    public HagStew()
    {
        // stew
        AddComponent(new AddonComponent(2416)
        {
            Name = "stew",
            Visible = true
        }, 0, 0, -7);
    }

    public override void OnComponentUsed(AddonComponent stew, Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.SendMessage("You are too far away.");
        }
        else
        {
            stew.Visible = false;

            var hagstew = new BreadLoaf(); // this decides your fillrate
            hagstew.Eat(from);

            Timer timer = new ShowStew(stew);
            timer.Start();
        }
    }

    public class ShowStew : Timer
    {
        private readonly AddonComponent _stew;

        public ShowStew(AddonComponent ac) : base(TimeSpan.FromSeconds(30)) => _stew = ac;

        protected override void OnTick()
        {
            if (_stew.Visible == false)
            {
                Stop();
                _stew.Visible = true;
            }
        }
    }
}
