using System;
using System.Collections.Generic;
using System.Text;

namespace L1Runtime.SyntaxTree
{
    /// <summary>
    /// Primitive type
    /// </summary>
    public enum VariableTypeEnum
    {
        Integer,
        Char,
        Bool,
        Array,
        NULL
        //AnyArray
    }

    /// <summary>
    /// Any type
    /// </summary>
    public class VariableType : IEquatable<VariableType>
    {
        
        #region Константы

        public static readonly VariableType IntType = new VariableType(VariableTypeEnum.Integer);
        public static readonly VariableType CharType = new VariableType(VariableTypeEnum.Char);
        public static readonly VariableType BoolType = new VariableType(VariableTypeEnum.Bool);
        public static readonly VariableType NullType = new VariableType(VariableTypeEnum.NULL);

        public static readonly VariableType StrType = new VariableType(VariableTypeEnum.Array, VariableType.CharType);

        #endregion

        #region Constructors

        public VariableType(VariableTypeEnum typeEnum)
        {
            System.Diagnostics.Debug.Assert(typeEnum != VariableTypeEnum.Array);

            this.TypeEnum = typeEnum;
        }

        public VariableType(VariableTypeEnum typeEnum, VariableType nestedType)
        {
            System.Diagnostics.Debug.Assert(typeEnum == VariableTypeEnum.Array);

            this.TypeEnum = typeEnum;
            this.NestedType = nestedType;
        }

        #endregion

        #region Properties

        private VariableTypeEnum f_typeEnum;

        public VariableTypeEnum TypeEnum
        {
            get { return f_typeEnum; }
            private set { f_typeEnum = value; }
        }

        private VariableType f_nestedType;

        public VariableType NestedType
        {
            get { return f_nestedType; }
            private set { f_nestedType = value; }
        }

        private bool f_isReadonly;

        public bool IsReadonly
        {
            get { return f_isReadonly; }
            set { f_isReadonly = value; }
        }
	

        #endregion

        #region IEquatable<VariableType> 

        public bool Equals(VariableType other)
        {
            if (other == null)
                return false;

            if (NestedType != null)
                return (other.TypeEnum == TypeEnum && NestedType.Equals(other.NestedType));
            else
                return (TypeEnum == other.TypeEnum);
        }

        #endregion
    }
}
