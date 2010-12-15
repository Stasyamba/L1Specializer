using System;
using System.Collections.Generic;
using System.Text;

using L1Specializer.SyntaxTree;

using L1Runtime.SyntaxTree;

namespace L1Specializer.Metadata
{

    internal class SymbolLight
    {

        #region Constructors

        public SymbolLight()
        {
        }

        public SymbolLight(string symbolName, VariableType type)
        {
            this.Name = symbolName;
            this.Type = type;
        }

        #endregion

        #region Properties

        private string f_name;

        public string Name
        {
            get { return f_name; }
            set { f_name = value; }
        }


        private VariableType f_type;

        public VariableType Type
        {
            get { return f_type; }
            set { f_type = value; }
        }

        private bool f_isReadonly;

        public bool IsReadonly
        {
            get { return f_isReadonly; }
            set { f_isReadonly = value; }
        }

        #endregion


    }

    /// <summary>
    /// Defines symbols, visible in context
    /// </summary>
    internal class SymbolTableLight
    {

        #region Constructors

        public SymbolTableLight()
        {
            f_symbols = new List<SymbolLight>();
        }

        public SymbolTableLight(SymbolTableLight parent)
        {
            f_symbols = new List<SymbolLight>(parent.f_symbols);
        }

        #endregion

        #region Fields

        private List<SymbolLight> f_symbols;

        #endregion

        #region Methods

        public void AddSymbol(SymbolLight symbol)
        {
            f_symbols.Add(symbol);
        }

        public SymbolLight TryGetSymbol(string symbolName)
        {
            foreach (SymbolLight symbol in f_symbols)
            {
                if (symbol.Name == symbolName)
                    return symbol;
            }
            return null;
        }

        #endregion


    }
}
