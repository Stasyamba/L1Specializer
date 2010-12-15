using System;
using System.Collections.Generic;
using System.Text;

using System.Reflection;
using System.Reflection.Emit;

using L1Runtime;
using L1Runtime.SyntaxTree;

using L1Specializer.Metadata;
using L1Specializer.SyntaxTree;
using L1Specializer.SyntaxTree.IfStatements;


namespace L1Specializer
{
    internal static class EmitServices
    {

        #region Methods for code generation from syntax tree

        #region Expression

        private static bool wasVoidFuncCalled = false;

        public static void EmitUnaryExpression(Expression expr, SymbolTable table, ILGenerator ilGen)
        {
            System.Diagnostics.Debug.Assert(expr.IsLeaf);

            #region Const

            if (expr.LeafType == ExpressionLeafType.Constant)
            {
                if (expr.ResultType.TypeEnum == VariableTypeEnum.Integer)
                {
                    ilGen.Emit(OpCodes.Ldc_I4, expr.IntValue);
                }
                if (expr.ResultType.TypeEnum == VariableTypeEnum.Char)
                {
                    ilGen.Emit(OpCodes.Ldc_I4, (int)expr.CharValue);
                }
                if (expr.ResultType.TypeEnum == VariableTypeEnum.Bool)
                {
                    ilGen.Emit(OpCodes.Ldc_I4, Convert.ToInt32(expr.BoolValue));
                }
                if (expr.ResultType.TypeEnum == VariableTypeEnum.Array)
                {
                    string s = expr.Value.ToString();
                    ilGen.Emit(OpCodes.Ldstr, s);
                    ilGen.Emit(OpCodes.Call, GetStrArr());
                }

                System.Diagnostics.Debug.Assert(expr.ResultType.TypeEnum != VariableTypeEnum.NULL);
                //System.Diagnostics.Debug.Assert(expr.ResultType.TypeEnum != VariableTypeEnum.Array);
            }

            #endregion

            #region Variable

            if (expr.LeafType == ExpressionLeafType.VariableAccess)
            {
                Symbol symbol = table.FindSymbol(expr.Value.ToString());
                if (symbol.IsParameter)
                {
                    ilGen.Emit(OpCodes.Ldarg, symbol.ParameterIndex);
                }
                else
                {
                    ilGen.Emit(OpCodes.Ldloc, symbol.LocalBuilder);
                }
            }

            #endregion

            #region FunctionCall

            if (expr.LeafType == ExpressionLeafType.FunctionCall)
            {
                List<VariableType> argumentTypes = new List<VariableType>();
                foreach (Expression e in expr.VAList)
                {
                    Type t = GetTypeForCompilerType(e.ResultType);
                    EmitExpression(e, table, ilGen);
                    argumentTypes.Add(e.ResultType);
                }
                MethodInfo mi = GetMethodForFunctionNameAndArgumentTypes(
                    expr.Value.ToString(), 
                    argumentTypes.ToArray(), 
                    expr.Location);
                ilGen.Emit(OpCodes.Call, mi);

                if (mi.ReturnType == typeof(void))
                    wasVoidFuncCalled = true;
            }

            #endregion

            #region New array

            if (expr.LeafType == ExpressionLeafType.ArrayAlloc)
            {
                Type arrayType = GetTypeForCompilerType(expr.ResultType);
                EmitExpression(expr.LeftNode, table, ilGen);
                ilGen.Emit(OpCodes.Newobj, GetArrayCtor(arrayType));
            }

            #endregion

            #region Array length

            if (expr.LeafType == ExpressionLeafType.ArrayLength)
            {
                Type arrayType = GetTypeForCompilerType(expr.LeftNode.ResultType);
                EmitExpression(expr.LeftNode, table, ilGen);
                ilGen.Emit(OpCodes.Call, GetArrayLengthGetter(arrayType));
            }

            #endregion

        }

