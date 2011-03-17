using System;
using System.Collections.Generic;
using System.Reflection;

using L1Specializer.Environment;

using L1Runtime.SyntaxTree;

namespace L1Specializer.IL
{
	internal class ILFunction
	{
		
		#region Constants
		
		public static readonly object VoidReturn = new object();
		
		#endregion
		
		#region Constructor
		
		public ILFunction ()
		{
			Parameters = new List<string>();
			Body = new List<ILInstuction>();
			CanBeCalculatedWithoutRun = true;
			LocalTypes = new Dictionary<string, VariableType>();
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
		
		public VariableType ReturnType {
			get;
			set;
		}
		
		public List<ILInstuction> Body {
			get;
			set;
		}
		
		public MethodInfo EmbeddedBody {
			get;
			set;
		}
		
		public bool CanBeCalculatedWithoutRun {
			get;
			set;
		}
		
		public bool IsVoidReturn {
			get;
			set;
		}
		
		public Dictionary<string, VariableType> LocalTypes {
			get;
			set;
		}
		
		public void AddLocal(string name, VariableType type) {
			if (LocalTypes.ContainsKey(name) == false) {
				LocalTypes.Add(name, type);
			}
		}
			 
		
		
		
		#endregion
		
		#region Methods
		
		public ILInstuction FindInstruction(int line)
		{
			foreach (var inst in Body)
			{
				if (inst.Line == line)
					return inst;
			}
			throw new InvalidOperationException("Interpreter bad error: bad instruction reference");
		}
		
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
			
			int PC = 1;
			var curr = Body[0];			
			while (curr is ILReturn == false)
			{
				curr = Body[PC - 1];
				if (curr is ILExpression)
				{
					object r = (curr as ILExpression).Eval(env);
					if (r == Dynamic.Value)
						throw new InvalidOperationException("Interpreter bad error: try to calculate dynamic function");
					PC++;
				}
				else if (curr is ILBranch)
				{
					object r = (curr as ILBranch).Condition.Eval(env);
					if (r == Dynamic.Value)
						throw new InvalidOperationException("Interpreter bad error: try to calculate dynamic function");
					bool br = (bool)r;
					if (br)
						PC = (curr as ILBranch).SuccessJump;
					else
						PC = (curr as ILBranch).FailJump;
				}
				else if (curr is ILGoto)
				{
					PC = (curr as ILGoto).GoTo;
				}
			}
			object result = (curr as ILReturn).Return.Eval(env);
			if (result == Dynamic.Value)
				throw new InvalidOperationException("Interpreter bad error: try to calculate dynamic function");			
			
			return result;
		}
		
		#endregion
		
		
	}
}

