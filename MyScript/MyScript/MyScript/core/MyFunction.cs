using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyScript
{
    /// <summary>
    /// 运行时函数
    /// </summary>
    public class MyFunction : ICall
    {
#nullable disable
        public VM vm;// 保存住来自哪个vm，外部调用就方便好多了。
        public FunctionBody code;
        public MyTable module_table;
        // 环境闭包值，比较特殊的是：当Value == null，指这个变量是全局变量。
        public Dictionary<string, LocalValue> upvalues;
#nullable restore
        // 默认参数。有副作用，这些obj是常驻内存的，有可能被修改。给所有的参数都加上好了，方便实现裁剪出**name
        public Dictionary<string, object?> default_args = new Dictionary<string, object?>();


        public object? Call(params object[] objs)
        {
            MyArgs args = new MyArgs(objs);
            return Call(args);
        }

        public object? Call(Dictionary<string, object?> name_args, params object[] objs)
        {
            MyArgs args = new MyArgs(name_args, objs);
            return Call(args);
        }

        public object? Call()
        {
            MyArgs args = new MyArgs();
            return Call(args);
        }

        public object? Call(MyArgs args)
        {
            Frame frame = new Frame(this);
            // 先填充个this
            frame.AddLocalVal(Utils.MAGIC_THIS, args.that);

            int name_cnt = 0;
            if (code.param_list is ParamList param)
            {
                name_cnt = param.name_list.Count;
                for (int i = 0; i < name_cnt; i++)
                {
                    var name = param.name_list[i].token.m_string;
                    object? obj;
                    _ = args.TryGetValue(i, name, out obj) || default_args.TryGetValue(name, out obj);
                    frame.AddLocalVal(name, obj);
                }
                if (param.ls_name != null)
                {
                    string ls_name = param.ls_name.m_string!;
                    MyArray arr = new MyArray();
                    for (int i = name_cnt; i < args.args.Count; i++)
                    {
                        arr.Add(args.args[i]);// 截断下
                    }
                }
                foreach (var it in param.kw_list)
                {
                    var name = it.token.m_string!;
                    object? obj;
                    _ = args.name_args.TryGetValue(name, out obj) || default_args.TryGetValue(name, out obj);
                    frame.AddLocalVal(name, obj);
                }

                if (param.kw_name != null)
                {
                    // @om 取所有命名参数好了
                    MyTable t = new MyTable();
                    foreach (var it in args.name_args)
                    {
                        if (default_args.ContainsKey(it.Key) == false)
                            t.Set(it.Key, it.Value);
                    }
                    frame.AddLocalVal(param.kw_name.m_string!, t);// 直接获取所有参数好了
                }
            }
            var cur_block = frame.CurrentBlock;
            try
            {
                code.block.Exec(frame);
            }
            catch (ReturnException ep)
            {
                return ep.result;
            }
            catch (ContineException ep)
            {
                throw new RunException(code.Source, ep.line, "unexpect contine");
            }
            catch (BreakException ep)
            {
                throw new RunException(code.Source, ep.line, "unexpect break");
            }
            finally
            {
                frame.CurrentBlock = cur_block;// 保证释放下 using 里的东西。 
            }

            // 其他的异常就透传出去好了。
            return null;
        }
    }
}
