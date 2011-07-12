using System;
using System.Text;
using System.Collections.Generic;

using L1Runtime.SyntaxTree;

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
	
	
	internal class ILExpression : ILInstuction
	{
		public ILExpression ()
		{
			ArrayTypeString = "";
		}
		
		#region Properties
		
		public VariableType OriginalType {
			get;
			set;
		}
		
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
		
		public string ArrayTypeString {
			get;
			set;
		}
		
		
		#endregion
		
		#region Methods for specialization
		
		private string rc(object o) {
			return SpecializerServices.RenderConst(o);
		}
		
		//REDNER variable or it's value
		private object v(AbstractEnvironment env, string vname) {
			if (env.IsDynamic(vname))
				return vname;
			else
				return rc(env.GetValue(vname));
		}
		
		//Reduce RIGHT part of il_expression
		public object AbstactReduce(string leftVariable, AbstractEnvironment state, Dictionary<string, VariableType> localTypeScope)
		{
			if (Type == ILExpressionType.Const) {
				if (Const is string) {
					var s = (string)Const;
					var a = new object[s.Length];
					for (int i = 0; i < s.Length; ++i) {
						a[i] = (int)s[i];
					}
					return a;
				}
				return Const;
			}
			//Only variable
			else if (Type == ILExpressionType.VariableAccess) {
			
				if (state.IsDynamic(Const.ToString()))
				    return Const.ToString();
				else
				    return state.GetValue(Const.ToString());
			}
			else if (Type == ILExpressionType.Alloc) {
				bool isDynamicLeftPart = state.IsDynamic(leftVariable);
				if (isDynamicLeftPart || state.IsDynamic(LeftNode.Const.ToString())) {
					return "new " + localTypeScope[leftVariable].NestedType.ToCompileableString() + "[" + v(state, LeftNode.Const.ToString()) + "]";
				}
				else
				{
					int arraySize = (int)state.GetValue(LeftNode.Const.ToString());
					var a = new object[arraySize];
					object defElem = null;
					if (localTypeScope[leftVariable].NestedType.TypeEnum == VariableTypeEnum.Integer) {
						defElem = 0;
					}
					if (localTypeScope[leftVariable].NestedType.TypeEnum == VariableTypeEnum.Bool) {
						defElem = false;
					}
					for (int i = 0; i < arraySize; ++i) {
					    a[i] = defElem;
					}
					return a;
				}
			}
			else if (Type == ILExpressionType.FunctionCall) {
				return this;
			}
			//Binary expression & unary
			else {
				object r = Eval(state);
				if (r == Dynamic.Value)
					return GetOp(LeftNode.Const.ToString(), (RightNode != null) ? RightNode.Const.ToString() : "", Type, state);
				else
					return r;
			}
		}
		
		private string GetOp(string arg1, string arg2, ILExpressionType type, AbstractEnvironment env) {
			if (type == ILExpressionType.ArrayLength) {
				return "ArrayLength(" + arg1 + ")";
			}
			if (type == ILExpressionType.Unot) {
				return "not " + arg1;
			}
			if (type == ILExpressionType.Uminus) {
				return "-" + arg1;
			}
			if (type == ILExpressionType.ArrayAccess) {
				return v(env, arg1) + "[" + v(env, arg2) + "]";
			}
			
			string op = "<<unknown_binary_op>>";
			if (type == ILExpressionType.Plus) op = " + ";
			if (type == ILExpressionType.Minus) op = " - ";
			if (type == ILExpressionType.Mul) op = " * ";
			if (type == ILExpressionType.Div) op = " / ";
			if (type == ILExpressionType.Pow) op = " ** ";
			if (type == ILExpressionType.Mod) op = " mod ";
			if (type == ILExpressionType.Gr) op = " > ";
			if (type == ILExpressionType.Greq) op = " >= ";
			if (type == ILExpressionType.Le) op = " < ";
			if (type == ILExpressionType.Leeq) op = " <= ";
			if (type == ILExpressionType.Eq) op = " = ";
			if (type == ILExpressionType.NotEq) op = " <> ";
			if (type == ILExpressionType.Xor) op = " xor ";
			
			return v(env, arg1) + op + v(env, arg2);
		}
		
		#endregion
		
		#region Methods for interpretation
		
		public object Eval(AbstractEnvironment state)
		{
			
			#region +, -, *, /, mod, pow
			
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
			if (Type == ILExpressionType.Pow)
			{
				object left = LeftNode.Eval(state);
				object right = RightNode.Eval(state);
				if (left != Dynamic.Value && right != Dynamic.Value)
					return L1Runtime.L1Runtime.Deg((int)left, (int)right);
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
				if (left != Dynamic.Value) {
					int size = (int)left;
					object[] r = new object[size];
					if (OriginalType.NestedType == VariableType.IntType) {
						for (int i = 0; i < size; ++i) {
							r[i] = 0;
						}
					}
					if (OriginalType.NestedType == VariableType.BoolType) {
						for (int i = 0; i < size; ++i) {
							r[i] = false;
						}
					}
					return r;
				}
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
				if (Const is string) {
					var s = (string)Const;
					var a = new object[s.Length];
					for (int i = 0; i < s.Length; ++i) {
						a[i] = (int)s[i];
					}
					return a;
				}
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
				
				var args = new List<object>();
				bool isDynamic = false;
				foreach (ILExpression expr in VAList)
				{
					object arg = expr.Eval(state);
					args.Add(arg);
					if (arg == Dynamic.Value) 
						isDynamic = true;
				}
				if (isDynamic)
					return Dynamic.Value;
				else
					return Function.Call(args.ToArray());;
			}
			
			
			#endregion
			
			throw new InvalidOperationException("Bad interpreter error! =(");
			
		}
		
		#endregion
		
		#region Object Override
		
		public override string ToString ()
		{
			var sb = new StringBuilder();
			sb.Append("(");
			
			if (Type == ILExpressionType.Plus)
			{
				sb.Append(LeftNode.ToString());
				sb.Append(" + ");
				sb.Append(RightNode.ToString());
			}
			if (Type == ILExpressionType.Minus)
			{
				sb.Append(LeftNode.ToString());
				sb.Append(" - ");
				sb.Append(RightNode.ToString());				
			}
			if (Type == ILExpressionType.Mul)
			{
				sb.Append(LeftNode.ToString());
				sb.Append(" * ");
				sb.Append(RightNode.ToString());				
			}
			if (Type == ILExpressionType.Div)
			{
				sb.Append(LeftNode.ToString());
				sb.Append(" / ");
				sb.Append(RightNode.ToString());				
			}
			if (Type == ILExpressionType.Mod)
			{
				sb.Append(LeftNode.ToString());
				sb.Append(" mod ");
				sb.Append(RightNode.ToString());				
			}		
			if (Type == ILExpressionType.Pow)
			{
				sb.Append(LeftNode.ToString());
				sb.Append(" ** ");
				sb.Append(RightNode.ToString());				
			}	
			if (Type == ILExpressionType.Gr)
			{
				sb.Append(LeftNode.ToString());
				sb.Append(" > ");
				sb.Append(RightNode.ToString());
			}
			if (Type == ILExpressionType.Greq)
			{
				sb.Append(LeftNode.ToString());
				sb.Append(" >= ");
				sb.Append(RightNode.ToString());
			}
			if (Type == ILExpressionType.Le)
			{
				sb.Append(LeftNode.ToString());
				sb.Append(" < ");
				sb.Append(RightNode.ToString());
			}
			if (Type == ILExpressionType.Leeq)
			{
				sb.Append(LeftNode.ToString());
				sb.Append(" <= ");
				sb.Append(RightNode.ToString());
			}
			if (Type == ILExpressionType.Eq)
			{
				sb.Append(LeftNode.ToString());
				sb.Append(" = ");
				sb.Append(RightNode.ToString());
			}
			if (Type == ILExpressionType.NotEq)
			{
				sb.Append(LeftNode.ToString());
				sb.Append(" <> ");
				sb.Append(RightNode.ToString());
			}
			if (Type == ILExpressionType.Xor)
			{
				sb.Append(LeftNode.ToString());
				sb.Append(" xor ");
				sb.Append(RightNode.ToString());
			}
			
			if (Type == ILExpressionType.Alloc)
			{
				sb.Append("new ");
				sb.Append(ArrayTypeString);
				sb.Append("[");
				sb.Append(LeftNode.ToString());
				sb.Append("]");
			}
			if (Type == ILExpressionType.ArrayAccess)
			{
				sb.Append(LeftNode.ToString());
				sb.Append(ArrayTypeString);
				sb.Append("[");
				sb.Append(RightNode.ToString());
				sb.Append("]");
			}
			if (Type == ILExpressionType.ArrayLength)
			{
				sb.Append("ArrayLength");
				sb.Append(LeftNode.ToString());
			}
			if (Type == ILExpressionType.FunctionCall)
			{
				sb.Append(Const.ToString());
				sb.Append("(");
				sb.Append(String.Join(", ", VAList.ConvertAll<string>(e => e.ToString())));
				sb.Append(")");
			}
			if (Type == ILExpressionType.VariableAccess)
			{
				sb.Append(Const.ToString());
			}
			if (Type == ILExpressionType.Const)
			{
				if (Const != null) {
					if (Const is Int32 || Const is Boolean) {
						sb.Append(Const.ToString());
					} else {
						sb.Append("\"");
						sb.Append(Const.ToString());
						sb.Append("\"");
					}
				}
				else
					sb.Append("null");
			}
			if (Type == ILExpressionType.Uminus)
			{
				sb.Append("-");
				sb.Append(LeftNode.ToString());
			}
			if (Type == ILExpressionType.Unot)
			{
				sb.Append("not");
				sb.Append(LeftNode.ToString());
			}
			if (Type == ILExpressionType.Assign)
			{
				sb.Append(LeftNode.ToString());
				sb.Append(" := ");
				sb.Append(RightNode.ToString());				
			}
			
			sb.Append(")");
			return sb.ToString();
		}
		
		#endregion
		
	}
}

