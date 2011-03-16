using System;
using System.Collections.Generic;
using System.Linq;

using L1Runtime.SyntaxTree;
using L1Specializer.Metadata;
using L1Specializer.SyntaxTree;
using L1Specializer.SyntaxTree.IfStatements;

using L1Specializer.IL;

namespace L1Specializer
{
	internal static class ILEmitServices
	{
		
		#region Fields
		
		private static List<ILFunction> f_ilProgram;
		private static Dictionary<FunctionDefinition, ILFunction> f_functionFindMap;
		
		private static int f_label = 1000000000;
		
		private static int f_variable = 0;
		private static int f_intVariable = 0;
		private static int f_boolVariable = 0;
		
		#endregion
		
		#region Common methods
		
		private static String GetVariableName() {
			return "_V_" + (f_variable++);
		}
		
		private static String GetIntVariableName() {
			return "_I_" + (f_intVariable++);
		}
		
		private static String GetBoolVariableName() {
			return "_B_" + (f_boolVariable++);
		}
		
		
		private static int GetLabel()
		{
			return f_label++;
		}
		
		private static ILFunction CurrentFunction;
		
		#endregion
		
		#region Custom methods
		
		public static List<ILFunction> EmitProgram(L1Program program)
		{
			f_functionFindMap = new Dictionary<FunctionDefinition, ILFunction>();
			f_ilProgram = new List<ILFunction>();
			foreach (var fDef in program.Functions)
			{
				ILFunction ilFun = new ILFunction();
				if (fDef.Header.ReturnType == null) {
					ilFun.IsVoidReturn = true;
				}
				ilFun.Name = fDef.Header.FunctionName;
				foreach (var p in fDef.Header.Parameters)
				{
					ilFun.Parameters.Add(p.Name);
				}
				if (fDef.IsEmbedded)
				{
					ilFun.EmbeddedBody = fDef.Body;
					bool t = !L1Runtime.DynamicResultAttribute.IsDynamic(fDef.Body);
					ilFun.CanBeCalculatedWithoutRun = t;
				}
				else
				{
					f_ilProgram.Add(ilFun);
				}
				f_functionFindMap.Add(fDef, ilFun);
			}
			
			foreach (var KV in f_functionFindMap)
			{
				if (KV.Key.IsEmbedded == false)
				{
					//EmitStatementList(KV.Key.Statements, KV.Value.Body);
					CurrentFunction = KV.Value;
					
					foreach (var p in KV.Key.Header.Parameters) {
						CurrentFunction.AddLocal(p.Name, p.Type);
					}
					
					_EmitStatementList(KV.Key.Statements, KV.Value.Body);
					KV.Value.Body = ReduceList(KV.Value.Body);
					CurrentFunction = null;
				}
			}
			
			MarkDynamicFunctions();
			
			//Debug output
			foreach (var KV in f_functionFindMap)
			{
				if (KV.Key.IsEmbedded == false)
				{	
					Console.WriteLine (String.Format(".specentry {0} {1}", KV.Value.Name, KV.Value.CanBeCalculatedWithoutRun ? "" : ".dynamic"));
					foreach (ILInstuction inst in KV.Value.Body)
						Console.WriteLine(String.Format("{0}\t\t{1}", inst.Line, inst));
					
					Console.WriteLine(".end");
					Console.WriteLine("");
					Console.WriteLine (".f_calls " + KV.Value.Name);
					var fcalls = FindAllFunctionCalls(KV.Value);
					foreach (var fcall in fcalls)
						Console.WriteLine(String.Format("{0}", fcall));
					Console.WriteLine(".end");
					Console.WriteLine("");
				}
			}
			
			return f_ilProgram;
		}
		
		private static ILFunction FindFunction(string name, VAList vaList)
		{
			foreach (var KV in f_functionFindMap)
			{
				if (KV.Key.Header.FunctionName == name)
				{
					if (KV.Key.Header.ParametersCount == vaList.Count)
					{
						int i = 0;
						bool ok = true;
						foreach (var p in KV.Key.Header.Parameters)
						{
							if (p.Type.Equals(vaList[i].ResultType) == false)
							{
								ok = false;
								break;
							}
							i++;
						}
						if (ok)
							return KV.Value;
					}
				}
			}
			throw new InvalidOperationException("Bad ILEmit situation - can't find function =(");
		}
		
