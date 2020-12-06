using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class ForStatement : SyntaxTree
    {
        public ForStatement(int line_)
        {
            _line = line_;
        }
        public Token name;
        public ExpSyntaxTree exp1;
        public ExpSyntaxTree exp2;
        public ExpSyntaxTree exp3;
        public BlockTree block;
        protected override void _Exec(Frame frame)
        {
            var start = exp1.GetValidNumber(frame);
            var end = exp2.GetValidNumber(frame);
            if (start <= end)
            {
                double step = exp3 ? exp3.GetValidNumber(frame) : 1;
                if (step <= 0)
                {
                    throw frame.NewRunException(line, $"for step {step} should greater than 0, or will cause forerver loop");
                }
                var cur_block = frame.cur_block;
                for (double it = start; it <= end; it += step)
                {
                    frame.cur_block = cur_block;
                    try
                    {
                        var b = frame.EnterBlock();
                        frame.AddLocalVal(name.m_string, it);
                        block.Exec(frame);
                    }
                    catch (ContineException)
                    {
                        continue;
                    }
                    catch (BreakException)
                    {
                        break;
                    }
                }
                frame.cur_block = cur_block;
            }
            else
            {
                double step = exp3 ? exp3.GetValidNumber(frame) : -1;
                if (step >= 0)
                {
                    throw frame.NewRunException(line, $"for step {step} should less than 0, or will cause forerver loop");
                }
                var cur_block = frame.cur_block;
                for (double it = start; it >= end; it += step)
                {
                    frame.cur_block = cur_block;
                    try
                    {
                        var b = frame.EnterBlock();
                        frame.AddLocalVal(name.m_string, it);
                        block.Exec(frame);
                    }
                    catch (ContineException)
                    {
                        continue;
                    }
                    catch (BreakException)
                    {
                        break;
                    }
                }
                frame.cur_block = cur_block;
            }
        }
    }

    public class ForInStatement : SyntaxTree
    {
        public ForInStatement(int line_)
        {
            _line = line_;
        }
        public NameList name_list;
        public ExpSyntaxTree exp;
        public BlockTree block;

        protected override void _Exec(Frame frame)
        {
            var obj = exp.GetOneResult(frame);
            if (obj == null) return;// 无事发生，虽然按理应该报个错啥的。

            var cur_block = frame.cur_block;
            if (obj is IForKeys)
            {
                var iter = obj as IForKeys;
                var keys = iter.GetKeys();
                foreach (var k in keys)
                {
                    frame.cur_block = cur_block;
                    try
                    {
                        frame.EnterBlock();
                        name_list.AddLocals(frame, k, iter.Get(k));
                        block.Exec(frame);
                    }
                    catch (ContineException)
                    {
                        continue;
                    }
                    catch (BreakException)
                    {
                        break;
                    }
                }
            }
            else if (obj is Function)
            {
                for (; ; )
                {
                    var results = (obj as Function).Call();
                    if (results.GetValueOrDefault(0) != null)
                    {
                        frame.cur_block = cur_block;
                        try
                        {
                            frame.EnterBlock();
                            name_list.AddLocals(frame, results);
                            block.Exec(frame);
                        }
                        catch (ContineException)
                        {
                            continue;
                        }
                        catch (BreakException)
                        {
                            break;
                        }
                    }
                }
            }
            // 想了想，统一支持下 IEnumerate
            else if (obj is IEnumerable)
            {
                foreach (var a in (obj as IEnumerable))
                {
                    frame.cur_block = cur_block;
                    try
                    {
                        frame.EnterBlock();
                        name_list.AddLocals(frame, a);
                        block.Exec(frame);
                    }
                    catch (ContineException)
                    {
                        continue;
                    }
                    catch (BreakException)
                    {
                        break;
                    }
                }
            }
            else
            {
                throw frame.NewRunException(exp.line, $"for in does not support type {obj.GetType().Name}");
            }
            frame.cur_block = cur_block;
        }
    }

    public class ForeverStatement : SyntaxTree
    {
        public ForeverStatement(int line_)
        {
            _line = line_;
        }
        public BlockTree block;

        protected override void _Exec(Frame frame)
        {
            var cur_block = frame.cur_block;
            //int cnt = 0;
            for (; ; )
            {
                //if(cnt++ >= int.MaxValue)
                //{
                //    throw frame.NewRunException(line, "forever loop seens can not ended");
                //}
                frame.cur_block = cur_block;
                try
                {
                    frame.EnterBlock();
                    block.Exec(frame);
                }
                catch (ContineException)
                {
                    continue;
                }
                catch (BreakException)
                {
                    break;
                }
            }
            frame.cur_block = cur_block;
        }
    }
}
