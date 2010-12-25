using System;

using L1Specializer;

namespace L1Specializer.IL
{
	internal abstract class ILInstuction
	{
	
		#region Constants
		
		public readonly int LineNotSet = Int16.MaxValue;
		
		#endregion
		
		#region Constructors
		
		public ILInstuction()
		{
			this.Line = LineNotSet;
		}
		
		public ILInstuction(int line)
		{
			this.Line = line;
		}
		
		#endregion
		
		#region Properties
		
		
		public int Line {
			get;
			set;
		}
		
		
		#endregion
		
		#region Methods
	
		public abstract object Execute(ILMachineState state);
		
		#endregion
		
	}
}