	    private static List<ILInstuction> ReduceList(List<ILInstuction> il)
		{
			il = new List<ILInstuction>(il);
			foreach (var inst in il) {
				if (inst.Line < 1000000000)
					inst.Line = GetLabel();
			}
				
			for (int i = 0; i < il.Count; ++i)
			{
				if (il[i] is ILDummy)
				{
					if (i == il.Count - 1)
					{
						throw new InvalidOperationException("Bad ILEmit situation, jump out function, possibly need to resolve");
					}
					ChangeLables(il, il[i].Line, il[i + 1].Line);
				}
			}
			il.RemoveAll(i => i is ILDummy);
			
			for (int i = 1; i <= il.Count; ++i)
			{
				ChangeLables(il, il[i - 1].Line, i);
				il[i - 1].Line = i;
			}
			
			return il;
		}
		
		private static void ChangeLables(List<ILInstuction> il, int what, int to)
		{
			foreach (ILInstuction inst in il)
			{
				if (inst is ILBranch)
				{
					if ((inst as ILBranch).SuccessJump == what)
						(inst as ILBranch).SuccessJump = to;
					if ((inst as ILBranch).FailJump == what)
						(inst as ILBranch).FailJump = to;
				}
				if (inst is ILGoto)
				{
					if ((inst as ILGoto).GoTo == what)
						(inst as ILGoto).GoTo = to;
				}
			}
		}
		
		private static void MarkDynamicFunctions()
		{	
			int d = 0;
			do 
			{
				d = 0;
				foreach (var ilfunc in f_ilProgram)
				{
					var calls = FindAllFunctionCalls(ilfunc, f => !f.Function.CanBeCalculatedWithoutRun);
					if (calls.Count > 0 && ilfunc.CanBeCalculatedWithoutRun == true)
					{
						d++;
						ilfunc.CanBeCalculatedWithoutRun = false;
					}
				}
			} while (d != 0);
		}
		
		#endregion
		
		#region IL Code analys
		
		public static List<ILExpression> FindAllFunctionCalls(ILFunction function)
		{
			return FindAllFunctionCalls(function, ilExpr => true);
		}
		
		public static List<ILExpression> FindAllFunctionCalls(ILFunction function, Func<ILExpression, bool> predicate)
		{
			List<ILExpression> result = new List<ILExpression>();
			foreach (ILInstuction inst in function.Body)
			{
				if (inst is ILExpression)
					result.AddRange(FindAllFunctionCallsInExpr((ILExpression)inst).Where(predicate));
				else if (inst is ILBranch)
					result.AddRange(FindAllFunctionCallsInExpr((inst as ILBranch).Condition).Where(predicate));
				else if (inst is ILReturn)
					result.AddRange(FindAllFunctionCallsInExpr((inst as ILReturn).Return).Where(predicate));
			}
			return result;
		}
		
		private static List<ILExpression> FindAllFunctionCallsInExpr(ILExpression expression)
		{
			if (expression == null)
				return new List<ILExpression>();
			if (expression.Type == ILExpressionType.FunctionCall)
				return new List<ILExpression>(new ILExpression[]{expression});
			else
			{
				var r = FindAllFunctionCallsInExpr(expression.LeftNode);
				r.AddRange(FindAllFunctionCallsInExpr(expression.RightNode));
				return r;
			}
		}
		
		#endregion
		
		#region Genarate IL (old)
		
		public static void EmitStatementList(StatementList statements, List<ILInstuction> il)
		{
			foreach (var s in statements)
			{
				if (s is Expression)
					EmitExpression((Expression)s, il);
				else if (s is ReturnStatement)
					EmitReturn((ReturnStatement)s, il);
				else if (s is IfStatement)
					EmitIf((IfStatement)s, il);
				else if (s is WhileDoStatement)
					EmitWhileDo((WhileDoStatement)s, il);
				else if (s is VariableDefinitionList)
					EmitVariableDefinition((VariableDefinitionList)s, il);
			}
		}	
		
