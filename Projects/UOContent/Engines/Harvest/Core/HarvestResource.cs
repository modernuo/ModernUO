using System;

namespace Server.Engines.Harvest
{
    public class HarvestResource
    {
        public HarvestResource(double reqSkill, double minSkill, double maxSkill, TextDefinition message, params Type[] types)
        {
            ReqSkill = reqSkill;
            MinSkill = minSkill;
            MaxSkill = maxSkill;
            Types = types;
            SuccessMessage = message;
        }

        public Type[] Types { get; set; }

        public double ReqSkill { get; set; }

        public double MinSkill { get; set; }

        public double MaxSkill { get; set; }

        public TextDefinition SuccessMessage { get; }

        public void SendSuccessTo(Mobile m)
        {
            if (SuccessMessage != null)
            {
                if (SuccessMessage.Number > 0)
                {
                    m.SendLocalizedMessage(SuccessMessage.Number);
                }
                else
                {
                    m.SendMessage(SuccessMessage.String);
                }
            }
        }
    }
}
