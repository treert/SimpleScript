using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MyScript
{
    public class BinaryExpression : ExpSyntaxTree
    {
        public ExpSyntaxTree left;
        public Token op;
        public ExpSyntaxTree right;
        public BinaryExpression(ExpSyntaxTree left_, Token op_, ExpSyntaxTree right_)
        {
            left = left_;
            op = op_;
            right = right_;
            
            Line = op_.m_line;
            Source = left_.Source;
        }

        void CheckNumberType(object l, object r, Frame frame, bool check_right_zero = false)
        {
            if (l is double == false)
            {
                throw frame.NewRunException(left.Line, "expect bin_op left to be a number");
            }
            if (r is double == false)
            {
                throw frame.NewRunException(right.Line, "expect bin_op right to be a number");
            }
            if (check_right_zero)
            {
                var t = (double)r;
                if (t == 0)
                {
                    throw frame.NewRunException(right.Line, "bin_op right value is zero");
                }
            }
        }

        protected override object? _GetResults(Frame frame)
        {
            object? ret = null;
            if (op.Match(Keyword.AND))
            {
                ret = left.GetBool(frame) && right.GetBool(frame);
            }
            else if (op.Match(Keyword.OR))
            {
                ret = left.GetBool(frame) || right.GetBool(frame);
            }
            else if (op.Match('+'))
            {
                ret = left.GetNumber(frame) + right.GetNumber(frame);
            }
            else if (op.Match('-'))
            {
                ret = left.GetNumber(frame) - right.GetNumber(frame);
            }
            else if (op.Match('*'))
            {
                ret = left.GetNumber(frame) * right.GetNumber(frame);
            }
            else if (op.Match('/'))
            {
                ret = left.GetNumber(frame) / right.GetNumber(frame);
            }
            else if (op.Match('%'))
            {
                ret = left.GetNumber(frame) % right.GetNumber(frame);
            }
            else if (op.Match(TokenType.DIVIDE))
            {
                ret = MyNumber.IntegerDivide(left.GetNumber(frame), right.GetNumber(frame));
            }
            else if (op.Match('^'))
            {
                ret = MyNumber.Pow(left.GetNumber(frame), right.GetNumber(frame));
            }
            else if (op.Match('<'))
            {
                ret = Utils.Compare(left.GetResult(frame), right.GetResult(frame)) < 0;
            }
            else if (op.Match('>'))
            {
                ret = Utils.Compare(left.GetResult(frame), right.GetResult(frame)) > 0;
            }
            else if (op.Match(TokenType.LE))
            {
                ret = Utils.Compare(left.GetResult(frame), right.GetResult(frame)) <= 0;
            }
            else if (op.Match(TokenType.GE))
            {
                ret = Utils.Compare(left.GetResult(frame), right.GetResult(frame)) >= 0;
            }
            else if (op.Match(TokenType.EQ))
            {
                ret = Utils.CheckEquals(left.GetResult(frame), right.GetResult(frame));
            }
            else if (op.Match(TokenType.NE))
            {
                ret = !Utils.CheckEquals(left.GetResult(frame), right.GetResult(frame));
            }
            else if (op.Match(TokenType.THREE_CMP))
            {
                ret = Utils.Compare(left.GetResult(frame), right.GetResult(frame));
            }
            else if (op.Match(TokenType.CONCAT))
            {
                ret = left.GetString(frame) + right.GetString(frame);
            }
            else
            {
                Debug.Assert(false);
            }

            return ret;
        }
    }

}
