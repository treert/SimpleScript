using System;
using System.Collections.Generic;
using System.Text;
/// <summary>
/// 一些基础的扩展
/// </summary>
namespace SScript
{
    class BaseExt
    {
        [ExtGlobalFunc]
        static void print(object obj)
        {
            Console.WriteLine($"{obj}");
        }
    }
}