		private static ILExpression ConstructILExpression(Expression expr)
		{
			if (expr.OpType == OperationType.And || expr.OpType == OperationType.Or)
				throw new InvalidOperationException("And an Or doesn't supported yet");
				
			if (expr == null)
				return null;
			
			//Binary ops
			if (expr.IsLeaf == false)
			{
				ILExpression ilExpr = new ILExpression();
				
				ilExpr.LeftNode = ConstructILExpression(expr.LeftNode);
				ilExpr.RightNode = ConstructILExpression(expr.RightNode);                                   
				
				//+, -, *, /, mod, pow, xor
				if (expr.OpType == OperationType.Plus)
					ilExpr.Type = ILExpressionType.Plus;
				if (expr.OpType == OperationType.Minus)
					ilExpr.Type = ILExpressionType.Minus;
				if (expr.OpType == OperationType.Mult)
					ilExpr.Type = ILExpressionType.Mul;
				if (expr.OpType == OperationType.Div)
					ilExpr.Type = ILExpressionType.Div;
				if (expr.OpType == OperationType.Mod)
					ilExpr.Type = ILExpressionType.Mod;
				if (expr.OpType == OperationType.Power)
					ilExpr.Type = ILExpressionType.Pow;
				if (expr.OpType == OperationType.Xor)
					ilExpr.Type = ILExpressionType.Xor;
				//>,>=,<,<=,=,<>
				if (expr.OpType == OperationType.Gr)
					ilExpr.Type = ILExpressionType.Gr;
				if (expr.OpType == OperationType.Greq)
					ilExpr.Type = ILExpressionType.Greq;
				if (expr.OpType == OperationType.Le)
					ilExpr.Type = ILExpressionType.Leeq;
				if (expr.OpType == OperationType.Equals)
					ilExpr.Type = ILExpressionType.Eq;
				if (expr.OpType == OperationType.NotEquals)
					ilExpr.Type = ILExpressionType.NotEq;
				//UNot, Uminus
				if (expr.OpType == OperationType.UNot)
					ilExpr.Type = ILExpressionType.Unot;
				if (expr.OpType == OperationType.UMinus)
					ilExpr.Type = ILExpressionType.Uminus;
				//ArrayAccess
				if (expr.OpType == OperationType.ArrayAccess)
					ilExpr.Type = ILExpressionType.ArrayAccess;
				//Assign
				if (expr.OpType == OperationType.Assign)
					ilExpr.Type = ILExpressionType.Assign;
				
				return ilExpr;
			}
			else
			{
				ILExpression ilExpr = new ILExpression();
				
				if (expr.LeafType == ExpressionLeafType.Constant)
				{
					ilExpr.Type = ILExpressionType.Const;
					if (expr.ResultType == VariableType.IntType)
						ilExpr.Const = expr.IntValue;
					else if (expr.ResultType == VariableType.BoolType)
						ilExpr.Const = expr.BoolValue;
					else
						ilExpr.Const = expr.Value;
				}
				else if (expr.LeafType == ExpressionLeafType.VariableAccess)
				{
					ilExpr.Type = ILExpressionType.VariableAccess;
					ilExpr.Const = expr.Value;
				}
				else if (expr.LeafType == ExpressionLeafType.FunctionCall)
				{
					ilExpr.Type = ILExpressionType.FunctionCall;
					ilExpr.Const = expr.Value;
					ilExpr.VAList = new List<ILExpression>();
					foreach (var expression in expr.VAList)
						ilExpr.VAList.Add(ConstructILExpression(expression));
					ilExpr.Function = FindFunction((string)ilExpr.Const, expr.VAList);
				}
				else
				{
					ilExpr.LeftNode = ConstructILExpression(expr.LeftNode);
					
					if (expr.LeafType == ExpressionLeafType.ArrayLength)
						ilExpr.Type = ILExpressionType.ArrayLength;
					if (expr.LeafType == ExpressionLeafType.ArrayAlloc)
						ilExpr.Type = ILExpressionType.Alloc;
				}
			
				return ilExpr;
			}
		}
		
		public static void EmitExpression(Expression expr, List<ILInstuction> il)
		{
			ILExpression ilExpr = ConstructILExpression(expr);
			ilExpr.Line = GetLabel();
			il.Add(ilExpr);
		}
		
		#endregion
		
		#region Flow control blocks emit (old)
		
		public static void EmitWhileDo(WhileDoStatement wd, List<ILInstuction> il)
		{
			ILBranch branch = new ILBranch();
			branch.Line = GetLabel();
			branch.Condition = ConstructILExpression(wd.Condition);
			ILDummy succ = new ILDummy();
			succ.Line = GetLabel();
			ILDummy fail = new ILDummy();
			fail.Line = GetLabel();
			ILGoto gotoBegin = new ILGoto(branch.Line);
			
			branch.SuccessJump = succ.Line;
			branch.FailJump = fail.Line;
			il.Add(branch);
			il.Add(succ);
			EmitStatementList(wd.Statements, il);
			il.Add(gotoBegin);
			il.Add(fail);
			
		}
		