        public static void EmitBinaryExpression(Expression expr, SymbolTable table, ILGenerator ilGen)
        {
            System.Diagnostics.Debug.Assert(!expr.IsLeaf);

            #region +

            if (expr.OpType == OperationType.Plus)
            {
                EmitExpression(expr.LeftNode, table, ilGen);
                EmitExpression(expr.RightNode, table, ilGen);
                ilGen.Emit(OpCodes.Add);
                if (expr.LeftNode.ResultType.TypeEnum == VariableTypeEnum.Char ||
                    expr.RightNode.ResultType.TypeEnum == VariableTypeEnum.Char)
                {
                    ilGen.Emit(OpCodes.Conv_U2);
                }
            }

            #endregion

            #region -

            if (expr.OpType == OperationType.Minus)
            {
                EmitExpression(expr.LeftNode, table, ilGen);
                EmitExpression(expr.RightNode, table, ilGen);
                ilGen.Emit(OpCodes.Sub);
                if (expr.LeftNode.ResultType.TypeEnum == VariableTypeEnum.Char)
                {
                    ilGen.Emit(OpCodes.Conv_U2);
                }
            }

            #endregion

            #region **, *, /

            if (expr.OpType == OperationType.Power)
            {
                EmitExpression(expr.LeftNode, table, ilGen);
                EmitExpression(expr.RightNode, table, ilGen);
                ilGen.Emit(OpCodes.Call, GetDeg());
            }

            if (expr.OpType == OperationType.Mult)
            {
                EmitExpression(expr.LeftNode, table, ilGen);
                EmitExpression(expr.RightNode, table, ilGen);
                ilGen.Emit(OpCodes.Mul);
            }

            if (expr.OpType == OperationType.Div)
            {
                EmitExpression(expr.LeftNode, table, ilGen);
                EmitExpression(expr.RightNode, table, ilGen);
                ilGen.Emit(OpCodes.Div);
            }

            #endregion

            #region unary -

            if (expr.OpType == OperationType.UMinus)
            {
                EmitExpression(expr.LeftNode, table, ilGen);
                ilGen.Emit(OpCodes.Neg);
            }

            #endregion

            #region unary not

            if (expr.OpType == OperationType.UNot)
            {
                EmitExpression(expr.LeftNode, table, ilGen);
                ilGen.Emit(OpCodes.Not);
            }

            #endregion

            #region and, or, xor

            if (expr.OpType == OperationType.And)
            {
                Label label = ilGen.DefineLabel();
                EmitExpression(expr.LeftNode, table, ilGen);
                ilGen.Emit(OpCodes.Dup);
                ilGen.Emit(OpCodes.Brfalse, label);
                EmitExpression(expr.RightNode, table, ilGen);
                ilGen.Emit(OpCodes.And);
                ilGen.MarkLabel(label);
            }

            if (expr.OpType == OperationType.Or)
            {
                Label label = ilGen.DefineLabel();
                EmitExpression(expr.LeftNode, table, ilGen);
                ilGen.Emit(OpCodes.Dup);
                ilGen.Emit(OpCodes.Brtrue, label);
                EmitExpression(expr.RightNode, table, ilGen);
                ilGen.Emit(OpCodes.Or);
                ilGen.MarkLabel(label);
            }

            if (expr.OpType == OperationType.Xor)
            {
                EmitExpression(expr.LeftNode, table, ilGen);
                EmitExpression(expr.RightNode, table, ilGen);
                ilGen.Emit(OpCodes.Xor);
            }

            #endregion

            #region <, >, <=, >=

            if (expr.OpType == OperationType.Le)
            {
                EmitExpression(expr.LeftNode, table, ilGen);
                EmitExpression(expr.RightNode, table, ilGen);
                ilGen.Emit(OpCodes.Clt);
            }

            if (expr.OpType == OperationType.Leeq)
            {
                EmitExpression(expr.LeftNode, table, ilGen);
                EmitExpression(expr.RightNode, table, ilGen);
                ilGen.Emit(OpCodes.Cgt);
                ilGen.Emit(OpCodes.Ldc_I4_0);
                ilGen.Emit(OpCodes.Ceq);
            }

            if (expr.OpType == OperationType.Gr)
            {
                EmitExpression(expr.LeftNode, table, ilGen);
                EmitExpression(expr.RightNode, table, ilGen);
                ilGen.Emit(OpCodes.Cgt);
            }

            if (expr.OpType == OperationType.Greq)
            {
                EmitExpression(expr.LeftNode, table, ilGen);
                EmitExpression(expr.RightNode, table, ilGen);
                ilGen.Emit(OpCodes.Clt);
                ilGen.Emit(OpCodes.Ldc_I4_0);
                ilGen.Emit(OpCodes.Ceq);
            }

            #endregion

            #region =, <>

            if (expr.OpType == OperationType.Equals)
            {
                if (expr.LeftNode.ResultType.TypeEnum == VariableTypeEnum.NULL &&
                    expr.RightNode.ResultType.TypeEnum == VariableTypeEnum.NULL)
                {
                    ilGen.Emit(OpCodes.Ldc_I4_1);
                }
                else if (expr.LeftNode.ResultType.TypeEnum == VariableTypeEnum.NULL)
                {
                    Type arrayType = GetTypeForCompilerType(expr.RightNode.ResultType);
                    EmitExpression(expr.RightNode, table, ilGen);
                    ilGen.Emit(OpCodes.Call, GetArrayNullGetter(arrayType));
                    ilGen.Emit(OpCodes.Ceq);
                }
                else if (expr.RightNode.ResultType.TypeEnum == VariableTypeEnum.NULL)
                {
                    Type arrayType = GetTypeForCompilerType(expr.LeftNode.ResultType);
                    EmitExpression(expr.LeftNode, table, ilGen);
                    ilGen.Emit(OpCodes.Call, GetArrayNullGetter(arrayType));
                    ilGen.Emit(OpCodes.Ceq);
                }
                else
                {
                    EmitExpression(expr.LeftNode, table, ilGen);
                    EmitExpression(expr.RightNode, table, ilGen);
                    ilGen.Emit(OpCodes.Ceq);
                }
            }

            if (expr.OpType == OperationType.NotEquals)
            {
                if (expr.LeftNode.ResultType.TypeEnum == VariableTypeEnum.NULL &&
                    expr.RightNode.ResultType.TypeEnum == VariableTypeEnum.NULL)
                {
                    ilGen.Emit(OpCodes.Ldc_I4_1);
                }
                else if (expr.LeftNode.ResultType.TypeEnum == VariableTypeEnum.NULL)
                {
                    Type arrayType = GetTypeForCompilerType(expr.RightNode.ResultType);
                    EmitExpression(expr.RightNode, table, ilGen);
                    ilGen.Emit(OpCodes.Call, GetArrayNullGetter(arrayType));
                    ilGen.Emit(OpCodes.Ceq);
                }
                else if (expr.RightNode.ResultType.TypeEnum == VariableTypeEnum.NULL)
                {
                    Type arrayType = GetTypeForCompilerType(expr.LeftNode.ResultType);
                    EmitExpression(expr.LeftNode, table, ilGen);
                    ilGen.Emit(OpCodes.Call, GetArrayNullGetter(arrayType));
                    ilGen.Emit(OpCodes.Ceq);
                }
                else
                {
                    EmitExpression(expr.LeftNode, table, ilGen);
                    EmitExpression(expr.RightNode, table, ilGen);
                    ilGen.Emit(OpCodes.Ceq);
                }
                ilGen.Emit(OpCodes.Ldc_I4_0);
                ilGen.Emit(OpCodes.Ceq);
            }


            #endregion

            #region []

            if (expr.OpType == OperationType.ArrayAccess)
            {
                Type arrayType = GetTypeForCompilerType(expr.LeftNode.ResultType);
                EmitExpression(expr.LeftNode, table, ilGen);
                EmitExpression(expr.RightNode, table, ilGen);
                ilGen.Emit(OpCodes.Call, GetArrayGetter(arrayType));
            }

            #endregion

            #region Assign
             
            if (expr.OpType == OperationType.Assign)
            {
                if (expr.LeftNode.OpType == OperationType.ArrayAccess)
                {
                    Type arrayType = GetTypeForCompilerType(expr.LeftNode.LeftNode.ResultType);
                    //Array
                    EmitExpression(expr.LeftNode.LeftNode, table, ilGen);
                    //Index
                    EmitExpression(expr.LeftNode.RightNode, table, ilGen);
                    //Value
                    EmitExpression(expr.RightNode, table, ilGen);
                    //Setter call
                    ilGen.Emit(OpCodes.Call, GetArraySetter(arrayType));
                    //Banditism: after setter call - there is specified value on the stack's top
                }
                else if (expr.LeftNode.LeafType == ExpressionLeafType.VariableAccess)
                {
                    Symbol symbol = table.FindSymbol(expr.LeftNode.Value.ToString());
                    //Reight part
                    if (expr.RightNode.ResultType.TypeEnum == VariableTypeEnum.NULL)
                    {
                        ilGen.Emit(OpCodes.Call, GetArrayNullGetter(symbol.Type));
                        ilGen.Emit(OpCodes.Dup);
                    }
                    else
                    {
                        EmitExpression(expr.RightNode, table, ilGen);
                        ilGen.Emit(OpCodes.Dup);
                    }
                    //Setter for variable
                    if (symbol.IsParameter)
                    {
                        ilGen.Emit(OpCodes.Starg, symbol.ParameterIndex);
                    }
                    else
                    {
                        ilGen.Emit(OpCodes.Stloc, symbol.LocalBuilder);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Fail("Bad compiler error: bad assign construction");
                }
            }

            #endregion
        }

        public static void EmitExpression(Expression expr, SymbolTable table, ILGenerator ilGen)
        {
            if (expr.IsLeaf)
            {
                EmitUnaryExpression(expr, table, ilGen);
            }
            else
            {
                EmitBinaryExpression(expr, table, ilGen);
            }
        }

        #endregion

        #region Function

        public static void EmitFunction(FunctionDefinition fDef, SymbolTable table, ILGenerator ilGen)
        {
            EmitStatementList(fDef.Statements, table, ilGen);
            if (fDef.Header.ReturnType == null)
                ;
            else if (fDef.Header.ReturnType.TypeEnum != VariableTypeEnum.Array)
            {
                ilGen.Emit(OpCodes.Ldc_I4_0);
            }
            else
            {
                ilGen.Emit(OpCodes.Ldnull);
            }
            ilGen.Emit(OpCodes.Ret);
        }

        public static void EmitStatementList(StatementList statements, SymbolTable table, ILGenerator ilGen)
        {
            foreach (Statement statement in statements)
            {

                #region Expression

                if (statement is Expression)
                {
                    Expression expr = (Expression)statement;

                    EmitExpression(expr, table, ilGen);

                    if (!wasVoidFuncCalled)
                    {
                        ilGen.Emit(OpCodes.Pop);
                    }
                    wasVoidFuncCalled = false;
                }

                #endregion

                #region VariableDefinitionList

                if (statement is VariableDefinitionList)
                {
                    VariableDefinitionList vdList = (VariableDefinitionList)statement;

                    foreach (VariableSymbol vSymbol in vdList.Definitions)
                    {
                        EmitVariableDefinition(vSymbol, table, ilGen);
                    }
                }

                #endregion

                #region Return

                if (statement is ReturnStatement)
                {
                    ReturnStatement returnStatement = (ReturnStatement)statement;

                    EmitReturn(returnStatement, table, ilGen);
                }

                #endregion

                #region Assert

                if (statement is AssertStatement)
                {
                    AssertStatement assertStatement = (AssertStatement)statement;

                    EmitExpression(assertStatement.Expression, table, ilGen);
                    ilGen.Emit(OpCodes.Ldc_I4, assertStatement.Expression.Location.eLin);
                    ilGen.Emit(OpCodes.Call, GetAssert());
                }

                #endregion

                #region WhileDo

                if (statement is WhileDoStatement)
                {
                    WhileDoStatement wds = (WhileDoStatement)statement;
                    SymbolTable newTable = new SymbolTable(table);
                    Label condition = ilGen.DefineLabel();
                    Label endLoop = ilGen.DefineLabel();

                    ilGen.MarkLabel(condition);
                    EmitExpression(wds.Condition, table, ilGen);
                    ilGen.Emit(OpCodes.Brfalse, endLoop);
                    EmitStatementList(wds.Statements, newTable, ilGen);
                    ilGen.Emit(OpCodes.Br, condition);
                    ilGen.MarkLabel(endLoop);
                }

                #endregion

                #region DoWhile

                if (statement is DoWhileStatement)
                {
                    DoWhileStatement dws = (DoWhileStatement)statement;
                    SymbolTable newTable = new SymbolTable(table);
                    Label begin = ilGen.DefineLabel();

                    ilGen.MarkLabel(begin);
                    EmitStatementList(dws.Statements, newTable, ilGen);
                    EmitExpression(dws.Condition, table, ilGen);
                    ilGen.Emit(OpCodes.Brtrue, begin);
                }

                #endregion

                #region If

                if (statement is IfStatement)
                {
                    IfStatement ifStatement = (IfStatement)statement;
                    Label end = ilGen.DefineLabel();
                    Label elseLabel = ilGen.DefineLabel();
                    List<Label> labels = new List<Label>();
                    labels.Add(new Label());
                    for (int i = 1; i < ifStatement.Clauses.Count; ++i)
                    {
                        labels.Add(ilGen.DefineLabel());
                    }
                    labels.Add(elseLabel);

                    for (int i = 0; i < ifStatement.Clauses.Count; ++i)
                    {
                        SymbolTable newTable = new SymbolTable(table);
                        if (i != 0)
                            ilGen.MarkLabel(labels[i]);

                        EmitExpression(ifStatement.Clauses[i].Condition, table, ilGen);
                        ilGen.Emit(OpCodes.Brfalse, labels[i + 1]);
                        EmitStatementList(ifStatement.Clauses[i].Statements, newTable, ilGen);
                        ilGen.Emit(OpCodes.Br, end);
                    }
                    ilGen.MarkLabel(elseLabel);
                    if (ifStatement.AlternativeStatements != null)
                    {
                        SymbolTable newTable = new SymbolTable(table);
                        EmitStatementList(ifStatement.AlternativeStatements, newTable, ilGen);
                    }
                    ilGen.MarkLabel(end);
                }

                #endregion

                #region СycleFor

                if (statement is CycleStatement)
                {
                    CycleStatement cycle = (CycleStatement)statement;
                    SymbolTable newTable = new SymbolTable(table);

                    Symbol cycleIndex = null;

                    if (cycle.DeclareVariable != String.Empty)
                    {
                        Type localType = GetTypeForCompilerType(cycle.VariableType);
                        LocalBuilder indx = ilGen.DeclareLocal(localType);
                        newTable.AddSymbol(cycle.DeclareVariable, localType, indx);
                        cycleIndex = newTable.FindSymbol(cycle.DeclareVariable);
                    }
                    else
                    {
                        string indexName = cycle.Init.LeftNode.Value.ToString();
                        Symbol symbol = table.FindSymbol(indexName);
                        cycleIndex = symbol;
                    }

                    LocalBuilder end = ilGen.DeclareLocal(typeof(int));
                    LocalBuilder step = ilGen.DeclareLocal(typeof(int));

                    Label loopLabel = ilGen.DefineLabel();
                    Label endLabel = ilGen.DefineLabel();

					//Save cycle step and end condition to local variables
                    EmitExpression(cycle.Step, table, ilGen);
                    ilGen.Emit(OpCodes.Stloc, step);
                    EmitExpression(cycle.EndValue, table, ilGen);
                    ilGen.Emit(OpCodes.Stloc, end);

                    //Init variable
					
                    if (cycle.DeclareVariable == String.Empty)
                    {
                        EmitExpression(cycle.Init, table, ilGen);
                        ilGen.Emit(OpCodes.Pop);
                    }
                    else
                    {
                        EmitExpression(cycle.Init, table, ilGen);
                        ilGen.Emit(OpCodes.Stloc, cycleIndex.LocalBuilder);
                    }

                    ilGen.MarkLabel(loopLabel);

                    //Check for cycle' condition

                    if (cycleIndex.IsParameter)
                        ilGen.Emit(OpCodes.Ldarg, cycleIndex.ParameterIndex);
                    else
                        ilGen.Emit(OpCodes.Ldloc, cycleIndex.LocalBuilder);
                    ilGen.Emit(OpCodes.Ldloc, end);
                    ilGen.Emit(OpCodes.Cgt);
                    ilGen.Emit(OpCodes.Brtrue, endLabel);

                    //Cycle body

                    EmitStatementList(cycle.Statements, newTable, ilGen);

                    //Increment

                    if (cycleIndex.IsParameter)
                        ilGen.Emit(OpCodes.Ldarg, cycleIndex.ParameterIndex);
                    else
                        ilGen.Emit(OpCodes.Ldloc, cycleIndex.LocalBuilder);
                    ilGen.Emit(OpCodes.Ldloc, step);
                    ilGen.Emit(OpCodes.Add);
                    if (cycleIndex.Type == typeof(char))
                        ilGen.Emit(OpCodes.Conv_U2);
                    if (cycleIndex.IsParameter)
                        ilGen.Emit(OpCodes.Starg, cycleIndex.ParameterIndex);
                    else
                        ilGen.Emit(OpCodes.Stloc, cycleIndex.LocalBuilder);

                    ilGen.Emit(OpCodes.Br, loopLabel);

                    ilGen.MarkLabel(endLabel);

                }

                #endregion

            }
        }

        public static void EmitVariableDefinition(VariableSymbol vSymbol, SymbolTable table, ILGenerator ilGen)
        {
            Type localType = GetTypeForCompilerType(vSymbol.VariableType);

            LocalBuilder localBuilder = ilGen.DeclareLocal(localType);
            if (vSymbol.InitExpression != null)
            {
                if (vSymbol.InitExpression.ResultType.TypeEnum == VariableTypeEnum.NULL)
                {
                    Type arrayType = GetTypeForCompilerType(vSymbol.VariableType);
                    ilGen.Emit(OpCodes.Call, GetArrayNullGetter(arrayType));
                }
                else
                {
                    EmitExpression(vSymbol.InitExpression, table, ilGen);
                }
            }
            else
            {
                if (vSymbol.VariableType.TypeEnum == VariableTypeEnum.Array)
                {
                    MethodInfo mi = GetArrayNullGetter(localType);
                    ilGen.Emit(OpCodes.Call, mi);
                }
                else
                {
                    ilGen.Emit(OpCodes.Ldc_I4_0);
                }
            }
            ilGen.Emit(OpCodes.Stloc, localBuilder);

            table.AddSymbol(vSymbol.Name, localType, localBuilder);
        }

        public static void EmitReturn(ReturnStatement returnStatement, SymbolTable table, ILGenerator ilGen)
        {
            if (returnStatement.Expression == null)
            {
                ilGen.Emit(OpCodes.Ret);
            }
            else
            {
                EmitExpression(returnStatement.Expression, table, ilGen);
                ilGen.Emit(OpCodes.Ret);
            }
        }

        #endregion

        #endregion

        #region Methods (main)

        private static MethodInfo f_mainRuntimeMethod;

        static EmitServices()
        {
            f_mainRuntimeMethod = typeof(L1Runtime.L1Runtime).GetMethod("L1Main", BindingFlags.Public | BindingFlags.Static);
        }

        public static AssemblyBuilder GenerateAssembly(string name, L1Program program)
        {
            AssemblyName assemblyName = new AssemblyName(name);
            AssemblyBuilder assemblyBuilder 
                = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.ToString(), assemblyName + ".exe");

            //Main method

            TypeBuilder mainTypeBuilder = moduleBuilder.DefineType("L1ProgramMain", TypeAttributes.Class | TypeAttributes.Public);
            MethodBuilder mainMethodBuilder = mainTypeBuilder.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static);
            mainMethodBuilder.SetParameters(typeof(string[]));
            ILGenerator mainIlGenerator = mainMethodBuilder.GetILGenerator();

