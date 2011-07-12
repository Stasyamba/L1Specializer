%namespace L1Specializer.Postprocessor

%parsertype L1ExpressionParser
%partial

%using L1Specializer.SyntaxTree
%using L1Specializer.SyntaxTree.IfStatements
%using L1Specializer.Metadata
%using L1Runtime.SyntaxTree

%start s

%YYSTYPE L1Specializer.ValueType


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

s : expression
			{
				this.ParsedExpression = (L1Specializer.SyntaxTree.Expression)$$.Tag;
			}
;


//EXPRESSION

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
					//TODO: ïðîâåðêà íà íå Bool
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
					//Ñþäà íóæíî áóäåò åùå äîáàâèòü òèï ïåðåìåííîé (äëÿ ñåìàíòè÷åñêîãî àíàëèçà)
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

