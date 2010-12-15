using System;
using System.Collections.Generic;
using System.Text;

namespace L1Specializer.SyntaxTree
{
    internal class VAList : List<Expression>
    {

        #region Constructors

        public VAList()
            : base()
        {
        }

        public VAList(int capacity)
            : base(capacity)
        {
        }

        public VAList(IEnumerable<Expression> collection)
            : base(collection)
        {
        }

        #endregion

        #region Methods

        public void AddForward(Expression expr)
        {
            Insert(0, expr);
        }

        #endregion

    }
}
