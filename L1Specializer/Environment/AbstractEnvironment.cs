using System;
using System.Collections.Generic;

namespace L1Specializer
{
	
	#region Динамическая переменная
	
	internal class Dynamic
	{
		public static readonly Dynamic Value = new Dynamic();
	}
	
	#endregion
	
	internal class AbstractEnvironment : IEquatable<AbstractEnvironment>
	{
		
		#region Constructors
		
		public AbstractEnvironment ()
		{
			f_values = new Dictionary<string, object>();
		}
		
		#endregion
		
		#region Fields
		
		private Dictionary<string, object> f_values;
		
		#endregion
		
		#region Methods
		
		public bool IsDynamic(string name) {
			if (f_values.ContainsKey(name))
				return f_values[name] == Dynamic.Value;
			else
				return false;	
		}

		public object GetValue(string name)
		{
			return f_values[name];
		}
		
		public void SetValue(string name, object val)
		{
			if (f_values.ContainsKey(name))
				f_values[name] = val;
			else
				f_values.Add(name, val);
		}
		
		public void SetArrayAsDynamic(object array)
		{
			foreach (var key in f_values.Keys)
			{
				var val = f_values[key];
				if (val == array)
				{
					f_values[key] = Dynamic.Value;
					return;
				}
				else if (val is Array)
				{
					bool find = SetArrayAsDynamicInArray((Array)val, array);
					if (find)
						return;
				}
			}
			
			Console.WriteLine("Anonymous array considered dynamic =)");
		}
		
		private bool SetArrayAsDynamicInArray(Array search, object array)
		{
			int L = search.GetLength(0);
			for (int i = 0; i < L; ++i)
			{
				var av = search.GetValue(i);
				if (array == av)
				{
					search.SetValue(Dynamic.Value, i);
					return true;
				}
				else if (av is Array)
				{
					bool find = SetArrayAsDynamicInArray((Array)av, array);
					if (find)
						return true;					
				}
			}
			return false;
		}
		
		
		#endregion
		
		#region IEquatable[AbstractEnvironment] 
		
		private bool ValsEquals(object v1, object v2)
		{
			if (v1 == null || v2 == null)
			{
				if (v1 == null && v2 == null)
					return true;
				return false;
			}
			if (v1 is Int32 || v2 is Int32)
			{
				if (v1 is Int32 && v2 is Int32 && (Convert.ToInt32(v1) == Convert.ToInt32(v2)))
					return true;
				return false;
			}
			if (v1 is Boolean || v2 is Boolean)
			{
				if (v1 is Boolean && v2 is Boolean && (Convert.ToBoolean(v1) == Convert.ToBoolean(v2)))
					return true;
				return false;
			}
			if (v1 is Array || v2 is Array)
			{
				if (v1 is Array && v2 is Array && ArrayEquals((Array)v1, (Array)v2))
					return true;
				return false;
			}
			if (v1 == Dynamic.Value || v2 == Dynamic.Value)
			{
				if (v1 == Dynamic.Value && v2 == Dynamic.Value)
					return true;
				return false;
			}
			throw new InvalidOperationException("Something bad occured in specializer =(");
		}
		
		private bool ArrayEquals(Array a1, Array a2)
		{
			int L1 = a1.GetLength(0);
			int L2 = a2.GetLength(0);
			if (L1 != L2)
				return false;
			for (int i = 0; i < L1; ++i)
			{
				object o1 = a1.GetValue(i);
				object o2 = a2.GetValue(i);
				
				bool eq = ValsEquals(o1, o2);
				if (!eq)
					return false;
			}
			return true;
		}
		
		private bool IsDefaultValue(object val) {
			if (val == null)
				return true;
			if (val is Int32 && Convert.ToInt32(val) == 0)
				return true;
			if (val is Boolean && Convert.ToBoolean(val) == false)
				return true;
			
			return false;
		}
		
		public bool Equals (AbstractEnvironment other)
		{
			//New variant - undefined variables and definded with default value are equals
			
			foreach (var varName in this.f_values.Keys) {
				if (other.f_values.ContainsKey(varName)) {
					var eq = ValsEquals(this.f_values[varName], other.f_values[varName]);
					if (!eq) return false;
				}
				else {
					var isDefault = IsDefaultValue(this.f_values[varName]);
					if (!isDefault) return false;
				}
			}
			foreach (var varName in other.f_values.Keys) {
				if (this.f_values.ContainsKey(varName) == false) {
					var isDefault = IsDefaultValue(other.f_values[varName]);
					if (!isDefault) return false;					
				}
			}
			return true;
			
			//Old variant
//			if (f_values.Keys.Count != other.f_values.Keys.Count)
//				return false;
//			foreach (var KV in f_values)
//			{
//				if (other.f_values.ContainsKey(KV.Key) == false)
//					return false;
//				bool eq = ValsEquals(KV.Value, other.f_values[KV.Key]);
//				if (!eq)
//					return false;
//			}
//			return true;
		}
		
		#endregion
		
		#region Debug
		
		public override string ToString ()
		{
			var sb = new System.Text.StringBuilder();
			foreach (var kvp in f_values) {
				sb.Append(kvp.Key); sb.Append(" -> ");
				if (kvp.Value == Dynamic.Value) {
					sb.Append("D");
				} else {
					sb.Append("S");
				}
				sb.Append(System.Environment.NewLine);
			}
			return sb.ToString();
		}
		
		public void Trace() {
			System.Console.WriteLine(ToString());
		}
		
		#endregion
		
		#region Fast compare
		
		private int f_hash = -1;
		
		public void Freeze() {
			//TODO: Calculate complex hash
			f_hash = -1;
		}
		
		public override int GetHashCode ()
		{
			return f_hash;
		}
		
		
		#endregion
		
		#region Clone
		
		public AbstractEnvironment Clone() {
			var newEnv = new AbstractEnvironment();
			
			foreach (var kvp in this.f_values) {
				if (kvp.Value is Array) {
					var clonedArray = m_cloneArray(kvp.Value as Array);
					newEnv.f_values.Add(kvp.Key, clonedArray);
				}
				else {
					var cloned = m_cloneVal(kvp.Value);
					newEnv.f_values.Add(kvp.Key, cloned);
				}
			}
			return	newEnv;
		}
		
		public object m_cloneVal(object val) {
			if (val is Int32 || val is Boolean) {
				var cloned = val;
				return cloned;
			}
			else if (val == Dynamic.Value || val == null) {
				return val;
			}	
			else
				throw new InvalidOperationException("Bad val to clone in AbstactEnv!!!");
		}
		
		public Array m_cloneArray(Array a) {
			int size =  a.GetLength(0);
			Array newArray = Array.CreateInstance(typeof(object), size);
			for (int i = 0; i < size; ++i) {
				object e = a.GetValue(i);
				object cloned = null;
				if (e is Array) {
					cloned = m_cloneArray(e as Array);
				}
				else {
					cloned = m_cloneVal(e);
				}	
				newArray.SetValue(cloned, i);
			}
			return newArray;
		}
		
		
		#endregion
		
	}
}

