using System;
namespace L1Specializer.IL
{
	internal class ILBranch : ILInstuction
	{
		public ILBranch ()
		{
		}
		
		#region Properties
		
		public ILExpression Condition {
			get;
			set;
		}
		
		public int SuccessJump {
			get;
			set;
		}
		
		public int FailJump {
			get;
			set;
		}
		
		#endregion
		
		#region Methods
		
		public override string ToString ()
		{
			return String.Format("if {0} then {1} else {2}", Condition, SuccessJump, FailJump);
		}
		
		#endregion
		
	}
}

