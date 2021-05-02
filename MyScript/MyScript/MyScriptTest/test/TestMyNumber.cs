using MyScript.Test;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript.Test
{
    class TestMyNumber1 : TestBase
    {
        public override void Run()
        {
            ExpectTrue(MyScript.MyNumber.TryParse("0x1234") is not null);

            //throw new NotImplementedException();
        }
    }
}
