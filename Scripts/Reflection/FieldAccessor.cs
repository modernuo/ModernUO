using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Server.Reflection
{
	public sealed class FieldAccessor<V>
	{
		private Type m_Type;
		private FieldInfo m_Field;
		private bool m_FieldEmpty;

		public Type Type { get { return m_Type; } }
		public FieldInfo Field { get { return m_Field; } }
		public bool IsFieldEmpty { get { return m_FieldEmpty; } }

		private FieldGetInvokeHandler<V> m_GetMethodHandler;
		private FieldSetInvokeHandler<V> m_SetMethodHandler;

		public FieldAccessor( Type type, FieldInfo field )
		{
			m_Type = type;
			m_Field = field;

			if ( field == null  )
			{
				m_FieldEmpty = true;
				return;
			}

			m_FieldEmpty = false;
			m_GetMethodHandler = BaseFieldAccessor.GetFieldInvoker<V>( m_Type, field );
			m_SetMethodHandler = BaseFieldAccessor.SetFieldInvoker<V>( m_Type, field );
		}

		public V GetValue( object target )
		{
			if ( m_FieldEmpty )
				return default( V );

			return m_GetMethodHandler( target );
		}

		public void SetValue( object target, V value )
		{
			if ( m_FieldEmpty )
				return;

			m_SetMethodHandler( target, value );
		}
	}
}
