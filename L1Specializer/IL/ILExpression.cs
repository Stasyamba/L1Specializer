using System;
using System.Collections.Generic;

using L1Specializer.Environment;

namespace L1Specializer.IL
{
	
	#region ExpressionTypes
	
	internal enum ILExpressionType
	{
		Plus, Minus, Mul, Div, Mod, Pow,
		ArrayAccess,
		Gr, Greq, Le, Leeq, Eq, NotEq,
		Uminus, Unot, 
		And, Or, Xor,
		Assign,
		Alloc, ArrayLength, VariableAccess, FunctionCall, Const
	}
	
	#endregion
	
	
	internal class ILExpression
	{
		public ILExpression ()
		{
		
		}
		
		#region Properties
		
		public ILExpressionType Type {
			get;
			set;
		}
		
		
		public ILExpression LeftNode {
			get;
			set;
		}
		
		public ILExpression RightNode {
			get;
			set;
		}
		
		public object Const {
			get;
			set;
		}
		
		public List<ILExpression> VAList {
			get;
			set;
		}
		
		public ILFunction Function {
			get;
			set;		
		}
		
		
		#endregion
		
		#region Methods for specialization
		
		public object AbstactReduce(AbstractEnvironment state)
		{
			return null;
		}
		
		#endregion
		
		#region Methods for interpretation
		
		public object Eval(AbstractEnvironment state)
		{
			
			#region +, -, *, /, mod
			
			if (Type == ILExpressionType.Plus)
			{
				object left = LeftNode.Eval(state);
				object right = RightNode.Eval(state);
				if (left != Dynamic.Value && right != Dynamic.Value)
					return (int)left + (int)right;
				else
					return Dynamic.Value;
			}
			if (Type == ILExpressionType.Minus)
			{
				object left = LeftNode.Eval(state);
				object right = RightNode.Eval(state);
				if (left != Dynamic.Value && right != Dynamic.Value)
					return (int)left - (int)right;
				else
					return Dynamic.Value;
			}
			if (Type == ILExpressionType.Mul)
			{
				object left = LeftNode.Eval(state);
				object right = RightNode.Eval(state);
				if (left != Dynamic.Value && right != Dynamic.Value)
					return (int)left * (int)right;
				else
					return Dynamic.Value;
			}
			if (Type == ILExpressionType.Div)
			{
				object left = LeftNode.Eval(state);
				object right = RightNode.Eval(state);
				if (left != Dynamic.Value && right != Dynamic.Value)
					return (int)left / (int)right;
				else
					return Dynamic.Value;
			}
			if (Type == ILExpressionType.Mod)
			{
				object left = LeftNode.Eval(state);
				object right = RightNode.Eval(state);
				if (left != Dynamic.Value && right != Dynamic.Value)
					return (int)left % (int)right;
				else
					return Dynamic.Value;
			}
			
			#endregion
			
			#region []
			
			if (Type == ILExpressionType.ArrayAccess)
			{
				object left = LeftNode.Eval(state);
				object right = RightNode.Eval(state);
				if (left != Dynamic.Value && right != Dynamic.Value)
					return ((Array)left).GetValue((int)right);
				else
					return Dynamic.Value;
			}
			
			#endregion
			
			#region >, >=, <, <=, =, <>
			
