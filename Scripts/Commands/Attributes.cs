using System;

namespace Server
{
	public class UsageAttribute : Attribute
	{
		public string Usage { get; }

		public UsageAttribute( string usage )
		{
			Usage = usage;
		}
	}

	public class DescriptionAttribute : Attribute
	{
		public string Description { get; }

		public DescriptionAttribute( string description )
		{
			Description = description;
		}
	}

	public class AliasesAttribute : Attribute
	{
		public string[] Aliases { get; }

		public AliasesAttribute( params string[] aliases )
		{
			Aliases = aliases;
		}
	}
}