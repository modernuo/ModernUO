using System;
using System.Collections;

namespace Server.Engines.Reports
{
	public abstract class PersistableObject
	{
		public abstract PersistableType TypeID{ get; }

		public virtual void SerializeAttributes( PersistanceWriter op )
		{
		}

		public virtual void SerializeChildren( PersistanceWriter op )
		{
		}

		public void Serialize( PersistanceWriter op )
		{
			op.BeginObject( this.TypeID );
			SerializeAttributes( op );
			op.BeginChildren();
			SerializeChildren( op );
			op.FinishChildren();
			op.FinishObject();
		}

		public virtual void DeserializeAttributes( PersistanceReader ip )
		{
		}

		public virtual void DeserializeChildren( PersistanceReader ip )
		{
		}

		public void Deserialize( PersistanceReader ip )
		{
			DeserializeAttributes( ip );

			if ( ip.BeginChildren() )
			{
				DeserializeChildren( ip );
				ip.FinishChildren();
			}
		}

		public PersistableObject()
		{
		}
	}
}