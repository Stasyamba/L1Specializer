using System;
namespace L1Specializer.IL
{
	internal class ILGoto : ILInstuction
	{
		
		#region Constructor

		public ILGoto (int gotoLabel)
		{
			this.GoTo = gotoLabel;
		}
		
		#endregion
		
		#region Properties
		
		public int GoTo {
			get;
			set;
		}
		
		#endregion
		
		#region Methods
		
		public override string ToString ()
		{
			return String.Format("goto {0}", GoTo);
		}
		
		#endregion
		
		
	}
}

