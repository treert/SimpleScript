using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    public enum TokenType
    {
        // reserved words
        AND = 257,
        BREAK,
        CONTINUE,// break and continue use exception to implement
        ELSE,
        ELSEIF,
        FALSE,
        FOR,
        FUNCTION,
        GLOBAL,
        IF,
        IN,
        LOCAL,
        NIL,// use null
        NOT,
        OR,
        RETURN,
        TRUE,
        WHILE,
        // other terminal symbols
        CONCAT,// .. string concat
        DOTS,// ...
        EQ,// ==
        GE,// >=
        LE,// <=
        NE,// !=
        ADD_SELF,// +=
        DEC_SELF,// -=
        CONCAT_SELF,// .=
        ADD_ONE,// ++
        DEC_ONE,// --
        NUMBER,
        STRING_BEGIN,// 方便词法解析代码编写，字符串可能被$语法打断
        STRING,
        NAME,
        // End
        EOS,
    }



    public class Token
    {
        public int m_type;
        public double m_number;
        public string m_string;
        // for error report
        public int m_line;
        public int m_column;

        public Token()
        {
            m_type = (int)TokenType.EOS;
        }
        public Token(double number_)
        {
            m_type = (int)TokenType.NUMBER;
            m_number = number_;
        }
        public Token(string string_)
        {
            m_type = (int)TokenType.STRING;
            m_string = string_;
        }
        public Token(TokenType type_, string string_)
        {
            Debug.Assert(type_ == TokenType.NAME);
            m_type = (int)type_;
            m_string = string_;
        }
        public Token(TokenType type_)
        {
            m_type = (int)type_;
        }
        public Token(char char_)
        {
            m_type = (int)char_;
        }

        public bool EqualTo(char char_)
        {
            return m_type == (int)char_;
        }

        public bool EqualTo(TokenType type_)
        {
            return m_type == (int)type_;
        }

        public override string ToString()
        {
            return string.Format("token_type:{0},\tstring:{1},\tnumber:{2}",
                m_type, m_string, m_number);
        }
    }

    public class Lex
    {
        static Dictionary<string, TokenType> s_reserve_keys;
        static Lex()
        {
            s_reserve_keys = new Dictionary<string, TokenType>()
            {
                {"and", TokenType.AND},
                {"break", TokenType.BREAK},
                {"continue", TokenType.CONTINUE},
                {"else", TokenType.ELSE},
                {"elseif", TokenType.ELSEIF},
                {"false", TokenType.FALSE},
                {"for", TokenType.FOR},
                {"global", TokenType.GLOBAL},
                {"function", TokenType.FUNCTION},
                {"if", TokenType.IF},
                {"in", TokenType.IN},
                {"local", TokenType.LOCAL},
                {"nil", TokenType.NIL},
                {"not", TokenType.NOT},
                {"or", TokenType.OR},
                {"return", TokenType.RETURN},
                {"true", TokenType.TRUE},
                {"while", TokenType.WHILE},
            };
        }

        // 块类型。字符串会被 ${a} 这种语法打断，需要一个栈维护打断的字符串
        public enum BlockType
        {
            Begin,
            
            BigBracket,

            StringBegin,

            SingleQuotation,// ' $x '' x '
            DoubleQuotation,// " $x \n \" \t "
            
            InverseQuotation,// ` ${abc}  `
            InverseThreeQuotation, // ```bash ```
        }
        
        Stack<BlockType> _block_stack = new Stack<BlockType>();

        StringBuilder _buf = new StringBuilder();

        public BlockType CurStringType => _block_stack.Peek();
        public bool IsStringEnded => _block_stack.Peek() < BlockType.StringBegin;

        bool _TryReadNewLine()
        {
            if (_current == '\r' || _current == '\n')
            {
                var c = _current;
                _NextChar();
                if ((_current == '\r' || _current == '\n') && _current != c)
                {
                    _NextChar();
                }
                ++_line;
                _column = 1;
                return true;
            }
            return false;
        }

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

            try
            {
                double num = 0;
                if (_buf[0] == '0' && _buf.Length > 2 && _buf[1] != '.')
                {
                    num = (double)Convert.ToUInt32(_buf.ToString(), 16);
                }
                else
                {
                    num = Convert.ToDouble(_buf.ToString());
                }
                return new Token(num);
            }
            catch
            {
                throw NewLexException($"{_buf} is not valid double");
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
                if (cnt == 'u') cnt = 4;
                else if (cnt == 'U') cnt = 8;
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
                if (i == 0) throw NewLexException($"expect {cnt} hexadecimal number after '\\{head}'");
                _buf.Append(char.ConvertFromUtf32(code));
                return;
            }
            else if (char.IsDigit(_current))// 特殊支持\d[d][d]，三位十进制（不是C++的八进制），需要小于255。
            {
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
            return ret;
        }

        Token _GetNextTokenInString(ref int line, ref int column)
        {
            // $ 语法统一处理
            _buf.Clear();
            if(_current == '$')
            {
                _NextChar();
                if(_current == '{')
                {
                    _NextChar();
                    _block_stack.Push(BlockType.BigBracket);
                    column++;
                    return new Token('{');
                }
                else if(_GetName() != null)
                {
                    column++;
                    return new Token(TokenType.NAME, _buf.ToString());
                }
                else if(_current == '$')
                {
                    _NextChar();// $$ => $
                }
                _buf.Append('$');
            }
            switch (_block_stack.Peek())
            {
                case BlockType.SingleQuotation:
                    _ReadStringInSingleQuotation();
                    break;
                case BlockType.DoubleQuotation:
                    _ReadStringInDoubleQuotation();
                    break;
                case BlockType.InverseQuotation:
                    _ReadStringInInverseQuotation();
                    break;
                case BlockType.InverseThreeQuotation:
                    _ReadStringInInverseThreeQuotation();
                    break;
            }
            if(_current == '\0' && IsStringEnded == false)
            {
                _block_stack.Pop();// 文件尾也可以用于表示字符串结束
            }

            return new Token(_buf.ToString());
        }

        void _ReadStringInSingleQuotation()
        {
            while(_current != '\0' && _current != '$')
            {
                if (_TryReadNewLine())
                {
                    _buf.Append('\n');
                }
                else if (_current == '\'')
                {
                    _NextChar();
                    if (_current == '\'')
                    {
                        _NextChar();
                        _buf.Append('\'');// '' => '
                    }
                    else
                    {
                        _block_stack.Pop();
                        return;
                    }
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
                if (_TryReadNewLine())
                {
                    _buf.Append('\n');
                }
                else if(_current == '\\')
                {
                    _PutSpecialCharInBuf();
                }
                else if (_current == '\"')
                {
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

        void _ReadStringInInverseQuotation()
        {
            while (_current != '\0' && _current != '$')
            {
                if (_TryReadNewLine())
                {
                    _buf.Append('\n');
                }
                else if (_current == '`')
                {
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

        void _ReadStringInInverseThreeQuotation()
        {
            while (_current != '\0' && _current != '$')
            {
                if (_TryReadNewLine())
                {
                    _buf.Append('\n');
                }
                else if (_current == '`')
                {
                    _NextChar();
                    if(_current == '`')
                    {
                        _NextChar();
                        if (_current == '`')
                        {
                            _NextChar();
                            _block_stack.Pop();
                            return;
                        }
                        else
                        {
                            _buf.Append("``");
                        }
                    }
                    else
                    {
                        _buf.Append('`');
                    }
                }
                else
                {
                    _buf.Append(_current);
                    _NextChar();
                }
            }
        }

        void _ReadStringInSquareBrackets()
        {
            Debug.Assert(_current == '[' || _current == '=');

            int equal_cnt = 0;
            while (_current == '=')
            {
                ++equal_cnt;
                _NextChar();
            }
            if (_current != '[')
                throw NewLexException("expect '[' for string");
            _NextChar();
            _buf.Clear();
            _TryReadNewLine();// ignore first \n

            while (_current != '\0')
            {
                if (_current == ']')
                {
                    _NextChar();
                    int i = 0;
                    for (; i < equal_cnt; ++i)
                    {
                        if (_current != '=') break;
                        _NextChar();
                    }
                    if (i == equal_cnt && _current == ']')
                    {
                        if (_buf.Length > 1 && _buf[_buf.Length - 1] == '\n')
                        {
                            _buf.Remove(_buf.Length - 1, 1);// ignore last '\n'
                        }
                        _NextChar();
                        break;// end while
                    }
                    else
                    {
                        _buf.Append(']');
                        _buf.Append('=', i);
                    }
                }
                if (_TryReadNewLine())
                {
                    _buf.Append('\n');
                }
                else
                {
                    _buf.Append(_current);
                    _NextChar();
                }
            }
        }

        void _ReadStringInComment()
        {
            Debug.Assert(_current == '/');
            _NextChar();
            _buf.Clear();
            if(_current == '[')
            {
                _buf.Append(_current);
                _NextChar();
                if(_current == '=' || _current == '[')
                {
                    _ReadStringInSquareBrackets();
                    return;
                }
            }
            // util line end
            while (_current != '\r' && _current != '\n' && _current != '\0')
            {
                _buf.Append(_current);
                _NextChar();
            }
        }

        Token _GetNextToken(ref int line, ref int column)
        {
            if(_block_stack.Peek() > BlockType.StringBegin)
            {
                return _GetNextTokenInString(ref line, ref column);
            }

            while (_current != '\0')
            {
                switch (_current)
                {
                    case '\r':
                    case '\n':
                        _TryReadNewLine();
                        line = _line; column = _column;
                        break;
                    case '{':
                        _NextChar();
                        _block_stack.Push(BlockType.BigBracket);
                        return new Token('{');
                    case '}':
                        _NextChar();
                        _block_stack.Pop();
                        return new Token('}');
                    case '/':
                        _NextChar();
                        if (_current == '/')
                        {
                            _ReadStringInComment();
                            line = _line; column = _column;
                            break;
                        }
                        else
                        {
                            return new Token('/');
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
                    case '.':
                        _NextChar();
                        if (_current == '.')
                        {
                            _NextChar();
                            if (_current == '.')
                            {
                                _NextChar();
                                return new Token(TokenType.DOTS);
                            }
                            else
                            {
                                return new Token(TokenType.CONCAT);
                            }
                        }
                        else if (char.IsDigit(_current))
                        {
                            _buf.Clear();
                            _buf.Append('.');
                            return _ReadNumber();
                        }
                        else
                        {
                            return new Token('.');
                        }
                    //break;
                    case '~':
                        _NextChar();
                        if (_current == '=')
                        {
                            _NextChar();
                            return new Token(TokenType.NE);
                        }
                        else
                        {
                            throw NewLexException("expect '=' after '~'");
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
                        if (_current != '=') return new Token('<');
                        _NextChar();
                        return new Token(TokenType.LE);
                    case '>':
                        _NextChar();
                        if (_current != '=') return new Token('>');
                        _NextChar();
                        return new Token(TokenType.GE);
                    case '\'':
                        _NextChar();
                        _block_stack.Push(BlockType.SingleQuotation);
                        return new Token(TokenType.STRING_BEGIN);
                    case '"':
                        _NextChar();
                        _block_stack.Push(BlockType.DoubleQuotation);
                        return new Token(TokenType.STRING_BEGIN);
                    case '`':
                        _NextChar();
                        if(_current == '`')
                        {
                            _NextChar();
                            if (_current == '`')
                            {
                                _NextChar();
                                _block_stack.Push(BlockType.InverseThreeQuotation);
                                return new Token(TokenType.STRING_BEGIN);
                            }
                            else
                            {
                                throw NewLexException("not support ``, do you mean ```");
                            }
                        }
                        else
                        {
                            _block_stack.Push(BlockType.InverseQuotation);
                            return new Token(TokenType.STRING_BEGIN);
                        }
                    case '[':
                        _NextChar();
                        if (_current == '[' || _current == '=')
                        {
                            _ReadStringInSquareBrackets();
                            return new Token(_buf.ToString());
                        }
                        else
                            return new Token('[');
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
                            if(name != null)
                            {
                                TokenType token_type;
                                if (s_reserve_keys.TryGetValue(name, out token_type))
                                {
                                    return new Token(token_type);
                                }
                                else
                                {
                                    return new Token(TokenType.NAME, name);
                                }
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
        private void _NextChar()
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
        }

        public void Init(string input_, string name_ = "")
        {
            _source_name = name_;
            _source = input_;
            _pos = 0;
            _line = 1;
            _column = 0;
            _block_stack.Clear();
            _block_stack.Push(BlockType.Begin);
            _NextChar();
        }
    }
}
