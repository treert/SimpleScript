using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MyScript
{
    /// <summary>
    /// MyScript 语法解析。
    /// 
    /// PS：有打算把各个前缀关键词语句的解析提取到各个Syntax.XXX.cs里的，想想了又没这么做，似乎好处不够明显。
    /// </summary>
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
        public Token CurrentToken
        {
            get
            {
                return _current;
            }
        }
        public Token NextToken()
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
        public Token LookAhead()
        {
            if (_look_ahead == null)
                _look_ahead = _lex.GetNextToken();
            return _look_ahead;
        }
        public Token LookAhead2()
        {
            LookAhead();
            if (_look_ahead2 == null)
                _look_ahead2 = _lex.GetNextToken();
            return _look_ahead2;
        }
        public bool IsMainExpNext()
        {
            int token_type = LookAhead().m_type;
            return
                token_type == (int)Keyword.NIL ||
                token_type == (int)Keyword.FALSE ||
                token_type == (int)Keyword.TRUE ||
                token_type == (int)TokenType.NUMBER ||
                token_type == (int)TokenType.STRING ||
                token_type == (int)TokenType.STRING_BEGIN ||
                token_type == (int)TokenType.NAME ||
                token_type == (int)Keyword.FN ||
                token_type == (int)'(' ||
                token_type == (int)'{' ||
                token_type == (int)'[' ||
                token_type == (int)'-' ||
                token_type == (int)Keyword.NOT;
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
                case (int)Keyword.AND: return 40;
                case (int)Keyword.OR: return 30;
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
                exp = TryParseQuestionExp(exp);
            }
            return exp;
        }

        QuestionExp TryParseQuestionExp(ExpSyntaxTree exp)
        {
            NextToken();// skip ?
            var qa = new QuestionExp(exp.line);
            qa.a = exp;
            if (LookAhead().Match(':', '?') == false)
            {
                qa.b = ParseExp();
            }

            if (LookAhead().Match(':'))
            {
                qa.isqq = false;
            }
            else if (LookAhead().Match('?'))
            {
                qa.isqq = true;
            }
            else
            {
                throw NewParserException("expect second '?' or ':' for ? exp", LookAhead());
            }
            NextToken();

            switch (LookAhead().m_type)
            {
                case (int)Keyword.THROW:
                    qa.c = ParseThrowStatement(); break;
                case (int)Keyword.BREAK:
                    qa.c = ParseBreakStatement(); break;
                case (int)Keyword.CONTINUE:
                    qa.c = ParseContinueStatement(); break;
                case (int)Keyword.RETURN:
                    qa.c = ParseReturnStatement(); break;
                default:
                    qa.c = ParseExp();break;
            }
            return qa;
        }

        ExpSyntaxTree ParseConditionExp()
        {
            // 想了想，这个就算了，不做限制了。
            //if (LookAhead().Match('{'))
            //{
            //    throw NewParserException("condition exp should not start with '{'", _look_ahead);
            //}
            return ParseExp();
        }

        ExpSyntaxTree ParseMainExp()
        {
            ExpSyntaxTree exp;
            switch (LookAhead().m_type)
            {
                case (int)Keyword.NIL:
                case (int)Keyword.FALSE:
                case (int)Keyword.TRUE:
                case (int)TokenType.NUMBER:
                case (int)TokenType.STRING:
                    exp = new Terminator(NextToken());
                    break;
                case (int)TokenType.NAME:
                    exp = new Terminator(NextToken());
                    break;
                case (int)TokenType.STRING_BEGIN:
                    exp = ParseComplexString();
                    break;
                case (int)Keyword.FN:
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
                case (int)'[':
                    exp = ParseArrayConstructor();
                    break;
                // unop exp priority is 90 less then ^
                case (int)'-':
                case (int)Keyword.NOT:
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
            NextToken();// skip StringBegin

            var exp = new ComplexString(_current.m_line);
            do
            {
                NextToken();
                if (_current.Match(TokenType.STRING) || _current.Match(TokenType.NAME))
                {
                    var term = new Terminator(_current);
                    exp.list.Add(term);
                }
                else if (_current.Match('{'))
                {
                    var term = ParseComplexItem();
                    exp.list.Add(term);
                }
                else
                {
                    throw NewParserException("expect string,name,'{' in complex-string", _current);
                }
            } while (_current.IsStringEnded == false);
            return exp;
        }

        ComplexStringItem ParseComplexItem()
        {
            var item = new ComplexStringItem(_current.m_line);
            item.exp = ParseExp();
            var next = NextToken();
            if (next.Match(','))
            {
                next = NextToken();
                if (next.Match(TokenType.NUMBER))
                {
                    item.len = (int)next.m_number;
                    if (item.len != next.m_number)
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
            NextToken();// skip '['
            Debug.Assert(_current.Match('['));

            var index_access = new TableAccess(_current.m_line);
            index_access.table = table;
            index_access.index = ParseExp();
            if (NextToken().Match(']') == false)
                throw NewParserException("expect ']'", _current);
            return index_access;
        }

        public ExpSyntaxTree ParseDotTailExp(ExpSyntaxTree exp)
        {
            NextToken();// skip "."
            int line_dot = _current.m_line;
            ExpSyntaxTree idx = null;
            var tok = LookAhead();
            if (tok.CanBeNameString())
            {
                // 当成字符串来用
                idx = new Terminator(NextToken().ConvertToStringToken());
            }
            else if(tok.Match(TokenType.STRING))
            {
                idx = new Terminator(NextToken());
            }
            else if (tok.Match(TokenType.STRING_BEGIN))
            {
                idx = ParseComplexString();
            }
            else
            {
                throw NewParserException("expect Name or String after '.'", tok);
            }
            var call = TryGetFuncCallExp(exp, idx);
            if (call)
            {
                return call;
            }
            var index_access = new TableAccess(line_dot);
            index_access.table = exp;
            index_access.index = idx;
            return index_access;

        }
        FuncCall TryGetFuncCallExp(ExpSyntaxTree caller, ExpSyntaxTree idx = null)
        {
            if (LookAhead().Match('('))
            {
                var func_call = new FuncCall(LookAhead().m_line);
                func_call.caller = caller;
                func_call.idx = idx;
                func_call.args = ParseArgs();
                return func_call;
            }
            else
            {
                // 这个语法糖想了想，在这儿支持吧，和lua一样，写出的代码可能会很诡异。
                var str = TryGetStringExp();
                if (str)
                {
                    var func_call = new FuncCall(str.line);
                    func_call.caller = caller;
                    func_call.idx = idx;
                    ArgsList args = new ArgsList(str.line);
                    args.exp_list.Add(str);
                    func_call.args = args;
                    return func_call;
                }
            }
            return null;
        }

        ExpSyntaxTree TryGetStringExp()
        {
            if (LookAhead().Match(TokenType.STRING))
            {
                return new Terminator(NextToken());
            }
            else if (LookAhead().Match(TokenType.STRING_BEGIN))
            {
                return ParseComplexString();
            }
            return null;
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
                    list.kw_exp_list.Add(kw);
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

        ExpSyntaxTree ParseTailExp(ExpSyntaxTree exp, bool str_call_valid = false)
        {
            // table index or func call
            for (; ; )
            {
                if (LookAhead().Match('.'))
                {
                    exp = ParseDotTailExp(exp);
                }
                else if (LookAhead().Match('['))
                {
                    exp = ParseTableAccessor(exp);
                }
                else
                {
                    var call = TryGetFuncCallExp(exp, null);
                    if (call)
                    {
                        exp = call;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return exp;
        }

        SyntaxTree ParseOtherStatement()
        {
            // 没什么限制，基本可以随意写些MainExp
            // 重点处理的是一些赋值类语句，赋值类语句的左值必须是var类型的
            // SS还增加几个语法支持，+=，-=，++，--
            if (IsMainExpNext() == false) return null;

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
                    var str = TryGetStringExp();
                    if (str)
                    {
                        last_field.index = str;
                    }
                    else if (LookAhead().CanBeNameString())
                    {
                        last_field.index = new Terminator(NextToken().ConvertToStringToken());
                    }
                    else
                    {
                        throw NewParserException("expect <name>,<string>,'[' to define table-key", _current);
                    }

                    if (LookAhead().Match('=', ':'))
                    {
                        NextToken();
                        last_field.value = ParseExp();
                    }
                    else
                    {
                        throw NewParserException("expect '=' to between key and value", _current);
                    }
                }

                table.fields.Add(last_field);
                if (LookAhead().Match(','))
                {
                    NextToken();
                }
                else
                {
                    break;
                }
            }
            if (NextToken().m_type != '}')
                throw NewParserException("expect '}' to end table-define", _current);

            return table;
        }

        ArrayDefine ParseArrayConstructor()
        {
            NextToken();
            ArrayDefine arr = new ArrayDefine(_current.m_line);
            while(LookAhead().Match(']') == false)
            {
                var exp = ParseExp();
                arr.fileds.Add(exp);
                if (LookAhead().Match(','))
                {
                    NextToken();// 不支持[,,]这种
                }
                else
                {
                    break;
                }
            }
            if(NextToken().Match(']') == false)
            {
                throw NewParserException("expect ']' to end array-define", _current);
            }
            return arr;
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
                    case (int)Keyword.WHILE:
                        statement = ParseWhileStatement(); break;
                    case (int)Keyword.DO:
                        statement = ParseDoStatement(); break;
                    case (int)Keyword.IF:
                        statement = ParseIfStatement(); break;
                    case (int)Keyword.FOR:
                        statement = ParseForStatement(); break;
                    case (int)Keyword.FN:
                        statement = ParseFunctionStatement(); break;
                    case (int)Keyword.LOCAL:
                    case (int)Keyword.GLOBAL:
                        {
                            var state = ParseDefineStatement();
                            state.is_global = token_ahead.Match(Keyword.GLOBAL);
                            statement = state;
                            break;
                        }
                    case (int)Keyword.SCOPE:
                        statement = ParseScopeStatement(); break;
                    case (int)Keyword.RETURN:
                        statement = ParseReturnStatement(); break;
                    case (int)Keyword.BREAK:
                        statement = ParseBreakStatement(); break;
                    case (int)Keyword.CONTINUE:
                        statement = ParseContinueStatement(); break;
                    case (int)Keyword.TRY:
                        statement = ParseTryStatement(); break;
                    case (int)Keyword.THROW:
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
            if (LookAhead().Match(Keyword.CATCH))
            {
                NextToken();
                if (LookAhead().Match(TokenType.NAME))
                {
                    statement.catch_name = NextToken();
                }
                statement.catch_block = ParseBlock();
            }
            if (LookAhead().Match(Keyword.FINNALY))
            {
                NextToken();
                statement.finally_block = ParseBlock();
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

        DoWhileStatement ParseDoStatement()
        {
            NextToken();// skip 'do'
            var statement = new DoWhileStatement(_current.m_line);
            statement.block = ParseBlock();
            if(NextToken().Match(Keyword.WHILE) == false)
            {
                throw NewParserException("expect 'while' for 'do { ... } while exp'", _current);
            }
            statement.exp = ParseConditionExp();
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
            if (LookAhead().Match(Keyword.ELSEIF))
            {
                // syntax sugar for elseif
                return ParseIfStatement();
            }
            else if (LookAhead().Match(Keyword.ELSE))
            {
                NextToken();
                if (LookAhead().Match(Keyword.IF))
                {
                    return ParseIfStatement();// else if 
                }
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
                    if (_current.Match(',') == false && statement.name_list.Count > 0)
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
                throw NewParserException("expect <id> or '{' after 'for'", _look_ahead);
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
                throw NewParserException("expect ',' in for-num-range-statement", _current);
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
            if (NextToken().Match(Keyword.IN) == false)
                throw NewParserException("expect 'in' in for-in-statement", _current);
            // 比较特殊，可能是：1. Table 1-1. iter 2. function
            statement.exp = ParseExp();

            statement.block = ParseBlock();

            return statement;
        }

        /// <summary>
        /// scope namelist = explist {  }
        /// scope = explist {  }
        /// 修改了下语法，可以用LL(2)解析
        /// </summary>
        /// <returns></returns>
        ScopeStatement ParseScopeStatement()
        {
            NextToken();
            var statement = new ScopeStatement(_current.m_line);
            if (LookAhead().Match('='))
            {
                NextToken();
                statement.exp_list = ParseExpList();
            }
            else if(LookAhead().CanBeNameString())
            {
                statement.name_list = ParseNameList();
                if(NextToken().Match('=') == false)
                {
                    throw NewParserException("expect '=' in scope statement after name_list", _current);
                }
                statement.exp_list = ParseExpList();
            }
            else
            {
                throw NewParserException("expect '=' or <name> to after 'scope'", _current);
            }
            statement.block = ParseBlock();
            return statement;
        }

        DefineStatement ParseDefineStatement()
        {
            NextToken();// skip 'local' or 'global'

            if (LookAhead().Match(Keyword.FN))
                return ParseDefineFunction();
            else if (LookAhead().m_type == (int)TokenType.NAME)
                return ParseDefineNameList();
            else
                throw NewParserException("unexpect token after 'local' or 'global'", _look_ahead);
        }
        DefineFunctionStatement ParseDefineFunction()
        {
            NextToken();
            var statement = new DefineFunctionStatement(_current.m_line);

            if (NextToken().m_type != (int)TokenType.NAME)
                throw NewParserException("expect 'id' to name function", _current);

            statement.name = _current;
            statement.func_body = ParseFunctionBody();
            return statement;
        }
        DefineNameListStatement ParseDefineNameList()
        {
            var statement = new DefineNameListStatement(_current.m_line);
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
