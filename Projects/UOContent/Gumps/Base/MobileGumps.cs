using Server.Network;
using System.Collections.Generic;

namespace Server.Gumps.Base
{
    public readonly ref struct MobileGumps
    {
        private readonly List<BaseGump> _gumps;
        private readonly NetState _state;

        public MobileGumps(List<BaseGump> gumps, NetState state)
        {
            _gumps = gumps;
            _state = state;
        }

        public bool Close<T>() where T : BaseGump
        {
            if (_gumps == null)
            {
                return false;
            }

            for (int i = 0; i < _gumps.Count; i++)
            {
                if (_gumps[i] is T tGump)
                {
                    _state.SendCloseGump(tGump.TypeID, 0);
                    tGump.OnServerClose(_state);

                    _gumps.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public T Find<T>() where T : BaseGump
        {
            if (_gumps == null)
            {
                return null;
            }

            for (int i = 0; i < _gumps.Count; i++)
            {
                if (_gumps[i] is T tGump)
                {
                    return tGump;
                }
            }

            return null;
        }

        public bool Has<T>() where T : BaseGump
        {
            return Find<T>() != null;
        }

        public void Send(BaseGump gump)
        {
            if (_state == null || _state.CannotSendPackets())
            {
                return;
            }

            if (gump.Singleton)
            {
                for (int i = 0; i < _gumps.Count; i++)
                {
                    BaseGump old = _gumps[i];

                    if (old.TypeID == gump.TypeID)
                    {
                        _state.SendCloseGump(old.TypeID, 0);
                        old.OnServerClose(_state);

                        _gumps[i] = gump;
                        gump.SendTo(_state);
                        return;
                    }
                }
            }

            _gumps.Add(gump);
            gump.SendTo(_state);
        }
    }
}
