using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using gppg;

using L1Specializer.Metadata;
using L1Specializer.SyntaxTree;
using L1Specializer.SyntaxTree.IfStatements;

using L1Runtime.SyntaxTree;

namespace L1Specializer
{
    internal class CompilerMessage
    {

        #region Конструктор

        public CompilerMessage(LexLocation location, string message)
        {
            this.Location = location;
            this.Message = message;
        }

        #endregion

        #region Свойства

        private LexLocation f_location;

        public LexLocation Location
        {
            get { return f_location; }
            private set { f_location = value; }
        }

        private string f_message;

        public string Message
        {
            get { return f_message; }
            private set { f_message = value; }
        }
	
        #endregion

        #region Методы Object

        public override string ToString()
        {
            return Location + " " + Message;
        }

        #endregion

    }


    internal static class CompilerServices
    {

        #region Поля

        private static List<CompilerMessage> f_errors = new List<CompilerMessage>();
        private static List<CompilerMessage> f_warnings = new List<CompilerMessage>();

        #endregion

        #region Статический коструктор

        #endregion

        #region Инифиализация стандартных функций

        private static VariableType GetVariableTypeByTypeId(L1Runtime.VariableTypeId typeId)
        {
            if (typeId == L1Runtime.VariableTypeId.Int)
                return VariableType.IntType;
            else if (typeId == L1Runtime.VariableTypeId.Char)
                return VariableType.CharType;
            else if (typeId == L1Runtime.VariableTypeId.Bool)
                return VariableType.BoolType;
            else if (typeId == L1Runtime.VariableTypeId.String)
                return VariableType.StrType;
            else
                return null;

        }

        public static void InitStdFunctions(L1Program program)
        {
            Dictionary<FunctionHeader, MethodInfo> stdlib = new Dictionary<FunctionHeader, MethodInfo>();
            Type runtimeContainer = typeof(L1Runtime.L1Stdlib);

            MethodInfo[] methods = runtimeContainer.GetMethods(BindingFlags.Static | BindingFlags.Public);

            foreach (MethodInfo mi in methods)
            {

                Attribute signatureAttrA = Attribute.GetCustomAttribute(mi, typeof(L1Runtime.SignatureAttribute), false);
                if (signatureAttrA != null)
                {
                    L1Runtime.SignatureAttribute signatureAttr = (L1Runtime.SignatureAttribute)signatureAttrA;
                    FunctionHeader fh = new FunctionHeader(mi.Name, GetVariableTypeByTypeId(signatureAttr.ReturnTypeId));

                    int i = 0;
                    foreach (L1Runtime.VariableTypeId typeId in signatureAttr.ParametersTypeIds)
                    {
                        FunctionParameter p = 
                            new FunctionParameter("a"+i.ToString(), GetVariableTypeByTypeId(typeId));
                        System.Diagnostics.Debug.Assert(p.Type != null);
                        fh.AddParameter(p);
                        i++;
                    }

                    FunctionDefinition fdef = new FunctionDefinition();
                    fdef.Header = fh;
                    fdef.IsEmbedded = true;
                    fdef.Location = new LexLocation();
                    fdef.Statements = new StatementList();

                    program.AddFunctionToForward(fdef);

                    stdlib.Add(fh, mi);
                }
            }

            EmitServices.DefineStandartFunctions(stdlib);
        }

        #endregion

        #region Информация об ошибках

        public static void AddWarning(LexLocation location, string message)
        {
            System.Diagnostics.Debug.Assert(location != null);

            f_warnings.Add(new CompilerMessage(location, message));
        }

        public static void AddError(LexLocation location, string message)
        {
            System.Diagnostics.Debug.Assert(location != null);

            f_errors.Add(new CompilerMessage(location, message));
        }

        #endregion

        #region Семантический анализ

        public static bool IsAssignable(VariableType left, VariableType right)
        {
            if (left == null || right == null)
                return false;

            if (left.TypeEnum == VariableTypeEnum.Integer)
            {
                return (right.TypeEnum == VariableTypeEnum.Integer || right.TypeEnum == VariableTypeEnum.Char);
            }
            else if (left.TypeEnum == VariableTypeEnum.Array && right.TypeEnum == VariableTypeEnum.NULL)
            {
                return true;
            }
            return (left.Equals(right));
        }

