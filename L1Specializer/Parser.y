%namespace L1Specializer

%using L1Specializer.SyntaxTree
%using L1Specializer.SyntaxTree.IfStatements
%using L1Specializer.Metadata
%using L1Runtime.SyntaxTree

%start program


%union 
{ 
    public char cVal; 
    public int iVal; 
    public string sVal; 

    public object Tag;
}


%token INT CHAR BOOL ARRAY
%token NEW ARRAY_LENGTH ASSERT RETURN DEFINE END NULL T F
%token WHILE DO TO STEP IF THEN ELSE ELSIF
%token INTEGER CHAR_LITERAL IDENTIFIER STRING_LITERAL
%token NOT
%token EOF LP RP LAP RAP LFP RFP SEMI COMMA GOTO PERIPERI ILLEGAL


%right ASSIGN
%left OR XOR
%left AND
%left EQ NEQ GR GREQ LE LEEQ
%left PLUS MINUS
%left MULT DIV MOD
%right POWER
%right LAP
%nonassoc NOT
%nonassoc UMINUS

%%

s : program EOF
			{

			}
;

program : function_definition program
            {
				L1Program program = (L1Program)$2.Tag;
				program.AddFunctionToForward((FunctionDefinition)$1.Tag);
				$$.Tag = program;
            }
        | function_definition
            {
				L1Program program = CompilerServices.Program;
				program.AddFunctionToForward((FunctionDefinition)$1.Tag);
				$$.Tag = program;
				CompilerServices.Program = program;
            }
;

function_definition : function_header statement_list END 
            {
				FunctionDefinition definition = new FunctionDefinition();
				definition.Header = (FunctionHeader)$1.Tag;
				definition.Statements = (StatementList)$2.Tag;
				definition.Location = @$;
				$$.Tag = definition;
            }
;

function_header : DEFINE type IDENTIFIER LP parameters_list RP
            {
 				FunctionHeader header = new FunctionHeader($3.sVal, (VariableType)$2.Tag);
				List<FunctionParameter> parameters = (List<FunctionParameter>)$5.Tag;
				foreach (FunctionParameter p in parameters)
				{
					header.AddParameter(p);
				}
				header.Location = @$;
				$$.Tag = header;           
            }
                | DEFINE IDENTIFIER LP parameters_list RP
            {
				FunctionHeader header = new FunctionHeader($2.sVal, null);
				List<FunctionParameter> parameters = (List<FunctionParameter>)$4.Tag;
				foreach (FunctionParameter p in parameters)
				{
					header.AddParameter(p);
				}
				header.Location = @$;
				$$.Tag = header;
            }
;

type : type ARRAY
            {
				VariableType type = new VariableType(VariableTypeEnum.Array, (VariableType)$1.Tag);
				$$.Tag = type;
            }
     | primitive_type
            {
				$$.Tag = $1.Tag;
            }
;

primitive_type : INT
            {
				VariableType type = new VariableType(VariableTypeEnum.Integer);
				$$.Tag = type;
            }
 //              | CHAR
 //           {
 //				VariableType type = new VariableType(VariableTypeEnum.Char);
 //				$$.Tag = type;
 //           }
               | BOOL
            {
 				VariableType type = new VariableType(VariableTypeEnum.Bool);
 				$$.Tag = type;
            }
;

parameters_list : type IDENTIFIER SEMI parameters_list
            {
				List<FunctionParameter> list = (List<FunctionParameter>)$4.Tag;
				FunctionParameter parameter = new FunctionParameter($2.sVal, (VariableType)$1.Tag);
				parameter.Location = @$;
				list.Insert(0, parameter);
				$$.Tag = list;
			}
                | type IDENTIFIER
            {
				List<FunctionParameter> list = new List<FunctionParameter>();
				FunctionParameter parameter = new FunctionParameter($2.sVal, (VariableType)$1.Tag);
				parameter.Location = @$;
				list.Insert(0, parameter);
				$$.Tag = list;
            }
				|
			{
				$$.Tag = new List<FunctionParameter>();
			}
;

statement_list : statement COMMA statement_list
            {
				StatementList statementList = (StatementList)$3.Tag;
				statementList.AddForward((Statement)$1.Tag);
				$$.Tag = statementList;
            }
               | statement
            {
				StatementList statementList = new StatementList();
				statementList.AddForward((Statement)$1.Tag);
				$$.Tag = statementList;
            }
;

//STATEMENT

