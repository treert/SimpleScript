using System;
using System.Collections.Generic;

public static class DelegateFactory
{
    public static void RegisterDelegateGenerater(SimpleScript.VM vm)
    {
        vm.RegisterDelegateGenerater(typeof(System.Func<int,int>), Gen_System_Func_2_System_Int32_System_Int32_);
        vm.RegisterDelegateGenerater(typeof(System.Action), Gen_System_Action);
    }
    public static Delegate Gen_System_Func_2_System_Int32_System_Int32_(SimpleScript.Closure closure)
    {
        System.Func<int,int> d = (param0) =>
        {
            object[] objs = closure.Call(param0);
            return (int)objs[0];
        };
        return d;
    }
    public static Delegate Gen_System_Action(SimpleScript.Closure closure)
    {
        System.Action d = () =>
        {
            closure.Call();
        };
        return d;
    }
}