        public static bool ValidateFunctionCall(string name, VAList args, SymbolTableLight table, LexLocation location, ref VariableType returnType)
        {
            bool ok = true;
            List<FunctionHeader> callCandidates = new List<FunctionHeader>();

            foreach (FunctionDefinition functionDef in Program.Functions)
            {
                if (functionDef.Header.FunctionName == name && functionDef.Header.ParametersCount == args.Count)
                {
                    callCandidates.Add(functionDef.Header);
                }
            }
            if (callCandidates.Count == 0)
            {
                CompilerServices.AddError(
                    location,
                    "Не найден подходящий прототип функции под заданное имя и число параметров"
                );
                return false;
            }

            foreach (Expression ex in args)
            {
                bool valid = ex.Validate(table);
                ok = ok && valid;
            }

            if (ok)
            {
                foreach (FunctionHeader header in callCandidates)
                {
                    bool match = true;
                    int i = 0;
                    foreach (FunctionParameter parameter in header.Parameters)
                    {
                        if (!IsAssignable(parameter.Type, args[i].ResultType))
                            match = false;
                    }
                    if (match)
                    {
                        returnType = header.ReturnType;
                        return true;
                    }
                }
                CompilerServices.AddError(
                    location,
                    "Не найден подходящий протип функции под переданные параметры"
                );
                return false;
            }

            return ok;
        }

        public static bool HasReturn(StatementList statements)
        {
            foreach (Statement statement in statements)
            {
                if (statement is ReturnStatement)
                    return true;
                if (statement is IfStatement)
                {
                    IfStatement ifStatement = (IfStatement)statement;
                    bool ok = false;
                    if (ifStatement.AlternativeStatements != null)
                    {
                        ok = HasReturn(ifStatement.AlternativeStatements);
                    }
                    if (ok)
                        return ok;
                }
            }
            return false;
        }


        private static FunctionDefinition f_currentFunction = null;

        public static bool ValidateReturnStatement(ReturnStatement returnStatement, SymbolTableLight table)
        {
            bool validReturn = returnStatement.Validate(table);

            if (validReturn && (returnStatement.Expression != null || f_currentFunction.Header.ReturnType != null) && (
                (returnStatement.Expression == null && f_currentFunction.Header.ReturnType != null) ||
                (returnStatement.Expression != null && f_currentFunction.Header.ReturnType == null) ||
                !IsAssignable(f_currentFunction.Header.ReturnType, returnStatement.Expression.ResultType)
                ))
            {
                CompilerServices.AddError(
                    returnStatement.Location,
                    "Тип возвращаемого значения не соотвествует прототипу функции"
                );
                return false;
            }

            return validReturn;
        }

        public static bool ValidateStatementList(StatementList statements, SymbolTableLight table)
        {
            bool ok = true;
            foreach (Statement statement in statements)
            {
                if (statement is ReturnStatement)
                {
                    bool valid = ValidateReturnStatement((ReturnStatement)statement, table);
                    ok = ok && valid;
                }
                else
                {
                    bool valid = statement.Validate(table);
                    ok = ok && valid;
                }
            }
            return ok;
        }

        public static void SemanticAnalise(L1Program program)
        {
            foreach (FunctionDefinition functionDef in program.Functions)
            {
                if (functionDef.IsEmbedded)
                    continue;

                SymbolTableLight table = new SymbolTableLight();
                foreach (FunctionParameter parameter in functionDef.Header.Parameters)
                {
                    SymbolLight symbol = new SymbolLight(parameter.Name, parameter.Type);
                    table.AddSymbol(symbol);
                }

                f_currentFunction = functionDef;

                ValidateStatementList(functionDef.Statements, table);

                if (functionDef.Header.ReturnType != null && !HasReturn(functionDef.Statements))
                {
                    CompilerServices.AddError(
                        functionDef.Location,
                        "Не все ветви выполнения возвращают значение"
                    );
                }
            }
        }

