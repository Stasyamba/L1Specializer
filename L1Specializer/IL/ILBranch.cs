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
		
		public override object Execute (ILMachineState state)
		{
			//bool ok = (bool)this.Condition.Eval(state);
			//if (ok)
			//	state.InstructionLine = SuccessJump;
			//else
			//	state.InstructionLine = FailJump;
			//return null;
			return null;
		}
		
		#endregion
		
	}
}

