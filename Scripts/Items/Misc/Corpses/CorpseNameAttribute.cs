using System;

namespace Server
{
	[AttributeUsage( AttributeTargets.Class )]
	public class CorpseNameAttribute : Attribute
	{
		public string Name { get; }

		public CorpseNameAttribute( string name )
		{
			Name = name;
		}
	}
}