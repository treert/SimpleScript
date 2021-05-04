using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace MyScript
{
    /// <summary>
    /// MyScript 里用到关键字单词，和实际单词匹配。
    /// 用途：提升点性能+方便编码重构。
    /// 语言设计理念：MyScript的关键词和Name不做区分，语法解析时确定是语法关键字还是普通变量名。（语法关键字优先）
    /// 
    /// PS：本来想直接用小写字母，和关键词精确匹配的，奈何c#不支持把break当做枚举名。所以约定关键词全部是小写字母。
    /// </summary>
    public enum Keyword
    {
        AND = TokenType.NAME + 1,// 搞不懂了，一方面不支持enum隐式转换成int，一方面又允许这种语法
        BREAK,
        CONTINUE,// break and continue use exception to implement
        DO,
        ELSE,
        ELSEIF,
        FALSE,
        FINNALY,
        FOR,
        // 像rust一样，使用fn关键字，之前想着用?的，虽然也挺好，但感觉没有fn方便。
        // 之所以不想用function，一是长，二是function是名词。
        FN,
        GLOBAL,
        USING,// like c# using(var st = FileStream(path)){ ... }
        IF,
        IN,
        LOCAL,
        NIL,
        NOT,
        OR,
        RETURN,
        TRUE,
        WHILE,
        // exception handle, not need for finally
        TRY,
        CATCH,
        // FINALLY,
        THROW,
    }
    public enum TokenType
    {
        EOS = 256,

        DIVIDE, // // 整除
        CONCAT,// .. string concat
        EQ,// ==
        GE,// >=
        LE,// <=
        NE,// !=
        SHIFT_LEFT,// <<
        SHIFT_RIGHT,// >>

        THREE_CMP, // <>，c++里是 <=>，MyScript模版，就简单的<>好了

        // 自加运算系列
        SpecialAssignBegin,
        CONCAT_SELF,// .=
        ADD_SELF,// +=
        DEC_SELF,// -=
        MUL_SELF,// *=
        DIV_SELF,// /=
        MOD_SELF,// %=
        DIVIDE_SELF,// //=
        BIT_AND_SELF,// &=
        BIT_OR_SELF,// |=
        BIT_XOR_SELF,// ~=
        POW_SELF,// ^=
        SpecialAssignSelfEnd,
        ADD_ONE,// ++
        DEC_ONE,// --
        SpecialAssignEnd,

        NUMBER,
        STRING_BEGIN,// 方便词法解析代码编写，字符串可能被$语法打断
        STRING,

        // Name，Must place at last
        NAME = 0xFFFF,
    }

    // 块类型。字符串会被 ${a} 这种语法打断，需要一个栈维护打断的字符串
    public enum StringBlockType
    {
        Begin,

        BigBracket,

        StringBegin,

        SingleQuotation,// ' $x x ' 就不支持 '' 语法了
        DoubleQuotation,// " $x \n \" \t "
        ThreeSingleQuotation,// '''  'abc'   '''
        ThreeDoubleQuotation,// """  "\xx" """
    }

    public class Token
    {
        public int m_type;
        public MyNumber m_number;
        public string m_string;
        // for complex string
        public StringBlockType m_string_type;
        public bool IsStringEnded => m_type == (int)TokenType.STRING && m_string_type < StringBlockType.StringBegin;
        // for error report
        public int m_line;
        public int m_column;

        public Token()
        {
            m_type = (int)TokenType.EOS;
        }
        public Token(MyNumber number_)
        {
            m_type = (int)TokenType.NUMBER;
            m_number = number_;
        }
        public Token(TokenType type_)
        {
            m_type = (int)type_;
        }
        public Token(char char_)
        {
            m_type = (int)char_;
        }
        public Token(string string_, bool is_string = true)
        {
            m_string = string_;
            if (is_string)
            {
                m_type = (int)TokenType.STRING;
            }
            else
            {
                Keyword key;
                if(Regex.IsMatch(string_,"[a-z]") && Enum.TryParse<Keyword>(string_, true, out key))
                {
                    m_type = (int)key;
                }
                else
                {
                    m_type = (int)TokenType.NAME;
                }
            }
        }

        public Token ConvertToStringToken()
        {
            var ret = new Token();
            ret.m_type = (int)TokenType.STRING;
            ret.m_line = m_line;
            ret.m_column = m_column;

            ret.m_string = m_string;
            return ret;
        }
        public bool Match(char char_)
        {
            return m_type == (int)char_;
        }

        public bool Match(char ch1, char ch2)
        {
            return m_type == (int)ch1 || m_type == (int)ch2;
        }

        public bool Match(char ch1, char ch2, char ch3)
        {
            return m_type == (int)ch1 || m_type == (int)ch2 || m_type == (int)ch3;
        }

        public bool Match(TokenType type_)
        {
            return m_type == (int)type_;
        }

        public bool Match(Keyword key)
        {
            return m_type == (int)key;
        }

        public bool CanBeNameString()
        {
            return m_type >= (int)TokenType.NAME;// name + keyword
        }

        public override string ToString()
        {
            return string.Format("token_type:{0},\tstring:{1},\tnumber:{2}",
                m_type, m_string, m_number);
        }

        public static implicit operator bool(Token exsit)
        {
            return exsit != null;
        }


    }

    public class Lex
    {
        Stack<StringBlockType> _block_stack = new Stack<StringBlockType>();

        StringBuilder _buf = new StringBuilder();

        public StringBlockType CurStringType => _block_stack.Peek();
        public bool IsStringEnded => _block_stack.Peek() < StringBlockType.StringBegin;

        private Token _ReadNumber()
        {
            Debug.Assert(char.IsDigit(_current));
            do
            {
                _buf.Append(_current);
                _NextChar();
            } while (char.IsDigit(_current) || '.' == _current);
            if (_current == 'e' || _current == 'E')
            {
                _buf.Append(_current);
                _NextChar();
                if (_current == '+' || _current == '-')
                {
                    _buf.Append(_current);
                    _NextChar();
                }
            }
            while (char.IsLetterOrDigit(_current))
            {
                _buf.Append(_current);
                _NextChar();
            }

            MyNumber? num = MyNumber.TryParse(_buf.ToString());
            if (num is null)
            {
                throw NewLexException($"{_buf} is not valid double");
            }
            else
            {
                return new Token(num);
            }
        }

        void _PutSpecialCharInBuf()
        {
            Debug.Assert(_current == '\\');
            _NextChar();
            if (_current == 'a')
                _buf.Append('\a');
            else if (_current == 'b')
                _buf.Append('\b');
            else if (_current == 'f')
                _buf.Append('\f');
            else if (_current == 'n')
                _buf.Append('\n');
            else if (_current == 'r')
                _buf.Append('\r');
            else if (_current == 't')
                _buf.Append('\t');
            else if (_current == 'v')
                _buf.Append('\v');
            else if (_current == '\\')
                _buf.Append('\\');
            else if (_current == '"')
                _buf.Append('"');
            else if (_current == '\'')
                _buf.Append('\'');
            else if (_current == '0')
                _buf.Append('\0');
            else if (_current == 'x' || _current == 'u' || _current == 'U')
            {
                char head = _current;
                _NextChar();
                int cnt = 2;
                if (head == 'u') cnt = 4;
                else if (head == 'U') cnt = 8;
                int code = 0;
                int i = 0;
                for (; i < cnt; ++i)
                {
                    if (char.IsDigit(_current))
                    {
                        code = code * 16 + _current - '0';
                    }
                    else if (_current >= 'a' && _current <= 'f')
                    {
                        code = code * 16 + _current - 'a';
                    }
                    else if (_current >= 'A' && _current <= 'F')
                    {
                        code = code * 16 + _current - 'A';
                    }
                    else
                    {
                        break;
                    }
                    _NextChar();
                }
                if (i != cnt) throw NewLexException($"expect {cnt} hexadecimal number after '\\{head}'");
                _buf.Append(char.ConvertFromUtf32(code));
                return;
            }
            else if (char.IsDigit(_current))// 特殊支持\d[d][d]，三位十进制（不是C++的八进制），需要小于255。
            {
                // todo 可以考虑支持到所有的Unicode
                int code = 0;
                int i = 0;
                for (; i < 3; ++i)
                {
                    if (char.IsDigit(_current))
                    {
                        code = code * 10 + _current - '0';
                    }
                    else
                    {
                        break;
                    }
                    _NextChar();
                }
                if (code > byte.MaxValue) throw NewLexException("char code big than 255, not support");
                _buf.Append(char.ConvertFromUtf32(code));
                return;
            }
            else
                throw NewLexException("unexpect character after '\\'");

            _NextChar();
        }

        public Token GetNextToken()
        {
            int line = _line;
            int column = _column;
            Token ret = _GetNextToken(ref line, ref column);
            ret.m_line = line;
            ret.m_column = column;
            ret.m_string_type = CurStringType;
            return ret;
        }

        Token _GetNextTokenInString(ref int line, ref int column)
        {
            // $ 语法统一处理
            _buf.Clear();
            if (_current == '$')
            {
                _NextChar();
                if (_current == '{')
                {
                    _NextChar();
                    _block_stack.Push(StringBlockType.BigBracket);
                    column++;
                    return new Token('{');
                }
                else if (_GetName() is string tmp)
                {
                    column++;
                    return new Token(tmp, false);
                }
                else if (_current == '$')
                {
                    _NextChar();// $$ => $
                }
                _buf.Append('$'); // 稍微支持下吧
            }

            switch (_block_stack.Peek())
            {
                case StringBlockType.SingleQuotation:
                    _ReadStringInSingleQuotation();
                    break;
                case StringBlockType.ThreeSingleQuotation:
                    _ReadStringInThreeSingleQuotation();
                    break;
                case StringBlockType.DoubleQuotation:
                    _ReadStringInDoubleQuotation();
                    break;
                case StringBlockType.ThreeDoubleQuotation:
                    _ReadStringInThreeDoubleQuotation();
                    break;
            }
            if (_current == '\0' && IsStringEnded == false) 
                throw NewUnexpectEndException("unexpect <eof> when define string");
            return new Token(_buf.ToString());
        }

        void _ReadStringInThreeSingleQuotation()
        {
            while (_current != '\0' && _current != '$')
            {
                if (_current == '\'')
                {
                    _NextChar();
                    if (_current == '\'')
                    {
                        _NextChar();
                        if(_current == '\'')
                        {
                            _NextChar();
                            _block_stack.Pop();// complete
                            return;
                        }
                        _buf.Append('\'');
                    }
                    _buf.Append('\'');
                }
                else
                {
                    _buf.Append(_current);
                    _NextChar();
                }
            }
        }

        void _ReadStringInSingleQuotation()
        {
            while (_current != '\0' && _current != '$')
            {
                if (_current == '\'')
                {
                    _NextChar();
                    _block_stack.Pop();// complete
                    return;
                }
                else
                {
                    _buf.Append(_current);
                    _NextChar();
                }
            }
        }
        void _ReadStringInThreeDoubleQuotation()
        {
            while (_current != '\0' && _current != '$')
            {
                if (_current == '\\')
                {
                    _PutSpecialCharInBuf();
                }
                else if (_current == '\"')
                {
                    _NextChar();
                    if (_current == '\"')
                    {
                        _NextChar();
                        if (_current == '\"')
                        {
                            _NextChar();
                            _block_stack.Pop();// complete
                            return;
                        }
                        _buf.Append('\"');
                    }
                    _buf.Append('\"');
                }
                else
                {
                    _buf.Append(_current);
                    _NextChar();
                }
            }
        }
        void _ReadStringInDoubleQuotation()
        {
            while (_current != '\0' && _current != '$')
            {
                if (_current == '\\')
                {
                    _PutSpecialCharInBuf();
                }
                else if (_current == '\"')
                {
                    // complete
                    _NextChar();
                    _block_stack.Pop();
                    return;
                }
                else
                {
                    _buf.Append(_current);
                    _NextChar();
                }
            }
        }

        // 读取注释，实际就跳过了字符串。
        void _ReadComment()
        {
            _NextChar();
            if(_current == '[')
            {
                _ReadStringInRaw();
            }
            else
            {
                // 忽略到行尾
                while(_current != '\n' && _current != '\0')
                {
                    _NextChar();
                }
                _NextChar();// 如果是'\0',也没坏处
            }
        }

        void _ReadStringInRaw()
        {
            Debug.Assert(_current == '[');
            _NextChar();
            _buf.Clear();
            if (_current == '[')// [[ ... ]]
            {
                for(; ; )
                {
                    _NextChar();
                    if (_current == '\0') throw NewUnexpectEndException("unexpect <end>");
                    if (_current == ']')
                    {
                        _NextChar();
                        if (_current == ']')
                        {
                            _NextChar();
                            return;
                        }
                        _buf.Append(']');
                    }
                    _buf.Append(_current);
                }
            }
            else if (_current == '=')// [=xxx[ ... ]=xxx]
            {
                string xxx;
                for(; ; )
                {
                    _NextChar();
                    if (_current == '\0') throw NewUnexpectEndException("unexpect <end>");
                    if(_current == '[')
                    {
                        xxx = _buf.ToString();
                        _buf.Clear();
                        break;
                    }
                    else if (char.IsWhiteSpace(_current))
                    {
                        throw NewLexException("[=xxx[ do not support whitespace to be xxx");
                    }
                    _buf.Append(_current);
                }
                for(; ; )
                {
                    _NextChar();
                    if (_current == '\0') throw NewUnexpectEndException("unexpect <end>");
                    if(_current == ']')
                    {
                        _NextChar();
                        if(_current == '=')
                        {
                            int i = 0;
                            for(; i < xxx.Length; i++)
                            {
                                _NextChar();
                                if (xxx[i] != _current) break;
                            }
                            if (_current == '\0') throw NewUnexpectEndException("unexpect <end>");
                            if (i == xxx.Length && _current == ']')
                            {
                                _NextChar();
                                return;
                            }
                            _buf.Append(']'); _buf.Append('=');
                            _buf.Append(xxx, 0, i);
                        }
                        else
                        {
                            _buf.Append(']');
                        }
                    }
                    _buf.Append(_current);
                }
            }
            else// [...] 顺带支持吧，也挺简单的
            {
                for (; ; )
                {
                    if (_current == '\0') throw NewUnexpectEndException("unexpect <end>");
                    if (_current == ']')
                    {
                        _NextChar();
                        return;
                    }
                    _buf.Append(_current);
                    _NextChar();
                }
            }
        }

        Token _GetNextToken(ref int line, ref int column)
        {
            if (_block_stack.Peek() > StringBlockType.StringBegin)
            {
                return _GetNextTokenInString(ref line, ref column);
            }

            while (_current != '\0')
            {
                switch (_current)
                {
                    case '\n':
                        _NextChar();// 空行过滤
                        line = _line; column = _column;
                        break;
                    case '{':
                        _NextChar();
                        _block_stack.Push(StringBlockType.BigBracket);
                        return new Token('{');
                    case '}':
                        if (_block_stack.Peek() != StringBlockType.BigBracket)
                        {
                            throw NewLexException("unexpect '}', miss corresponding '{'");
                        }
                        _NextChar();
                        _block_stack.Pop();
                        return new Token('}');
                    case '\\':
                        _NextChar();
                        if (_current == '\\')
                        {
                            _ReadComment();
                            line = _line; column = _column;
                            break;
                        }
                        else if(_current == '[')
                        {
                            _ReadStringInRaw();
                            return new Token(_buf.ToString());
                        }
                        throw NewLexException("single '\\' do not support");
                    case '+':
                        _NextChar();
                        if (_current == '+')
                        {
                            _NextChar();
                            return new Token(TokenType.ADD_ONE);
                        }
                        else if (_current == '=')
                        {
                            _NextChar();
                            return new Token(TokenType.ADD_SELF);
                        }
                        else
                        {
                            return new Token('+');
                        }
                    case '-':
                        _NextChar();
                        if (_current == '-')
                        {
                            _NextChar();
                            return new Token(TokenType.DEC_ONE);
                        }
                        else if (_current == '=')
                        {
                            _NextChar();
                            return new Token(TokenType.DEC_SELF);
                        }
                        else
                        {
                            return new Token('-');
                        }
                    case '*':
                        _NextChar();
                        if(_current == '=')
                        {
                            _NextChar();
                            return new Token(TokenType.MUL_SELF);
                        }
                        else
                        {
                            return new Token('*');
                        }
                    case '/':
                        _NextChar();
                        if (_current == '/')
                        {
                            _NextChar();
                            if (_current == '=')
                            {
                                _NextChar();
                                return new Token(TokenType.DIVIDE_SELF);
                            }
                            return new Token(TokenType.DIVIDE);
                        }
                        else if (_current == '=')
                        {
                            _NextChar();
                            return new Token(TokenType.DIV_SELF);
                        }
                        else
                        {
                            return new Token('/');
                        }
                    case '%':
                        _NextChar();
                        if (_current == '=')
                        {
                            _NextChar();
                            return new Token(TokenType.MOD_SELF);
                        }
                        else
                        {
                            return new Token('%');
                        }
                    case '&':
                        _NextChar();
                        if (_current == '=')
                        {
                            _NextChar();
                            return new Token(TokenType.BIT_AND_SELF);
                        }
                        else
                        {
                            return new Token('&');
                        }
                    case '|':
                        _NextChar();
                        if (_current == '=')
                        {
                            _NextChar();
                            return new Token(TokenType.BIT_OR_SELF);
                        }
                        else
                        {
                            return new Token('&');
                        }
                    case '~':
                        _NextChar();
                        if (_current == '=')
                        {
                            _NextChar();
                            return new Token(TokenType.BIT_XOR_SELF);
                        }
                        else
                        {
                            return new Token('~');
                        }
                    case '^':
                        _NextChar();
                        if (_current == '=')
                        {
                            _NextChar();
                            return new Token(TokenType.POW_SELF);
                        }
                        else
                        {
                            return new Token('^');
                        }
                    case '.':
                        _NextChar();
                        if (_current == '.')
                        {
                            _NextChar();
                            return new Token(TokenType.CONCAT);
                        }
                        else if (char.IsDigit(_current))
                        {
                            _buf.Clear();
                            _buf.Append('.');
                            return _ReadNumber();
                        }
                        else if (_current == '=')
                        {
                            _NextChar();
                            return new Token(TokenType.CONCAT_SELF);
                        }
                        else
                        {
                            return new Token('.');
                        }
                    //break;
                    case '!':
                        _NextChar();
                        if (_current == '=')
                        {
                            _NextChar();
                            return new Token(TokenType.NE);
                        }
                        else
                        {
                            throw NewLexException("dot not support '!', use 'not' instead.");
                        }
                    //break;
                    case '=':
                        _NextChar();
                        if (_current == '=')
                        {
                            _NextChar();
                            return new Token(TokenType.EQ);
                        }
                        else
                        {
                            return new Token('=');
                        }
                    //break;
                    case '<':
                        _NextChar();
                        if(_current == '=')
                        {
                            _NextChar();
                            return new Token(TokenType.LE);
                        }
                        else if(_current == '<')
                        {
                            _NextChar();
                            return new Token(TokenType.SHIFT_LEFT);
                        }
                        else if(_current == '>')
                        {
                            _NextChar();
                            return new Token(TokenType.THREE_CMP);
                        }
                        return new Token('<');
                    case '>':
                        _NextChar();
                        if (_current == '=')
                        {
                            _NextChar();
                            return new Token(TokenType.GE);
                        }
                        else if (_current == '>')
                        {
                            _NextChar();
                            return new Token(TokenType.SHIFT_RIGHT);
                        }
                        return new Token('>');
                    case '\'':
                        _NextChar();
                        if(_current == '\'')
                        {
                            _NextChar();
                            if(_current == '\'')
                            {
                                _block_stack.Push(StringBlockType.ThreeSingleQuotation);
                            }
                            return new Token("");
                        }
                        else
                        {
                            _block_stack.Push(StringBlockType.SingleQuotation);
                        }
                        return new Token(TokenType.STRING_BEGIN);
                    case '"':
                        _NextChar();
                        if (_current == '\"')
                        {
                            _NextChar();
                            if (_current == '\"')
                            {
                                _block_stack.Push(StringBlockType.ThreeDoubleQuotation);
                            }
                            return new Token("");
                        }
                        else
                        {
                            _block_stack.Push(StringBlockType.DoubleQuotation);
                        }
                        return new Token(TokenType.STRING_BEGIN);
                    default:
                        if (char.IsWhiteSpace(_current))
                        {
                            _NextChar();
                            line = _line; column = _column;
                            break;
                        }
                        else if (char.IsDigit(_current))
                        {
                            _buf.Clear();
                            return _ReadNumber();
                        }
                        else
                        {
                            var name = _GetName();
                            if (name != null)
                            {
                                return new Token(name, false);
                            }
                            else
                            {
                                var c = _current;
                                _NextChar();
                                return new Token(c);
                            }
                        }
                }
            }
            return new Token();
        }

        string _GetName()
        {
            if (_current == '_' || char.IsLetter(_current))
            {
                _buf.Clear();
                do
                {
                    _buf.Append(_current);
                    _NextChar();
                } while (_current == '_' || char.IsLetterOrDigit(_current));
                return _buf.ToString();
            }
            return null;
        }

        private LexException NewLexException(string msg)
        {
            return new LexException(_source_name, _line, _column, msg);
        }

        LexUnexpectEndException NewUnexpectEndException(string msg)
        {
            return new LexUnexpectEndException(_source_name, _line, _column, msg);
        }

        public string GetSourceName()
        {
            return _source_name;
        }

        private string _source_name;
        private string _source;
        private char _current;
        private int _pos;
        private int _line;
        private int _column;
        private char _NextChar()
        {
            var ch = __NextChar();

            // 从底层就忽略掉回车。想想，还可以忽略很多哎，先不管吧
            while (ch == '\r')
            {
                ch = __NextChar();  
            }

            if(ch == '\n')
            {
                ++_line;
                _column = 0;
            }
            return _current;
        }

        char __NextChar()
        {
            if (_pos < _source.Length)
            {
                _current = _source[_pos];
                ++_pos;
                ++_column;
            }
            else
            {
                _current = '\0';
            }
            return _current;
        }

        public void Init(string input_, string name_ = "")
        {
            _source_name = name_;
            _source = input_;
            _pos = 0;
            _line = 1;
            _column = 0;
            _block_stack.Clear();
            _block_stack.Push(StringBlockType.Begin);
            _NextChar();
        }
    }
}
