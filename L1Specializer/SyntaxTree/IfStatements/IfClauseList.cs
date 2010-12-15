using System;
using System.Collections.Generic;
using System.Text;

namespace L1Specializer.SyntaxTree.IfStatements
{
    internal class IfClauseList : List<IfClause>
    {
            
        #region Constructor

        public IfClauseList()
            : base()
        {
        }

        public IfClauseList(IEnumerable<IfClause> collection)
            : base(collection)
        {
        }

        public IfClauseList(int capacity)
            : base(capacity)
        {
        }

        #endregion

        #region Methods

        public void AddForward(IfClause clause)
        {
            Insert(0, clause);
        }

        #endregion

    }
}
