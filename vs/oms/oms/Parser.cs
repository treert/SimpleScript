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
        static bool IsExpReturnAnyCountValue(SyntaxTree t)
        {
            if(t is Terminator)
            {
                return (t as Terminator).token.m_type == (int)TokenType.DOTS;
            }
            else if(t is FuncCall)
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
                token_type == (int)'{' ||
                token_type == (int)'-' ||
                token_type == (int)'#' ||
                token_type == (int)TokenType.NOT;
        }
        int GetOpPriority(Token t)
        {
            switch(t.m_type)
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
        SyntaxTree ParseExp(SyntaxTree left = null,Token op = null,int left_priority = 0)
        {
            var exp = ParseMainExp();
            while(true)
            {
                int right_priority = GetOpPriority(LookAhead());
                if(left_priority < right_priority ||
                    (left_priority == right_priority
                    && IsRightAssociation(LookAhead())))
                {
                    exp = ParseExp(exp, NextToken(), right_priority);
                }
                else if(left_priority == right_priority)
                {
                    if (left_priority == 0)
                        return exp;
                    Debug.Assert(left != null);
                    left = new BinaryExpression(left, op, exp);
                    op = NextToken();
                    exp = ParseMainExp();
                }
                else
                {
                    if(left != null)
                    {
                        exp = new BinaryExpression(left, op, exp); 
                    }
                    return exp;
                }
            }
        }
        SyntaxTree ParseMainExp()
        {
            SyntaxTree exp;
            switch(LookAhead().m_type)
            {
                case (int)TokenType.NIL:
                case (int)TokenType.FALSE:
                case (int)TokenType.TRUE:
                case (int)TokenType.NUMBER:
                case (int)TokenType.STRING:
                case (int)TokenType.DOTS:
                    exp = new Terminator(NextToken());
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
                case (int)'#':
                case (int)TokenType.NOT:
                    var unexp = new UnaryExpression();
                    unexp.op = NextToken();
                    unexp.exp = ParseExp(null, null, 90);
                    exp = unexp;
                    break;
                default:
                    throw new ParserException("unexpect token for main exp");
            }
            return exp;
        }
        ExpressionList ParseExpList()
        {
            var exp = new ExpressionList();
            exp.exp_list.Add(ParseExp());
            while(LookAhead().m_type == (int)',')
            {
                NextToken();
                exp.exp_list.Add(ParseExp());
            }
            exp.return_any_value = IsExpReturnAnyCountValue(exp.exp_list[exp.exp_list.Count - 1]);
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

            var index_access = new TableAccess();
            index_access.table = table;
            if(_current.m_type == (int)'[')
            {
                index_access.index = ParseExp();
                if (NextToken().m_type != (int)']')
                    throw new ParserException("expect ']'");
            }
            else
            {
                if (NextToken().m_type != (int)TokenType.NAME)
                    throw new ParserException("expect 'id' after '.'");
                index_access.index = new Terminator(new Token(_current.m_string));
            }
            return index_access;
        }
        FuncCall ParseFunctionCall(SyntaxTree caller)
        {
            var func_call = new FuncCall();
            func_call.caller = caller;
            if(LookAhead().m_type == (int)':')
            {
                NextToken();
                if (NextToken().m_type != (int)TokenType.NAME)
                    throw new ParserException("expect 'id' after ':'");
                func_call.member_name = _current;
            }
            func_call.args = ParseArgs();
            return func_call;
        }
        ExpressionList ParseArgs()
        {
            ExpressionList exp_list = null;
            if(LookAhead().m_type == (int)'(')
            {
                NextToken();
                if (LookAhead().m_type != (int)')')
                    exp_list = ParseExpList();
                if (NextToken().m_type != (int)')')
                    throw new ParserException("expect '(' to end function-args");
            }
            else if(LookAhead().m_type == (int)'{')
            {
                exp_list = new ExpressionList();
                exp_list.exp_list.Add(ParseTableConstructor());
            }
            else
                throw new ParserException("expect '(' or '{' to start function-args");
            return exp_list;
        }
        SyntaxTree ParsePrefixExp()
        {
            NextToken();
            Debug.Assert(_current.m_type == (int)TokenType.NAME
                || _current.m_type == (int)'(');
            SyntaxTree exp;
            if(_current.m_type == (int)'(')
            {
                exp = ParseExp();
                if (NextToken().m_type != (int)')')
                    throw new ParserException("expect ')'");
            }
            else
            {
                exp = new Terminator(_current);
            }

            // table index or func call
            for(;;)
            {
                if(LookAhead().m_type == (int)'['
                    || LookAhead().m_type == (int)'.')
                {
                    exp = ParseTableAccessor(exp);
                }
                else if(LookAhead().m_type == (int)':'
                    || LookAhead().m_type == (int)'('
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
            // oms放松了些，可以有'('开头的prefixexp
            SyntaxTree exp;
            if (LookAhead().m_type == (int)TokenType.NAME)
            {
                exp = ParsePrefixExp();
                if(IsVar(exp))
                {
                    // assign statement
                    var assign_statement = new AssignStatement();
                    assign_statement.var_list.Add(exp);
                    while(LookAhead().m_type != (int)'=')
                    {
                        if (NextToken().m_type != (int)',')
                            throw new ParserException("expect ',' to split var-list");
                        if (LookAhead().m_type != (int)TokenType.NAME)
                            throw new ParserException("expect 'id' to start var");
                        exp = ParsePrefixExp();
                        if (!IsVar(exp))
                            throw new ParserException("expect var here");
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
            else if (LookAhead().m_type == '(')
            {
                // special handle, so can use (ok and dosomething())
                return ParsePrefixExp();
            }
            else
            {
                if (IsMainExp())
                    throw new ParserException("incomplete statement");
                return null;
            }
        }
        TableField ParseTableIndexField()
        {
            NextToken();
            var field = new TableField();
            field.index = ParseExp();
            if (NextToken().m_type != ']')
                throw new ParserException("expect ']'");
            if (NextToken().m_type != '=')
                throw new ParserException("expect '='");
            field.value = ParseExp();
            return field;
        }
        TableField ParseTableNameField()
        {
            var field = new TableField();
            field.index = new Terminator(new Token(NextToken().m_string));
            NextToken();// skip '='
            field.value = ParseExp();
            return field;
        }
        TableField ParseTableArrayField()
        {
            var field = new TableField();
            field.index = null;// default is null
            field.value = ParseExp();
            return field;
        }
        TableDefine ParseTableConstructor()
        {
            NextToken();
            var table = new TableDefine();
            TableField last_field = null;
            while(LookAhead().m_type != '}')
            {
                if (LookAhead().m_type == (int)'[')
                    last_field = ParseTableIndexField();
                else if(LookAhead().m_type == (int)TokenType.NAME
                    && LookAhead2().m_type == (int)'=')
                    last_field = ParseTableNameField();
                else
                    last_field = ParseTableArrayField();

                table.fields.Add(last_field);

                if(LookAhead().m_type != '}')
                {
                    NextToken();
                    if(_current.m_type != (int)','
                        && _current.m_type != (int)';')
                        throw new ParserException("expect ',' or ';' to split table fields");
                }
            }
            if (NextToken().m_type != '}')
                throw new ParserException("expect '}' for table");

            if(last_field != null && last_field.index == null)
            {
                table.last_field_append_table = IsExpReturnAnyCountValue(last_field.value);
            }

            return table;
        }
        Chunk ParseChunk()
        {
            var block = ParseBlock();
            if (NextToken().m_type != (int)TokenType.EOS)
                throw new ParserException("expect <eof>");
            var tree = new Chunk();
            tree.block = block;
            return tree;
        }
        Block ParseBlock()
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
                    case (int)TokenType.FOREACH:
                        statement = ParseForEachStatement(); break;
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
                        statement = ParseOtherStatement();
                        break;
                }
                if (statement == null)
                    break;
                // lua will check {return,break}
                block.statements.Add(statement);
            }
            return block;
        }
        ReturnStatement ParseReturnStatement()
        {
            NextToken();
            var statement = new ReturnStatement();
            if(IsMainExp())
            {
                statement.exp_list = ParseExpList();
            }
            return statement;
        }
        BreakStatement ParseBreakStatement()
        {
            return new BreakStatement();
        }
        ContinueStatement ParseContinueStatement()
        {
            return new ContinueStatement();
        }
        DoStatement ParseDoStatement()
        {
            NextToken();// skip 'do'

            var do_statement = new DoStatement();
            do_statement.block = ParseBlock();
            if (NextToken().m_type != (int)TokenType.END)
                throw new ParserException("expect 'end' for do-statement");
            return do_statement;
        }
        WhileStatement ParseWhileStatement()
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
        IfStatement ParseIfStatement()
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
                // syntax sugar for elseif
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
        FunctionStatement ParseFunctionStatement()
        {
            NextToken();// skip 'function'

            var statement = new FunctionStatement();
            bool add_self = false;
            statement.func_name = ParseFunctionName(out add_self);
            statement.func_body = ParseFunctionBody(add_self);
            return statement;
        }
        FunctionName ParseFunctionName(out bool add_self)
        {
            if (NextToken().m_type != (int)TokenType.NAME)
                throw new ParserException("unexpect token after 'function'");

            add_self = false;
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
                add_self = true;
                func_name.names.Add(_current);
            }

            return func_name;
        }
        FunctionBody ParseFunctionBody(bool add_self = false)
        {
            if (NextToken().m_type != (int)'(')
                throw new ParserException("expect '(' to start function-body");
            var statement = new FunctionBody();
            statement.param_list = ParseParamList(add_self);
            if (NextToken().m_type != (int)')')
                throw new ParserException("expect ')' after param-list");
            statement.block = ParseBlock();
            if (NextToken().m_type != (int)TokenType.END)
                throw new ParserException("expect 'end' after function-body");
            
            return statement;
        }
        ParamList ParseParamList(bool add_self)
        {
            var statement = new ParamList();
            if (add_self)
            {
                statement.name_list.Add(new Token("self"));
            }
            if (LookAhead().m_type == (int)')')
                return statement;
            
            if(LookAhead().m_type == (int)TokenType.NAME)
            {
                statement.name_list.Add(NextToken());
                while(LookAhead().m_type == (int)',')
                {
                    NextToken();
                    if (LookAhead().m_type == (int)TokenType.NAME)
                        statement.name_list.Add(NextToken());
                    else if (LookAhead().m_type == (int)TokenType.DOTS)
                    {
                        NextToken();
                        statement.is_var_arg = true;
                        break;
                    }
                    else
                        throw new ParserException("unexpect token in param list");
                }
            }
            else if(LookAhead().m_type == (int)TokenType.DOTS)
            {
                NextToken();
                statement.is_var_arg = true;
            }
            else
                throw new ParserException("unexpect token in param list");

            return statement;
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
        ForStatement ParseForNumStatement()
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
            if (LookAhead().m_type == ',')
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
        ForInStatement ParseForInStatement()
        {
            var statement = new ForInStatement();
            statement.name_list = ParseNameList();
            if (NextToken().m_type != (int)TokenType.IN)
                throw new ParserException("expect 'in' in for-in-statement");
            // 这个结构特殊，返回的是迭代器，
            statement.exp_list = ParseExpList();

            if (NextToken().m_type != (int)TokenType.DO)
                throw new ParserException("expect 'do' to start for-in-body");
            statement.block = ParseBlock();
            if (NextToken().m_type != (int)TokenType.END)
                throw new ParserException("expect 'do' to complete for-in-body");

            return statement;
        }
        ForEachStatement ParseForEachStatement()
        {
            NextToken();// skip 'foreach'

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
        LocalFunctionStatement ParseLocalFunction()
        {
            NextToken();// skip 'function'

            if (NextToken().m_type != (int)TokenType.NAME)
                throw new ParserException("expect 'id' after 'local function'");

            var statement = new LocalFunctionStatement();
            statement.name = _current;
            statement.func_body = ParseFunctionBody();
            return statement;
        }
        LocalNameListStatement ParseLocalNameList()
        {
            var statement = new LocalNameListStatement();
            statement.name_list = ParseNameList();
            if(LookAhead().m_type == (int)'=')
            {
                NextToken();
                statement.exp_list = ParseExpList();
            }
            return statement;
        }
        NameList ParseNameList()
        {
            var statement = new NameList();
            statement.names.Add(NextToken());
            Debug.Assert(_current.m_type == (int)TokenType.NAME);
            while(LookAhead().m_type == ',')
            {
                NextToken();
                if (NextToken().m_type != (int)TokenType.NAME)
                    throw new ParserException("expect 'id' after ','");
                statement.names.Add(_current);
            }
            return statement;
        }
        public SyntaxTree Parse(Lex lex_)
        {
            _lex = lex_;
            _current = null;
            _look_ahead = null;
            _look_ahead2 = null;
            return ParseChunk();
        }
    }
}
