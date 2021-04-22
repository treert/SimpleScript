using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class TryStatement : SyntaxTree
    {
        public TryStatement(int line_)
        {
            _line = line_;
        }
        public BlockTree block;
        public Token catch_name;
        public BlockTree catch_block;
        public BlockTree finally_block;

        protected override void _Exec(Frame frame)
        {
            // todo@om 需要处理出现异常栈平衡的问题
            try
            {
                block.Exec(frame);
            }
            catch (ContineException)
            {
                throw;
            }
            catch (BreakException)
            {
                throw;
            }
            catch (Exception e)
            {
                frame.EnterBlock();
                if (catch_name)
                {
                    frame.AddLocalVal(catch_name.m_string, e);
                }
                catch_block.Exec(frame);
                frame.LeaveBlock();
            }
            finally
            {
                finally_block.Exec(frame);
            }
        }
    }
}
