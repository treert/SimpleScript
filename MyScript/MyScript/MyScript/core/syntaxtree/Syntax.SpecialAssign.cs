using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MyScript
{
    public class SpecialAssginStatement : SyntaxTree
    {
#nullable disable
        public SpecialAssginStatement(int line_)
        {
            _line = line_;
        }
#nullable restore
        public ExpSyntaxTree var;
        public ExpSyntaxTree exp;// ++ or -- when exp is null
        public TokenType op;

        public static bool NeedWork(TokenType type)
        {
            return type > TokenType.SpecialAssignBegin && type < TokenType.SpecialAssignEnd;
        }

        public static bool IsSelfMode(TokenType type)
        {
            return type > TokenType.SpecialAssignBegin && type < TokenType.SpecialAssignSelfEnd;
        }

        protected override void _Exec(Frame frame)
        {
            // 读
            if (var is TableAccess access)
            {
                // to
                var table = access.table.GetResult(frame);
                if (table == null)
                {
                    throw frame.NewRunException(access.table.line, "table can not be null when do self-assign op");
                }
                var idx = access.index.GetResult(frame);
                if (idx == null)
                {
                    throw frame.NewRunException(access.index.line, "index can not be null when do self-assign op");
                }
                var val = ExtUtils.Get(table, idx);
                val = _Calculate(frame, val);
                ExtUtils.Set(table, idx, val);
            }
            else if(var is Terminator ter)
            {
                Debug.Assert(ter.token.Match(TokenType.NAME));
                var name = ter.token.m_string;
                var val = frame.Read(name);
                val = _Calculate(frame, val);
                frame.Write(name, val);
            }
            else
            {
                Debug.Assert(false);
            }
        }

        object? _Calculate(Frame frame, object? val)
        {
            // 运算
            if (op == TokenType.CONCAT_SELF)
            {
                string str = exp.GetString(frame);
                val = Utils.ToString(val) + str;
            }
            else if (op == TokenType.ADD_SELF)
            {
                var n = exp.GetNumber(frame);
                val = Utils.ToNumber(val) + n;
            }
            else if (op == TokenType.DEC_SELF)
            {
                var n = exp.GetNumber(frame);
                val = Utils.ToNumber(val) - n;
            }
            else if (op == TokenType.MUL_SELF)
            {
                var n = exp.GetNumber(frame);
                val = Utils.ToNumber(val) * n;
            }
            else if (op == TokenType.DIV_SELF)
            {
                var n = exp.GetNumber(frame);
                val = Utils.ToNumber(val) / n;
            }
            else if (op == TokenType.MOD_SELF)
            {
                var n = exp.GetNumber(frame);
                val = Utils.ToNumber(val) % n;
            }
            else if (op == TokenType.DIVIDE_SELF)
            {
                var n = exp.GetNumber(frame);
                val = MyNumber.IntegerDivide(Utils.ToNumber(val), n);
            }
            else if (op == TokenType.BIT_AND_SELF)
            {
                var n = exp.GetNumber(frame);
                val = Utils.ToNumber(val) & n;
            }
            else if (op == TokenType.BIT_OR_SELF)
            {
                var n = exp.GetNumber(frame);
                val = Utils.ToNumber(val) | n;
            }
            else if (op == TokenType.BIT_XOR_SELF)
            {
                var n = exp.GetNumber(frame);
                val = Utils.ToNumber(val) ^ n;
            }
            else if (op == TokenType.POW_SELF)
            {
                var n = exp.GetNumber(frame);
                val = MyNumber.Pow(Utils.ToNumber(val), n);
            }
            else if (op == TokenType.ADD_ONE)
            {
                val = Utils.ToNumber(val) + MyNumber.One;
            }
            else if (op == TokenType.DEC_ONE)
            {
                val = Utils.ToNumber(val) - MyNumber.One;
            }
            else
            {
                Debug.Assert(false);
            }
            return val;
        }
    }

}
