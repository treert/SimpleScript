using MyScript.Test;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyScriptTest.test
{
    class TestMyNumber1 : TestBase
    {
        public override void Run()
        {
            ExpectTrue(MyScript.MyNumber.TryParse("0x1234").HasValue);

            //throw new NotImplementedException();
        }
    }
}
