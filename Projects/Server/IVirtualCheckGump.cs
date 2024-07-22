using Server.Items;

namespace Server.Gumps
{
    public interface IVirtualCheckGump
    {
        public VirtualCheck Check { get; }

        public void Send();
        public void Refresh(bool recompile);
        public void Close();
    }
}