		public static void EmitIf(IfStatement ifst, List<ILInstuction> il)
		{
			ILDummy end = new ILDummy();
			end.Line = GetLabel();
			foreach (var ic in ifst.Clauses)
			{
				ILBranch branch = new ILBranch();
				ILDummy blockBegin = new ILDummy();
				ILDummy blockEnd = new ILDummy();
				branch.Line = GetLabel();
				blockBegin.Line = GetLabel();
				blockEnd.Line = GetLabel();
				
				ILGoto gotoEnd = new ILGoto(end.Line);
				gotoEnd.Line = GetLabel();
				branch.SuccessJump = blockBegin.Line;
				branch.FailJump = blockEnd.Line;
				branch.Condition = ConstructILExpression(ic.Condition);
				il.Add(branch);
				il.Add(blockBegin);
				EmitStatementList(ic.Statements, il);
				il.Add(gotoEnd);
				il.Add(blockEnd);
			}
			if (ifst.AlternativeStatements != null)
			{
				EmitStatementList(ifst.AlternativeStatements, il);
			}
			il.Add(end);
		}
		
		public static void EmitReturn(ReturnStatement rs,  List<ILInstuction> il)
		{
			ILReturn ilReturn = new ILReturn();
			ilReturn.Line = GetLabel();
			ilReturn.Return = ConstructILExpression(rs.Expression);
			il.Add(ilReturn);
		}
		
		public static void EmitVariableDefinition(VariableDefinitionList vdl, List<ILInstuction> il)
		{
			foreach (var vd in vdl.Definitions)
			{
				ILExpression initExpr = new ILExpression();
				initExpr.Type = ILExpressionType.Assign;
				
				ILExpression varAccess = new ILExpression();
				varAccess.Type = ILExpressionType.VariableAccess;
				varAccess.Const = vd.Name;
				
				initExpr.LeftNode = varAccess;
				if (vd.InitExpression != null)
					initExpr.RightNode = ConstructILExpression(vd.InitExpression);
				else
				{
					initExpr.RightNode = new ILExpression();
					initExpr.RightNode.Type = ILExpressionType.Const;
					if (vd.VariableType.Equals(VariableType.IntType))
						initExpr.RightNode.Const = 0;
					else if (vd.VariableType.Equals(VariableType.BoolType))
						initExpr.RightNode.Const = false;
					else 
						initExpr.RightNode.Const = null;
				}
				initExpr.Line = GetLabel();
				il.Add(initExpr);
			}
		}
		
		#endregion
		
		#region New code
		
		// var = var1 op var2
		// var = var1[var2]
		// var = var1
		//
		// Return string = name of resulting variable
		//
		// emit (a + b) =;  v1 = emit (a ) ; v2 = emit(
		//
		
		#region Help methods
		
		private static ILExpression _constructBinaryExpressionFromVars(string leftOp, string rightOp, OperationType opType) {
			var ilExpr = new ILExpression();
			
			//+, -, *, /, mod, pow, xor
			if (opType == OperationType.Plus)
				ilExpr.Type = ILExpressionType.Plus;
			if (opType == OperationType.Minus)
				ilExpr.Type = ILExpressionType.Minus;
			if (opType == OperationType.Mult)
				ilExpr.Type = ILExpressionType.Mul;
			if (opType == OperationType.Div)
				ilExpr.Type = ILExpressionType.Div;
			if (opType == OperationType.Mod)
				ilExpr.Type = ILExpressionType.Mod;
			if (opType == OperationType.Power)
				ilExpr.Type = ILExpressionType.Pow;
			if (opType == OperationType.Xor)
				ilExpr.Type = ILExpressionType.Xor;
			//>,>=,<,<=,=,<>
			if (opType == OperationType.Gr)
				ilExpr.Type = ILExpressionType.Gr;
			if (opType == OperationType.Greq)
				ilExpr.Type = ILExpressionType.Greq;
			if (opType == OperationType.Le)
				ilExpr.Type = ILExpressionType.Le;
			if (opType == OperationType.Leeq)
				ilExpr.Type = ILExpressionType.Leeq;
			if (opType == OperationType.Equals)
				ilExpr.Type = ILExpressionType.Eq;
			if (opType == OperationType.NotEquals)
				ilExpr.Type = ILExpressionType.NotEq;
			//ArrayAccess
			if (opType == OperationType.ArrayAccess)
				ilExpr.Type = ILExpressionType.ArrayAccess;
			
			var left = new ILExpression();
			left.Type = ILExpressionType.VariableAccess;
			left.Const = leftOp;
			ilExpr.LeftNode = left;
			
			if (opType == OperationType.UMinus || opType == OperationType.UNot) {
				if (opType == OperationType.UMinus)
					ilExpr.Type = ILExpressionType.Uminus;
				if (opType == OperationType.UNot)
					ilExpr.Type = ILExpressionType.Unot;
				ilExpr.RightNode = null;
			}
			else {	
				var right = new ILExpression();
				right.Type = ILExpressionType.VariableAccess;
				right.Const = rightOp;
				ilExpr.RightNode = right;
			}
			
			return ilExpr;
		}
		
