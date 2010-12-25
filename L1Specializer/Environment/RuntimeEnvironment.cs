using System;
using System.Collections.Generic;

namespace L1Specializer.Environment
{
	public class RuntimeEnvironment
	{
		
		#region Constructors
		
		public RuntimeEnvironment()
		{
			f_variables = new Dictionary<string, object>();
		}
		
		public RuntimeEnvironment(RuntimeEnvironment ancestor)
		{
			f_variables = new Dictionary<string, object>();
			foreach (var p in ancestor.f_variables)
			{
				f_variables.Add(p.Key, p.Value);
			}
		}
		
		#endregion
		
		#region Fields
		
		
		private Dictionary<string, object> f_variables;
		
		
		#endregion
		
		#region Methods
		
		
		public void AddSymbol(string name)
		{
			f_variables.Add(name, null);
		}
		
		public void SetValue(string name, object val)
		{
			f_variables[name] = val;
		}
		
		public object GetValiue(string name, object val)
		{
			return f_variables[name];
		}
		
		#endregion
		
		
	}
}

