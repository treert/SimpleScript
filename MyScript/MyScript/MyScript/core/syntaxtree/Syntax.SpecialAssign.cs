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
                table = access.table.GetOneResult(frame);
                if (table == null)
                {
                    throw frame.NewRunException(access.table.line, "table can not be null when run TableAccess");
                }
                idx = access.index.GetOneResult(frame);
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
            else
            {
                double delta = 1;
                if (exp != null)
                {
                    delta = exp.GetValidNumber(frame);
                }
                if (op == TokenType.DEC_SELF)
                {
                    delta *= -1;
                }
                // @om 要不要检查下NaN
                val = Utils.ToNumber(val) + delta;
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
