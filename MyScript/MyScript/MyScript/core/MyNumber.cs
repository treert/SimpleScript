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
    /// </summary>
    public struct MyNumber :IComparable,IEquatable<MyNumber>
    {
        BigInteger big;
        double num;
        bool is_big;// 整数
        public MyNumber(double value)
        {
            is_big = false;
            num = value;
            big = BigInteger.Zero;
        }
        public MyNumber(BigInteger value)
        {
            is_big = true;
            big = value;
            num = 0;
        }
        public static MyNumber Parse(string value)
        {
            throw new Exception();
        }
        public static MyNumber ConvertFrom(object obj)
        {
            throw new Exception();
        }

        public static explicit operator double(MyNumber value)
        {
            return value.is_big ? (double)value.big : value.num;
        }
        public static explicit operator BigInteger(MyNumber value)
        {
            return value.is_big ? value.big : (BigInteger)value.num;
        }
        public static implicit operator MyNumber(double value)
        {
            return new MyNumber(value);
        }
        public static implicit operator MyNumber(BigInteger value)
        {
            return new MyNumber(value);
        }
        public static implicit operator MyNumber(int value)
        {
            return new MyNumber((BigInteger)value);
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
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            return is_big ? big.GetHashCode() : num.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if(obj is MyNumber n)
            {
                return Equals(n);
            }
            return false;
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

        public int CompareTo(object obj)
        {
           
            throw new NotImplementedException();
        }

        public bool Equals(MyNumber other)
        {
            // @om 这儿讨巧
            if (this.is_big)
            {
                if (other.is_big)
                {
                    return this.big.Equals(other.big);
                }
                else
                {
                    return ((double)this.big).Equals(other.num);
                }
            }
            else
            {
                if (other.is_big)
                {
                    return this.num.Equals((double)other.big);
                }
                else
                {
                    return this.num.Equals(other.num);
                }
            }
        }
    }
} 