            mainIlGenerator.Emit(OpCodes.Ldarg_0);
            mainIlGenerator.Emit(OpCodes.Call, f_mainRuntimeMethod);
            mainIlGenerator.Emit(OpCodes.Ret);

            //Program methods

            TypeBuilder functionsTypeBuilder = moduleBuilder.DefineType("L1ProgramFunctions", TypeAttributes.Class | TypeAttributes.Public);
            List<MethodBuilder> functionBuilders = new List<MethodBuilder>();

            foreach (FunctionDefinition fDef in program.Functions)
            {
                if (fDef.IsEmbedded)
                    continue;

                MethodBuilder functionBuilder = functionsTypeBuilder.DefineMethod(fDef.Header.FunctionName, MethodAttributes.Public | MethodAttributes.Static);
                if (fDef.Header.ReturnType != null)
                {
                    Type t = GetTypeForCompilerType(fDef.Header.ReturnType);
                    functionBuilder.SetReturnType(t);
                }
                else
                {
                    functionBuilder.SetReturnType(typeof(void));
                }
                List<Type> paramTypes = new List<Type>();
                foreach (FunctionParameter parameter in fDef.Header.Parameters)
                {
                    Type t = GetTypeForCompilerType(parameter.Type);
                    paramTypes.Add(t);
                }
                functionBuilder.SetParameters(paramTypes.ToArray());

                f_functions.Add(fDef.Header, functionBuilder);
            }

