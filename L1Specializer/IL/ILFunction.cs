using System;
using System.Collections.Generic;

using L1Specializer.Environment;

namespace L1Specializer.IL
{
	internal class ILFunction
	{
		
		#region Constructor
		
		public ILFunction ()
		{
		}
		
		#endregion
		
		#region Properties
		
		public string Name {
			get;
			set;
		}
		
		public List<string> Parameters {
			get;
			set;
		}
		
		public List<ILInstuction> Body {
			get;
			set;
		}
		
		
		#endregion
		
		#region Methods
		
		public virtual object Call(object[] arguments)
		{
			if (arguments.Length != Parameters.Count)
			{
				throw new InvalidOperationException("Interpreter bad error: function call with bad number of arguments =(");
			}
			AbstractEnvironment env = new AbstractEnvironment();
			for (int i = 0; i < Parameters.Count; ++i)
			{
				env.SetValue(Parameters[i], arguments[i]);
			}
			
			return null;
		}
		
		#endregion
		
		
	}
}

