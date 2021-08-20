namespace Server.Items
{
    [Serializable(0, false)]
    public partial class BlazeDyeTub : DyeTub
    {
        [Constructible]
        public BlazeDyeTub()
        {
            Hue = DyedHue = 0x489;
            Redyable = false;
        }
    }
}
