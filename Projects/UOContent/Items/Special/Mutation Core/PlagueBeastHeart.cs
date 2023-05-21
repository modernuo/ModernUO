using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class PlagueBeastHeart : PlagueBeastInnard
{
    private Timer _timer;

    public PlagueBeastHeart() : base(0x1363, 0x21)
    {
        _timer = new InternalTimer(this);
        _timer.Start();
    }

    public override void OnAfterDelete()
    {
        if (_timer?.Running == true)
        {
            _timer.Stop();
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        _timer = new InternalTimer(this);
        _timer.Start();
    }

    private class InternalTimer : Timer
    {
        private readonly PlagueBeastHeart _heart;
        private bool _delay;

        public InternalTimer(PlagueBeastHeart heart) : base(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5)) =>
            _heart = heart;

        protected override void OnTick()
        {
            if (_heart?.Deleted != false || _heart.Owner?.Alive != true)
            {
                Stop();
                return;
            }

            if (_heart.ItemID == 0x1363)
            {
                if (_delay)
                {
                    _heart.ItemID = 0x1367;
                    _heart.Owner.PlaySound(0x11F);
                }

                _delay = !_delay;
            }
            else
            {
                _heart.ItemID = 0x1363;
                _heart.Owner.PlaySound(0x120);
                _delay = false;
            }
        }
    }
}
