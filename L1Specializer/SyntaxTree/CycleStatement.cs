using System;
using System.Collections.Generic;
using System.Text;

using gppg;

using L1Specializer.Metadata;
using L1Runtime.SyntaxTree;

namespace L1Specializer.SyntaxTree
{
    internal class CycleStatement : Statement
    {

        #region Constructor

        public CycleStatement()
        {
            Step = new Expression();
            Step.IntValue = 1;
            Step.IsLeaf = true;
            Step.LeafType = ExpressionLeafType.Constant;
            Step.ResultType = VariableType.IntType;
            Step.OpType = OperationType.None;

            DeclareVariable = String.Empty;
        }

        #endregion

        #region Properties

        private VariableType f_variableType;

        public VariableType VariableType
        {
            get { return f_variableType; }
            set { f_variableType = value; }
        }
	

        private string f_declareVariable;

        public string DeclareVariable
        {
            get { return f_declareVariable; }
            set { f_declareVariable = value; }
        }
	
        private Expression f_init;

        public Expression Init
        {
            get { return f_init; }
            set { f_init = value; }
        }
	

        private Expression f_endValue;

        public Expression EndValue
        {
            get { return f_endValue; }
            set { f_endValue = value; }
        }

        private Expression f_step;

        public Expression Step
        {
            get { return f_step; }
            set { f_step = value; }
        }

        private StatementList f_statements;

        public StatementList Statements
        {
            get { return f_statements; }
            set { f_statements = value; }
        }

//        private LexLocation f_location;
//
//        public LexLocation Location
//        {
//            get { return f_location; }
//            set { f_location = value; }
//        }
	

        #endregion

        #region Base class methods

        public override bool Validate(L1Specializer.Metadata.SymbolTableLight table)
        {
            if (DeclareVariable != String.Empty)
            {
                if (VariableType.TypeEnum != VariableTypeEnum.Integer &&
                    VariableType.TypeEnum != VariableTypeEnum.Char)
                {
                    CompilerServices.AddError(
                        Location,
                        "Cycle variable must be type of int or char!"
                    );
                    return false;
                }
                if (table.TryGetSymbol(DeclareVariable) != null)
                {
                    CompilerServices.AddError(
                        Location,
                        "Declared cycle variable already exists!"
                    );
                    return false;
                }
                bool valid = Init.Validate(table);
                if (valid)
                {
                    if (!CompilerServices.IsAssignable(VariableType, Init.ResultType))
                    {
                        CompilerServices.AddError(
                            Location,
                            "Cycle variable initialization expression has different type!"
                        );
                        return false;
                    }
                }
                else
                    return false;
            }
            else
            {
                if (Init.LeftNode.LeafType != ExpressionLeafType.VariableAccess)
                {
                    CompilerServices.AddError(
                        Location,
                        "Error in cycle definition: no variable reference presented!"
                    );
                    return false;
                }
                bool valid = Init.Validate(table);
                if (!valid)
                    return false;
            }

            bool validStep = Step.Validate(table);
            bool validEnd = EndValue.Validate(table);

            if (validEnd && validStep)
            {
                if (Step.ResultType.TypeEnum != VariableTypeEnum.Integer)
                {
                    CompilerServices.AddError(
                        Location,
                        "Cycle step must be type of int!"
                    );
                    return false;
                }
                if (EndValue.ResultType.TypeEnum != VariableTypeEnum.Integer
                    && EndValue.ResultType.TypeEnum != VariableTypeEnum.Char)
                {
                    CompilerServices.AddError(
                        Location,
                        "Cycle end value must be type of int or char!"
                    );
                    return false;
                }
            }
            else
                return false;

            SymbolTableLight newTable = new SymbolTableLight(table);

            if (DeclareVariable != String.Empty)
            {
                newTable.AddSymbol(new SymbolLight(DeclareVariable, VariableType));
            }

            bool res = CompilerServices.ValidateStatementList(Statements, newTable);

            return res;
        }

        public override void Execute()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

    }
}
