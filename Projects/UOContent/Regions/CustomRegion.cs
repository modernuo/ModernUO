using Server.Spells;
using Server.Spells.Sixth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Regions
{
    public class CustomRegion : GuardedRegion
    {
        private string _enterMessage;
        private string _outMessage;
        private bool _mountsAllowed;
        private bool _resurrectionAllowed;
        private bool _logoutAllowed;
        private bool _housellowed;
        private bool _canMark;
        private bool _travelTo;
        private bool _travelFrom;
        private bool _attackAllowed;
        private bool _castAllowed;
        private Dictionary<string,int> _excludeSpell = new Dictionary<string, int>();

        public override bool ResurrectionAllowed => _resurrectionAllowed;
        public override bool MountsAllowed => _mountsAllowed;
        public override bool LogoutAllowed => _logoutAllowed;

        public override bool AllowHousing(Mobile from, Point3D p) => _housellowed;

        public CustomRegion(string name,
            Map map,
            int priority,
            bool guarded,
            bool mountsAllowed,
            bool resurrectionAllowed,
            bool logoutAllowed,
            bool housellowed,
            bool canMark,
            bool TravelTo,
            bool TravelFrom,
            bool AttackAllowed,
            bool CastAllowed,
            string[] excludeSpell,
            string enterMessage,
            string outMessage,
            Rectangle2D bounds
            ) : base(name, map, priority, bounds)
        {

            Disabled = !guarded;
            _mountsAllowed = mountsAllowed;
            _canMark = canMark;
            _resurrectionAllowed = resurrectionAllowed;
            _logoutAllowed = logoutAllowed;
            _housellowed = housellowed;
            _enterMessage = enterMessage;
            _outMessage = outMessage;
            _travelTo = TravelTo;
            _travelFrom = TravelFrom;
            _attackAllowed = AttackAllowed;
            _castAllowed = CastAllowed;

            for (int i = 0; i < excludeSpell.Length; i++)
            {
                _excludeSpell.Add(excludeSpell[i], 0);
            }
        }

        public override bool OnBeginSpellCast(Mobile m, ISpell s)
        {
            if (_excludeSpell.ContainsKey((s as Spell).Name))
            {
                m.SendLocalizedMessage(502345);
                return false;
            }
            if (!_castAllowed)
                return false;

            return true;
        }
      
        public override bool AllowHarmful(Mobile from, Mobile target)
        {
            if(!_attackAllowed)
                return false;

            return base.AllowHarmful(from, target);
        }

        public override void OnEnter(Mobile m)
        {
            if(!string.IsNullOrEmpty(_enterMessage))
                m.SendMessage(_enterMessage);
        }

        public override void OnExit(Mobile m)
        {
            if (!string.IsNullOrEmpty(_outMessage))
                m.SendMessage(_outMessage);
        }

        public override bool CheckTravel(Mobile m, Point3D newLocation, TravelCheckType travelType)
        {
            if (!_canMark && travelType == TravelCheckType.Mark && m.AccessLevel == AccessLevel.Player)
                return false;

            if (!_travelTo  && (travelType == TravelCheckType.RecallTo || travelType == TravelCheckType.GateTo) && m.AccessLevel == AccessLevel.Player)
                return false;

            if (!_travelFrom && (travelType == TravelCheckType.RecallFrom || travelType == TravelCheckType.GateFrom) && m.AccessLevel == AccessLevel.Player)
                return false;

            return base.CheckTravel(m, newLocation, travelType);
        }

    }
}
