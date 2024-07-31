namespace Server.ContextMenus
{
    public class OpenBankEntry : ContextMenuEntry
    {
        public OpenBankEntry() : base(6105, 12)
        {
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (!from.CheckAlive() || target is not Mobile banker)
            {
                return;
            }

            if (from.Criminal)
            {
                banker.Say(500378); // Thou art a criminal and cannot access thy bank box.
            }
            else
            {
                from.BankBox.Open();
            }
        }
    }
}
