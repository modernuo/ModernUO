namespace Server.Commands
{
    public static class DragEffects
    {
        public static void Initialize()
        {
            CommandSystem.Register("DragEffects", AccessLevel.Developer, DragEffects_OnCommand);
        }

        [Usage("DragEffects [enable=false]")]
        [Description("Enables or disables the item drag and drop effects.")]
        public static void DragEffects_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 0)
            {
                if (Mobile.DragEffects)
                {
                    e.Mobile.SendMessage($"Drag effects are currently enabled.");
                }
                else
                {
                    e.Mobile.SendMessage($"Drag effects are currently disabled.");
                }
            }
            else
            {
                Mobile.DragEffects = e.GetBoolean(0);

                if (Mobile.DragEffects)
                {
                    e.Mobile.SendMessage($"Drag effects have been enabled.");
                }
                else
                {
                    e.Mobile.SendMessage($"Drag effects have been disabled.");
                }
            }
        }
    }
}
