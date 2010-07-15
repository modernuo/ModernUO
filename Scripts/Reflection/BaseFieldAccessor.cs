using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;

namespace Server.Reflection
{
	public static class BaseFieldAccessor
	{
		public static FieldSetInvokeHandler<V> SetFieldInvoker<V>( Type type, string fieldName )
		{
			FieldInfo field = type.GetField( fieldName );

			if ( field == null )
				return null;

			return SetFieldInvoker<V>( type, field );
		}

		public static FieldGetInvokeHandler<V> GetFieldInvoker<V>( Type type, string fieldName )
		{
			FieldInfo field = type.GetField( fieldName );

			if ( field == null )
				return null;

			return GetFieldInvoker<V>( type, field );
		}

		public static FieldSetInvokeHandler<V> SetFieldInvoker<V>( Type t, FieldInfo field )
		{
			DynamicMethod dm = new DynamicMethod( "Set" + field.Name, null, new Type[] { t, typeof( V ) }, t );
			ILGenerator il = dm.GetILGenerator();

			// Load the instance of the object (argument 0) onto the stack
			il.Emit( OpCodes.Ldarg_0 );
			il.Emit( OpCodes.Ldarg_1 );

			// Load the value of the object's field (fi) onto the stack
			il.Emit( OpCodes.Stfld, field );

			// return the value on the top of the stack
			il.Emit( OpCodes.Ret );

			return (FieldSetInvokeHandler<V>)dm.CreateDelegate( typeof( FieldSetInvokeHandler<V> ) );
		}

		public static FieldGetInvokeHandler<V> GetFieldInvoker<V>( Type t, FieldInfo field )
		{
			// Member is a Field...

			DynamicMethod dm = new DynamicMethod( "Get" + field.Name, typeof( V ), new Type[] { t }, t );
			ILGenerator il = dm.GetILGenerator();

			// Load the instance of the object (argument 0) onto the stack
			il.Emit( OpCodes.Ldarg_0 );
			// Load the value of the object's field (fi) onto the stack
			il.Emit( OpCodes.Ldfld, field );
			// return the value on the top of the stack
			il.Emit( OpCodes.Ret );

			return (FieldGetInvokeHandler<V>)dm.CreateDelegate( typeof( FieldGetInvokeHandler<V> ) );
		}
	}
}
