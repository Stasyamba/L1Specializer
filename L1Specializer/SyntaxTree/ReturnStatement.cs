using System;
using System.Collections.Generic;
using System.Text;
using gppg;

namespace L1Specializer.SyntaxTree
{
    internal class ReturnStatement : Statement
    {

        #region Properties

        private Expression f_expression;

        public Expression Expression
        {
            get { return f_expression; }
            set { f_expression = value; }
        }

        private LexLocation f_location;

        public LexLocation Location
        {
            get { return f_location; }
            set { f_location = value; }
        }
	

        #endregion

        #region Base class methods

        public override bool Validate(L1Specializer.Metadata.SymbolTableLight table)
        {
            if (Expression == null)
                return true;
            bool valid = Expression.Validate(table);
            return valid;
        }

        public override void Execute()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

    }
}
