using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    class LibCoroutine
    {
        public static int Create(Thread th)
        {
            int arg_count = th.GetStatckSize();

            Closure func = th.GetValue(0) as Closure;
            if (func == null)
            {
                return 0;
            }
            Thread co = new Thread(th.VM);
            co.Reset(func);
            th.PushValue(co);
            return 1;
        }

        public static int Resume(Thread th)
        {
            int arg_count = th.GetStatckSize();

            var co = th.GetValue(0) as Thread;
            if(co == null || co.GetStatus() != ThreadStatus.Stop)
            {
                th.PushValue(false);
                th.PushValue("coroutine status is not right");
                return 2;
            }

            for (int i = 1; i < arg_count; ++i )
            {
                co.PushValue(th.GetValue(i));
            }
            co.Resume();

            // consume result
            if(co.GetStatus() == ThreadStatus.Finished || co.GetStatus() == ThreadStatus.Stop)
            {
                int ret_count = co.GetStatckSize();
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
            th.StopToWait();
            return th.GetStatckSize();
        }

        public static int Status(Thread th)
        {
            var co = th.GetValue(0) as Thread;
            // todo error check
            co.PushValue(co.GetStatus());
            return 1;
        }

        public static void Register(VM vm)
        {
            var table = new Table();
            vm.m_global.SetValue("coroutine", table);
            table.SetValue("create", (CFunction)Create);
            table.SetValue("resume", (CFunction)Resume);
            table.SetValue("yield", (CFunction)Yield);
            table.SetValue("status", (CFunction)Status);
        }
    }
}
