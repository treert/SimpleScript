﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    enum TokenType
    {
        // reserved words
        AND = 257,
        BREAK,
        CONTINUE,
        DO,
        ELSE,
        ELSEIF,
        END,
        FALSE,
        FOR,
        FOREACH,
        FUNCTION,
        GOTO,// current not used
        IF,
        IN,// current not used
        LOCAL,
        NIL,
        NOT,
        OR,
        RETURN,
        THEN,
        TRUE,
        WHILE,
        // other terminal symbols
        CONCAT,// .. string concat
        DOTS,// ...
        EQ,// ==
        GE,// >=
        LE,// <=
        NE,// !=
        NUMBER,
        STRING,
        NAME,
        EOS,
    }

    class Token
    {
        public int m_type;
        public double m_number;
        public string m_string;

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
            s_reserve_keys = new Dictionary<string, TokenType>();
            s_reserve_keys.Add("and", TokenType.AND);
            s_reserve_keys.Add("break", TokenType.BREAK);
            s_reserve_keys.Add("continue", TokenType.CONTINUE);
            s_reserve_keys.Add("do", TokenType.DO);
            s_reserve_keys.Add("else", TokenType.ELSE);
            s_reserve_keys.Add("elseif", TokenType.ELSEIF);
            s_reserve_keys.Add("end", TokenType.END);
            s_reserve_keys.Add("false", TokenType.FALSE);
            s_reserve_keys.Add("for", TokenType.FOR);
            s_reserve_keys.Add("foreach", TokenType.FOREACH);
            s_reserve_keys.Add("function", TokenType.FUNCTION);
            s_reserve_keys.Add("if", TokenType.IF);
            s_reserve_keys.Add("in", TokenType.IN);
            s_reserve_keys.Add("local", TokenType.LOCAL);
            s_reserve_keys.Add("nil", TokenType.NIL);
            s_reserve_keys.Add("not", TokenType.NOT);
            s_reserve_keys.Add("or", TokenType.OR);
            s_reserve_keys.Add("return", TokenType.RETURN);
            s_reserve_keys.Add("then", TokenType.THEN);
            s_reserve_keys.Add("true", TokenType.TRUE);
            s_reserve_keys.Add("while", TokenType.WHILE);
        }

        private StringBuilder _buf;

        private void _NewLine()
        {
            var c = _current;
            _NextChar();
            if((_current == '\r' || _current == '\n') && _current != c)
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
            if(_current == 'e' || _current == 'E')
            {
                _buf.Append(_current);
                _NextChar();
                if(_current == '+' || _current == '-')
                {
                    _buf.Append(_current);
                    _NextChar();
                }
            }
            while(char.IsLetterOrDigit(_current))
            {
                _buf.Append(_current);
                _NextChar();
            }

            double ret = 0;
            if(double.TryParse(_buf.ToString(), out ret))
            {
                return new Token(ret);
            }
            else
            {
                throw new LexException(_source_name,_line,_column,String.Format("{0} is not valid double",_buf));
            }
        }

        private Token _ReadSingleLineString()
        {
            var quote = _current;
            _NextChar();
            _buf.Clear();
            while(_current != quote)
            {
                if (_current == '\0')
                    throw new LexException(_source_name,_line,_column,"incomplete string at file end");
                if (_current == '\r' || _current == '\n')
                    throw new LexException(_source_name, _line, _column, "incomplete string at line end");
                _PutCharInBuf();
            }
            _NextChar();
            return new Token(_buf.ToString());
        }

        private void _PutCharInBuf()
        {
            if(_current == '\\')
            {
                _NextChar();
                if(_current == 'a')
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
                    for(; i < 2; ++i)
                    {
                        if(char.IsDigit(_current))
                        {
                            code = code*16 + _current - '0';
                        }
                        else if(_current >= 'a' && _current <= 'f')
                        {
                            code = code*16 + _current - 'a';
                        }
                        else{
                            break;
                        }
                        _NextChar();
                    }
                    if(i == 0) throw new LexException(_source_name,_line,_column,"unexpect char after '\\x'");
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
                    if (code > byte.MaxValue) throw new LexException(_source_name,_line,_column,"char code too big");
                    _buf.Append(char.ConvertFromUtf32(code));
                    return;
                }
                else
                    throw new LexException(_source_name,_line,_column,"unexpect character after '\\'");
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
            while(_current == '=')
            {
                ++equal_cnt;
                _NextChar();
            }
            if (_current != '[')
                throw new LexException(_source_name,_line,_column,"incomplete multi line string");
            _NextChar();
            _buf.Clear();
            if (_current == '\r' || _current == '\n')
                _NewLine();

            while(_current != '\0')
            {
                if(_current == ']')
                {
                    _NextChar();
                    int i = 0;
                    for(; i < equal_cnt; ++i)
                    {
                        if (_current != '=') break;
                        _NextChar();
                    }
                    if(i == equal_cnt && _current == ']')
                    {
                        _NextChar();
                        return new Token(_buf.ToString());
                    }
                    else
                    {
                        _buf.Append(']');
                        _buf.Append('=', i);
                    }
                }
                else if(_current == '\r' || _current == '\n')
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

            throw new LexException(_source_name,_line,_column,"incomplete multi line string");
        }

        private void _SkipComment()
        {
            Debug.Assert(_current == '-');
            _NextChar();
            if(_current == '[')
            {
                _NextChar();
                int equal_cnt = 0;
                while(_current == '=')
                {
                    ++equal_cnt;
                    _NextChar();
                }
                if (_current != '[')
                    throw new LexException(_source_name,_line,_column,"incomplete multi line comment");
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
                while(_current != '\r' && _current != '\n' && _current != '\0')
                {
                    _NextChar();
                }
            }
        }

        public Token GetNextToken()
        {
            while(_current != '\0')
            {
                switch(_current){
                    case '\r': case '\n':
                        _NewLine();
                        break;
                    case '-':
                        _NextChar();
                        if (_current != '-') return new Token('-');
                        _SkipComment();
                        break;
                    case '.':
                        _NextChar();
                        if (_current == '.')
                        {
                            _NextChar();
                            if(_current == '.')
                            {
                                _NextChar();
                                return new Token(TokenType.DOTS);
                            }
                            else
                            {
                                return new Token(TokenType.CONCAT);
                            }
                        }
                        else if(char.IsDigit(_current))
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
                        if(_current == '=')
                        {
                            _NextChar();
                            return new Token(TokenType.NE);
                        }
                        else
                        {
                            throw new LexException(_source_name,_line,_column,"expect '=' after '~'");
                        }
                        //break;
                    case '=':
                        _NextChar();
                        if(_current == '=')
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
                        if(char.IsWhiteSpace(_current))
                        {
                            _NextChar();
                            continue;
                        }
                        else if(char.IsDigit(_current))
                        {
                            _buf.Clear();
                            return _ReadNumber();
                        }
                        else if(_current == '_' || char.IsLetter(_current))
                        {
                            _buf.Clear();
                            do{
                                _buf.Append(_current);
                                _NextChar();
                            } while (_current == '_' || char.IsLetterOrDigit(_current));
                            TokenType token_type;
                            if(s_reserve_keys.TryGetValue(_buf.ToString(), out token_type))
                            {
                                return new Token(token_type);
                            }
                            else
                            {
                                return new Token(TokenType.NAME,_buf.ToString());
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

        private string _source_name;
        private string _source;
        private char _current;
        private int _pos;
        private int _line;
        private int _column;
        private void _NextChar()
        {
            if(_pos < _source.Length)
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

        public void Init(string input_)
        {
            _source_name = "test";
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