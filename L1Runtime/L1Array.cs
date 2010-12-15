using System;
using System.Collections.Generic;
using System.Text;

namespace L1Runtime
{

    public class L1Array<T>
    {

        #region Fields

        private T[] f_array;
        private bool f_isNull;

        #endregion

        #region Constructors

        public L1Array(int size)
        {
            if (size < 0)
                L1Runtime.FallCriticalError();

            f_array = new T[size];
        }

        #endregion

        #region Methods

        public int GetLength()
        {
            if (f_isNull)
                L1Runtime.FallNullReference();
            return f_array.Length;
        }

        public T GetValue(int index)
        {
            if (f_isNull)
                L1Runtime.FallNullReference();
            if (index < 0 || index >= f_array.Length)
                L1Runtime.FallIndexOutOfRange();

            return f_array[index];
        }

        public T SetValue(int index, T value)
        {
            if (f_isNull)
                L1Runtime.FallNullReference();
            if (index < 0 || index >= f_array.Length)
                L1Runtime.FallIndexOutOfRange();

            f_array[index] = value;
            return value;
        }

        #endregion

        #region Static methods

        private static L1Array<T> f_nullInstance = null;

        public static L1Array<T> GetNullInstance()
        {
            L1Array<T> nullArray = null;
            if (f_nullInstance == null)
            {
                nullArray = new L1Array<T>(1);
                nullArray.f_isNull = true;
                f_nullInstance = nullArray;
            }
            else
            {
                nullArray = f_nullInstance;
            }
            return nullArray;
        }

        #endregion

    }

}
