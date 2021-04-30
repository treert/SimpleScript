using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MyScript
{
    public class SpecialAssginStatement : SyntaxTree
    {
        public SpecialAssginStatement(int line_)
        {
            _line = line_;
        }
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
            object table = null, idx = null, val;
            string name = null;
            // 读
            if (var is TableAccess)
            {
                var access = var as TableAccess;
                table = access.table.GetResult(frame);
                if (table == null)
                {
                    throw frame.NewRunException(access.table.line, "table can not be null when run TableAccess");
                }
                idx = access.index.GetResult(frame);
                if (idx == null)
                {
                    throw frame.NewRunException(access.index.line, "index can not be null when run TableAccess");
                }
                val = ExtUtils.Get(table, idx);
            }
            else
            {
                var ter = var as Terminator;
                Debug.Assert(ter.token.Match(TokenType.NAME));
                name = ter.token.m_string;
                val = frame.Read(name);
            }
            // 运算
            if (op == TokenType.CONCAT_SELF)
            {
                // .=
                string str = exp.GetString(frame);
                val = Utils.ToString(val) + str;
            }
            else if(op == TokenType.ADD_SELF)
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
                val = MyNumber.IntegerDivide(Utils.ToNumber(val) , n);
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
            else if(op == TokenType.ADD_ONE)
            {
                val = Utils.ToNumber(val) + MyNumber.One;
            }
            else if(op == TokenType.DEC_ONE)
            {
                val = Utils.ToNumber(val) - MyNumber.One;
            }
            else{
                Debug.Assert(false);
            }

            // 写
            if (var is TableAccess)
            {
                ExtUtils.Set(table, idx, val);
            }
            else
            {
                frame.Write(name, val);
            }
        }
    }

}
