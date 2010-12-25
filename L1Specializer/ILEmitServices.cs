using System;
using System.Collections.Generic;

using L1Runtime.SyntaxTree;
using L1Specializer.SyntaxTree;
using L1Specializer.SyntaxTree.IfStatements;

using L1Specializer.IL;

namespace L1Specializer
{
	internal static class ILEmitServices
	{
		
		#region Custom methods
		
		
		
		#endregion
		
		#region Genarate IL
		
		private ILFunction FindFunction(string name, VAList vaList)
		{
			return null;
		}
		
		private static ILExpression ConstructILExpression(Expression expr)
		{
			if (expr == null)
				return null;
			
			//Binary ops
			if (expr.IsLeaf == false)
			{
				ILExpression ilExpr = new ILExpression();
				
				ilExpr.LeftNode = ConstructILExpression(expr.LeftNode);
				ilExpr.RightNode = ConstructILExpression(expr.RightNode);                                   
				
				//+, -, *, /, mod, pow
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
			
		}
		
		#endregion
		
	}
}

