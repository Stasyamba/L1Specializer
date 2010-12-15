%namespace L1Specializer
%option verbose, summary

EXP     ((E|e)("+"|"-")?[0-9]+)
DELIM    (\n|\r\n|" "|\t)
ENDL    ["\n""\r\n"]
STR_S   [^"\n""\r\n""\""]
CHAR_S  [^"\n""\r\n""'"]

%%

"ArrayLength" return (int)Tokens.ARRAY_LENGTH;
"int"		return (int)Tokens.INT;
"char"		return (int)Tokens.CHAR;
"bool"		return (int)Tokens.BOOL;
"array"		return (int)Tokens.ARRAY;
"define"	return (int)Tokens.DEFINE;
"assert"	return (int)Tokens.ASSERT;
"do"	    return (int)Tokens.DO;
"if"		return (int)Tokens.IF;
"then"		return (int)Tokens.THEN;
"else"		return (int)Tokens.ELSE;
"elsif"		return (int)Tokens.ELSIF;
"while"		return (int)Tokens.WHILE;
"end"		return (int)Tokens.END;
"new"		return (int)Tokens.NEW;
"return"	return (int)Tokens.RETURN;
"step"		return (int)Tokens.STEP;
"to"		return (int)Tokens.TO;
"F" 		return (int)Tokens.F;
"T" 		return (int)Tokens.T;
"NULL"		return (int)Tokens.NULL;
"+"			return (int)Tokens.PLUS;
"-"			return (int)Tokens.MINUS;
"/"			return (int)Tokens.DIV;
"mod"		return (int)Tokens.MOD;
"*"			return (int)Tokens.MULT;
"**"		return (int)Tokens.POWER;
"="	    	return (int)Tokens.EQ;
"<>"		return (int)Tokens.NEQ;
">"			return (int)Tokens.GR;
">="		return (int)Tokens.GREQ;
"<"			return (int)Tokens.LE;
"<="		return (int)Tokens.LEEQ;
"or"        return (int)Tokens.OR;
"and"       return (int)Tokens.AND;
"not"       return (int)Tokens.NOT;
"xor"       return (int)Tokens.XOR;
":="		return (int)Tokens.ASSIGN;
"("			return (int)Tokens.LP;
")"			return (int)Tokens.RP;
"["			return (int)Tokens.LAP;
"]"			return (int)Tokens.RAP;
";"         return (int)Tokens.COMMA;
","         return (int)Tokens.SEMI;
"\""{STR_S}*"\"" {
    yylval.sVal = yytext.Substring(1, yytext.Length - 2);
    return (int)Tokens.STRING_LITERAL;
}
"$"[A-Z0-9]+ {
    yylval.sVal = CompilerServices.GetCharLiteral(yytext.Substring(1, yytext.Length - 1)).ToString();
    return (int)Tokens.STRING_LITERAL; 
}
"'"{CHAR_S}"'" {
    yylval.cVal = Convert.ToChar(yytext.Substring(1, 1));
    return (int)Tokens.CHAR_LITERAL;
}
"#{"[0-9a-fA-F]+"}" {
    yylval.cVal = CompilerServices.ParseCharFromCode(yytext);
    return (int)Tokens.CHAR_LITERAL;
}
"#"[A-Z0-9]+ {
    yylval.cVal = CompilerServices.GetCharLiteral(yytext.Substring(1, yytext.Length - 1));
    return (int)Tokens.CHAR_LITERAL;
}
"''"  {
    yylval.cVal = '\'';
    return (int)Tokens.CHAR_LITERAL;
}
([A-Za-z]([A-Za-z0-9])*)+	{
    yylval.sVal = yytext;
	return (int)Tokens.IDENTIFIER;
}
[0-9]+	{
    yylval.iVal = Convert.ToInt32(yytext);
	return (int)Tokens.INTEGER;
}
"{"[0-9]+"}"[0-9a-z]+	{
    yylval.iVal = CompilerServices.ParseInt(yytext);
	return (int)Tokens.INTEGER;
}
(" "|\t|\n|\r\n)* ;
.           return (int)Tokens.ILLEGAL;

%%
