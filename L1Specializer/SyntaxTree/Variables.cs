using System;
using System.Collections.Generic;
using System.Text;
using gppg;


using L1Runtime.SyntaxTree;

namespace L1Specializer.SyntaxTree
{

    #region Definition

    internal enum VariableSource
    {
        Local,
        Parameter,
        Ambiguous
    }

    internal class VariableSymbol
    {

        #region Properties

        private VariableSource f_type;

        public VariableSource Type
        {
            get { return f_type; }
            set { f_type = value; }
        }

        private string f_name;

        public string Name
        {
            get { return f_name; }
            set { f_name = value; }
        }

        private VariableType f_variableType;

        public VariableType VariableType
        {
            get { return f_variableType; }
            set { f_variableType = value; }
        }

        private LexLocation f_location;

        public LexLocation Location
        {
            get { return f_location; }
            set { f_location = value; }
        }

        private Expression f_initExpression;

        public Expression InitExpression
        {
            get { return f_initExpression; }
            set { f_initExpression = value; }
        }

        public bool IsReadOnly
        {
            get { return Char.IsUpper(Name[0]); }
        }


        #endregion



    }


    #endregion

    #region DefinitionList

    internal class VariableDefinitionList : Statement
    {

        #region Constructor


        public VariableDefinitionList()
        {
            f_defintions = new List<VariableSymbol>();
        }

        #endregion

        #region Properties

        private List<VariableSymbol> f_defintions;

        public List<VariableSymbol> Definitions
        {
            get { return f_defintions; }
            private set { f_defintions = value; }
        }
	

        #endregion

        #region Methods

        public void Add(VariableSymbol symbol)
        {
            Definitions.Add(symbol);
        }

        #endregion

        #region Base class methods

        public override bool Validate(L1Specializer.Metadata.SymbolTableLight table)
        {
            VariableType type = Definitions[0].VariableType;
            bool ok = true;

            foreach (VariableSymbol symbol in Definitions)
            {
                if (table.TryGetSymbol(symbol.Name) != null)
                {
                    CompilerServices.AddError(
                        symbol.Location,
                        "Variable name dublicate!"
                    );
                    ok = false;
                }
                else
                {
                    bool typeConflict = false;

                    if (symbol.InitExpression != null)
                    {
                        bool validInit = symbol.InitExpression.Validate(table);
                        if (validInit)
                        {
                            if (!CompilerServices.IsAssignable(symbol.VariableType, symbol.InitExpression.ResultType))
                            {
                                typeConflict = true;
                            }
                        }
                    }

                    if (typeConflict)
                    {
                        CompilerServices.AddError(
                            symbol.Location,
                            "Variable initialization expression has different type!"
                        );
                        ok = false;
                    }
                    else
                    {
                        table.AddSymbol(new L1Specializer.Metadata.SymbolLight(symbol.Name, type));
                    }
                }
            }
            return ok;
        }

        public override void Execute()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

    }

    #endregion

}