            foreach (FunctionDefinition fDef in program.Functions)
            {
                if (fDef.IsEmbedded)
                    continue;
                MethodBuilder functionBuilder = (MethodBuilder)f_functions[fDef.Header];
                ILGenerator ilGen = functionBuilder.GetILGenerator();
                SymbolTable table = new SymbolTable();
                int i = 0;
                foreach (FunctionParameter parameter in fDef.Header.Parameters)
                {
                    Type t = GetTypeForCompilerType(parameter.Type);
                    table.AddSymbol(parameter.Name, t, i++);
                }

                EmitFunction(fDef, table, ilGen);
            }

            //Завершение процесса генерации

            Type tMain = mainTypeBuilder.CreateType();
            Type tFunctions = functionsTypeBuilder.CreateType();

            moduleBuilder.CreateGlobalFunctions();
            assemblyBuilder.SetEntryPoint(mainMethodBuilder, PEFileKinds.ConsoleApplication);
            
            return assemblyBuilder;
        }

        #endregion

        #region Support methods

        #region Support of built-in methods

        private static MethodInfo f_assert = null;

        public static MethodInfo GetAssert()
        {
            if (f_assert != null)
                return f_assert;
            Type runtime = typeof(L1Runtime.L1Runtime);
            MethodInfo mi = runtime.GetMethod("Assert", BindingFlags.Public | BindingFlags.Static);
            System.Diagnostics.Debug.Assert(mi != null);
            f_assert = mi;
            return mi;
        }

