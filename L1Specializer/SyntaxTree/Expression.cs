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

        private LexLocation f_location;

        public LexLocation Location
        {
            get { return f_location; }
            set { f_location = value; }
        }
	
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

    }
}
