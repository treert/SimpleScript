\\[[
    > File Name: test.ms
    > Create Time: 2017-07-23 星期日 15时48分51秒
    > Athor: treertzhu
    > -----
    > Last Modified: 2017-09-22 星期五 22时26分13秒
    > Modified By: treertzhu
    > -----
\\]]

echo("hello World")

a = 1
b = a + 2;

echo("1+2=$b");
echo("${2^2^3}  ${2^3^2} ${2//1}");
echo("========  test number caculate  =========")
echo("${-2^2} ${(-2)^2}")
echo("${2^2^3}  ${2^3^2} ${(2^3)^2}");
echo("${2.2//1} ${2//0.0} ${-1/0.0} ${0.0/0.0} ${1.2 % 0.0} ${0.0 % 0.0}");
echo("${2//1} ${2//0} ${-1/0} ${0/0} ${1 % 0} ${0 % 0}");
echo("========  test complex string  =========")
echo "${1_232,-10:N0}x\n${1_232,10:N0}x"
echo.test(1,2,4,5);

echo("string".sub(-1))
echo(string.join(",", 1,2,3,4))