		private static ILExpression _constructAssignExpression(string assignTo, ILExpression right) {
			var assingExpr = new ILExpression();
			assingExpr.Type = ILExpressionType.Assign;
			assingExpr.LeftNode = new ILExpression();
			assingExpr.LeftNode.Type = ILExpressionType.VariableAccess;
			assingExpr.LeftNode.Const = assignTo;
			assingExpr.RightNode = right;
			return assingExpr;
		}
		
		private static ILExpression _constructAssignExpressionToConst(string assignTo, object constant) {
			var assingExpr = new ILExpression();
			assingExpr.Type = ILExpressionType.Assign;
			assingExpr.LeftNode = new ILExpression();
			assingExpr.LeftNode.Type = ILExpressionType.VariableAccess;
			assingExpr.LeftNode.Const = assignTo;
			
			assingExpr.RightNode = new ILExpression();
			assingExpr.RightNode.Type = ILExpressionType.Const;
			assingExpr.RightNode.Const = constant;
			return assingExpr;
		}
		
		private static ILExpression _constructAssignExpression(string assignTo, string assignWhat) {
			var assingExpr = new ILExpression();
			assingExpr.Type = ILExpressionType.Assign;
			
			assingExpr.LeftNode = new ILExpression();
			assingExpr.LeftNode.Type = ILExpressionType.VariableAccess;
			assingExpr.LeftNode.Const = assignTo;
		
			assingExpr.RightNode = new ILExpression();
			assingExpr.RightNode.Type = ILExpressionType.VariableAccess;
			assingExpr.RightNode.Const = assignWhat;
			
			return assingExpr;
		}
		
		private static ILExpression _constructAssignExpression(ILExpression left, string assignWhat) {
			var assingExpr = new ILExpression();
			assingExpr.Type = ILExpressionType.Assign;
			
			assingExpr.LeftNode = left;
		
			assingExpr.RightNode = new ILExpression();
			assingExpr.RightNode.Type = ILExpressionType.VariableAccess;
			assingExpr.RightNode.Const = assignWhat;
			
			return assingExpr;
		}
		
		private static ILExpression _constructVariableAccess(string accessTo) {
			var ilExpr = new ILExpression();
			ilExpr.Type = ILExpressionType.VariableAccess;
			ilExpr.Const = accessTo;
			
			return ilExpr;
		}
		
		#endregion
		
		#region New emit for statement list
		
		public static void _EmitStatementList(StatementList statements, List<ILInstuction> il)
		{
			foreach (var s in statements)
			{
				if (s is Expression) {
					var r = _EmitExpression((Expression)s, il);	
				}
				else if (s is ReturnStatement)
					_EmitReturn((ReturnStatement)s, il);
				else if (s is IfStatement)
					_EmitIf((IfStatement)s, il);
				else if (s is WhileDoStatement)
					_EmitWhileDo((WhileDoStatement)s, il);
				else if (s is DoWhileStatement)
					_EmitDoWhile((DoWhileStatement)s, il);
				else if (s is CycleStatement)
					_EmitCycle((CycleStatement)s, il);
				else if (s is VariableDefinitionList)
					_EmitVariableDefinition((VariableDefinitionList)s, il);
			}
		}	
		
		#endregion
		
		#region New emit for expressions and variable definitions
		
