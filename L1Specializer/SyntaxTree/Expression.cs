using System;
using System.Collections.Generic;
using System.Text;

using gppg;

using L1Specializer.Metadata;

using L1Runtime.SyntaxTree;

namespace L1Specializer.SyntaxTree
{

    internal enum OperationType
    {
        None,
        UMinus,
        UNot,
        Power,
        Mult,
        Div,
        Mod,
        Plus,
        Minus,
        Assign,
        ArrayAccess,
        Equals,
        NotEquals,
        Le,
        Gr,
        Leeq,
        Greq,
        And,
        Or,
        Xor
    }

    internal enum ExpressionLeafType
    {
        None,
        VariableAccess,
        ArrayAlloc,
        FunctionCall,
        ArrayLength,
        Constant
    }


    internal class Expression : Statement
    {
        
        #region Properties

        private Expression f_leftNode;

        public Expression LeftNode
        {
            get { return f_leftNode; }
            set { f_leftNode = value; }
        }

        private Expression f_rightNode;

        public Expression RightNode
        {
            get { return f_rightNode; }
            set { f_rightNode = value; }
        }

        private bool f_isLeaf;

        public bool IsLeaf
        {
            get { return f_isLeaf; }
            set { f_isLeaf = value; }
        }

        private OperationType f_opType = OperationType.None;

        public OperationType OpType
        {
            get { return f_opType; }
            set { f_opType = value; }
        }

        private ExpressionLeafType f_leafType = ExpressionLeafType.None;

        public ExpressionLeafType LeafType
        {
            get { return f_leafType; }
            set { f_leafType = value; }
        }

        private VariableType f_resultType;

        public VariableType ResultType
        {
            get { return f_resultType; }
            set { f_resultType = value; }
        }

        private VAList f_vaList;

        /// <summary>
        /// Function argument list (if expression is function call)
        /// </summary>
        public VAList VAList
        {
            get { return f_vaList; }
            set { f_vaList = value; }
        }
	

        private Object f_value;

        /// <summary>
        /// Expression value (for interpretation)
        /// </summary>
        public Object Value
        {
            get { return f_value; }
            set { f_value = value; }
        }

        private int f_intValue;

        public int IntValue
        {
            get { return f_intValue; }
            set { f_intValue = value; }
        }

        private char f_charValue;

        public char CharValue
        {
            get { return f_charValue; }
            set { f_charValue = value; }
        }

        private bool f_boolValue;

        public bool BoolValue
        {
            get { return f_boolValue; }
            set { f_boolValue = value; }
        }

//        private LexLocation f_location;
//
//        public LexLocation Location
//        {
//            get { return f_location; }
//            set { f_location = value; }
//        }
	
        #endregion

        #region Base class methods

