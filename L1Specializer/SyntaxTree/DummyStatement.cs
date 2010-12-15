using System;
using System.Collections.Generic;
using System.Text;

namespace L1Specializer.SyntaxTree
{
    internal class DummyStatement : Statement
    {

        #region Base class methods
		
		
        public override bool Validate(L1Specializer.Metadata.SymbolTableLight table)
        {
            return true;
        }

        public override void Execute()
        {
            ;
        }

        #endregion

    }
}