        #endregion

        #region Свойсвта

        private static L1Program f_program = new L1Program();

        public static L1Program Program
        {
            get { return f_program; }
            set { f_program = value; }
        }

        public static List<CompilerMessage> Errors
        {
            get { return f_errors; }
        }

        public static List<CompilerMessage> Warnings
        {
            get { return f_warnings; }
        }

        #endregion

        #region Лексический анализ

        public static int ParseInt(string str)
        {
            try
            {
                string osn = str.Substring(1, str.LastIndexOf('}') - 1);
                ;
                string number = str.Substring(str.LastIndexOf('}') + 1, str.Length - str.LastIndexOf('}') - 1);

                int iosn = int.Parse(osn);

                int num = 0;
                int acc = 1;
                for (int i = number.Length - 1; i >= 0; --i)
                {
                    char c = number[i];

                    int pp = 0;
                    if (c >= '0' && c <= '9')
                        pp = c - '0';
                    else
                        pp = 10 + (c - 'a');

                    num = num + acc * pp;
                    acc = acc * iosn;
                }
                return num;
            }

            catch
            {
                return -1;
            }
        }

        public static char ParseCharFromCode(string str)
        {
            try
            {
                string number = str.Substring(2, str.Length - 3);

                int num = 0;
                int acc = 1;
                for (int i = number.Length - 1; i >= 0; --i)
                {
                    char c = number[i];

                    int pp = 0;
                    if (c >= '0' && c <= '9')
                        pp = c - '0';
                    else
                        pp = 10 + (c - 'a');

                    num = num + acc * pp;
                    acc = acc * 16;
                }


                int code = num;
                char ch = (char)code;
                return ch;
            }
            catch
            {
                return '\0';
            }

        }

        public static char GetCharLiteral(string s)
        {
            if (s.ToLower() == "endl")
                return '\n';
            if (s.ToLower() == "quot")
                return '"';

            //0 - 7

            if (s.ToLower() == "nul")
                return (char)0;
            if (s.ToLower() == "soh")
                return (char)1;
            if (s.ToLower() == "stx")
                return (char)2;
            if (s.ToLower() == "etx")
                return (char)3;
            if (s.ToLower() == "eot")
                return (char)4;
            if (s.ToLower() == "enq")
                return (char)5;
            if (s.ToLower() == "ack")
                return (char)6;
            if (s.ToLower() == "bel")
                return (char)7;

            //8 - 15

            if (s.ToLower() == "bs")
                return (char)8;
            if (s.ToLower() == "tab")
                return (char)9;
            if (s.ToLower() == "lf")
                return (char)10;
            if (s.ToLower() == "vt")
                return (char)11;
            if (s.ToLower() == "ff")
                return (char)12;
            if (s.ToLower() == "cr")
                return (char)13;
            if (s.ToLower() == "so")
                return (char)14;
            if (s.ToLower() == "sl")
                return (char)15;

            //16 - 23

            if (s.ToLower() == "dle")
                return (char)16;
            if (s.ToLower() == "dc1")
                return (char)17;
            if (s.ToLower() == "dc2")
                return (char)18;
            if (s.ToLower() == "dc3")
                return (char)19;
            if (s.ToLower() == "dc4")
                return (char)20;
            if (s.ToLower() == "nak")
                return (char)21;
            if (s.ToLower() == "syn")
                return (char)22;
            if (s.ToLower() == "etb")
                return (char)23;

            //24 - 31

            if (s.ToLower() == "can")
                return (char)24;
            if (s.ToLower() == "em")
                return (char)25;
            if (s.ToLower() == "sub")
                return (char)26;
            if (s.ToLower() == "esc")
                return (char)27;
            if (s.ToLower() == "fs")
                return (char)28;
            if (s.ToLower() == "gs")
                return (char)29;
            if (s.ToLower() == "rs")
                return (char)30;
            if (s.ToLower() == "us")
                return (char)31;


            return '?';
        }

        

        #endregion

    }
}
