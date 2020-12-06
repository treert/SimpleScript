﻿using System;
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
                token_type == (int)TokenType.DOTS ||
                token_type == (int)TokenType.NAME ||
                token_type == (int)Keyword.FN ||
                token_type == (int)'(' ||
                token_type == (int)'{' ||
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
                case (int)Keyword.NIL:
                case (int)Keyword.FALSE:
                case (int)Keyword.TRUE:
                case (int)TokenType.NUMBER:
                case (int)TokenType.STRING:
                case (int)TokenType.DOTS:
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
            var head = NextToken();
            var next = LookAhead();

            var exp = new ComplexString(_current.m_line);
            if (head.m_string_type >= StringBlockType.InverseQuotation)
            {
                exp.is_shell = true;
                if (next.Match(TokenType.STRING)
                    && next.m_string.Length > 3
                    && head.m_string_type == StringBlockType.InverseThreeQuotation)
                {
                    int idx = 0;
                    var str = next.m_string;
                    while (idx < str.Length && char.IsLetter(str[idx]))
                    {
                        idx++;
                    }
                    if (idx < str.Length && str[idx] == ' ')
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
                else if (next.Match('{'))
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
                if (!NextToken().IsName())
                    throw NewParserException("expect <id> after '.'", _current);
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
                    if (LookAhead().Match(TokenType.STRING_BEGIN))
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
                    else if (LookAhead2().Match('='))
                    {
                        // must be kv
                        NextToken();
                        if (_current.Match(TokenType.STRING)
                            || _current.Match(TokenType.NUMBER))
                        {
                            last_field.index = new Terminator(_current);
                        }
                        else if (_current.IsName())
                        {
                            last_field.index = new Terminator(_current.ConvertToStringToken());
                        }
                        else
                        {
                            throw NewParserException("expect <name>,<string>,<number> to define table-key before '='", _current);
                        }
                        NextToken();// skip =
                        last_field.value = ParseExp();
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
        /// 这个语法LL(2)是不能支持的。
        /// </summary>
        /// <returns></returns>
        ScopeStatement ParseScopeStatement()
        {
            NextToken();
            var statement = new ScopeStatement(_current.m_line);
            List<Token> names = new List<Token>();
            List<ExpSyntaxTree> exps = new List<ExpSyntaxTree>();
            bool can_be_name_list = true;
            while (LookAhead().IsName())
            {
                var exp = ParseExp();
                exps.Add(exp);
                if (can_be_name_list && exp is Terminator ter)
                {
                    names.Add(ter.token);
                }
                else
                {
                    can_be_name_list = false;
                }
                if (LookAhead().Match(',')){
                    NextToken();
                }
                else if (LookAhead().Match('='))
                {
                    if (can_be_name_list)
                    {
                        statement.name_list = new NameList(names[0].m_line);
                        statement.name_list.names = names;
                        can_be_name_list = false;
                        exps.Clear();
                        NextToken();
                    }
                    else
                    {
                        throw NewParserException("unexpect '=' in scope statement", LookAhead());
                    }
                }
                else
                {
                    break;
                }
            }
            if(exps.Count == 0)
            {
                throw NewParserException("there must be some valid <id> start exp in scope statement", LookAhead());
            }
            if(LookAhead().Match('{') == false)
            {
                throw NewParserException("expect '{' to start scope block", LookAhead());
            }
            statement.exp_list = new ExpressionList(exps[0].line);
            statement.exp_list.exp_list = exps;
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
