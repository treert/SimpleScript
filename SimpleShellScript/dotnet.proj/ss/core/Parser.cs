using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SScript
{
    public class Parser
    {

        static bool IsVar(SyntaxTree t)
        {
            return t is TableAccess || (t is Terminator && (t as Terminator).token.Match(TokenType.NAME));
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
        bool IsMainExpNext()
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
                token_type == (int)TokenType.NAME ||
                token_type == (int)TokenType.FUNCTION ||
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

        ExpSyntaxTree ParseExp(int left_priority = 0)
        {
            var exp = ParseMainExp();
            while (true)
            {
                // 针对二目算符优先文法的算法
                int right_priority = GetOpPriority(LookAhead());
                if (left_priority < right_priority || (left_priority == right_priority && IsRightAssociation(LookAhead())))
                {
                    // C++的函数参数执行顺序没有明确定义，方便起见，不在函数参数里搞出两个有依赖的函数调用，方便往C++里迁移
                    var op = NextToken();
                    exp = new BinaryExpression(exp, op, ParseExp(right_priority));
                }
                else
                {
                    break;
                }
            }
            // for a ? b : c, 三目运算符的优先级最低，实际运行结果来看，三目运算符还具有右结合性质
            if (left_priority == 0 && LookAhead().Match('?'))
            {
                NextToken();
                var qa = new QuestionExp(exp.line);
                qa.a = exp;
                qa.b = ParseExp();
                if (LookAhead().Match(':'))
                {
                    qa.c = ParseExp();
                }
                exp = qa;
            }
            return exp;
        }

        ExpSyntaxTree ParseConditionExp()
        {
            if (LookAhead().Match('{'))
            {
                throw NewParserException("condition exp should not start with '{'", _look_ahead);
            }
            return ParseExp();
        }

        ExpSyntaxTree ParseMainExp()
        {
            ExpSyntaxTree exp;
            switch (LookAhead().m_type)
            {
                case (int)TokenType.NIL:
                case (int)TokenType.FALSE:
                case (int)TokenType.TRUE:
                case (int)TokenType.NUMBER:
                case (int)TokenType.STRING:
                case (int)TokenType.DOTS:
                case (int)TokenType.NAME:
                    exp = new Terminator(NextToken());
                    break;
                case (int)TokenType.STRING_BEGIN:
                    exp = ParseComplexString();
                    break;
                case (int)TokenType.FUNCTION:
                    exp = ParseFunctionDef();
                    break;
                case (int)'(':
                    NextToken();
                    exp = ParseExp();
                    if (NextToken().m_type != (int)')')
                        throw NewParserException("expect ')' to match Main Exp's head '('", _current);
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
            return ParseTailExp(exp);
        }

        private ComplexString ParseComplexString()
        {
            var head = NextToken();
            var next = LookAhead();

            var exp = new ComplexString(_current.m_line);
            if(head.m_string_type >= StringBlockType.InverseQuotation)
            {
                exp.is_shell = true;
                if (next.Match(TokenType.STRING)
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
                if (next.Match(TokenType.STRING) || next.Match(TokenType.NAME))
                {
                    var term = new Terminator(next);
                    exp.list.Add(term);
                }
                else if(next.Match('{'))
                {
                    var term = ParseComplexItem();
                    exp.list.Add(term);
                }
                else
                {
                    throw NewParserException("expect string,name,'{' in complex-string", next);
                }
            } while (next.IsStringEnded == false);


            return exp;
        }

        ComplexStringItem ParseComplexItem()
        {
            var item = new ComplexStringItem(_current.m_line);
            item.exp = ParseExp();
            var next = NextToken();
            if(next.Match(','))
            {
                next = NextToken();
                if (next.Match(TokenType.NUMBER))
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
            if (next.Match('|'))
            {
                next = NextToken();
                if (next.Match(TokenType.NAME))
                {
                    item.format = next.m_string;
                }
                else
                {
                    throw NewParserException($"complex string item format must be a string", next);
                }
                next = NextToken();
            }
            if (next.Match('}'))
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
        TableAccess ParseTableAccessor(ExpSyntaxTree table)
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
                index_access.index = new Terminator(_current.ConvertToStringToken());
            }
            return index_access;
        }
        FuncCall ParseFunctionCall(ExpSyntaxTree caller)
        {
            Debug.Assert(LookAhead().Match('('));// 基本的函数调用只支持语法 f(arg,...)，后面可以安排写语法糖什么的。
            var func_call = new FuncCall(LookAhead().m_line);
            func_call.caller = caller;
            func_call.args = ParseArgs();
            return func_call;
        }
        ArgsList ParseArgs()
        {
            NextToken();// skip '('
            ArgsList list = new ArgsList(_current.m_line);
            // arg,arg,*table,name=arg,name=arg,
            bool has_args = false;
            while (IsMainExpNext())
            {
                if (LookAhead().Match(TokenType.NAME) && LookAhead2().Match('=')) break;
                has_args = true;
                list.exp_list.Add(ParseExp());
                if (LookAhead().Match(','))
                {
                    NextToken();
                }
                else
                {
                    break;
                }
            }

            if (LookAhead().Match('*'))
            {
                if (has_args && !_current.Match(','))
                {
                    throw NewParserException("expect ',' to split args and *", _current);
                }
                has_args = true;
                NextToken();
                list.kw_table = ParseMainExp();
                if (LookAhead().Match(','))
                {
                    NextToken();
                }
            }

            if (LookAhead().Match(TokenType.NAME) && LookAhead2().Match('='))
            {
                if (has_args && !_current.Match(','))
                {
                    throw NewParserException("expect ',' to split args and name_args", _current);
                }
                while (LookAhead().Match(TokenType.NAME) && LookAhead2().Match('='))
                {
                    ArgsList.KW kw = new ArgsList.KW();
                    kw.k = NextToken();
                    NextToken();
                    kw.w = ParseExp();
                    if (LookAhead().Match(','))
                    {
                        NextToken();
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (NextToken().m_type != (int)')')
                throw NewParserException("expect ')' to end function-args", _current);

            return list;
        }

        ExpSyntaxTree ParseTailExp(ExpSyntaxTree exp)
        {
            // table index or func call
            for (; ; )
            {
                if (LookAhead().Match('[') || LookAhead().Match('.'))
                {
                    exp = ParseTableAccessor(exp);
                }
                else if (LookAhead().Match('('))
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
            // 没什么限制，基本可以随意写些MainExp
            // 重点处理的是一些赋值类语句，赋值类语句的左值必须是var类型的
            // SS还增加几个语法支持，+=，-=，++，--
            if(IsMainExpNext() == false) return null;

            ExpSyntaxTree exp = ParseMainExp();
            if (LookAhead().Match('=') || LookAhead().Match(','))
            {
                // assign statement
                if (!IsVar(exp))
                    throw NewParserException("expect var for assign statement", _current);
                var assign_statement = new AssignStatement(LookAhead().m_line);
                assign_statement.var_list.Add(exp);
                while (LookAhead().m_type != (int)'=')
                {
                    if (NextToken().m_type != (int)',')
                        throw NewParserException("expect ',' to split var-list", _current);
                    if (LookAhead().m_type != (int)TokenType.NAME)
                        throw NewParserException("expect 'id' to start var", _look_ahead);
                    exp = ParseMainExp();
                    if (!IsVar(exp))
                        throw NewParserException("expect var for assign statement", _current);
                    assign_statement.var_list.Add(exp);
                }
                NextToken();// skip '='
                assign_statement.exp_list = ParseExpList();

                return assign_statement;
            }
            var type = (TokenType)LookAhead().m_type;
            if (SpecialAssginStatement.NeedWork(type))
            {
                if (!IsVar(exp))
                    throw NewParserException("expect var here", _current);
                var special_statement = new SpecialAssginStatement(_current.m_line);
                special_statement.var = exp;
                special_statement.op = type;
                if (SpecialAssginStatement.IsSelfMode(type))
                {
                    special_statement.exp = ParseExp();
                }
                return special_statement;
            }

            return exp;// 不限制了
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

        TableDefine ParseTableConstructor()
        {
            NextToken();
            var table = new TableDefine(_current.m_line);
            TableField last_field = null;
            while (LookAhead().m_type != '}')
            {
                if (LookAhead().m_type == (int)'[')
                {
                    last_field = ParseTableIndexField();
                }
                else
                {
                    last_field = new TableField(LookAhead().m_line);
                    if (LookAhead2().Match('='))
                    {
                        // must be kv
                        NextToken();
                        if (_current.Match(TokenType.NAME))
                        {
                            last_field.index = new Terminator(_current.ConvertToStringToken());
                        }
                        else if (_current.Match(TokenType.STRING)
                            || _current.Match(TokenType.NUMBER))
                        {
                            last_field.index = new Terminator(_current);
                        }
                        else
                        {
                            throw NewParserException("expect name,string,number to define table-key", _current);
                        }
                        NextToken();
                        last_field.value = ParseExp();
                    }
                    else if (LookAhead().Match(TokenType.STRING_BEGIN))
                    {
                        var exp = ParseComplexString();
                        if (LookAhead().Match('='))
                        {
                            last_field.index = exp;
                            NextToken();
                            last_field.value = ParseExp();
                        }
                        else
                        {
                            last_field.value = exp;
                        }
                    }
                    else
                    {
                        last_field.value = ParseExp();
                    }
                }

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

            return table;
        }

        BlockTree ParseBlock()
        {
            if (!NextToken().Match('{'))
                throw NewParserException("expect '{' to begin block", _current);

            var block = new BlockTree(_current.m_line);
            ParseStatements(block.statements);

            if (!NextToken().Match('}'))
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
                    case '{':
                        statement = ParseBlock(); break;
                    case (int)TokenType.WHILE:
                        statement = ParseWhileStatement(); break;
                    case (int)TokenType.IF:
                        statement = ParseIfStatement(); break;
                    case (int)TokenType.FOR:
                        statement = ParseForStatement(); break;
                    case (int)TokenType.FUNCTION:
                        statement = ParseFunctionStatement(); break;
                    case (int)TokenType.LOCAL:
                    case (int)TokenType.GLOBAL:
                        {
                            var state = ParseScopeStatement();
                            state.is_global = token_ahead.Match(TokenType.GLOBAL);
                            statement = state;
                            break;
                        }
                    case (int)TokenType.RETURN:
                        statement = ParseReturnStatement(); break;
                    case (int)TokenType.BREAK:
                        statement = ParseBreakStatement(); break;
                    case (int)TokenType.CONTINUE:
                        statement = ParseContinueStatement(); break;
                    case (int)TokenType.TRY:
                        statement = ParseTryStatement(); break;
                    case (int)TokenType.THROW:
                        statement = ParseThrowStatement(); break;
                    default:
                        statement = ParseOtherStatement();
                        break;
                }
                if (statement == null)
                    break;
                list.Add(statement);
            }
        }

        ThrowStatement ParseThrowStatement()
        {
            NextToken();
            var statement = new ThrowStatement(_current.m_line);
            if (IsMainExpNext())
            {
                statement.exp = ParseExp();
            }
            return statement;
        }

        private TryStatement ParseTryStatement()
        {
            NextToken();
            var statement = new TryStatement(_current.m_line);
            statement.block = ParseBlock();
            if (LookAhead().Match(TokenType.CATCH))
            {
                NextToken();
                if (LookAhead().Match(TokenType.NAME))
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
            if (IsMainExpNext())
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
            NextToken();

            var statement = new FunctionStatement(_current.m_line);
            statement.func_name = ParseFunctionName();
            statement.func_body = ParseFunctionBody();
            return statement;
        }
        FunctionName ParseFunctionName()
        {
            if (NextToken().m_type != (int)TokenType.NAME)
                throw NewParserException("expect 'id' to name function", _current);

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
            statement.source_name = _lex.GetSourceName();

            return statement;
        }
        ParamList ParseParamList()
        {
            var statement = new ParamList(LookAhead().m_line);
            
            if (LookAhead().Match('('))
            {
                NextToken();
                // a,b,c,d
                while (LookAhead().Match(TokenType.NAME))
                {
                    statement.name_list.Add(NextToken());
                    if (LookAhead().Match(','))
                    {
                        NextToken();
                    }
                    else
                    {
                        break;
                    }
                }
                if (LookAhead().Match('*'))
                {
                    if(_current.Match(',') == false && statement.name_list.Count > 0)
                    {
                        throw NewParserException("expect ',' before *", _current);
                    }
                    NextToken();
                    if (LookAhead().Match(TokenType.NAME))
                    {
                        statement.kw_name = NextToken();
                    }
                }

                if (NextToken().Match(')') == false)
                {
                    throw NewParserException("expect ')' to end param-list", _current);
                }
            }

            return statement;
        }
        SyntaxTree ParseForStatement()
        {
            NextToken();// skip 'for'
            if (LookAhead().Match('{'))
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
            // 比较特殊，可能是：1. Table 1-1. iter 2. function
            statement.exp = ParseExp();

            statement.block = ParseBlock();

            return statement;
        }

        ScopeStatement ParseScopeStatement()
        {

            NextToken();// skip 'local'

            if (LookAhead().Match(TokenType.FUNCTION))
                return ParseScopeFunction();
            else if (LookAhead().m_type == (int)TokenType.NAME)
                return ParseScopeNameList();
            else
                throw NewParserException("unexpect token after 'local' or 'global'", _look_ahead);
        }
        ScopeFunctionStatement ParseScopeFunction()
        {
            NextToken();
            var statement = new ScopeFunctionStatement(_current.m_line);

            if (NextToken().m_type != (int)TokenType.NAME)
                throw NewParserException("expect 'id' to name scope function", _current);

            statement.name = _current;
            statement.func_body = ParseFunctionBody();
            return statement;
        }
        ScopeNameListStatement ParseScopeNameList()
        {
            var statement = new ScopeNameListStatement(_current.m_line);
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

        FunctionBody ParseModule()
        {
            var block = new BlockTree(LookAhead().m_line);
            ParseStatements(block.statements);
            if (NextToken().m_type != (int)TokenType.EOS)
                throw NewParserException("expect <eof>", _current);

            FunctionBody fn = new FunctionBody(1);
            fn.source_name = _lex.GetSourceName();
            fn.block = block;

            return fn;
        }

        public FunctionBody Parse(Lex lex_)
        {
            _lex = lex_;
            _current = null;
            _look_ahead = null;
            _look_ahead2 = null;
            return ParseModule();
        }
    }
}