        private static MethodInfo f_strArr = null;

        public static MethodInfo GetStrArr()
        {
            if (f_strArr != null)
                return f_strArr;
            Type runtime = typeof(L1Runtime.L1Runtime);
            MethodInfo mi = runtime.GetMethod("GetArrayFromString", BindingFlags.Public | BindingFlags.Static);
            System.Diagnostics.Debug.Assert(mi != null);
            f_strArr = mi;
            return mi;
        }

        private static MethodInfo f_deg = null;

        public static MethodInfo GetDeg()
        {
            if (f_deg != null)
                return f_deg;
            Type runtime = typeof(L1Runtime.L1Runtime);
            MethodInfo mi = runtime.GetMethod("Deg", BindingFlags.Public | BindingFlags.Static);
            System.Diagnostics.Debug.Assert(mi != null);
            f_deg = mi;
            return mi;
        }

        #endregion

        #region Type support

        /// <summary>
        /// Get runtime type for complirer's type
        /// </summary>
        public static Type GetTypeForCompilerType(VariableType compilerType)
        {
            //System.Diagnostics.Debug.Assert(compilerType.TypeEnum == VariableTypeEnum.Array);

            if (compilerType.TypeEnum == VariableTypeEnum.Integer)
                return typeof(int);
            if (compilerType.TypeEnum == VariableTypeEnum.Char)
                return typeof(char);
            if (compilerType.TypeEnum == VariableTypeEnum.Bool)
                return typeof(bool);

            Type root = typeof(L1Array<>);

            int arrayDepth = 0;
            VariableType curr = compilerType;
            VariableTypeEnum en = curr.TypeEnum;
            while (en == VariableTypeEnum.Array)
            {
                curr = curr.NestedType;
                en = curr.TypeEnum;
                arrayDepth++;
            }

            Type t = null;
            if (en == VariableTypeEnum.Bool)
                t = typeof(bool);
            else if (en == VariableTypeEnum.Char)
                t = typeof(char);
            else if (en == VariableTypeEnum.Integer)
                t = typeof(int);

            while (arrayDepth > 0)
            {
                t = root.MakeGenericType(t);
                arrayDepth--;
            }

            return t;
        }

