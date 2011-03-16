using System;
namespace L1Specializer.IL
{
	internal class ILReturn : ILInstuction
	{
		
		#region Constructor
		
		public ILReturn ()
		{
		}
		
		#endregion
		
		#region Properties
		
		public ILExpression Return {
			get;
			set;
		}
		
		#endregion
		
		#region Methods
		
		public override string ToString ()
		{
			return String.Format("return {0}", Return.ToString());
		}
		
		#endregion
		
	}
}

