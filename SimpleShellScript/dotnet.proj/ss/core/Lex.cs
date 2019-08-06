using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS
{
    enum TokenType
    {
        // reserved words
        AND = 257,
        BREAK,
        CONTINUE,// break and continue use exception to implement
        DO,// do not like lua, not use in do-while
        ELSE,
        ELSEIF,
        FALSE,
        FOR,
        FOREACH,
        FUNCTION,// use def like python
        IF,
        IN,
        LOCAL,
        NIL,// use null
        NOT,
        OR,
        REPEAT,// current not used
        RETURN,
        THEN,
        TRUE,
        UNTIL,// current not used
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
        ADD_ONE,// ++
        DEC_ONE,// --
        NUMBER,
        STRING,
        NAME,
        // End
        EOS,
    }

    class Token
    {
        public int m_type;
        public double m_number;
        public string m_string;
        // for error report
        public int m_line;
        public int m_column;

        public Token()
        {
            if xx {

            }
            else
            {

            }
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

    class Lex
    {
        static Dictionary<string, TokenType> s_reserve_keys;
        static Lex()
        {
            s_reserve_keys = new Dictionary<string, TokenType>()
            {
                {"and", TokenType.AND},
                {"break", TokenType.BREAK},
                {"continue", TokenType.CONTINUE},
                {"do", TokenType.DO},
                {"echo", TokenType.ECHO},
                {"else", TokenType.ELSE},
                {"elseif", TokenType.ELSEIF},
                {"end", TokenType.END},
                {"false", TokenType.FALSE},
                {"for", TokenType.FOR},
                {"foreach", TokenType.FOREACH},
                {"def", TokenType.FUNCTION},
                {"if", TokenType.IF},
                {"in", TokenType.IN},
                {"local", TokenType.LOCAL},
                {"nil", TokenType.NIL},
                {"not", TokenType.NOT},
                {"or", TokenType.OR},
                {"repeat", TokenType.REPEAT},
                {"return", TokenType.RETURN},
                {"then", TokenType.THEN},
                {"true", TokenType.TRUE},
                {"while", TokenType.WHILE},
                {"until", TokenType.UNTIL},
            };
        }

        private StringBuilder _buf;

        private void _NewLine()
        {
            var c = _current;
            _NextChar();
            if ((_current == '\r' || _current == '\n') && _current != c)
            {
                _NextChar();
            }
            ++_line;
            _column = 1;
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
                throw NewLexException(String.Format("{0} is not valid double", _buf));
            }
        }

        private Token _ReadSingleLineString()
        {
            var quote = _current;
            _NextChar();
            _buf.Clear();
            while (_current != quote)
            {
                if (_current == '\r' || _current == '\n' || _current == '\0')
                    throw NewLexException("incomplete string at line end");
                _PutCharInBuf();
            }
            _NextChar();
            return new Token(_buf.ToString());
        }

        private void _PutCharInBuf()
        {
            if (_current == '\\')
            {
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
                else if (_current == 'x')
                {
                    _NextChar();
                    int code = 0;
                    int i = 0;
                    for (; i < 2; ++i)
                    {
                        if (char.IsDigit(_current))
                        {
                            code = code * 16 + _current - '0';
                        }
                        else if (_current >= 'a' && _current <= 'f')
                        {
                            code = code * 16 + _current - 'a';
                        }
                        else
                        {
                            break;
                        }
                        _NextChar();
                    }
                    if (i == 0) throw NewLexException("unexpect char after '\\x'");
                    _buf.Append(char.ConvertFromUtf32(code));
                    return;
                }
                else if (char.IsDigit(_current))
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
                    if (code > byte.MaxValue) throw NewLexException("char code too big");
                    _buf.Append(char.ConvertFromUtf32(code));
                    return;
                }
                else
                    throw NewLexException("unexpect character after '\\'");
            }
            else
            {
                _buf.Append(_current);
            }
            _NextChar();
        }

        private Token _ReadMultiLineString()
        {
            int equal_cnt = 0;
            while (_current == '=')
            {
                ++equal_cnt;
                _NextChar();
            }
            if (_current != '[')
                throw NewLexException("incomplete multi line string");
            _NextChar();
            _buf.Clear();
            if (_current == '\r' || _current == '\n')
                _NewLine();// ignore first \n

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
                        _NextChar();
                        break;// break while
                    }
                    else
                    {
                        _buf.Append(']');
                        _buf.Append('=', i);
                    }
                }
                else if (_current == '\r' || _current == '\n')
                {
                    _buf.Append('\n');
                    _NewLine();
                }
                else
                {
                    _buf.Append(_current);
                    _NextChar();
                }
            }
            if (_current == '\0' && _buf.Length > 1)
            {
                if (_buf[_buf.Length - 1] == '\n')
                {
                    _buf.Remove(_buf.Length - 1, 1);// ignore last '\n'
                }
            }
            return new Token(_buf.ToString());
            //throw NewLexException("incomplete multi line string");
        }

        private void _SkipComment()
        {
            Debug.Assert(_current == '/');
            _NextChar();
            if (_current == '[')
            {
                _NextChar();
                int equal_cnt = 0;
                while (_current == '=')
                {
                    ++equal_cnt;
                    _NextChar();
                }
                if (_current != '[')
                    throw NewLexException("incomplete multi line comment");
                _NextChar();

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
                            _NextChar();
                            return;// end of multi line comment
                        }
                    }
                    else if (_current == '\r' || _current == '\n')
                    {
                        _NewLine();
                    }
                    else
                    {
                        _NextChar();
                    }
                }
            }
            else
            {
                while (_current != '\r' && _current != '\n' && _current != '\0')
                {
                    _NextChar();
                }
            }
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

        Token _GetNextToken(ref int line, ref int column)
        {
            while (_current != '\0')
            {
                switch (_current)
                {
                    case '\r':
                    case '\n':
                        _NewLine();
                        line = _line; column = _column;
                        break;
                    case '/':
                        _NextChar();
                        if (_current == '/')
                        {
                            _SkipComment();
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
                    case '"':
                        return _ReadSingleLineString();
                    case '[':
                        _NextChar();
                        if (_current == '[' || _current == '=')
                            return _ReadMultiLineString();
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
                        else if (_current == '_' || char.IsLetter(_current))
                        {
                            _buf.Clear();
                            do
                            {
                                _buf.Append(_current);
                                _NextChar();
                            } while (_current == '_' || char.IsLetterOrDigit(_current));
                            TokenType token_type;
                            if (s_reserve_keys.TryGetValue(_buf.ToString(), out token_type))
                            {
                                return new Token(token_type);
                            }
                            else
                            {
                                return new Token(TokenType.NAME, _buf.ToString());
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
            return new Token();
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
            _NextChar();
        }

        public Lex()
        {
            _buf = new StringBuilder();
        }
    }
}
