using System;
using System.Collections.Generic;
using System.Text;

using L1Specializer.Metadata;

using L1Runtime.SyntaxTree;

namespace L1Specializer.SyntaxTree.IfStatements
{
    internal class IfStatement : Statement
    {

        #region Constructor

        public IfStatement()
        {
            Clauses = new IfClauseList();
        }

        #endregion

        #region Properties

        private IfClauseList f_clauses;

        public IfClauseList Clauses
        {
            get { return f_clauses; }
            set { f_clauses = value; }
        }

        private StatementList f_alternative;

        public StatementList AlternativeStatements
        {
            get { return f_alternative; }
            set { f_alternative = value; }
        }

        #endregion

        #region Base class method override

        public override bool Validate(SymbolTableLight table)
        {
            SymbolTableLight tableInternal = null;
            bool ok = true;

            foreach (IfClause clause in Clauses)
            {
                tableInternal = new SymbolTableLight(table);

                bool validExpr = clause.Condition.Validate(tableInternal);
                ok = ok && validExpr;

                if (validExpr && clause.Condition.ResultType.TypeEnum != VariableTypeEnum.Bool)
                {
                    CompilerServices.AddError(
                        clause.Condition.Location,
                        "Expression in if statement must be type of bool"
                    );
                    ok = false;
                }

                bool validStatements = CompilerServices.ValidateStatementList(clause.Statements, tableInternal);
                ok = ok && validStatements;
            }

            if (AlternativeStatements != null)
            {
                tableInternal = new SymbolTableLight(table);
                bool validAlternative = CompilerServices.ValidateStatementList(AlternativeStatements, tableInternal);
                ok = ok && validAlternative;
            }

            return ok;
        }

        public override void Execute()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

    }
}
