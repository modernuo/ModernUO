using System;

namespace Server;

[AttributeUsage(AttributeTargets.Class)]
public class CorpseNameAttribute : Attribute
{
    public CorpseNameAttribute(string name) => Name = name;

    public string Name { get; }
}
