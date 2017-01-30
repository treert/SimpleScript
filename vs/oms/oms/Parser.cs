using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oms
{
    // 语法结构树

    class Parser
    {
        Lex _lex;
        Token _current;
        Token _look_ahead;
        Token _look_ahead2;
        Token NextToken()
        {
            if(_look_ahead != null)
            {
                _current = _look_ahead;
                _look_ahead = _look_ahead2;
                _look_ahead2 = null;
            }
            else
            {
                _current = _lex.GetNextToken();
            }
            return _current;
        }
        Token LookAhead()
        {
            if (_look_ahead == null)
                _look_ahead = _lex.GetNextToken();
            return _look_ahead;
        }
        Token LookAhead2()
        {
            LookAhead();
            if (_look_ahead2 == null)
                _look_ahead2 = _lex.GetNextToken();
            return _look_ahead2;
        }
        bool IsMainExp()
        {
            int token_type = LookAhead().m_type;
            return
                token_type == (int)TokenType.NIL ||
                token_type == (int)TokenType.FALSE ||
                token_type == (int)TokenType.TRUE ||
                token_type == (int)TokenType.NUMBER ||
                token_type == (int)TokenType.STRING ||
                token_type == (int)TokenType.DOTS ||
                token_type == (int)TokenType.FUNCTION ||
                token_type == (int)TokenType.NAME ||
                token_type == (int)'(' ||
                token_type == (int)'{';
        }
        SyntaxTree ParseExp()
        {
            return null;
        }
        SyntaxTree ParseMainExp()
        {
            return null;
        }
        SyntaxTree ParseExpList()
        {
            return null;
        }
        SyntaxTree ParseVarList()
        {
            return null;
        }
        SyntaxTree ParseChunk()
        {
            var block = ParseBlock();
            if (NextToken().m_type != (int)TokenType.EOS)
                throw new ParserException("expect <eof>");
            var tree = new Chunk();
            tree.block = block;
            return tree;
        }
        SyntaxTree ParseBlock()
        {
            var block = new Block();
            for (; ; )
            {
                SyntaxTree statement = null;
                var token_ahead = LookAhead();
                switch (token_ahead.m_type)
                {
                    case (int)';':
                        NextToken(); continue;
                    case (int)TokenType.DO:
                        statement = ParseDoStatement(); break;
                    case (int)TokenType.WHILE:
                        statement = ParseWhileStatement(); break;
                    case (int)TokenType.IF:
                        statement = ParseIfStatement(); break;
                    case (int)TokenType.FOR:
                        statement = ParseForStatement(); break;
                    case (int)TokenType.FUNCTION:
                        statement = ParseFunctionStatement(); break;
                    case (int)TokenType.LOCAL:
                        statement = ParseLocalStatement(); break;
                    case (int)TokenType.RETURN:
                        statement = ParseReturnStatement(); break;
                    case (int)TokenType.BREAK:
                        statement = ParseBreakStatement(); break;
                    case (int)TokenType.CONTINUE:
                        statement = ParseContinueStatement(); break;
                    default:
                        if (IsMainExp())
                        {
                            statement = ParseOtherStatement();
                        }
                        break;
                }
                if (statement == null)
                    break;
                // lua will check {return,break}
                block.statements.Add(statement);
            }
            return block;
        }
        SyntaxTree ParseFunctionDef()
        {
            return null;
        }
        SyntaxTree ParseFunctionBody()
        {
            return null;
        }
        SyntaxTree ParseParamList()
        {
            return null;
        }
        SyntaxTree ParseReturnStatement()
        {
            return new ReturnStatement();
        }
        SyntaxTree ParseBreakStatement()
        {
            return new BreakStatement();
        }
        SyntaxTree ParseContinueStatement()
        {
            return new ContinueStatement();
        }
        SyntaxTree ParseDoStatement()
        {
            NextToken();// skip 'do'

            var block = ParseBlock();
            if (NextToken().m_type != (int)TokenType.END)
                throw new ParserException("expect 'end' for do-statement");

            var do_statement = new DoStatement();
            do_statement.block = block;
            return do_statement;
        }
        SyntaxTree ParseWhileStatement()
        {
            NextToken();// skip 'while'

            var exp = ParseExp();
            if (NextToken().m_type != (int)TokenType.DO)
                throw new ParserException("expect 'do' for while-statement");

            var block = ParseBlock();
            if (NextToken().m_type != (int)TokenType.END)
                throw new ParserException("expect 'end' for while-statement");

            var statement = new WhileStatement();
            statement.exp = exp;
            statement.block = block;
            return statement;
        }
        SyntaxTree ParseIfStatement()
        {
            NextToken();// skip 'if' or 'elseif'

            var exp = ParseExp();
            if (NextToken().m_type != (int)TokenType.THEN)
                throw new ParserException("expect 'then' for if-statement");

            var true_branch = ParseBlock();
            var false_branch = ParseFalseBranchStatement();

            var statement = new IfStatement();
            statement.exp = exp;
            statement.true_branch = true_branch;
            statement.false_branch = false_branch;
            return statement;
        }
        SyntaxTree ParseFalseBranchStatement()
        {
            if (LookAhead().m_type == (int)TokenType.ELSEIF)
            {
                return ParseIfStatement();
            }
            else if (LookAhead().m_type == (int)TokenType.ELSE)
            {
                NextToken();
                var block = ParseBlock();
                if (NextToken().m_type != (int)TokenType.END)
                    throw new ParserException("expect 'end' for else-statement");
                return block;
            }
            else if (LookAhead().m_type == (int)TokenType.END)
            {
                NextToken();
                return null;
            }
            else
                throw new ParserException("expect 'end' for if-statement");
        }
        SyntaxTree ParseFunctionStatement()
        {
            NextToken();// skip 'function'

            var statement = new FunctionStatement();
            statement.func_name = ParseFunctionName();
            statement.func_body = ParseFunctionBody();
            return statement;
        }
        SyntaxTree ParseFunctionName()
        {
            if (NextToken().m_type != (int)TokenType.NAME)
                throw new ParserException("unexpect token after 'function'");

            var func_name = new FunctionName();
            func_name.names.Add(_current);
            while(LookAhead().m_type == (int)'.')
            {
                NextToken();
                if (NextToken().m_type != (int)TokenType.NAME)
                    throw new ParserException("unexpect token in function name after '.'");
                func_name.names.Add(_current);
            }

            if(LookAhead().m_type == (int)':')
            {
                NextToken();
                if (NextToken().m_type != (int)TokenType.NAME)
                    throw new ParserException("unexpect token in function name after ':'");
                func_name.member_name = _current;
            }

            return func_name;
        }
        SyntaxTree ParseForStatement()
        {
            NextToken();// skip 'for'

            if (LookAhead().m_type != (int)TokenType.NAME)
                throw new ParserException("expect 'id' after 'for'");
            if (LookAhead2().m_type == (int)'=')
                return ParseForNumStatement();
            else
                return ParseForInStatement();
        }
        SyntaxTree ParseForNumStatement()
        {
            var name = NextToken();
            Debug.Assert(_current.m_type == (int)TokenType.NAME);
            NextToken();// skip '='
            Debug.Assert(_current.m_type == (int)'=');

            var statement = new ForStatement();
            statement.name = name;
            statement.exp1 = ParseExp();
            if (NextToken().m_type != (int)',')
                throw new ParserException("expect ',' in for-statement");
            statement.exp2 = ParseExp();
            if(LookAhead().m_type == ',')
            {
                NextToken();
                statement.exp3 = ParseExp();
            }

            if (NextToken().m_type != (int)TokenType.DO)
                throw new ParserException("expect 'do' to start for-body");
            statement.block = ParseBlock();
            if (NextToken().m_type != (int)TokenType.END)
                throw new ParserException("expect 'do' to complete for-body");

            return statement;
        }
        SyntaxTree ParseForInStatement()
        {
            var statement = new ForInStatement();
            statement.name_list = ParseNameList();
            if (NextToken().m_type != (int)TokenType.IN)
                throw new ParserException("expect 'in' in for-in-statement");
            statement.exp_list = ParseExpList();

            if (NextToken().m_type != (int)TokenType.DO)
                throw new ParserException("expect 'do' to start for-in-body");
            statement.block = ParseBlock();
            if (NextToken().m_type != (int)TokenType.END)
                throw new ParserException("expect 'do' to complete for-in-body");

            return statement;
        }
        SyntaxTree ParseForEachStatement()
        {
            var statement = new ForEachStatement();
            if(NextToken().m_type != (int)TokenType.NAME)
                throw new ParserException("expect 'id' in foreach-statement");
            if(LookAhead().m_type == (int)',')
            {
                statement.k = _current;
                NextToken();
                if(NextToken().m_type != (int)TokenType.NAME)
                    throw new ParserException("expect 'id' in foreach-statement after ','");
                statement.v = _current;
            }
            else
            {
                statement.v = _current;
            }

            if (NextToken().m_type != (int)TokenType.IN)
                throw new ParserException("expect 'in' in foreach-statement");
            statement.exp = ParseExp();

            if (NextToken().m_type != (int)TokenType.DO)
                throw new ParserException("expect 'do' to start foreach-body");
            statement.block = ParseBlock();
            if (NextToken().m_type != (int)TokenType.END)
                throw new ParserException("expect 'do' to complete foreach-body");

            return statement;
        }
        SyntaxTree ParseLocalStatement()
        {
            NextToken();// skip 'local'

            if (LookAhead().m_type == (int)TokenType.FUNCTION)
                return ParseLocalFunction();
            else if (LookAhead().m_type == (int)TokenType.NAME)
                return ParseLocalNameList();
            else
                throw new ParserException("unexpect token after 'local'");
        }
        SyntaxTree ParseOtherStatement()
        {
            return null;
        }
        SyntaxTree ParseLocalFunction()
        {
            return null;
        }
        SyntaxTree ParseNameList()
        {
            return null;
        }
        SyntaxTree ParseLocalNameList()
        {
            return null;
        }

        public SyntaxTree Parse(Lex lex_)
        {
            _lex = lex_;
            return ParseChunk();
        }
    }
}