        public override bool Validate(SymbolTableLight table)
        {
            if (IsLeaf)
            {

                #region Leaf nodes

                if (LeafType == ExpressionLeafType.Constant)
                {
                    return true;
                }
                else if (LeafType == ExpressionLeafType.ArrayAlloc)
                {
                    bool valid = LeftNode.Validate(table);
                    if (valid)
                    {
                        if (LeftNode.ResultType.TypeEnum != VariableTypeEnum.Integer)
                        {
                            valid = false;
                            CompilerServices.AddError(
                                LeftNode.Location,
                                "Array size must be type of int!"
                            );
                        }
                    }
                    return valid;
                }
                else if (LeafType == ExpressionLeafType.VariableAccess)
                {
                    SymbolLight symbol = table.TryGetSymbol((string)Value);
                    if (symbol != null)
                    {
                        ResultType = symbol.Type;
                        ResultType.IsReadonly = symbol.IsReadonly;
                        return true;
                    }
                    else
                    {
                        CompilerServices.AddError(
                            Location,
                            "Unknown variable!"
                        );
                        return false;
                    }
                }
                else if (LeafType == ExpressionLeafType.FunctionCall)
                {
                    VAList args = VAList;
                    string name = (string)Value;
                    LexLocation location = Location;
                    VariableType returnType = null;

                    bool valid = CompilerServices.ValidateFunctionCall(name, args, table, location, ref returnType);
                    ResultType = returnType;
                    return valid;
                }
                else if (LeafType == ExpressionLeafType.ArrayLength)
                {
                    ResultType = VariableType.IntType;
                    bool valid = LeftNode.Validate(table);
                    if (valid)
                    {
                        if (LeftNode.ResultType.TypeEnum != VariableTypeEnum.Array)
                        {
                            CompilerServices.AddError(
                                LeftNode.Location,
                                "ArrayLength argument must be type of array!"
                            );
                            return false;
                        }
                    }
                    return valid;
                }

                #endregion

                return false;
            }
            else
            {

                #region Not leaf nodes

                if (OpType == OperationType.ArrayAccess)
                {
                    bool validLeft = LeftNode.Validate(table);
                    bool validRight = RightNode.Validate(table);

                    if (validLeft && validRight)
                    {
                        if (LeftNode.ResultType.TypeEnum != VariableTypeEnum.Array)
                        {
                            CompilerServices.AddError(
                                Location,
                                "Not array variable used as array!"
                            );
                            return false;
                        }
                        ResultType = LeftNode.ResultType.NestedType;

                        if  (RightNode.ResultType.TypeEnum != VariableTypeEnum.Integer &&
                             RightNode.ResultType.TypeEnum != VariableTypeEnum.Char)
                        {
                            CompilerServices.AddError(
                                LeftNode.Location,
                                "Array index must be type of int!"
                            );
                            return false;
                        }
                        return true;
                    }
                    return false;
                }
                else if (OpType == OperationType.Assign)
                {
                    bool validLeft = LeftNode.Validate(table);
                    bool validRight = RightNode.Validate(table);

                    if (!validLeft || !validRight)
                    {
                        return false;
                    }

                    if ((LeftNode.LeafType != ExpressionLeafType.VariableAccess) &&
                        (LeftNode.OpType != OperationType.ArrayAccess))
                    {
                        CompilerServices.AddError(
                            LeftNode.Location,
                            "Left part of assign expression must be local variable, function parameter or array element!"
                        );
                        return false;
                    }


                    if (LeftNode.ResultType.IsReadonly)
                    {
                        CompilerServices.AddError(
                            LeftNode.Location,
                            "This variable can't be changed after initialization!"
                        );
                        return false;
                    }

                    if (!CompilerServices.IsAssignable(LeftNode.ResultType, RightNode.ResultType))
                    {
                        CompilerServices.AddError(
                            Location,
                            "Assign opertion impossible bacause of operators type difference!"
                        );
                        return false;
                    }
                    ResultType = LeftNode.ResultType;

                    return true;
                }
                else if (OpType == OperationType.Plus)
                {
                    bool validLeft = LeftNode.Validate(table);
                    bool validRight = RightNode.Validate(table);

                    if (!validLeft || !validRight)
                    {
                        return false;
                    }

                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Integer &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Integer)
                    {
                        ResultType = VariableType.IntType;
                        return true;
                    }
                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Integer &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Char)
                    {
                        ResultType = VariableType.CharType;
                        return true;
                    }
                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Char &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Integer)
                    {
                        ResultType = VariableType.CharType;
                        return true;
                    }

