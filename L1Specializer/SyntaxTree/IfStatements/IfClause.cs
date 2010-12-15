using System;
using System.Collections.Generic;
using System.Text;

using gppg;
using L1Specializer.SyntaxTree;

namespace L1Specializer.SyntaxTree.IfStatements
{
    internal class IfClause
    {

        #region Properties

        private Expression f_condition;

        public Expression Condition
        {
            get { return f_condition; }
            set { f_condition = value; }
        }

        private StatementList f_statementList;

        public StatementList Statements
        {
            get { return f_statementList; }
            set { f_statementList = value; }
        }

        private LexLocation f_location;

        public LexLocation Location
        {
            get { return f_location; }
            set { f_location = value; }
        }
	

        #endregion

    }
}
