using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oms
{
    enum ThreadStatus
    {
        InValid,
        Stop,
        Runing,
        Finished,
        Error,
    }
    /// <summary>
    /// 执行线程
    /// 1. 维护运行栈：局部变量栈，函数调用栈
    /// 2. 状态机：stop, runing, finished, error。
    /// </summary>
    class Thread
    {
        OValue[] _stack = new OValue[OmsConf.MAX_STACK_SIZE];
        
        struct CallInfo
        {
            int base_idx;
            int pc;
        }

        CallInfo[] _calls = new CallInfo[OmsConf.MAX_CALL_DEPTH];
        int _cur_call_idx = -1;

        

        ThreadStatus _status = ThreadStatus.Stop;
        public ThreadStatus GetStatus()
        {
            return _status;
        }

        VM _vm;
        public Thread(VM vm_)
        {
            _vm = vm_;
        }

        public void Reset(Function func)
        {
            _cur_call_idx = 0;
        }

        public void Resume()
        {

        }

        public void StopToWait()
        {

        }

        void Execute()
        {
            
            while(_cur_call_idx >= 0)
            {

            }
        }

        void ExecuteFrame()
        {

        }
    }
}
