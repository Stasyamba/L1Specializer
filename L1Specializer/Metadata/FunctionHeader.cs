using System;
using System.Collections.Generic;
using System.Text;

using gppg;
using L1Specializer.SyntaxTree;

using L1Runtime.SyntaxTree;

namespace L1Specializer.Metadata
{

    public class FunctionParameter
    {

        #region Constructor

        public FunctionParameter(string name, VariableType type)
        {
            this.Name = name;
            this.Type = type;
        }

        #endregion

        #region Properties

        private string f_name;

        public string Name
        {
            get { return f_name; }
            private set { f_name = value; }
        }

        private VariableType f_type;

        public VariableType Type
        {
            get { return f_type; }
            private set { f_type = value; }
        }

        private LexLocation f_location;

        public LexLocation Location
        {
            get { return f_location; }
            set { f_location = value; }
        }


        public bool IsReadOnly
        {
            get { return Char.IsUpper(Name[0]); }
        }
	

        #endregion

    }

    public class FunctionHeader
    {

        #region Constructor

        public FunctionHeader(string functionName, VariableType returnType)
        {
            this.FunctionName = functionName;
            this.ReturnType = returnType;
        }

        #endregion

        #region Properties

        private string f_functionName;

        public string FunctionName
        {
            get { return f_functionName; }
            private set { f_functionName = value; }
        }

        private VariableType f_returnType;
        
        public VariableType ReturnType
        {
            get { return f_returnType; }
            private set { f_returnType = value; }
        }


        public int ParametersCount
        {
            get { return f_parameters.Count; }
        }
	

        private List<FunctionParameter> f_parameters = new List<FunctionParameter>();

        public IEnumerable<FunctionParameter> Parameters
        {   
            get { return f_parameters; }
        }

        private LexLocation f_location;

        public LexLocation Location
        {
            get { return f_location; }
            set { f_location = value; }
        }
	
        #endregion

        #region Methods

        public void AddParameter(FunctionParameter parameter)
        {
            foreach (FunctionParameter p in Parameters)
            {
                if (p.Name == parameter.Name)
                    CompilerServices.AddError(parameter.Location, "Duplicate function parameter name");
            }
            f_parameters.Add(parameter);
        }

        #endregion
		
		#region Methods Override
		
		public override string ToString ()
		{
			return FunctionName;
		}
		
		#endregion


    }
}
