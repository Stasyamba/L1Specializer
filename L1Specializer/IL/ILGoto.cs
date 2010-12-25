using System;
namespace L1Specializer.IL
{
	[Obsolete]
	internal class ILAssignment : ILInstuction
	{

		public ILAssignment ()
		{
		}
		
		
		#region implemented abstract members of L1Specializer.IL.ILInstuction
		
		public override object Execute (ILMachineState state)
		{
			throw new System.NotImplementedException();
		}
		
		#endregion
		
		
	}
}

