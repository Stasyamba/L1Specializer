using System;

using L1Specializer.SyntaxTree;

namespace L1Specializer
{
	internal class GotoStatement : Statement
	{
		public GotoStatement ()
		{
		}
		
		#region Properties
		
		public string GoTo {
			get;
			set;
		}
		
		#endregion
		
		#region implemented abstract members of L1Specializer.SyntaxTree.Statement
		
		public override bool Validate (Metadata.SymbolTableLight table)
		{
			bool ok = CompilerServices.CheckLabel(GoTo);
			if (!ok)
			{
				CompilerServices.AddError(Location, "Label doesn't exists!");
			}
			return ok;
		}
		
		
		public override void Execute ()
		{
			throw new System.NotImplementedException();
		}
		
		#endregion
		
	}
}