		public static string _EmitExpression(Expression expr, List<ILInstuction> il) {
			if (expr.IsLeaf) {
				//Variable access
				if (expr.LeafType == ExpressionLeafType.VariableAccess) {
					
					CurrentFunction.AddLocal(expr.Value.ToString(), expr.ResultType);
					
					return expr.Value.ToString();
				} else if (expr.LeafType == ExpressionLeafType.Constant) {
					var resultVariable = GetVariableName();
					object c;
					if (expr.ResultType == VariableType.IntType)
						c = expr.IntValue;
					else if (expr.ResultType == VariableType.BoolType)
						c = expr.BoolValue;
					else
						c = expr.Value;
					var ilExpr = _constructAssignExpressionToConst(resultVariable, c);
					il.Add(ilExpr);
					
					//ilExpr.OriginalType = expr.ResultType;
					CurrentFunction.AddLocal(resultVariable, expr.ResultType);
					
					return resultVariable;
				
				} else if (expr.LeafType == ExpressionLeafType.FunctionCall) {
					var ilExpr = new ILExpression();
					ilExpr.Type = ILExpressionType.FunctionCall;
					ilExpr.Const = expr.Value;
					ilExpr.VAList = new List<ILExpression>();
					foreach (var expression in expr.VAList) {
						var parameterVariable = _EmitExpression(expression, il);
						ilExpr.VAList.Add(_constructVariableAccess(parameterVariable));
					}
					ilExpr.Function = FindFunction((string)ilExpr.Const, expr.VAList);	
					//TODO: Add support for void functions;
					
					//ilExpr.OriginalType = expr.ResultType;
					
					if (ilExpr.Function.IsVoidReturn) {
						il.Add(ilExpr);
						return "void";
					} else {
						var resultVariable = GetVariableName();
						var resultExpr = _constructAssignExpression(resultVariable, ilExpr);
						il.Add(resultExpr);
						
						CurrentFunction.AddLocal(resultVariable, expr.ResultType);
						
						return resultVariable;
					}
					
				} else if (expr.LeafType == ExpressionLeafType.ArrayLength) {
					var leftOp = _EmitExpression(expr.LeftNode, il);
					var ilExpr = new ILExpression();
					ilExpr.Type = ILExpressionType.ArrayLength;
					ilExpr.LeftNode = _constructVariableAccess(leftOp);
					var resultVariable = GetVariableName();
					var resultExpr = _constructAssignExpression(resultVariable, ilExpr);
					il.Add(resultExpr);
					
					//resultExpr.OriginalType = expr.ResultType;
					CurrentFunction.AddLocal(resultVariable, expr.ResultType);
					
					return resultVariable;
					
				} else if (expr.LeafType == ExpressionLeafType.ArrayAlloc) {
					var leftOp = _EmitExpression(expr.LeftNode, il);
					var ilExpr = new ILExpression();
					ilExpr.Type = ILExpressionType.Alloc;
					ilExpr.LeftNode = _constructVariableAccess(leftOp);
					var resultVariable = GetVariableName();
					var resultExpr = _constructAssignExpression(resultVariable, ilExpr);
					il.Add(resultExpr);
					
					CurrentFunction.AddLocal(resultVariable, expr.ResultType);
					
					return resultVariable;
				} 
			} else {
				if (expr.OpType == OperationType.Assign) {
					var rightOp = _EmitExpression(expr.RightNode, il);
					if (expr.LeftNode.LeafType == ExpressionLeafType.VariableAccess) {
						var leftOp = _EmitExpression(expr.LeftNode, il);
						il.Add(_constructAssignExpression(leftOp, rightOp));
						
						CurrentFunction.AddLocal(leftOp, expr.ResultType);
						
						return leftOp;
					} else if (expr.LeftNode.OpType == OperationType.ArrayAccess) {
						
						// Emit ( E1[E2] = E3) =
						// Emit (E3) --> R
						// Emit (E1) --> A
						// Emit (E2) --> I
						// A[I] = R
						// return R;
						
						var arrLeftOp = _EmitExpression(expr.LeftNode.LeftNode, il);
						var arrIndexOp = _EmitExpression(expr.LeftNode.RightNode, il);
						var arrAccess = _constructBinaryExpressionFromVars(arrLeftOp, arrIndexOp, OperationType.ArrayAccess);
						
						var assignExpr = _constructAssignExpression(arrAccess, rightOp);
						il.Add(assignExpr);
						
//						assignExprstring resultVar = GetVariableName();
//						var secondAssignExpr = _constructAssignExpression(resultVar, arrAccess);
//						il.Add(secondAssignExpr);
						return rightOp;
					} else {
						throw new InvalidOperationException("Bad assign construction!");
					}
				} else if (expr.OpType == OperationType.And) {
					var leftOp = _EmitExpression(expr.LeftNode, il);
					
					CurrentFunction.AddLocal(leftOp, expr.ResultType);
					
					var branch = new ILBranch();
					branch.Line = GetLabel();
					branch.Condition = _constructVariableAccess(leftOp);
					var succ = new ILDummy();
					succ.Line = GetLabel();
					var fail = new ILDummy();
					fail.Line = GetLabel();
					var end = new ILDummy();
					end.Line = GetLabel();
					var gotoEnd = new ILGoto(end.Line);
					branch.SuccessJump = succ.Line;
					branch.FailJump = fail.Line;
					
					var resultVariable = GetVariableName();
					il.Add(branch);
					il.Add(succ);
					var rightOp = _EmitExpression(expr.RightNode, il);
					
					CurrentFunction.AddLocal(rightOp, expr.ResultType);
					
					il.Add(_constructAssignExpression(resultVariable, rightOp));
					il.Add(gotoEnd);
					il.Add(fail);
					il.Add(_constructAssignExpressionToConst(resultVariable, false));
					il.Add(end);
					
					CurrentFunction.AddLocal(resultVariable, expr.ResultType);
					
					return resultVariable;
				} else if (expr.OpType == OperationType.Or) {
					var leftOp = _EmitExpression(expr.LeftNode, il);
					
					CurrentFunction.AddLocal(leftOp, expr.ResultType);
					
					var branch = new ILBranch();
					branch.Line = GetLabel();
					branch.Condition = _constructVariableAccess(leftOp);
					var succ = new ILDummy();
					succ.Line = GetLabel();
					var fail = new ILDummy();
					fail.Line = GetLabel();
					var end = new ILDummy();
					end.Line = GetLabel();
					var gotoEnd = new ILGoto(end.Line);
					branch.SuccessJump = succ.Line;
					branch.FailJump = fail.Line;
					
					var resultVariable = GetVariableName();
					il.Add(branch);
					il.Add(fail);
					var rightOp = _EmitExpression(expr.RightNode, il);
					
					CurrentFunction.AddLocal(rightOp, expr.ResultType);
					
					il.Add(_constructAssignExpression(resultVariable, rightOp));
					il.Add(gotoEnd);
					il.Add(succ);
					il.Add(_constructAssignExpressionToConst(resultVariable, true));
					il.Add(end);
					
					CurrentFunction.AddLocal(resultVariable, expr.ResultType);
					
					return resultVariable;					
				} 
				else if (expr.OpType == OperationType.UNot || expr.OpType == OperationType.UMinus) {
					var op1 = _EmitExpression(expr.LeftNode, il);
					string varName=  GetVariableName();
					var ilExpr = _constructBinaryExpressionFromVars(op1, "", expr.OpType);
					il.Add(_constructAssignExpression(varName, ilExpr));
					
					CurrentFunction.AddLocal(varName, expr.ResultType);
					
					return varName;
				}	
				else {
					var op1 = _EmitExpression(expr.LeftNode, il);
					var op2 = _EmitExpression(expr.RightNode, il);
					string varName = GetVariableName();
					var ilExpr = _constructBinaryExpressionFromVars(op1, op2, expr.OpType);
					il.Add(_constructAssignExpression(varName, ilExpr));
					
					CurrentFunction.AddLocal(varName, expr.ResultType);
					
					return varName;
				}
			}
			
			throw new InvalidOperationException("Bad expression! " + expr.ToString());
		}
		
