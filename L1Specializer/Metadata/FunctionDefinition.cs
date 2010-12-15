using System;
using System.Collections.Generic;
using System.Text;

using gppg;
using L1Specializer.SyntaxTree;

namespace L1Specializer.Metadata
{
    internal class FunctionDefinition
    {

        #region Properties

        private FunctionHeader f_header;

        public FunctionHeader Header
        {
            get { return f_header; }
            set { f_header = value; }
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

        private bool f_isEmbedded;

        public bool IsEmbedded
        {
            get { return f_isEmbedded; }
            set { f_isEmbedded = value; }
        }
	
        #endregion

    }
}
