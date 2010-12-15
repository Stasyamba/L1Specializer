using System;
using System.Collections.Generic;
using System.Text;
using gppg;


using L1Runtime.SyntaxTree;

namespace L1Specializer.SyntaxTree
{
    internal class AssertStatement : Statement
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
            bool valid = Expression.Validate(table);
            if (valid && !(Expression.ResultType.TypeEnum == VariableTypeEnum.Bool))
            {
                CompilerServices.AddError(
                    Location,
                    "Expression under assert must be type of bool"
                );
                valid = false;
            }
            return valid;
        }

        public override void Execute()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

    }
}
