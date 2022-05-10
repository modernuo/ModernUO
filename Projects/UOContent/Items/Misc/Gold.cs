using System;
using Server.Accounting;

namespace Server.Items
{
    public class Gold : Item
    {
        [Constructible]
        public Gold(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
        {
        }

        [Constructible]
        public Gold(int amount = 1) : base(0xEED)
        {
            Stackable = true;
            Amount = amount;
        }

        public Gold(Serial serial) : base(serial)
        {
        }

        public override double DefaultWeight => Core.ML ? 0.02 / 3 : 0.02;

        public override int GetDropSound()
        {
            if (Amount <= 1)
            {
                return 0x2E4;
            }

            if (Amount <= 5)
            {
                return 0x2E5;
            }

            return 0x2E6;
        }

        protected override void OnAmountChange(int oldValue)
        {
            var newValue = Amount;

            UpdateTotal(this, TotalType.Gold, newValue - oldValue);
        }

        public override void OnAdded(IEntity parent)
        {
            base.OnAdded(parent);

            if (!AccountGold.Enabled)
            {
                return;
            }

            Mobile owner = null;
            SecureTradeInfo tradeInfo = null;

            var root = parent as Container;

            while (root?.Parent is Container container)
            {
                root = container;
            }

            parent = root ?? parent;

            if (parent is SecureTradeContainer trade && AccountGold.ConvertOnTrade)
            {
                if (trade.Trade.From.Container == trade)
                {
                    tradeInfo = trade.Trade.From;
                    owner = tradeInfo.Mobile;
                }
                else if (trade.Trade.To.Container == trade)
                {
                    tradeInfo = trade.Trade.To;
                    owner = tradeInfo.Mobile;
                }
            }
            else if (parent is BankBox box && AccountGold.ConvertOnBank)
            {
                owner = box.Owner;
            }

            if (owner?.Account?.DepositGold(Amount) != true)
            {
                return;
            }

            if (tradeInfo != null)
            {
                if (owner.NetState?.NewSecureTrading == false)
                {
                    var plat = Math.DivRem(Amount, AccountGold.CurrencyThreshold, out var gold);

                    tradeInfo.Plat += plat;
                    tradeInfo.Gold += gold;
                }

                tradeInfo.VirtualCheck?.UpdateTrade(tradeInfo.Mobile);
            }

            owner.SendLocalizedMessage(1042763, $"{Amount:N0}");

            Delete();

            ((Container)parent).UpdateTotals();
        }

        public override int GetTotal(TotalType type)
        {
            var baseTotal = base.GetTotal(type);

            if (type == TotalType.Gold)
            {
                baseTotal += Amount;
            }

            return baseTotal;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