		public static void _EmitVariableDefinition(VariableDefinitionList vdl, List<ILInstuction> il)
		{
			foreach (var vd in vdl.Definitions)
			{
				if (vd.InitExpression != null) {
					var initExprVariable = _EmitExpression(vd.InitExpression, il);
					
					CurrentFunction.AddLocal(initExprVariable, vd.VariableType);
					
					var ilExpr = _constructAssignExpression(vd.Name, initExprVariable);
					
					CurrentFunction.AddLocal(vd.Name, vd.VariableType);
					
					il.Add(ilExpr);
				}	
				else
				{
					object c;
					if (vd.VariableType.Equals(VariableType.IntType))
						c = 0;
					else if (vd.VariableType.Equals(VariableType.BoolType))
						c = false;
					else 
						c = null;
					var ilExpr = _constructAssignExpressionToConst(vd.Name, c);
					
					CurrentFunction.AddLocal(vd.Name, vd.VariableType);
					
					il.Add(ilExpr);
				}
			}
		}
		
		#endregion
		
		#region New emit for flow control constructions
		
		public static void _EmitReturn(ReturnStatement rs,  List<ILInstuction> il)
		{
			if (rs.Expression != null) {	
				var returnVariable = _EmitExpression(rs.Expression, il);
				
				CurrentFunction.AddLocal(returnVariable, rs.Expression.ResultType);
				
				ILReturn ilReturn = new ILReturn();
				ilReturn.Line = GetLabel();
				ilReturn.Return = _constructVariableAccess(returnVariable);
				il.Add(ilReturn);
			} else {
				ILReturn ilReturn = new ILReturn();
				ilReturn.Line = GetLabel();
				il.Add(ilReturn);
			}
		}
		
