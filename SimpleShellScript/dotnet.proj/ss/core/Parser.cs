using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleScript
{
    class Parser
    {
        static bool IsExpReturnAnyCountValue(SyntaxTree t)
        {
            if (t is Terminator)
            {
                return (t as Terminator).token.m_type == (int)TokenType.DOTS;
            }
            else if (t is FuncCall)
            {
                return true;
            }
            return false;
        }

        static bool IsVar(SyntaxTree t)
        {
            return t is TableAccess || t is Terminator;
        }

        Lex _lex;
        Token _current;
        Token _look_ahead;
        Token _look_ahead2;
        Token NextToken()
        {
            if (_look_ahead != null)
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
                token_type == (int)TokenType.STRING_BEGIN || 
                token_type == (int)TokenType.DOTS ||
                token_type == (int)TokenType.FUNCTION ||
                token_type == (int)TokenType.NAME ||
                token_type == (int)'(' ||
                token_type == (int)'{' ||
                token_type == (int)'-' ||
                token_type == (int)TokenType.NOT;
        }
        int GetOpPriority(Token t)
        {
            switch (t.m_type)
            {
                case (int)'^': return 100;
                case (int)'*':
                case (int)'/':
                case (int)'%': return 80;
                case (int)'+':
                case (int)'-': return 70;
                case (int)TokenType.CONCAT: return 60;
                case (int)'>':
                case (int)'<':
                case (int)TokenType.GE:
                case (int)TokenType.LE:
                case (int)TokenType.NE:
                case (int)TokenType.EQ: return 50;
                case (int)TokenType.AND: return 40;
                case (int)TokenType.OR: return 30;
                default: return 0;
            }
        }
        bool IsRightAssociation(Token t)
        {
            return t.m_type == (int)'^';
        }

        SyntaxTree ParseExp(int left_priority = 0)
        {
            var exp = ParseMainExp();
            while (true)
            {
                int right_priority = GetOpPriority(LookAhead());
                if (left_priority < right_priority || (left_priority == right_priority && IsRightAssociation(LookAhead())))
                {
                    // C++的函数参数执行顺序没有明确定义，方便起见，不在函数参数里搞出两个有依赖的函数调用，方便往C++里迁移
                    var op = NextToken();
                    exp = new BinaryExpression(exp, op, ParseExp(right_priority));
                }
                else
                {
                    return exp;
                }
            }
        }

        SyntaxTree ParseConditionExp()
        {
            if (LookAhead().EqualTo('{'))
            {
                throw NewParserException("condition exp should not start with '{'", _look_ahead);
            }
            return ParseExp();
        }

        SyntaxTree ParseMainExp()
        {
            SyntaxTree exp;
            switch (LookAhead().m_type)
            {
                case (int)TokenType.NIL:
                case (int)TokenType.FALSE:
                case (int)TokenType.TRUE:
                case (int)TokenType.NUMBER:
                case (int)TokenType.STRING:
                case (int)TokenType.DOTS:
                    exp = new Terminator(NextToken());
                    break;
                case (int)TokenType.STRING_BEGIN:
                    exp = ParseComplexString();
                    break;
                case (int)TokenType.FUNCTION:
                    exp = ParseFunctionDef();
                    break;
                case (int)TokenType.NAME:
                case (int)'(':
                    exp = ParsePrefixExp();
                    break;
                case (int)'{':
                    exp = ParseTableConstructor();
                    break;
                // unop exp priority is 90 less then ^
                case (int)'-':
                case (int)TokenType.NOT:
                    var unexp = new UnaryExpression(LookAhead().m_line);
                    unexp.op = NextToken();
                    unexp.exp = ParseExp(90);
                    exp = unexp;
                    break;
                default:
                    throw NewParserException("unexpect token for main exp", _look_ahead);
            }
            return exp;
        }

        private ComplexString ParseComplexString()
        {
            var head = NextToken();
            var next = LookAhead();

            var exp = new ComplexString(_current.m_line);
            if(head.m_string_type >= StringBlockType.InverseQuotation)
            {
                exp.is_shell = true;
                if (next.EqualTo(TokenType.STRING)
                    && next.m_string.Length > 3
                    && head.m_string_type == StringBlockType.InverseThreeQuotation)
                {
                    int idx = 0;
                    var str = next.m_string;
                    while(idx < str.Length && char.IsLetter(str[idx]))
                    {
                        idx++;
                    }
                    if(idx < str.Length && str[idx] == ' ')
                    {
                        exp.shell_name = str.Substring(0, idx);
                        next.m_string = str.Substring(idx + 1);
                    }
                }
            }

            do
            {
                next = NextToken();
                if (next.EqualTo(TokenType.STRING) || next.EqualTo(TokenType.NAME))
                {
                    var term = new Terminator(next);
                    exp.list.Add(term);
                }
                else if(next.EqualTo('{'))
                {
                    var term = ParseComplexItem();
                    exp.list.Add(term);
                }
            } while (next.IsStringEnded);


            return exp;
        }

        ComplexStringItem ParseComplexItem()
        {
            var item = new ComplexStringItem(_current.m_line);
            item.exp = ParseExp();
            var next = NextToken();
            if(next.EqualTo(','))
            {
                next = NextToken();
                if (next.EqualTo(TokenType.NUMBER))
                {
                    item.len = (int)next.m_number;
                    if(item.len != next.m_number)
                    {
                        throw NewParserException($"complex string item len must be int, now is {next.m_number}", next);
                    }
                }
                else
                {
                    throw NewParserException($"complex string item len must be int type", next);
                }
                next = NextToken();
            }
            if (next.EqualTo('|'))
            {
                next = NextToken();
                if (next.EqualTo(TokenType.NAME))
                {
                    item.format = next.m_string;
                }
                else
                {
                    throw NewParserException($"complex string item format must be a string", next);
                }
                next = NextToken();
            }
            if (next.EqualTo('}'))
            {
                return item;
            }
            else
            {
                throw NewParserException("complex string item expect '}' to end", next);
            }
        }

        ExpressionList ParseExpList(bool is_args = false)
        {
            var exp = new ExpressionList(LookAhead().m_line);
            exp.exp_list.Add(ParseExp());
            while (LookAhead().m_type == (int)',')
            {
                NextToken();
                if (is_args && LookAhead().m_type == (int)')')
                {
                    break;// func call args can have a extra ","
                }
                exp.exp_list.Add(ParseExp());
            }
            return exp;
        }
        FunctionBody ParseFunctionDef()
        {
            NextToken();
            return ParseFunctionBody();
        }
        TableAccess ParseTableAccessor(SyntaxTree table)
        {
            NextToken();// skip '[' or '.'

            var index_access = new TableAccess(_current.m_line);
            index_access.table = table;
            if (_current.m_type == (int)'[')
            {
                index_access.index = ParseExp();
                if (NextToken().m_type != (int)']')
                    throw NewParserException("expect ']'", _current);
            }
            else
            {
                if (NextToken().m_type != (int)TokenType.NAME)
                    throw NewParserException("expect 'id' after '.'", _current);
                index_access.index = new Terminator(new Token(_current.m_string));
            }
            return index_access;
        }
        FuncCall ParseFunctionCall(SyntaxTree caller)
        {
            var func_call = new FuncCall(LookAhead().m_line);
            func_call.caller = caller;
            func_call.args = ParseArgs();
            return func_call;
        }
        ExpressionList ParseArgs()
        {
            ExpressionList exp_list = null;
            if (LookAhead().m_type == (int)'(')
            {
                NextToken();
                if (LookAhead().m_type != (int)')')
                    exp_list = ParseExpList(true);

                if (NextToken().m_type != (int)')')
                    throw NewParserException("expect ')' to end function-args", _current);
            }
            else if (LookAhead().m_type == (int)'{')
            {
                exp_list = new ExpressionList(LookAhead().m_line);
                exp_list.exp_list.Add(ParseTableConstructor());
            }
            else
                throw NewParserException("expect '(' or '{' to start function-args", _look_ahead);
            return exp_list;
        }
        SyntaxTree ParsePrefixExp()
        {
            NextToken();
            Debug.Assert(_current.m_type == (int)TokenType.NAME
                || _current.m_type == (int)'(');
            SyntaxTree exp;
            if (_current.m_type == (int)'(')
            {
                exp = ParseExp();
                if (NextToken().m_type != (int)')')
                    throw NewParserException("expect ')'", _current);
            }
            else
            {
                exp = new Terminator(_current);
            }

            // table index or func call
            for (; ; )
            {
                if (LookAhead().m_type == (int)'['
                    || LookAhead().m_type == (int)'.')
                {
                    exp = ParseTableAccessor(exp);
                }
                else if (LookAhead().m_type == (int)'('
                    || LookAhead().m_type == (int)'{')
                {
                    exp = ParseFunctionCall(exp);
                }
                else
                {
                    break;
                }
            }
            return exp;
        }
        SyntaxTree ParseOtherStatement()
        {
            // lua做了限制，其他语句只有两种，assign statement and func call
            // SS还增加几个语法支持，+=，-=，++，--
            SyntaxTree exp;
            if (LookAhead().m_type == (int)TokenType.NAME)
            {
                exp = ParsePrefixExp();
                if (IsVar(exp))
                {
                    if (LookAhead().m_type == (int)TokenType.ADD_ONE)
                    {
                        // ++
                        NextToken();
                        var special_statement = new SpecialAssginStatement(_current.m_line);
                        special_statement.var = exp;
                        special_statement.is_add_op = true;
                        return special_statement;
                    }
                    else if (LookAhead().m_type == (int)TokenType.ADD_SELF)
                    {
                        // +=
                        NextToken();
                        var special_statement = new SpecialAssginStatement(_current.m_line);
                        special_statement.var = exp;
                        special_statement.exp = ParseExp();
                        special_statement.is_add_op = true;
                        return special_statement;
                    }
                    else if (LookAhead().m_type == (int)TokenType.DEC_ONE)
                    {
                        // --
                        NextToken();
                        var special_statement = new SpecialAssginStatement(_current.m_line);
                        special_statement.var = exp;
                        special_statement.is_add_op = false;
                        return special_statement;
                    }
                    else if (LookAhead().m_type == (int)TokenType.DEC_SELF)
                    {
                        // -=
                        NextToken();
                        var special_statement = new SpecialAssginStatement(_current.m_line);
                        special_statement.var = exp;
                        special_statement.exp = ParseExp();
                        special_statement.is_add_op = false;
                        return special_statement;
                    }

                    // assign statement
                    var assign_statement = new AssignStatement(LookAhead().m_line);
                    assign_statement.var_list.Add(exp);
                    while (LookAhead().m_type != (int)'=')
                    {
                        if (NextToken().m_type != (int)',')
                            throw NewParserException("expect ',' to split var-list", _current);
                        if (LookAhead().m_type != (int)TokenType.NAME)
                            throw NewParserException("expect 'id' to start var", _look_ahead);
                        exp = ParsePrefixExp();
                        if (!IsVar(exp))
                            throw NewParserException("expect var here", _current);
                        assign_statement.var_list.Add(exp);
                    }
                    NextToken();// skip '='
                    assign_statement.exp_list = ParseExpList();

                    return assign_statement;
                }
                else
                {
                    Debug.Assert(exp is FuncCall);
                    return exp;
                }
            }
            else
            {
                if (IsMainExp())
                    throw NewParserException("unsupport statement", _look_ahead);
                return null;
            }
        }
        TableField ParseTableIndexField()
        {
            NextToken();
            var field = new TableField(_current.m_line);
            field.index = ParseExp();
            if (NextToken().m_type != ']')
                throw NewParserException("expect ']'", _current);
            if (NextToken().m_type != '=')
                throw NewParserException("expect '='", _current);
            field.value = ParseExp();
            return field;
        }
        TableField ParseTableNameField()
        {
            var field = new TableField(LookAhead().m_line);
            field.index = new Terminator(new Token(NextToken().m_string));
            NextToken();// skip '='
            field.value = ParseExp();
            return field;
        }
        TableField ParseTableArrayField()
        {
            var field = new TableField(LookAhead().m_line);
            field.index = null;// default is null
            field.value = ParseExp();
            return field;
        }
        TableDefine ParseTableConstructor()
        {
            NextToken();
            var table = new TableDefine(_current.m_line);
            TableField last_field = null;
            while (LookAhead().m_type != '}')
            {
                if (LookAhead().m_type == (int)'[')
                    last_field = ParseTableIndexField();
                else if (LookAhead().m_type == (int)TokenType.NAME
                    && LookAhead2().m_type == (int)'=')
                    last_field = ParseTableNameField();
                else
                    last_field = ParseTableArrayField();

                table.fields.Add(last_field);

                if (LookAhead().m_type != '}')
                {
                    NextToken();
                    if (_current.m_type != (int)','
                        && _current.m_type != (int)';')
                        throw NewParserException("expect ',' or ';' to split table fields", _current);
                }
            }
            if (NextToken().m_type != '}')
                throw NewParserException("expect '}' for table", _current);

            if (last_field != null && last_field.index == null)
            {
                table.last_field_append_table = IsExpReturnAnyCountValue(last_field.value);
            }

            return table;
        }
        ModuleTree ParseModule()
        {
            var block = new BlockTree(LookAhead().m_line);
            ParseStatements(block.statements);
            if (NextToken().m_type != (int)TokenType.EOS)
                throw NewParserException("expect <eof>", _current);

            var tree = new ModuleTree();
            tree.block = block;
            return tree;
        }
        BlockTree ParseBlock()
        {
            if (NextToken().EqualTo('{'))
                throw NewParserException("expect '{' to begin block", _current);

            var block = new BlockTree(_current.m_line);
            ParseStatements(block.statements);

            if (NextToken().EqualTo('}'))
                throw NewParserException("expect '}' to end block", _current);
            return block;
        }

        void ParseStatements(List<SyntaxTree> list)
        {
            for (; ; )
            {
                SyntaxTree statement = null;
                var token_ahead = LookAhead();
                switch (token_ahead.m_type)
                {
                    case (int)';':
                        NextToken(); continue;
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
                    case (int)TokenType.TRY:
                        statement = ParseTryStatement(); break;
                    default:
                        statement = ParseOtherStatement();
                        break;
                }
                if (statement == null)
                    break;
                list.Add(statement);
            }
        }

        private SyntaxTree ParseTryStatement()
        {
            NextToken();
            var statement = new TryStatement(_current.m_line);
            statement.block = ParseBlock();
            if (LookAhead().EqualTo(TokenType.CATCH))
            {
                NextToken();
                if (LookAhead().EqualTo(TokenType.NAME))
                {
                    statement.catch_name = NextToken();
                }
                statement.catch_block = ParseBlock();
            }
            return statement;
        }

        ReturnStatement ParseReturnStatement()
        {
            NextToken();
            var statement = new ReturnStatement(_current.m_line);
            if (IsMainExp())
            {
                statement.exp_list = ParseExpList();
            }
            return statement;
        }
        BreakStatement ParseBreakStatement()
        {
            NextToken();
            return new BreakStatement(_current.m_line);
        }
        ContinueStatement ParseContinueStatement()
        {
            NextToken();
            return new ContinueStatement(_current.m_line);
        }

        WhileStatement ParseWhileStatement()
        {
            NextToken();// skip 'while'
            var statement = new WhileStatement(_current.m_line);

            var exp = ParseConditionExp();
            var block = ParseBlock();

            statement.exp = exp;
            statement.block = block;
            return statement;
        }
        IfStatement ParseIfStatement()
        {
            NextToken();// skip 'if' or 'elseif'
            var statement = new IfStatement(_current.m_line);

            var exp = ParseConditionExp();
            var true_branch = ParseBlock();
            var false_branch = ParseFalseBranchStatement();

            statement.exp = exp;
            statement.true_branch = true_branch;
            statement.false_branch = false_branch;
            return statement;
        }
        SyntaxTree ParseFalseBranchStatement()
        {
            if (LookAhead().m_type == (int)TokenType.ELSEIF)
            {
                // syntax sugar for elseif
                return ParseIfStatement();
            }
            else if (LookAhead().m_type == (int)TokenType.ELSE)
            {
                NextToken();
                var block = ParseBlock();
                return block;
            }
            else
            {
                return null;
            }
        }
        FunctionStatement ParseFunctionStatement()
        {
            NextToken();// skip 'function'

            var statement = new FunctionStatement(_current.m_line);
            statement.func_name = ParseFunctionName();
            statement.func_body = ParseFunctionBody();
            return statement;
        }
        FunctionName ParseFunctionName()
        {
            if (NextToken().m_type != (int)TokenType.NAME)
                throw NewParserException("expect 'id' after 'function'", _current);

            var func_name = new FunctionName(_current.m_line);
            func_name.names.Add(_current);
            while (LookAhead().m_type == (int)'.')
            {
                NextToken();
                if (NextToken().m_type != (int)TokenType.NAME)
                    throw NewParserException("unexpect token in function name after '.'", _current);
                func_name.names.Add(_current);
            }

            return func_name;
        }
        FunctionBody ParseFunctionBody()
        {
            var statement = new FunctionBody(_current.m_line);
            statement.param_list = ParseParamList();
            statement.block = ParseBlock();

            return statement;
        }
        ParamList ParseParamList()
        {
            var statement = new ParamList(LookAhead().m_line);
            statement.name_list.Add(new Token(Config.MAGIC_THIS));

            if (LookAhead().EqualTo('('))
            {
                NextToken();
                // special func(a,b,c,) is OK
                while (LookAhead().m_type == (int)TokenType.NAME)
                {
                    statement.name_list.Add(NextToken());
                    if (LookAhead().m_type == (int)',')
                    {
                        NextToken();
                    }
                    else
                    {
                        break;
                    }
                }
                if (LookAhead().m_type == (int)TokenType.DOTS)
                {
                    NextToken();
                    statement.is_var_arg = true;
                }

                if (NextToken().EqualTo(')') == false)
                {
                    throw NewParserException("unexpect token at param-list end", _look_ahead);
                }
            }
            else if(LookAhead().EqualTo('{') == false)
            {
                throw NewParserException("expect '(' or '{' to start function body", _look_ahead);
            }

            return statement;
        }
        SyntaxTree ParseForStatement()
        {
            NextToken();// skip 'for'
            if (LookAhead().EqualTo('{'))
            {
                var statement = new ForeverStatement(_current.m_line);
                statement.block = ParseBlock();
                return statement;
            }
            else if (LookAhead().m_type != (int)TokenType.NAME)
            {
                throw NewParserException("expect 'id' or '{' after 'for'", _look_ahead);
            }

            if (LookAhead2().m_type == (int)'=')
                return ParseForNumStatement();
            else
                return ParseForInStatement();
        }
        ForStatement ParseForNumStatement()
        {
            var statement = new ForStatement(_current.m_line);
            var name = NextToken();
            Debug.Assert(_current.m_type == (int)TokenType.NAME);
            NextToken();// skip '='
            Debug.Assert(_current.m_type == (int)'=');

            statement.name = name;
            statement.exp1 = ParseExp();
            if (NextToken().m_type != (int)',')
                throw NewParserException("expect ',' in for-statement", _current);
            statement.exp2 = ParseExp();
            if (LookAhead().m_type == ',')
            {
                NextToken();
                statement.exp3 = ParseExp();
            }

            statement.block = ParseBlock();

            return statement;
        }
        ForInStatement ParseForInStatement()
        {
            var statement = new ForInStatement(_current.m_line);
            statement.name_list = ParseNameList();
            if (NextToken().m_type != (int)TokenType.IN)
                throw NewParserException("expect 'in' in for-in-statement", _current);
            // 比较特殊，可能是：1. iter 2. function,first_idx 3. Table or Array
            statement.exp_list = ParseExpList();

            statement.block = ParseBlock();

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
                throw NewParserException("unexpect token after 'local'", _look_ahead);
        }
        LocalFunctionStatement ParseLocalFunction()
        {
            NextToken();// skip 'function'
            var statement = new LocalFunctionStatement(_current.m_line);

            if (NextToken().m_type != (int)TokenType.NAME)
                throw NewParserException("expect 'id' after 'local function'", _current);

            statement.name = _current;
            statement.func_body = ParseFunctionBody();
            return statement;
        }
        LocalNameListStatement ParseLocalNameList()
        {
            var statement = new LocalNameListStatement(_current.m_line);
            statement.name_list = ParseNameList();
            if (LookAhead().m_type == '=')
            {
                NextToken();
                statement.exp_list = ParseExpList();
            }
            return statement;
        }
        NameList ParseNameList()
        {
            var statement = new NameList(LookAhead().m_line);
            statement.names.Add(NextToken());
            Debug.Assert(_current.m_type == (int)TokenType.NAME);
            while (LookAhead().m_type == ',')
            {
                NextToken();
                if (NextToken().m_type != (int)TokenType.NAME)
                    throw NewParserException("expect 'id' after ','", _current);
                statement.names.Add(_current);
            }
            return statement;
        }
        private ParserException NewParserException(string msg, Token token)
        {
            Debug.Assert(token != null);
            return new ParserException(_lex.GetSourceName(), token.m_line, token.m_column, msg);
        }

        public SyntaxTree Parse(Lex lex_)
        {
            _lex = lex_;
            _current = null;
            _look_ahead = null;
            _look_ahead2 = null;
            return ParseModule();
        }
    }
}
