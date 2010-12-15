using System;
using System.Collections.Generic;
using System.Text;

using System.Reflection.Emit;

namespace L1Specializer.Metadata
{

    internal class Symbol
    {

        #region Constructors

        public Symbol(string name, Type type, int parameterIndex)
        {
            Name = name;
            Type = type;
            IsParameter = true;
            ParameterIndex = parameterIndex;
            LocalBuilder = null;
        }

        public Symbol(string name, Type type, LocalBuilder localBuilder)
        {
            Name = name;
            Type = type;
            IsParameter = false;
            ParameterIndex = -1;
            LocalBuilder = localBuilder;
        }

        #endregion

        #region Properties

        private string f_name;

        public string Name
        {
            get { return f_name; }
            set { f_name = value; }
        }

        private Type f_type;

        public Type Type
        {
            get { return f_type; }
            set { f_type = value; }
        }

        private bool f_isParameter;

        public bool IsParameter
        {
            get { return f_isParameter; }
            set { f_isParameter = value; }
        }

        private int f_paramaterIndex;

        public int ParameterIndex
        {
            get { return f_paramaterIndex; }
            set { f_paramaterIndex = value; }
        }

        private LocalBuilder f_localBuilder;

        public LocalBuilder LocalBuilder
        {
            get { return f_localBuilder; }
            set { f_localBuilder = value; }
        }

        #endregion

    }

    internal class SymbolTable
    {

        #region Constructors

        public SymbolTable()
        {
            f_symbols = new Dictionary<string, Symbol>();
        }

        public SymbolTable(SymbolTable ancestor)
        {
            f_symbols = new Dictionary<string, Symbol>(ancestor.f_symbols);
        }

        #endregion

        #region Fields

        private Dictionary<string, Symbol> f_symbols;

        #endregion

        #region Methods

        public Symbol FindSymbol(string name)
        {
            System.Diagnostics.Debug.Assert(f_symbols.ContainsKey(name));
            return f_symbols[name];
        }

        public void AddSymbol(string name, Type type, int parameterIndex)
        {
            Symbol symbol = new Symbol(name, type, parameterIndex);
            f_symbols.Add(name, symbol);
        }

        public void AddSymbol(string name, Type type, LocalBuilder localBuilder)
        {
            Symbol symbol = new Symbol(name, type, localBuilder);
            f_symbols.Add(name, symbol);
        }

        #endregion

    }
}
