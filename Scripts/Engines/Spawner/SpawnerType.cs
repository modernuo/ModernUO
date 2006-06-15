using System;
using System.Collections;
using Server;

namespace Server.Mobiles
{
	public class SpawnerType
	{
		public static Type GetType( string name )
		{
			return ScriptCompiler.FindTypeByName( name );
		}
	}
}