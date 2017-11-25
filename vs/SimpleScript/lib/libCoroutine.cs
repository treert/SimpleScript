using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    public class LibCoroutine
    {
        public static int Create(Thread th)
        {
            int arg_count = th.GetStackSize();

            Closure func = th.GetValue(1) as Closure;
            if (func == null)
            {
                return 0;
            }
            // 这个协程不受管理！！！
            // 后面的想法(基于ss不支持多线程的前提)
            // 1. 不单独开协程堆栈，所有协程共享堆栈
            // 2. 协程暂停时，拷贝相应的数据，做个协程闭包，这个闭包提供resume功能，想想还满靠谱的。
            // 
            Thread co = new Thread(th.VM); 
            co.Reset(func);
            th.PushValue(co);
            return 1;
        }

        public static int Resume(Thread th)
        {
            int arg_count = th.GetStackSize();

            var co = th.GetValue(1) as Thread;
            if (co == null || co.GetStatus() != ThreadStatus.Stop)
            {
                th.PushValue(false);
                th.PushValue("coroutine status is not right");
                return 2;
            }

            for (int i = 2; i < arg_count; ++i)
            {
                co.PushValue(th.GetValue(i));
            }
            co.Resume();

            // consume result
            if (co.GetStatus() == ThreadStatus.Finished || co.GetStatus() == ThreadStatus.Stop)
            {
                int ret_count = co.GetStackSize();
                th.PushValue(true);
                for (int i = 0; i < ret_count; ++i)
                    th.PushValue(co.GetValue(i));
                co.ConsumeAllResult();
                return ret_count + 1;
            }
            else
            {
                th.PushValue(false);
                return 1;
            }
        }

        public static int Yield(Thread th)
        {
            th.Pause();
            var arg_cnt = th.GetStackSize() - 1;
            return arg_cnt;
        }

        public static int Status(Thread th)
        {
            var co = th.GetValue(1) as Thread;
            // todo error check
            co.PushValue(co.GetStatus());
            return 1;
        }

        #region 测试下协程
        static int Sleep(Thread th)
        {
            int ms = Convert.ToInt32( th.GetValue(1));
            th.Pause();
            var task = new Task(() =>
            {
                System.Threading.Thread.Sleep(ms);
                CoroutineMgr.AddAction(() =>
                {
                    th.PushValue("awake");
                    th.Resume();
                });
            });
            task.Start();
            return 0;
        }
        #endregion 测试下协程

        public static void Register(VM vm)
        {
            var table = new Table();
            vm.m_global.Set("coroutine", table);
            table.Set("create", (CFunction)Create);
            table.Set("resume", (CFunction)Resume);
            table.Set("yield", (CFunction)Yield);
            table.Set("status", (CFunction)Status);
            table.Set("sleep", (CFunction)Sleep);
        }
    }

    /// <summary>
    /// 协程控制测试,
    /// ss只能在一个线程里工作，这里就是主线程了，这儿测试，主线程需要update CoroutineMgr
    /// </summary>
    public static class CoroutineMgr
    {
        static object _syn_obj = new object();
        static Queue<Action> _action_list = new Queue<Action>();
        public static void AddAction(Action act)
        {
            lock (_syn_obj)
            {
                _action_list.Enqueue(act);
            }
        }

        public static bool Update()
        {
            Action one = null;
            lock (_syn_obj)
            {
                if (_action_list.Count > 0)
                {
                    one = _action_list.Dequeue();
                }
            }
            one?.Invoke();
            return one != null;
        }
    }
}