			if (Type == ILExpressionType.Gr)
			{
				object left = LeftNode.Eval(state);
				object right = RightNode.Eval(state);
				if (left != Dynamic.Value && right != Dynamic.Value)
					return (int)left > (int)right;
				else
					return Dynamic.Value;
			}
			if (Type == ILExpressionType.Greq)
			{
				object left = LeftNode.Eval(state);
				object right = RightNode.Eval(state);
				if (left != Dynamic.Value && right != Dynamic.Value)
					return (int)left >= (int)right;
				else
					return Dynamic.Value;
			}
			if (Type == ILExpressionType.Le)
			{
				object left = LeftNode.Eval(state);
				object right = RightNode.Eval(state);
				if (left != Dynamic.Value && right != Dynamic.Value)
					return (int)left < (int)right;
				else
					return Dynamic.Value;
			}
			if (Type == ILExpressionType.Leeq)
			{
				object left = LeftNode.Eval(state);
				object right = RightNode.Eval(state);
				if (left != Dynamic.Value && right != Dynamic.Value)
					return (int)left <= (int)right;
				else
					return Dynamic.Value;
			}
			if (Type == ILExpressionType.Eq)
			{
				object left = LeftNode.Eval(state);
				object right = RightNode.Eval(state);
				if (left != Dynamic.Value && right != Dynamic.Value)
					return (int)left == (int)right;
				else
					return Dynamic.Value;
			}
			if (Type == ILExpressionType.NotEq)
			{
				object left = LeftNode.Eval(state);
				object right = RightNode.Eval(state);
				if (left != Dynamic.Value && right != Dynamic.Value)
					return (int)left != (int)right;
				else
					return Dynamic.Value;
			}
			
			#endregion
			
			#region Uminus, Unot
			
			if (Type == ILExpressionType.Uminus)
			{
				object left = LeftNode.Eval(state);
				if (left != Dynamic.Value)
					return - (int)left;
				else
					return Dynamic.Value;
			}
			if (Type == ILExpressionType.Unot)
			{
				object left = LeftNode.Eval(state);
				if (left != Dynamic.Value)
					return !(bool)left;
				else
					return Dynamic.Value;
			}
			
			#endregion
			
			#region And, Or, Xor
			
			if (Type == ILExpressionType.And)
			{
				throw new InvalidOperationException("And operator is not allowed in IL!");
			}
			if (Type == ILExpressionType.Or)
			{
				throw new InvalidOperationException("Or operator is not allowed in IL!");
			}
			if (Type == ILExpressionType.Xor)
			{
				object left = LeftNode.Eval(state);
				object right = RightNode.Eval(state);
				if (left != Dynamic.Value && right != Dynamic.Value)
					return (bool)left ^ (bool)right;
				else
					return Dynamic.Value;
			}
			
			#endregion
			
			#region Alloc, ArrayLength, VariableAccess, Const
			
			if (Type == ILExpressionType.Alloc)
			{
				object left = LeftNode.Eval(state);
				if (left != Dynamic.Value)
					return new object[(int)left];
				else
					return Dynamic.Value;
			}
			if (Type == ILExpressionType.ArrayLength)
			{
				object left = LeftNode.Eval(state);
				if (left != Dynamic.Value)
					return ((Array)left).GetLength(0);
				else
					return Dynamic.Value;
			}
			if (Type == ILExpressionType.VariableAccess)
			{
				return state.GetValue((string)Const);
			}
			if (Type == ILExpressionType.Const)
			{
				return Const;
			}
			
			#endregion
			
			#region Assign
			
			if (Type == ILExpressionType.Assign)
			{
				object right = RightNode.Eval(state);
				if (LeftNode.Type == ILExpressionType.VariableAccess)
				{
					state.SetValue((string)LeftNode.Const, right);
					return right;
				}
				else if (LeftNode.Type == ILExpressionType.ArrayAccess)
				{
					object array = LeftNode.LeftNode.Eval(state);
					object index = LeftNode.RightNode.Eval(state);
					if (array == Dynamic.Value)
					{
						return right;
					}
					else
					{
						if (index == Dynamic.Value)
						{
							//Set whole array as Dynamic
							state.SetArrayAsDynamic(array);
						}
						else
						{
							((Array)array).SetValue(right, (int)index);
						}
						return right;
					}
				}
				else
				{
					throw new InvalidOperationException("Bad assign construction!");
				}
			}
			
			#endregion
			
			#region FunctionCall
			
			if (Type == ILExpressionType.FunctionCall)
			{
				string functionName = (string)Const;
				
				foreach (ILExpression expr in VAList)
				{
					expr.Eval(state);
				}
				
				return Dynamic.Value;
			}
			
			
			#endregion
			
			throw new InvalidOperationException("Bad interpreter error! =(");
			
		}
		
		#endregion
		
	}
}

