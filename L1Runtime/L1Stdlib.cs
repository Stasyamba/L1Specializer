using System;
using System.Collections.Generic;
using System.Text;

using L1Runtime.SyntaxTree;

namespace L1Runtime
{
    public class L1Stdlib
    {

        #region Built-in methods

        [Signature(VariableTypeId.Void, VariableTypeId.String)]
        public static void WriteLn(L1Array<char> str)
        {
            string s = L1Runtime.GetStringFromArray(str);
            Console.WriteLine(s);
        }

        [Signature(VariableTypeId.Void, VariableTypeId.String)]
        public static void Write(L1Array<char> str)
        {
            string s = L1Runtime.GetStringFromArray(str);
            Console.Write(s);
        }

        [Signature(VariableTypeId.String, VariableTypeId.Int)]
        public static L1Array<char> Str(int iVal)
        {
            string s = iVal.ToString();
            return L1Runtime.GetArrayFromString(s);
        }

        [Signature(VariableTypeId.String, VariableTypeId.Char)]
        public static L1Array<char> Str(char cVal)
        {
            string s = cVal.ToString();
            return L1Runtime.GetArrayFromString(s);
        }

        [Signature(VariableTypeId.String, VariableTypeId.Bool)]
        public static L1Array<char> Str(bool bVal)
        {
            string s = bVal.ToString();
            return L1Runtime.GetArrayFromString(s);
        }

        [Signature(VariableTypeId.String, VariableTypeId.String, VariableTypeId.String)]
        public static L1Array<char> StrCat(L1Array<char> str1, L1Array<char> str2)
        {
            string s1 = L1Runtime.GetStringFromArray(str1);
            string s2 = L1Runtime.GetStringFromArray(str2);
            return L1Runtime.GetArrayFromString(s1 + s2);
        }

        #endregion

    }
}
