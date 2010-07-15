using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Server.Reflection
{
	public delegate object InvokeHandler( object target, params object[] paramters );

	public delegate T InvokeHandler<T>( object target, params object[] paramters );

	public delegate V FieldGetInvokeHandler<V>( object target );

	public delegate void FieldSetInvokeHandler<V>( object target, V value );

}
