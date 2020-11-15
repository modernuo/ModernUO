using System;

namespace Server
{
    [AttributeUsage(AttributeTargets.Method)]
    public class UsageAttribute : Attribute
    {
        public UsageAttribute(string usage) => Usage = usage;

        public string Usage { get; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DescriptionAttribute : Attribute
    {
        public DescriptionAttribute(string description) => Description = description;

        public string Description { get; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class AliasesAttribute : Attribute
    {
        public AliasesAttribute(params string[] aliases) => Aliases = aliases;

        public string[] Aliases { get; }
    }
}