statement : cycle_to
            {	
				$$.Tag = $1.Tag;
            }
          | while_do
            {
				$$.Tag = $1.Tag;
            }
          | do_while
            {
				$$.Tag = $1.Tag;
            }
          | if_set
            {
				$$.Tag = $1.Tag;
            }
          | expression
            {
				$$.Tag = $1.Tag;
            }
          | RETURN expression
            {
            	ReturnStatement statement = new ReturnStatement();
            	statement.Expression = (Expression)$2.Tag;
            	statement.Location = @$;
				$$.Tag = statement;
            }
          | RETURN
            {
				ReturnStatement statement = new ReturnStatement();
				statement.Location = @$;
				$$.Tag = statement;
            }
          | ASSERT expression
            {
                AssertStatement statement = new AssertStatement();
            	statement.Expression = (Expression)$2.Tag;
            	statement.Location = @$;
				$$.Tag = statement;
            }
          | variable_definition_list
            {
				$$.Tag = $1.Tag;
            }
          | COMMA
            {
				$$.Tag = Statement.Dummy;
            }
          | IDENTIFIER PERIPERI statement
            {
            	$$.Tag = $3.Tag;
            	((Statement)$$.Tag).Location = @$;
            	((Statement)$$.Tag).Label = $1.sVal;
            }
          | GOTO IDENTIFIER
            {
            	GotoStatement statement = new GotoStatement();
            	statement.GoTo = $2.sVal;
            	statement.Location = @$;
            	$$.Tag = statement;
            }
;

//EXPRESSION

expression : expression ASSIGN expression
                { 	
                	Expression expr = new Expression();
					expr.OpType = OperationType.Assign;
					expr.LeftNode = (Expression)$1.Tag;
					expr.RightNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | expression LAP expression RAP
                { 
                    Expression expr = new Expression();
					expr.OpType = OperationType.ArrayAccess;
					expr.LeftNode = (Expression)$1.Tag;
					expr.RightNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | expression PLUS expression
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.Plus;
					expr.LeftNode = (Expression)$1.Tag;
					expr.RightNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | expression MINUS expression
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.Minus;
					expr.LeftNode = (Expression)$1.Tag;
					expr.RightNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | expression POWER expression
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.Power;
					expr.LeftNode = (Expression)$1.Tag;
					expr.RightNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | expression MULT expression
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.Mult;
					expr.LeftNode = (Expression)$1.Tag;
					expr.RightNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | expression DIV expression
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.Div;
					expr.LeftNode = (Expression)$1.Tag;
					expr.RightNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | expression MOD expression
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.Mod;
					expr.LeftNode = (Expression)$1.Tag;
					expr.RightNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 
                }           
           | MINUS expression %prec UMINUS
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.UMinus;
					expr.LeftNode = (Expression)$2.Tag;
					expr.RightNode = null;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | PLUS expression
                { 
					//TODO: проверка на не Bool
					$$.Tag = $2.Tag;
                }
           | NOT expression
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.UNot;
					expr.LeftNode = (Expression)$2.Tag;
					expr.RightNode = null;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | expression EQ expression
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.Equals;
					expr.LeftNode = (Expression)$1.Tag;
					expr.RightNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | expression NEQ expression
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.NotEquals;
					expr.LeftNode = (Expression)$1.Tag;
					expr.RightNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | expression GR expression
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.Gr;
					expr.LeftNode = (Expression)$1.Tag;
					expr.RightNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | expression GREQ expression
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.Greq;
					expr.LeftNode = (Expression)$1.Tag;
					expr.RightNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | expression LE expression
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.Le;
					expr.LeftNode = (Expression)$1.Tag;
					expr.RightNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | expression LEEQ expression
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.Leeq;
					expr.LeftNode = (Expression)$1.Tag;
					expr.RightNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | expression OR expression
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.Or;
					expr.LeftNode = (Expression)$1.Tag;
					expr.RightNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | expression AND expression
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.And;
					expr.LeftNode = (Expression)$1.Tag;
					expr.RightNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | expression XOR expression
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.Xor;
					expr.LeftNode = (Expression)$1.Tag;
					expr.RightNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 
                }
           | LP expression RP
                { 
					$$.Tag = $2.Tag;
                }
           | ARRAY_LENGTH LP expression RP
				{
					Expression expr = new Expression();
					expr.OpType = OperationType.None;
					expr.IsLeaf = true;
					expr.LeafType = ExpressionLeafType.ArrayLength;
					expr.LeftNode = (Expression)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 								
				}
           | IDENTIFIER LP va_list RP 
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.None;
					expr.IsLeaf = true;
					expr.LeafType = ExpressionLeafType.FunctionCall;
					expr.Value = $1.sVal;
					expr.VAList = (VAList)$3.Tag;
					expr.Location = @$;
					$$.Tag = expr; 					
                }
           | IDENTIFIER LP RP 
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.None;
					expr.IsLeaf = true;
					expr.LeafType = ExpressionLeafType.FunctionCall;
					expr.Value = $1.sVal;
					expr.VAList = new VAList();
					expr.Location = @$;
					$$.Tag = expr; 					
                }
           | NEW type LAP expression RAP
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.None;
					expr.IsLeaf = true;
					expr.LeafType = ExpressionLeafType.ArrayAlloc;
					expr.Value = $2.Tag;
					expr.ResultType = new VariableType(VariableTypeEnum.Array, (VariableType)$2.Tag);
					expr.LeftNode = (Expression)$4.Tag;
					expr.Location = @$;
					$$.Tag = expr; 						
                }    
           | IDENTIFIER 
                { 
					//Сюда нужно будет еще добавить тип переменной (для семантического анализа)
					Expression expr = new Expression();
					expr.OpType = OperationType.None;
					expr.IsLeaf = true;
					expr.LeafType = ExpressionLeafType.VariableAccess;
					expr.Value = $1.sVal;
					expr.Location = @$;
					$$.Tag = expr; 						
                }       
           | INTEGER
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.None;
					expr.IsLeaf = true;
					expr.LeafType = ExpressionLeafType.Constant;
					expr.IntValue = $1.iVal;
					expr.ResultType = VariableType.IntType;
					expr.Location = @$;
					$$.Tag = expr; 	                
                }
		   | CHAR_LITERAL
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.None;
					expr.IsLeaf = true;
					expr.LeafType = ExpressionLeafType.Constant;
					expr.IntValue = $1.iVal;
					expr.ResultType = VariableType.IntType;
					expr.Location = @$;
					$$.Tag = expr;                 
                }
           | T    
                { 
					Expression expr = new Expression();
					expr.OpType = OperationType.None;
					expr.IsLeaf = true;
					expr.LeafType = ExpressionLeafType.Constant;
					expr.BoolValue = true;
					expr.ResultType = VariableType.BoolType;
					expr.Location = @$;
					$$.Tag = expr;                 
                }
           | F    
                { 
 					Expression expr = new Expression();
					expr.OpType = OperationType.None;
					expr.IsLeaf = true;
					expr.LeafType = ExpressionLeafType.Constant;
					expr.BoolValue = false;
					expr.ResultType = VariableType.BoolType;
					expr.Location = @$;
					$$.Tag = expr;                
                }
           | NULL    
                { 
                 	Expression expr = new Expression();
					expr.OpType = OperationType.None;
					expr.IsLeaf = true;
					expr.LeafType = ExpressionLeafType.Constant;
					expr.Value = null;
					expr.ResultType = VariableType.NullType;
					expr.Location = @$;
					$$.Tag = expr;     
                }
           | string_from_literals
                {
                 	Expression expr = new Expression();
					expr.OpType = OperationType.None;
					expr.IsLeaf = true;
					expr.LeafType = ExpressionLeafType.Constant;
					expr.Value = $1.sVal;
					expr.ResultType = new VariableType(VariableTypeEnum.Array, VariableType.IntType);
					expr.Location = @$;
					$$.Tag = expr;     					 
                }
           ;

