using System;
using System.Collections.Generic;
using System.Text;

namespace L1Specializer.Metadata
{
    internal class L1Program
    {

        #region Constructor

        public L1Program()
        {
            f_definitions = new List<FunctionDefinition>();
        }

        #endregion

        #region Properties

        private List<FunctionDefinition> f_definitions;


        public IEnumerable<FunctionDefinition> Functions
        {
            get { return f_definitions; }
        }
	

        #endregion

        #region Methods

        public void AddFunctionToForward(FunctionDefinition definition)
        {
            bool ok = true;
            foreach (FunctionDefinition def in f_definitions)
            {
                if (def.Header.FunctionName == definition.Header.FunctionName)
                {
                    if (def.Header.ReturnType != definition.Header.ReturnType)
                    {
                        CompilerServices.AddError(
                            definition.Location,
                            "Can't overload function by returning value!");
                        ok = false;
                    }
                    else
                    {
                        List<FunctionParameter> p_d2 = new List<FunctionParameter>(def.Header.Parameters);
                        List<FunctionParameter> p_d1 
                            = new List<FunctionParameter>(definition.Header.Parameters);

                        if (p_d1.Count == p_d2.Count)
                        {
                            bool match = true;
                            for (int i = 0; i < p_d1.Count; ++i)
                            {
                                if (!p_d1[i].Type.Equals(p_d2[i].Type))
                                {
                                    match = false;
                                    break;
                                }
                            }
                            if (match)
                            {
                                CompilerServices.AddError(
                                    definition.Location,
                                    "Duplicate function header!");
                                ok = false;
                            }
                        }
                    }
                }
            }
            if (ok)
                f_definitions.Insert(0, definition);
        }
		
        #endregion

    }
}
