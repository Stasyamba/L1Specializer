using System;
using System.Collections.Generic;
using System.Text;

using gppg;
using L1Specializer.Metadata;

using L1Runtime.SyntaxTree;

namespace L1Specializer.SyntaxTree
{

    #region WhileDo

    internal class WhileDoStatement : Statement
    {

        #region Properties

        private Expression f_condition;

        public Expression Condition
        {
            get { return f_condition; }
            set { f_condition = value; }
        }

        private StatementList f_statements;

        public StatementList Statements
        {
            get { return f_statements; }
            set { f_statements = value; }
        }

        private LexLocation f_location;

        public LexLocation Location
        {
            get { return f_location; }
            set { f_location = value; }
        }
	
	
        #endregion

        #region Base class methods

        public override bool Validate(SymbolTableLight table)
        {
            SymbolTableLight tableInternal = new SymbolTableLight(table);
            bool valid = Condition.Validate(tableInternal);

            if (valid && Condition.ResultType.TypeEnum != VariableTypeEnum.Bool)
            {
                CompilerServices.AddError(
                    Condition.Location,
                    "WhileDo cycle condition must be type of bool!"
                );
                return false;
            }
            bool validBody = CompilerServices.ValidateStatementList(Statements, tableInternal);
            valid = valid && validBody;

            return valid;
        }

        public override void Execute()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

    }

    #endregion

    #region DoWhile

    internal class DoWhileStatement : Statement
    {

        #region Properties

        private Expression f_condition;

        public Expression Condition
        {
            get { return f_condition; }
            set { f_condition = value; }
        }

        private StatementList f_statements;

        public StatementList Statements
        {
            get { return f_statements; }
            set { f_statements = value; }
        }

        private LexLocation f_location;

        public LexLocation Location
        {
            get { return f_location; }
            set { f_location = value; }
        }

        #endregion

        #region Base class methods

        public override bool Validate(SymbolTableLight table)
        {
            SymbolTableLight tableInternal = new SymbolTableLight(table);
            bool valid = Condition.Validate(tableInternal);

            if (valid && Condition.ResultType.TypeEnum != VariableTypeEnum.Bool)
            {
                CompilerServices.AddError(
                    Condition.Location,
                    "DoWhile cycle condition must be type of bool!"
                );
                return false;
            }

            bool validBody = CompilerServices.ValidateStatementList(Statements, tableInternal);
            valid = valid && validBody;

            return valid;
        }

        public override void Execute()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

    }

    #endregion

}
