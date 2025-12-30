using System.Collections.Generic;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Targets;

public class AIControlMobileTarget : Target
{
    private readonly List<BaseAI> _list;

    public AIControlMobileTarget(BaseAI ai, OrderType order) : base(
        -1,
        false,
        order == OrderType.Attack ? TargetFlags.Harmful : TargetFlags.None
    )
    {
        _list = [ai];
        Order = order;
    }

    public OrderType Order { get; }

    public void AddAI(BaseAI ai)
    {
        if (!_list.Contains(ai))
        {
            _list.Add(ai);
        }
    }

    protected override void OnTarget(Mobile from, object o)
    {
        if (o is Mobile m)
        {
            for (var i = 0; i < _list.Count; ++i)
            {
                _list[i].EndPickTarget(from, m, Order);
            }
        }
    }
}
