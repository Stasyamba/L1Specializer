using System;
using System.Collections.Generic;
using System.Text;

using L1Specializer.Metadata;

namespace L1Specializer.SyntaxTree
{
    internal abstract class Statement
    {

        #region Methods

        /// <summary>
        /// Semanthic analize
        /// </summary>
        /// <returns>Everything was correct?</returns>
        public abstract bool Validate(SymbolTableLight table);

        /// <summary>
        /// Interpritation
        /// </summary>
        public abstract void Execute();

        #endregion

        #region Static constants

        public static readonly Statement Dummy = new DummyStatement();

        #endregion

    }
}
