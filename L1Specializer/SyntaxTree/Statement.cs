using System;
using System.Collections.Generic;
using System.Text;
using gppg;

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
		
		#region Properties
		
		public string Label {
			get;
			set;
		}
		
		private LexLocation f_location;

        public LexLocation Location
        {
            get { return f_location; }
            set { f_location = value; }
        }
		
		#endregion

        #region Static constants

        public static readonly Statement Dummy = new DummyStatement();

        #endregion

    }
}