        #endregion

        #region Array support

        private static Dictionary<Type, MethodInfo> f_lenGetters = new Dictionary<Type, MethodInfo>();

        /// <summary>
        /// Get array length method
        /// </summary>
        public static MethodInfo GetArrayLengthGetter(Type arrayType)
        {
            if (f_lenGetters.ContainsKey(arrayType))
                return f_lenGetters[arrayType];
            MethodInfo mi = arrayType.GetMethod("GetLength");
            System.Diagnostics.Debug.Assert(mi != null);
            f_lenGetters.Add(arrayType, mi);
            return mi;
        }

        private static Dictionary<Type, MethodInfo> f_getters = new Dictionary<Type, MethodInfo>();

        /// <summary>
        /// Get array getter method
        /// </summary>
        public static MethodInfo GetArrayGetter(Type arrayType)
        {
            if (f_getters.ContainsKey(arrayType))
                return f_getters[arrayType];
            MethodInfo mi = arrayType.GetMethod("GetValue");
            System.Diagnostics.Debug.Assert(mi != null);
            f_getters.Add(arrayType, mi);
            return mi;
        }

        private static Dictionary<Type, MethodInfo> f_setters = new Dictionary<Type, MethodInfo>();

        /// <summary>
        /// Get array setter method
        /// </summary>
        public static MethodInfo GetArraySetter(Type arrayType)
        {
            if (f_setters.ContainsKey(arrayType))
                return f_setters[arrayType];
            MethodInfo mi = arrayType.GetMethod("SetValue");
            System.Diagnostics.Debug.Assert(mi != null);
            f_setters.Add(arrayType, mi);
            return mi;
        }