string_from_literals : STRING_LITERAL string_from_literals
                {
					$$.sVal = $1.sVal + $2.sVal;
                }
                     | STRING_LITERAL
                {
					$$.sVal = $1.sVal;
                }
            ;

va_list : expression SEMI va_list
                {
					$$.Tag = $3.Tag;
					((VAList)$$.Tag).AddForward((Expression)$1.Tag);
                }
        | expression
                {
					VAList list = new VAList();
					list.Add((Expression)$1.Tag);
					$$.Tag = list;
                }
            ;

//CYCLES AND IFS

//CYCLE_TO

cycle_to : cycle_to_start TO expression DO statement_list END
            {
				CycleStatement cycle = (CycleStatement)$1.Tag;
				cycle.EndValue = (Expression)$3.Tag;
				cycle.Statements = (StatementList)$5.Tag;
				cycle.Location = @$;
				$$.Tag = cycle;
            }
         | cycle_to_start TO expression STEP expression DO statement_list END
            {
				CycleStatement cycle = (CycleStatement)$1.Tag;
				cycle.EndValue = (Expression)$3.Tag;
				cycle.Step = (Expression)$5.Tag;
				cycle.Statements = (StatementList)$7.Tag;
				cycle.Location = @$;
				$$.Tag = cycle;
            }
;

cycle_to_start : type IDENTIFIER ASSIGN expression
            {
				CycleStatement cycle = new CycleStatement();
				cycle.VariableType = (VariableType)$1.Tag;
				cycle.DeclareVariable = (string)$2.sVal;
				cycle.Init = (Expression)$4.Tag;
				$$.Tag = cycle;
            }
               | expression
            {
				CycleStatement cycle = new CycleStatement();
				cycle.Init = (Expression)$1.Tag;
				$$.Tag = cycle;				
            }
