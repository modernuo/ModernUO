using System;

namespace Server.Engines.Events;

[Flags]
public enum AllowedMonths
{
    None = 0,
    January = 1 << 0,
    February = 1 << 1,
    March = 1 << 2,
    April = 1 << 3,
    May = 1 << 4,
    June = 1 << 5,
    July = 1 << 6,
    August = 1 << 7,
    September = 1 << 8,
    October = 1 << 9,
    November = 1 << 10,
    December = 1 << 11,

    All = January | February | March | April | May | June | July | August | September | October | November | December
}
