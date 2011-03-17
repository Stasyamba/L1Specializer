using System;
using System.Collections.Generic;
using System.Text;

using L1Runtime.SyntaxTree;

namespace L1Runtime
{
    public class L1Stdlib
    {

        #region Built-in methods
		
		[DynamicResult]
        [Signature(VariableTypeId.Int)]
        public static int ReadInt()
        {
            int v = Int32.Parse(Console.ReadLine());
			return v;
        }
		
		[DynamicResult]
        [Signature(VariableTypeId.String)]
        public static L1Array<int> ReadLn()
        {
            L1Array<int> result = L1Runtime.GetArrayFromString(Console.ReadLine());
			return result;
        }
		
		[DynamicResult]
        [Signature(VariableTypeId.Void, VariableTypeId.String)]
        public static void WriteLn(L1Array<int> str)
        {
            string s = L1Runtime.GetStringFromArray(str);
            Console.WriteLine(s);
        }
		
		[DynamicResult]
        [Signature(VariableTypeId.Void, VariableTypeId.String)]
        public static void Write(L1Array<int> str)
        {
            string s = L1Runtime.GetStringFromArray(str);
            Console.Write(s);
        }

        [Signature(VariableTypeId.String, VariableTypeId.Int)]
        public static L1Array<int> Str(int iVal)
        {
            string s = iVal.ToString();
            return L1Runtime.GetArrayFromString(s);
        }
		
		[Signature(VariableTypeId.Int, VariableTypeId.Int, VariableTypeId.Int)]
        public static int TestSum(int a1, int a2)
        {
            return a1 + a2;
        }
		
		[Signature(VariableTypeId.Bool, VariableTypeId.Bool, VariableTypeId.Bool)]
        public static bool TestAnd(bool a, bool b)
        {
            return  a && b;
        }

//        [Signature(VariableTypeId.String, VariableTypeId.Char)]
//        public static L1Array<int> Str(char cVal)
//        {
//            string s = cVal.ToString();
//            return L1Runtime.GetArrayFromString(s);
//        }

        [Signature(VariableTypeId.String, VariableTypeId.Bool)]
        public static L1Array<int> Str(bool bVal)
        {
            string s = bVal.ToString();
            return L1Runtime.GetArrayFromString(s);
        }

        [Signature(VariableTypeId.String, VariableTypeId.String, VariableTypeId.String)]
        public static L1Array<int> StrCat(L1Array<int> str1, L1Array<int> str2)
        {
            string s1 = L1Runtime.GetStringFromArray(str1);
            string s2 = L1Runtime.GetStringFromArray(str2);
            return L1Runtime.GetArrayFromString(s1 + s2);
        }

        #endregion

    }
}
