﻿using System;
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
            _line = op_.m_line;
        }

        void CheckNumberType(object l, object r, Frame frame, bool check_right_zero = false)
        {
            if (l is double == false)
            {
                throw frame.NewRunException(left.line, "expect bin_op left to be a number");
            }
            if (r is double == false)
            {
                throw frame.NewRunException(right.line, "expect bin_op right to be a number");
            }
            if (check_right_zero)
            {
                var t = (double)r;
                if (t == 0)
                {
                    throw frame.NewRunException(right.line, "bin_op right value is zero");
                }
            }
        }

        protected override List<object> _GetResults(Frame frame)
        {
            object ret = null;
            if (op.Match(Keyword.AND))
            {
                ret = left.GetBool(frame) && right.GetBool(frame);
            }
            else if (op.Match(Keyword.OR))
            {
                ret = left.GetBool(frame) || right.GetBool(frame);
            }
            else
            {
                if (op.Match('+'))
                {
                    ret = left.GetValidNumber(frame) + right.GetValidNumber(frame);
                }
                else if (op.Match('-'))
                {
                    ret = left.GetValidNumber(frame) - right.GetValidNumber(frame);
                }
                else if (op.Match('*'))
                {
                    ret = left.GetValidNumber(frame) * right.GetValidNumber(frame);
                }
                else if (op.Match('/'))
                {
                    ret = left.GetValidNumber(frame) / right.GetValidNumber(frame);
                }
                else if (op.Match('%'))
                {
                    ret = left.GetValidNumber(frame) % right.GetValidNumber(frame);
                }
                else if (op.Match('<'))
                {
                    var l = left.GetOneResult(frame);
                    var r = right.GetOneResult(frame);
                    ret = Utils.Compare(l, r) < 0;
                }
                else if (op.Match('>'))
                {
                    var l = left.GetOneResult(frame);
                    var r = right.GetOneResult(frame);
                    ret = Utils.Compare(l, r) > 0;
                }
                else if (op.Match(TokenType.LE))
                {
                    var l = left.GetOneResult(frame);
                    var r = right.GetOneResult(frame);
                    ret = Utils.Compare(l, r) <= 0;
                }
                else if (op.Match(TokenType.GE))
                {
                    var l = left.GetOneResult(frame);
                    var r = right.GetOneResult(frame);
                    ret = Utils.Compare(l, r) >= 0;
                }
                else if (op.Match(TokenType.EQ))
                {
                    var l = left.GetOneResult(frame);
                    var r = right.GetOneResult(frame);
                    ret = Utils.CheckEquals(l, r);
                }
                else if (op.Match(TokenType.NE))
                {
                    var l = left.GetOneResult(frame);
                    var r = right.GetOneResult(frame);
                    ret = !Utils.CheckEquals(l, r);
                }
                else if (op.Match(TokenType.CONCAT))
                {
                    ret = left.GetString(frame) + right.GetString(frame);
                }
                else
                {
                    Debug.Assert(false);
                }

            }

            return new List<object>() { ret };
        }
    }

}