                    CompilerServices.AddError(
                        Location,
                        "Bad types in opertaion +"
                    );
                    return false;
                }
                else if (OpType == OperationType.Minus)
                {
                    bool validLeft = LeftNode.Validate(table);
                    bool validRight = RightNode.Validate(table);

                    if (!validLeft || !validRight)
                    {
                        return false;
                    }

                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Integer &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Integer)
                    {
                        ResultType = VariableType.IntType;
                        return true;
                    }
                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Char &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Char)
                    {
                        ResultType = VariableType.IntType;
                        return true;
                    }
                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Char &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Integer)
                    {
                        ResultType = VariableType.CharType;
                        return true;
                    }

                    CompilerServices.AddError(
                        Location,
                        "Bad types in opertaion -"
                    );
                    return false;
                }
                else if (OpType == OperationType.UMinus)
                {
                    bool validLeft = LeftNode.Validate(table);
                    if (!validLeft)
                    {
                        return false;
                    }

                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Integer)
                    {
                        ResultType = VariableType.IntType;
                        return true;
                    }
                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Char)
                    {
                        ResultType = VariableType.IntType;
                        return true;
                    }

                    CompilerServices.AddError(
                        Location,
                        "Bad types in opertaion unary -"
                    );
                    return false;
                }
                else if (OpType == OperationType.UNot)
                {
                    bool validLeft = LeftNode.Validate(table);
                    if (!validLeft)
                    {
                        return false;
                    }
                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Bool)
                    {
                        ResultType = VariableType.BoolType;
                        return true;
                    }

                    CompilerServices.AddError(
                        Location,
                        "Bad types in opertaion unary not"
                    );
                    return false;
                }
                else if (OpType == OperationType.Power || OpType == OperationType.Mult ||
                    OpType == OperationType.Div || OpType == OperationType.Mod)
                {
                    bool validLeft = LeftNode.Validate(table);
                    bool validRight = RightNode.Validate(table);

                    if (!validLeft || !validRight)
                    {
                        return false;
                    }

                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Integer &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Integer)
                    {
                        ResultType = VariableType.IntType;
                        return true;
                    }

                    CompilerServices.AddError(
                        Location,
                        "Bad types in opertaion " + OpType
                    );
                    return false;
                }
                else if (OpType == OperationType.And || OpType == OperationType.Or || OpType == OperationType.Xor)
                {
                    bool validLeft = LeftNode.Validate(table);
                    bool validRight = RightNode.Validate(table);

                    if (!validLeft || !validRight)
                    {
                        return false;
                    }

                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Bool &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Bool)
                    {
                        ResultType = VariableType.BoolType;
                        return true;
                    }

                    CompilerServices.AddError(
                        Location,
                        "Bad types in opertaion " + OpType
                    );
                    return false;
                }
                else if (OpType == OperationType.Gr || OpType == OperationType.Le ||
                    OpType == OperationType.Greq || OpType == OperationType.Leeq)
                {
                    bool validLeft = LeftNode.Validate(table);
                    bool validRight = RightNode.Validate(table);

                    if (!validLeft || !validRight)
                    {
                        return false;
                    }

                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Integer &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Integer)
                    {
                        ResultType = VariableType.BoolType;
                        return true;
                    }
                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Integer &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Char)
                    {
                        ResultType = VariableType.BoolType;
                        return true;
                    }
                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Char &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Integer)
                    {
                        ResultType = VariableType.BoolType;
                        return true;
                    }
                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Char &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Char)
                    {
                        ResultType = VariableType.BoolType;
                        return true;
                    }

                    CompilerServices.AddError(
                        Location,
                        "Bad types in opertaion " + OpType
                    );
                    return false;
                }
                else if (OpType == OperationType.Equals || OpType == OperationType.NotEquals)
                {
                    bool validLeft = LeftNode.Validate(table);
                    bool validRight = RightNode.Validate(table);

                    if (!validLeft || !validRight)
                    {
                        return false;
                    }

                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Integer &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Integer)
                    {
                        ResultType = VariableType.BoolType;
                        return true;
                    }
                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Integer &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Char)
                    {
                        ResultType = VariableType.BoolType;
                        return true;
                    }
                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Char &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Integer)
                    {
                        ResultType = VariableType.BoolType;
                        return true;
                    }
                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Char &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Char)
                    {
                        ResultType = VariableType.BoolType;
                        return true;
                    }
                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Bool &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Bool)
                    {
                        ResultType = VariableType.BoolType;
                        return true;
                    }
                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Array &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Array)
                    {
                        ResultType = VariableType.BoolType;
                        return true;
                    }
                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.Array &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.NULL)
                    {
                        ResultType = VariableType.BoolType;
                        return true;
                    }
                    if (LeftNode.ResultType.TypeEnum == VariableTypeEnum.NULL &&
                        RightNode.ResultType.TypeEnum == VariableTypeEnum.Array)
                    {
                        ResultType = VariableType.BoolType;
                        return true;
                    }

                    CompilerServices.AddError(
                        Location,
                        "Bad types in opertaion " + OpType
                    );
                    return false;
                }


                #endregion

                return false;
            }
        }

        public override void Execute()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
		
		#region Ovveride object methods
		
		public override string ToString ()
		{
			if (IsLeaf) {
				if (LeafType == ExpressionLeafType.FunctionCall) {
					StringBuilder sb = new StringBuilder();
					sb.Append(Value).Append("(");
					int i = 0;
					foreach (var argExpr in VAList) {
						if (i++ != 0) { sb.Append(", "); }
						sb.Append(argExpr.ToString());
					}
					sb.Append(")");
					return sb.ToString();
				} else if (LeafType == ExpressionLeafType.ArrayLength) {
					return "ArrayLength(" + LeftNode.ToString() + ")";
				} else if (LeafType == ExpressionLeafType.VariableAccess) {
					return (string)Value;
				} else if (LeafType == ExpressionLeafType.Constant) {
					return m_renderConst(this);
				} else {
					throw new InvalidOperationException("Not supproted leaf type in ToString()");
				}
			}
			else {
				
				//9
				if (OpType == OperationType.UMinus) {
					return "-" + m_getPreparedString(OpType, LeftNode);
				} 
				
				//8
				if (OpType == OperationType.UNot) {
					return "not " + m_getPreparedString(OpType, LeftNode);
				}
				
				//7
				if (OpType == OperationType.ArrayAccess) {
					return m_getPreparedString(OpType, LeftNode) + "[" + RightNode.ToString() + "]";
				}
				
				//6
				if (OpType == OperationType.Power) {
					return m_getPreparedString(OpType, LeftNode) + " ** " + m_getPreparedString(OpType, RightNode);
				}
				
				//5
				if (OpType == OperationType.Mult) {
					return m_getPreparedString(OpType, LeftNode) + " * " + m_getPreparedString(OpType, RightNode);
				}
				if (OpType == OperationType.Div) {
					return m_getPreparedString(OpType, LeftNode) + " / " + m_getPreparedString(OpType, RightNode);
				}			
				if (OpType == OperationType.Mod) {
					return m_getPreparedString(OpType, LeftNode) + " mod " + m_getPreparedString(OpType, RightNode);
				}		
				
				//4
				if (OpType == OperationType.Plus) {
					return m_getPreparedString(OpType, LeftNode) + " + " + m_getPreparedString(OpType, RightNode);
				}
				if (OpType == OperationType.Minus) {
					return m_getPreparedString(OpType, LeftNode) + " - " + m_getPreparedString(OpType, RightNode);
				}
				
				//3
				if (OpType == OperationType.Equals) {
					return m_getPreparedString(OpType, LeftNode) + " = " + m_getPreparedString(OpType, RightNode);
				}
				if (OpType == OperationType.NotEquals) {
					return m_getPreparedString(OpType, LeftNode) + " <> " + m_getPreparedString(OpType, RightNode);
				}			
				if (OpType == OperationType.Gr) {
					return m_getPreparedString(OpType, LeftNode) + " > " + m_getPreparedString(OpType, RightNode);
				}
				if (OpType == OperationType.Greq) {
					return m_getPreparedString(OpType, LeftNode) + " >= " + m_getPreparedString(OpType, RightNode);
				}
				if (OpType == OperationType.Le) {
					return m_getPreparedString(OpType, LeftNode) + " < " + m_getPreparedString(OpType, RightNode);
				}			
				if (OpType == OperationType.Leeq) {
					return m_getPreparedString(OpType, LeftNode) + " <= " + m_getPreparedString(OpType, RightNode);
				}
				
				//2
				if (OpType == OperationType.And) {
					return m_getPreparedString(OpType, LeftNode) + " and " + m_getPreparedString(OpType, RightNode);
				}
				
				//1
				if (OpType == OperationType.Xor) {
					return m_getPreparedString(OpType, LeftNode) + " xor " + m_getPreparedString(OpType, RightNode);
				}
				if (OpType == OperationType.Or) {
					return m_getPreparedString(OpType, LeftNode) + " or " + m_getPreparedString(OpType, RightNode);
				}
				
				//0
				if (OpType == OperationType.Assign) {
					return m_getPreparedString(OpType, LeftNode) + " := " + m_getPreparedString(OpType, RightNode);
				}
			}
			
			
			throw new InvalidOperationException("Bad Expression.ToString() situation =(");
		}
		
		#endregion
		
		#region Custom methods
		
		private static string m_renderConst(Expression expr) {
			if (expr.ResultType == VariableType.IntType) {
				return Convert.ToString(expr.IntValue);
			}
			if (expr.ResultType == VariableType.BoolType) {
				if (expr.BoolValue) {
					return "T";
				} else {
					return "F";
				}
			}
			if (expr.ResultType == VariableType.NullType) {
				return "NULL";
			}
			if (expr.ResultType.Equals(VariableType.StrType)) {
				return "\"" + expr.Value.ToString() + "\"";
			}
			//TODO: Add array constants
			return "<<CONST>>";
		}
		
		private static string m_getPreparedString(OperationType opType, Expression expr) {
			if (m_greaterPriority(opType, expr))
				return "(" + expr.ToString() + ")";
			else
				return expr.ToString();
		}
		
		//Prior(type) > Prior(subExpression.type), return false is subExpression is LEAF
		private static bool m_greaterPriority(OperationType type, Expression subExpression) {
			if (subExpression.IsLeaf) {
				return false;
			} else {
				int priorType = m_getPriority(type);
				int priorExpr = m_getPriority(subExpression.OpType);
				return priorType > priorExpr;
			}
		}
		
		private static int m_getPriority(OperationType opType) {
			if (opType == OperationType.Assign) { return 0; }
			
			if (opType == OperationType.Or || opType == OperationType.Xor) { return 1; }
			
			if (opType == OperationType.And) { return 2; }
			
			if (opType == OperationType.Equals || opType == OperationType.NotEquals ||
			    opType == OperationType.Gr || opType == OperationType.Greq ||
			    opType == OperationType.Le || opType == OperationType.Leeq) { return 3; }
			
			if (opType == OperationType.Plus || opType == OperationType.Minus) { return 4; }
			
			if (opType == OperationType.Mult || opType == OperationType.Div ||
			    opType == OperationType.Mod) { return 5; }
			
			if (opType == OperationType.Power) { return 6; }
			
			if (opType == OperationType.ArrayAccess) { return 7; }
			
			if (opType == OperationType.UNot) { return 8; }		
			
			if (opType == OperationType.UMinus) { return 9; };
			
			return 100;
		}
		
		#endregion
		
//		%right ASSIGN
//		%left OR XOR
//		%left AND
//		%left EQ NEQ GR GREQ LE LEEQ
//		%left PLUS MINUS
//		%left MULT DIV MOD
//		%right POWER
//		%right LAP
//		%nonassoc NOT
//		%nonassoc UMINUS

    }
}