;

//WHILE_DO

while_do : WHILE expression DO statement_list END
            {
				WhileDoStatement statement = new WhileDoStatement();
				statement.Condition = (Expression)$2.Tag;
				statement.Statements = (StatementList)$4.Tag;
				$$.Tag = statement;
            }
;

//DO_WHILE

do_while : DO statement_list WHILE expression
            {
            	DoWhileStatement statement = new DoWhileStatement();
				statement.Condition = (Expression)$4.Tag;
				statement.Statements = (StatementList)$2.Tag;
				$$.Tag = statement;
            }
;

//IF_SET

if_set : primitive_if
            {
				$$.Tag = $1.Tag;
            }
       | switch_if
            {
				$$.Tag = $1.Tag;
            }
       | alternative_if
            {
				$$.Tag = $1.Tag;
            }
;


if_clause : IF expression THEN statement_list
            {
				IfClause clause = new IfClause();
				clause.Condition = (Expression)$2.Tag;
				clause.Statements = (StatementList)$4.Tag;
				clause.Location = @$;
				$$.Tag = clause;
            }
;

primitive_if : if_clause END
            {
				IfStatement statement = new IfStatement();
				statement.Clauses.Add((IfClause)$1.Tag);
				$$.Tag = statement;
            }
;

switch_if_sep : if_clause ELSIF elsif_set
            {
				IfStatement statement = new IfStatement();
				statement.Clauses.Add((IfClause)$1.Tag);
				statement.Clauses.AddRange((IfClauseList)$3.Tag);
				$$.Tag = statement;
            }
;

switch_if : switch_if_sep END
            {
				$$.Tag = $1.Tag; 
            }
;

alternative_if : if_clause ELSE statement_list END
            {
				IfStatement statement = new IfStatement();
				statement.Clauses.Add((IfClause)$1.Tag);
				statement.AlternativeStatements = (StatementList)$3.Tag;
				$$.Tag = statement;
				
            }
               | switch_if_sep ELSE statement_list END
            {
				IfStatement statement = (IfStatement)$1.Tag;
				statement.AlternativeStatements = (StatementList)$3.Tag;
				$$.Tag = statement;
            }
;

elsif_set : elsif_set ELSIF expression THEN statement_list
            {
				IfClauseList clauseList = (IfClauseList)$1.Tag;
				IfClause clause = new IfClause();
				clauseList.Add(clause);
				clause.Condition = (Expression)$3.Tag;
				clause.Statements = (StatementList)$5.Tag;
				$$.Tag = clauseList;
            }
          | expression THEN statement_list
            {
				IfClauseList clauseList = new IfClauseList();
				IfClause clause = new IfClause();
				clauseList.Add(clause);
				clause.Condition = (Expression)$1.Tag;
				clause.Statements = (StatementList)$3.Tag;
				$$.Tag = clauseList;
            }   
;    

//VAR_DEF

variable_definition_list : variable_definition_list SEMI IDENTIFIER ASSIGN expression
            {
				VariableDefinitionList list = (VariableDefinitionList)$1.Tag;
				VariableSymbol symbol = new VariableSymbol();
				symbol.VariableType = list.Definitions[0].VariableType;
				symbol.Name = $3.sVal;
				symbol.InitExpression = (Expression)$5.Tag;
				symbol.Location = @$;
				list.Add(symbol);
				$$.Tag = list;  				
            }
                         | variable_definition_list SEMI IDENTIFIER 
            {
 				VariableDefinitionList list = (VariableDefinitionList)$1.Tag;
				VariableSymbol symbol = new VariableSymbol();
				symbol.VariableType = list.Definitions[0].VariableType;
				symbol.Name = $3.sVal;
				symbol.Location = @$;
				list.Add(symbol);
				$$.Tag = list;            
            }            
                         | type IDENTIFIER ASSIGN expression
            {
				VariableDefinitionList list = new VariableDefinitionList();
				VariableSymbol symbol = new VariableSymbol();
				symbol.VariableType = (VariableType)$1.Tag;
				symbol.Name = $2.sVal;
				symbol.InitExpression = (Expression)$4.Tag;
				symbol.Location = @$;
				list.Add(symbol);
				$$.Tag = list;            
            }
                         | type IDENTIFIER 
            {
				VariableDefinitionList list = new VariableDefinitionList();
				VariableSymbol symbol = new VariableSymbol();
				symbol.VariableType = (VariableType)$1.Tag;
				symbol.Name = $2.sVal;
				symbol.Location = @$;
				list.Add(symbol);
				$$.Tag = list;
            }
;
