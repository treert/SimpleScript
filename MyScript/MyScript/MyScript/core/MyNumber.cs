using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

namespace MyScript
{
    /// <summary>
    /// 提供数字类型相关功能：
    /// 1. 同时支持浮点数和整数，目前实现，使用double+BigInteger。
    /// 2. 整数退化成double后不可逆，简化逻辑
    /// 3. 数字是不可变的。
    /// 
    /// @om 选择？：class还是struct？
    /// - class可以避免装箱拆箱，struct可以减少垃圾回收。
    /// - for int range 如果用class，会产生太多的对象
    /// - MyScript 内置的是object，对于struct，会频繁的装箱拆箱
    /// </summary>
    public struct MyNumber :IComparable<MyNumber>,IEquatable<MyNumber>
    {
        BigInteger big;
        double num;
        bool is_big;// 整数
        private MyNumber(double value)
        {
            is_big = false;
            num = value;
            big = BigInteger.Zero;
        }
        private MyNumber(BigInteger value)
        {
            is_big = true;
            big = value;
            num = 0;
        }
        public static MyNumber? TryParse(string s)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(s))
                {
                    return double.NaN;
                }
                s = s.Trim();
                if (s[0] == '0')
                {
                    if (s.Length == 1)
                    {
                        return 0;
                    }

                    if (s[1] == 'x' || s[1] == 'X')
                    {
                        return Convert.ToUInt32(s.Substring(2), 16);// 0xff
                    }
                    else if (s[1] == 'b' || s[1] == 'B')
                    {
                        return Convert.ToUInt32(s.Substring(2), 2);// 0b01
                    }
                    else if (s[1] == '.')
                    {
                        return Convert.ToDouble(s);
                    }
                    else
                    {
                        return Convert.ToUInt32(s.Substring(1), 8);
                    }
                }
                else
                {
                    return Convert.ToDouble(s);
                }
            }
            catch
            {
                return double.NaN;
            }
        }

        public readonly static MyNumber NaN = double.NaN;
        public readonly static MyNumber One = 1;
        public readonly static MyNumber MinusOne = -1;
        
        public bool IsZero { get => is_big ? big.IsZero : num == 0; }

        /// <summary>
        /// 对象转换成MyNumber，一定会成功
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static MyNumber ForceConvertFrom(object obj)
        {
            MyNumber? n = null;
            if(obj is string str)
            {
                n = TryParse(str);
            }
            else
            {
                n = TryConvertFrom(obj);
            }
            return n.HasValue ? n.Value : NaN;
        }
        public static MyNumber? TryConvertFrom(object obj)
        {
            // @om 应该有性能更好的写法
            switch (obj)
            {
                case MyNumber n:
                    return n;
                case bool b:
                    return b;
                case int i:
                    return i;
                case uint i:
                    return i;
                case short i:
                    return i;
                case ushort i:
                    return i;
                case byte i:
                    return i;
                case sbyte i:
                    return i;
                case long i:
                    return i;
                case ulong i:
                    return i;
                case double i:
                    return i;
                case float i:
                    return i;
                case decimal i:
                    return i;
                case Enum e:
                    // c# enum 设计的不方便哎。
                    // https://social.msdn.microsoft.com/Forums/vstudio/en-US/92e31409-c9b6-4725-ac7e-6b912438f8f2/how-to-cast-an-enum-directly-to-int?forum=csharpgeneral
                    return e;
            }
            return null;
        }

        public static explicit operator int(MyNumber value)
        {
            return value.is_big ? (int)value.big : (int)value.num;
        }
        public static explicit operator uint(MyNumber value)
        {
            return value.is_big ? (uint)value.big : (uint)value.num;
        }
        public static explicit operator long(MyNumber value)
        {
            return value.is_big ? (long)value.big : (long)value.num;
        }
        public static explicit operator ulong(MyNumber value)
        {
            return value.is_big ? (ulong)value.big : (ulong)value.num;
        }
        public static explicit operator double(MyNumber value)
        {
            return value.is_big ? (double)value.big : value.num;
        }
        public static explicit operator BigInteger(MyNumber value)
        {
            return value.is_big ? value.big : (BigInteger)value.num;
        }
        public static implicit operator MyNumber(bool value)
        {
            return new MyNumber((BigInteger)(value?1:0));
        }
        public static implicit operator MyNumber(double value)
        {
            return new MyNumber(value);
        }
        public static implicit operator MyNumber(float value)
        {
            return new MyNumber(value);// 精度会丢失
        }
        public static implicit operator MyNumber(decimal value)
        {
            return new MyNumber((double)value);// 精度会丢失
        }
        public static implicit operator MyNumber(BigInteger value)
        {
            return new MyNumber(value);
        }
        public static implicit operator MyNumber(byte value)
        {
            return new MyNumber((BigInteger)value);
        }
        public static implicit operator MyNumber(sbyte value)
        {
            return new MyNumber((BigInteger)value);
        }
        public static implicit operator MyNumber(short value)
        {
            return new MyNumber((BigInteger)value);
        }
        public static implicit operator MyNumber(ushort value)
        {
            return new MyNumber((BigInteger)value);
        }
        public static implicit operator MyNumber(int value)
        {
            return new MyNumber((BigInteger)value);
        }
        public static implicit operator MyNumber(uint value)
        {
            return new MyNumber((BigInteger)value);
        }
        public static implicit operator MyNumber(long value)
        {
            return new MyNumber((BigInteger)value);
        }
        public static implicit operator MyNumber(ulong value)
        {
            return new MyNumber((BigInteger)value);
        }
        public static implicit operator MyNumber(Enum value)
        {
            return new MyNumber((BigInteger)Convert.ToUInt64(value));// 枚举最大支持到int64，统一当成 uint64 来处理
        }
        public static MyNumber operator +(MyNumber value)
        {
            return value;
        }
        public static MyNumber operator -(MyNumber value)
        {
            return value.is_big ? (MyNumber)(-value.big) : (MyNumber)(-value.num);
        }
        public static MyNumber operator +(MyNumber left, MyNumber right)
        {
            if(left.is_big && right.is_big)
            {
                return left.big + right.big;
            }
            else
            {
                return (double)left + (double)right;
            }
        }
        public static MyNumber operator -(MyNumber left, MyNumber right)
        {
            if (left.is_big && right.is_big)
            {
                return left.big - right.big;
            }
            else
            {
                return (double)left - (double)right;
            }
        }
        public static MyNumber operator *(MyNumber left, MyNumber right)
        {
            if (left.is_big && right.is_big)
            {
                return left.big * right.big;
            }
            else
            {
                return (double)left * (double)right;
            }
        }
        public static MyNumber operator /(MyNumber left, MyNumber right)
        {
            if (left.is_big && right.is_big)
            {
                return left.big / right.big;
            }
            else
            {
                return (double)left / (double)right;
            }
        }
        public static MyNumber operator %(MyNumber left, MyNumber right)
        {
            if (left.is_big && right.is_big)
            {
                return left.big % right.big;
            }
            else
            {
                return (double)left % (double)right;
            }
        }
        public static MyNumber operator &(MyNumber left, MyNumber right)
        {
            return (BigInteger)left & (BigInteger)right;
        }
        public static MyNumber operator |(MyNumber left, MyNumber right)
        {
            return (BigInteger)left | (BigInteger)right;
        }
        public static MyNumber operator ^(MyNumber left, MyNumber right)
        {
            return (BigInteger)left ^ (BigInteger)right;
        }
        public static MyNumber operator ~(MyNumber value)
        {
            return ~(BigInteger)value;
        }
        public static MyNumber operator ++(MyNumber value)
        {
            if (value.is_big)
            {
                return value.big + BigInteger.One;
            }
            else
            {
                return value.num + 1;
            }
        }
        public static MyNumber operator --(MyNumber value)
        {
            if (value.is_big)
            {
                return value.big - BigInteger.One;
            }
            else
            {
                return value.num - 1;
            }
        }



        public override string ToString()
        {
            if (is_big)
            {
                return big.ToString();
            }
            else
            {
                return num.ToString();
            }
        }
        public static bool operator ==(MyNumber left, MyNumber right)
        {
            if (left.is_big && right.is_big)
                return left.big == right.big;
            else
                return (double)left == (double)right;
        }
        public static bool operator !=(MyNumber left, MyNumber right)
        {
            if (left.is_big && right.is_big)
                return left.big != right.big;
            else
                return (double)left != (double)right;
        }
        public static bool operator <(MyNumber left, MyNumber right)
        {
            if (left.is_big && right.is_big)
                return left.big < right.big;
            else
                return (double)left.big < (double)right.big;
        }
        public static bool operator >(MyNumber left, MyNumber right)
        {
            if (left.is_big && right.is_big)
                return left.big > right.big;
            else
                return (double)left.big > (double)right.big;
        }
        public static bool operator <=(MyNumber left, MyNumber right)
        {
            if (left.is_big && right.is_big)
                return left.big <= right.big;
            else
                return (double)left.big <= (double)right.big;
        }
        public static bool operator >=(MyNumber left, MyNumber right)
        {
            if (left.is_big && right.is_big)
                return left.big >= right.big;
            else
                return (double)left.big >= (double)right.big;
        }

        public int CompareTo(MyNumber other)
        {
            if(this.is_big && other.is_big)
            {
                return this.big.CompareTo(other.big);
            }
            else
            {
                return ((double)this).CompareTo((double)other);
            }
        }
        #region 
        public static bool IsNAN(MyNumber n)
        {
            return !n.is_big && double.IsNaN(n.num);
        }

        #endregion
        public bool Equals(MyNumber other)
        {
            if(this.is_big && other.is_big)
            {
                return this.big.Equals(other.big);
            }
            else
            {
                return ((double)this).Equals((double)other);
            }
        }
        #region 定义值类型需要定义的一些函数，用于支持：Dictionary, Sort
        public override int GetHashCode()
        {
            return is_big ? big.GetHashCode() : num.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj is MyNumber n)
            {
                return Equals(n);
            }
            return false;
        }
        #endregion


        public const long MaxSafeIntForDouble = 9007199254740991;// 2^54 - 1
        public const long MinSafeIntForDouble = -9007199254740991;
        /// <summary>
        /// 判断是否是有限整数，也就是能不能转换成有效的BigInteger
        /// > https://stackoverflow.com/questions/9898512/how-to-test-if-a-double-is-an-integer/9898528
        /// </summary>
        public bool IsLimitInteger => is_big || (!double.IsInfinity(num) && num == Math.Floor(num));
        public bool IsInt32 => is_big ? (int.MinValue <= big && big <= int.MaxValue) : num == (int)num;
        public bool IsInt64 => is_big ? (long.MinValue <= big && big <= long.MaxValue) : num == (long)num;

        public static MyNumber Divide(MyNumber dividend, MyNumber divisor)
        {
            if(dividend.is_big && divisor.is_big)
            {
                return BigInteger.Divide(dividend.big, divisor.big);
            }
            else
            {
                return (BigInteger)Math.Floor((double)dividend / (double)divisor);// @om 负数有bug，要不要处理？
            }
        }
        public static MyNumber Pow(MyNumber left, MyNumber right)
        {
            // todo@om 后续不用这么测试
            if(left.IsLimitInteger && right.IsInt32)
            {
                return BigInteger.Pow((BigInteger)left, (int)right);
            }
            else
            {
                return Math.Pow((double)left, (double)right);
            }
        }
    }
} 
