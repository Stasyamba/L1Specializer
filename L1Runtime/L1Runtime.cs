using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using L1Runtime.SyntaxTree;

namespace L1Runtime
{
    public class L1Runtime
    {

        #region RuntimeMethods

        public static void FallIndexOutOfRange()
        {
            Console.WriteLine("OutOfRangeException");
        }

        public static void FallNullReference()
        {
            Console.WriteLine("NullReferenceException");
        }

        public static void FallCriticalError()
        {
            Console.WriteLine("UnknownErrorException");
        }

        public static L1Array<int> GetArrayFromString(string str)
        {
            L1Array<int> array = new L1Array<int>(str.Length);
            for (int i = 0; i < str.Length; ++i)
                array.SetValue(i, Convert.ToInt32(str[i]));
            return array;
        }

        public static string GetStringFromArray(L1Array<int> str)
        {
            int length = str.GetLength();
            StringBuilder sb = new StringBuilder(length);
            for (int i = 0; i < length; ++i)
            {
                int c = str.GetValue(i);
                if (c == (char)0)
                    break;
                sb.Append((char)c);
            }
            return sb.ToString();
        }

        public static void Assert(bool assertation, int line)
        {
            //Console.WriteLine("{0} - {1}", assertation, line);
            if (!assertation)
            {
                Console.WriteLine("Assertation failed");
            }
        }

        public static int Deg(int arg, int deg)
        {
            if (deg < 0)
            {
                FallCriticalError();
                return -1;
            }
            else if (deg == 0)
            {
                return 1;
            }
            else //(deg > 0)
            {
                int res = 1;
                for (int i = 0; i < deg; ++i)
                    res = arg * res;
                return res;
            }
        }

        public static void L1Main(string[] args)
        {
            Type program = null;
            Assembly[] asm = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly a in asm)
            {
                program = a.GetType("L1ProgramFunctions");
                if (program != null)
                    break;
            }

            MethodInfo mi = program.GetMethod("Main", new Type[] { typeof(int), typeof(L1Array<L1Array<int>>) });
            if (mi == null)
            {
                Console.WriteLine("Missing method: Main(int, int array array)");
            }
            else
            {
                L1Array<L1Array<int>> l1args = new L1Array<L1Array<int>>(args.Length);
                for (int i = 0; i < args.Length; ++i)
                { 
                    l1args.SetValue(i, GetArrayFromString(args[i]));
                }

                int code = (int)mi.Invoke(null, new object[] { args.Length, l1args });

                Console.WriteLine();
                Console.WriteLine("End program with code " + code);
            }
        }

        #endregion

    }

    #region Standard types

    public enum VariableTypeId
    {
        Int,
        Char,
        Bool,
        String,
        Void
        //AnyArray
    }

    public class SignatureAttribute : Attribute
    {

        #region Constructor

        public SignatureAttribute(VariableTypeId returnType, params VariableTypeId[] signature)
        {
            f_returnTypeId = returnType;
            f_parametersTypeIds = signature;
        }

        #endregion

        #region Properties

        private VariableTypeId f_returnTypeId;

        public VariableTypeId ReturnTypeId
        {
            get { return f_returnTypeId; }
            private set { f_returnTypeId = value; }
        }

        private VariableTypeId[] f_parametersTypeIds;

        public IEnumerable<VariableTypeId> ParametersTypeIds
        {
            get { return f_parametersTypeIds; }
        }
	
	

        #endregion

    }

    #endregion
	
	#region Specializer specific
	
	public class DynamicResultAttribute : Attribute
	{
	}
	
	#endregion
	
}
