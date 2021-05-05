using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Linq;

namespace MyScript
{
    /// <summary>
    /// 提供数字类型相关功能：
    /// 1. 同时支持浮点数和整数，目前实现，使用double+BigInteger。
    /// 2. 整数退化成double后不可逆，简化逻辑
    /// 3. 数字是不可变的。
    /// 
    /// @om 选择？：class还是struct？【选择class，选择有些不可逆，就这么愉快的决定了】
    /// - class可以避免装箱拆箱，struct可以减少垃圾回收。
    /// - for int range 如果用class，会产生太多的对象。【就算用struct，最终还是要装箱成object.】
    /// - MyScript 内置的是object，对于struct，会频繁的装箱拆箱
    /// </summary>
    public class MyNumber : IComparable, IComparable<MyNumber>,IEquatable<MyNumber>
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

        public static bool TryParse(string s, out MyNumber? n)
        {
            n = TryParse(s);
            return n is not null;
        }

        /// <summary>
        /// 字符串转MyNumber，发现还挺麻烦的。
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static MyNumber? TryParse(string s)
        {
            try
            {
                s = s.Trim().Replace("_", "");
                if (string.IsNullOrWhiteSpace(s))
                {
                    return null;
                }
                if(s.IndexOf('.') >= 0)
                {
                    // @om 可以特殊分离E来支持 1e100 这种的
                    return double.Parse(s);
                }
                int e_idx = s.IndexOf('e', StringComparison.OrdinalIgnoreCase);
                if(e_idx > 0)
                {
                    // 这儿就不支持十六进制之类的了。
                    BigInteger n = BigInteger.Parse(s.Substring(0, e_idx));
                    int exponent = int.Parse(s.Substring(e_idx + 1));
                    return n * BigInteger.Pow(10, exponent);
                }
                else if(e_idx == 0)
                {
                    return null;
                }
                // 支持 2,8,16 进制。
                if (s[0] == '0')
                {
                    if (s.Length == 1)
                    {
                        return 0;
                    }
                    if (s[1] == 'x' || s[1] == 'X')
                    {
                        return BigInteger.Parse(s.Substring(2), NumberStyles.HexNumber);
                    }
                    else if (s[1] == 'b' || s[1] == 'B')
                    {
                        // 哎，就很气，biginteger 原生不支持
                        return s.Substring(2).TryParseToBigIntegerBase2();
                    }
                    else if (char.IsDigit(s[1]))
                    {
                        return s.Substring(1).TryParseToBigIntegerBase8();
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    // 正常十进制
                    return BigInteger.Parse(s);
                }
            }
            catch
            {
                return null;
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
        public static MyNumber ForceConvertFrom(object? obj)
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
            return n ?? NaN;
        }
        public static MyNumber? TryConvertFrom(object? obj)
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
        /// <summary>
        /// 可能会抛异常，看上去似乎也用不到，暂时留着吧。
        /// </summary>
        /// <param name="value"></param>
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
                if (right.big.IsZero)
                {
                    if (left.big.IsZero) return double.NaN;
                    return left.big.Sign >= 0 ? double.PositiveInfinity : double.NegativeInfinity;
                }
                var n = BigInteger.DivRem(left.big, right.big, out var remainder);
                if (remainder.IsZero)
                {
                    return n;
                }
            }
            return (double)left / (double)right;
        }
        public static MyNumber operator %(MyNumber left, MyNumber right)
        {
            if (left.is_big && right.is_big)
            {
                if (right.big.IsZero)
                {
                    return double.NaN;
                }
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

        /// <summary>
        /// 特殊：标准定义里NaN参与比较，应该都返回false。
        /// 但是为了方便实现排序，省的在MyScript里还有特殊判断，MyScript内部的比较最终使用 CompareTo 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(MyNumber? other)
        {
            if(this.is_big && other!.is_big)
            {
                return this.big.CompareTo(other.big);
            }
            else
            {
                return ((double)this).CompareTo((double)other!);
            }
        }
        public int CompareTo(object? obj)
        {
            if (obj == null) return 1;
            if(obj is MyNumber n)
            {
                return CompareTo(n);
            }
            throw new ArgumentException("Argument Must Be MyNumber", nameof(obj));
        }
        #region 
        public static bool IsNaN(MyNumber n)
        {
            return !n.is_big && double.IsNaN(n.num);
        }

        public bool IsNaN() => !is_big && double.IsNaN(num);

        #endregion
        public bool Equals(MyNumber? other)
        {
            if(this.is_big && other!.is_big)
            {
                return this.big.Equals(other.big);
            }
            else
            {
                return ((double)this).Equals((double)other!);
            }
        }
        #region 定义值类型需要定义的一些函数，用于支持：Dictionary, Sort
        public override int GetHashCode()
        {
            return is_big ? big.GetHashCode() : num.GetHashCode();
        }
        public override bool Equals(object? obj)
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
        public bool IsBig => is_big;

        /// <summary>
        /// 整除
        /// </summary>
        /// <param name="dividend"></param>
        /// <param name="divisor"></param>
        /// <returns></returns>
        public static MyNumber IntegerDivide(MyNumber dividend, MyNumber divisor)
        {
            if(dividend.is_big && divisor.is_big)
            {
                if (divisor.big.IsZero)
                {
                    // @om 本来应该抛异常的，想想算了，不抛了。
                    if (dividend.big.IsZero) return double.NaN;
                    return dividend.big.Sign >= 0 ? double.PositiveInfinity : double.NegativeInfinity;
                }
                var n = BigInteger.DivRem(dividend.big, divisor.big, out var remainder);
                return n;
            }
            var f = Math.Floor((double)dividend / (double)divisor);
            if (double.IsFinite(f))
            {
                return (BigInteger)f;
            }
            else
            {
                return f;// 无穷或者NaN
            }
        }
        public static MyNumber Pow(MyNumber left, MyNumber right)
        {
            // 不按实际的值来处理，按意图来。
            if(left.is_big && right.is_big)
            {
                if(right.big <= int.MaxValue && right.big >= int.MinValue)
                    return BigInteger.Pow(left.big, (int)right.big);
            }
            return Math.Pow((double)left, (double)right);
        }
    }
} 
