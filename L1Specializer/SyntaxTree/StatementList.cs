using System;
using System.Collections.Generic;
using System.Text;

namespace L1Specializer.SyntaxTree
{
    internal class StatementList : List<Statement>
    {

        #region Constructors


        public StatementList()
            : base()
        {
        }

        public StatementList(IEnumerable<Statement> collection)
            : base(collection)
        {
        }

        public StatementList(int capacity)
            : base(capacity)
        {
        }

        #endregion

        #region Methods

        public void AddForward(Statement statement)
        {
            Insert(0, statement);
        }

        #endregion

    }
}