        public static Dictionary<Type, ConstructorInfo> f_ctors = new Dictionary<Type, ConstructorInfo>();

        /// <summary>
        /// Get array constructor method
        /// </summary>
        public static ConstructorInfo GetArrayCtor(Type arrayType)
        {
            if (f_ctors.ContainsKey(arrayType))
                return f_ctors[arrayType];
            ConstructorInfo mi = arrayType.GetConstructor(new Type[] { typeof(int) });
            System.Diagnostics.Debug.Assert(mi != null);
            f_ctors.Add(arrayType, mi);
            return mi;
        }

        public static Dictionary<Type, MethodInfo> f_getNullMethods = new Dictionary<Type, MethodInfo>();

        /// <summary>
        /// Get method for getting array null instance
        /// </summary>
        /// <param name="arrayType">Array runtime type</param>
        /// <returns></returns>
        public static MethodInfo GetArrayNullGetter(Type arrayType)
        {
            if (f_getNullMethods.ContainsKey(arrayType))
                return f_getNullMethods[arrayType];
            MethodInfo mi = arrayType.GetMethod("GetNullInstance", BindingFlags.Static | BindingFlags.Public);
            System.Diagnostics.Debug.Assert(mi != null);
            f_getNullMethods.Add(arrayType, mi);
            return mi;
        }