		public static void _EmitIf(IfStatement ifst,  List<ILInstuction> il)
		{
			ILDummy end = new ILDummy();
			end.Line = GetLabel();
			foreach (var ic in ifst.Clauses)
			{
				ILBranch branch = new ILBranch();
				ILDummy blockBegin = new ILDummy();
				ILDummy blockEnd = new ILDummy();
				branch.Line = GetLabel();
				blockBegin.Line = GetLabel();
				blockEnd.Line = GetLabel();
				
				ILGoto gotoEnd = new ILGoto(end.Line);
				gotoEnd.Line = GetLabel();
				branch.SuccessJump = blockBegin.Line;
				branch.FailJump = blockEnd.Line;
				
				var ifConditionVariable = _EmitExpression(ic.Condition, il);
				
				CurrentFunction.AddLocal(ifConditionVariable, ic.Condition.ResultType);
				
				branch.Condition = _constructVariableAccess(ifConditionVariable);
				
				il.Add(branch);
				il.Add(blockBegin);
				_EmitStatementList(ic.Statements, il);
				il.Add(gotoEnd);
				il.Add(blockEnd);
			}
			if (ifst.AlternativeStatements != null)
			{
				_EmitStatementList(ifst.AlternativeStatements, il);
			}
			il.Add(end);	
		}
		
		public static void _EmitWhileDo(WhileDoStatement wd, List<ILInstuction> il)
		{
			var begin = new ILDummy();
			begin.Line = GetLabel();
			il.Add(begin);
			var condtitionVariable = _EmitExpression(wd.Condition, il);
			
			CurrentFunction.AddLocal(condtitionVariable, wd.Condition.ResultType);
			
			var conditionIl = new ILBranch();
			conditionIl.Condition = _constructVariableAccess(condtitionVariable);
			il.Add(conditionIl);
			
			var bodyBegin = new ILDummy();
			bodyBegin.Line = GetLabel();
			var bodyEnd = new ILDummy();
			bodyEnd.Line = GetLabel();
			
			
			il.Add(bodyBegin);
			_EmitStatementList(wd.Statements, il);
			var gotoBeginIl = new ILGoto(begin.Line);
			il.Add(gotoBeginIl);
			il.Add(bodyEnd);
			
			conditionIl.FailJump = bodyEnd.Line;
			conditionIl.SuccessJump = bodyBegin.Line;;
		}
		
		public static void _EmitDoWhile(DoWhileStatement dw, List<ILInstuction> il)
		{
			var bodyBegin = new ILDummy();
			bodyBegin.Line = GetLabel();
			il.Add(bodyBegin);
			_EmitStatementList(dw.Statements, il);
			
			var condtitionVariable = _EmitExpression(dw.Condition, il);
			
			CurrentFunction.AddLocal(condtitionVariable, dw.Condition.ResultType);
			
			var conditionIl = new ILBranch();
			conditionIl.Condition = _constructVariableAccess(condtitionVariable);
			il.Add(conditionIl);
			
			var bodyEnd = new ILDummy();
			bodyEnd.Line = GetLabel();
			il.Add(bodyEnd);
			
			conditionIl.FailJump = bodyEnd.Line;
			conditionIl.SuccessJump = bodyBegin.Line;;
		}
		
		public static void _EmitCycle(CycleStatement cs, List<ILInstuction> il) {
			
			
			throw new NotSupportedException("eee");
		}
		
		#endregion
		
		#endregion
		
	}
}

