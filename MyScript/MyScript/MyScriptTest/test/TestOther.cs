using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyScript;
using MyScript.Test;

namespace MyScriptTest.test
{
    class TestOther1 : TestBase
    {
        public override void Run()
        {
            ExpectTrue(MyNumber.TryParse("0x1234") is not null);

            ExpectTrue(Utils.Compare(MyNumber.TryParse("0x1234"), MyNumber.TryParse("0x1234")) == 0);

            //throw new NotImplementedException();
        }
    }
}