        #endregion

        #region Function support

        private static Dictionary<FunctionHeader, MethodInfo> f_functions = new Dictionary<FunctionHeader, MethodInfo>();

        public static void DefineStandartFunctions(Dictionary<FunctionHeader, MethodInfo> stdlib)
        {
            foreach (KeyValuePair<FunctionHeader, MethodInfo> pair in stdlib)
            {
                f_functions.Add(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Get method for specified argument types
        /// </summary>
        /// <param name="argumentTypes">Argument types</param>
        public static MethodInfo GetMethodForFunctionNameAndArgumentTypes(
            string functionName, 
            VariableType[] argumentTypes, 
            gppg.LexLocation callLocation
            )
        {
            List<MethodInfo> callCandidates = new List<MethodInfo>();
            List<MethodInfo> weakCallCandidates = new List<MethodInfo>();
            List<int> unmatchesCount = new List<int>();


            foreach (FunctionHeader fh in f_functions.Keys)
            {
                if (fh.FunctionName == functionName && fh.ParametersCount == argumentTypes.Length)
                {
                    int i = 0;
                    bool match = true;
                    foreach (FunctionParameter p in fh.Parameters)
                    {
                        if (p.Type.Equals(argumentTypes[i]) ||
                            (p.Type.TypeEnum == VariableTypeEnum.Array && argumentTypes[i].TypeEnum == VariableTypeEnum.NULL))
                        {
                            continue;
                        }
                        else
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                        callCandidates.Add(f_functions[fh]);
                    if (!match)
                    {
                        match = true;
                        i = 0;
                        int umCount = 0;
                        foreach (FunctionParameter p in fh.Parameters)
                        {
                            if (CompilerServices.IsAssignable(argumentTypes[i], p.Type))
                            {
                                if (argumentTypes[i].TypeEnum == VariableTypeEnum.Integer && p.Type.TypeEnum == VariableTypeEnum.Char)
                                {
                                    umCount++;
                                }
                                continue;
                            }
                            else
                            {
                                match = false;
                                break;
                            }
                        }
                        if (match)
                        {
                            weakCallCandidates.Add(f_functions[fh]);
                            unmatchesCount.Add(umCount);
                        }
                    }
                }
            }

            if (callCandidates.Count > 0)
            {
                if (callCandidates.Count > 1)
                {
                    CompilerServices.AddWarning(
                        callLocation,
                        "Function ambiguous call becouse of NULL arguments"
                    );
                }
                return callCandidates[0];
            }

            int min = int.MaxValue;
            int minIndex = int.MaxValue;
            int minCount = 0;
            for (int i = 0; i < weakCallCandidates.Count; ++i)
            {
                if (unmatchesCount[i] < min)
                {
                    min = unmatchesCount[i];
                    minIndex = i;
                    minCount = 1;
                }
                if (unmatchesCount[i] == min)
                {
                    minCount++;
                }
            }

            if (minCount > 1)
            {
                CompilerServices.AddWarning(
                    callLocation,
                    "Function ambiguous call"
                );
            }

            return weakCallCandidates[minIndex];
        }

        #endregion

        #endregion

    }
}
