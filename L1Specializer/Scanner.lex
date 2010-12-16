%namespace L1Specializer
%option verbose, summary

EXP     ((E|e)("+"|"-")?[0-9]+)
DELIM    (\n|\r\n|" "|\t)
ENDL    ["\n""\r\n"]
STR_S   [^"\n""\r\n""\""]
CHAR_S  [^"\n""\r\n""'"]

%%

"ArrayLength" { yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.ARRAY_LENGTH; }
"int"		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.INT; }
//"char"		return (int)Tokens.CHAR;
"bool"		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.BOOL; }
"array"		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.ARRAY; }
"define"	{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.DEFINE; }
"assert"	{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.ASSERT; }
"do"	    { yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.DO; }
"if"		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.IF; }
"then"		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.THEN; }
"else"		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.ELSE; }
"elsif"		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.ELSIF; }
"while"		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.WHILE; }
"end"		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.END; }
"new"		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.NEW; }
"return"	{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.RETURN; }
"step"		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.STEP; }
"to"		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.TO; }
"F" 		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.F; }
"T" 		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.T; }
"NULL"		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.NULL; }
"+"			{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.PLUS; }
"-"			{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.MINUS; }
"/"			{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.DIV; }
"mod"		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.MOD; }
"*"			{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.MULT; }
"**"		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.POWER; }
"="	    	{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.EQ; }
"<>"		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.NEQ; }
">"			{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.GR; }
">="		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.GREQ; }
"<"			{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.LE; }
"<="		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.LEEQ; }
"or"        { yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.OR; }
"and"       { yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.AND; }
"not"       { yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.NOT; }
"xor"       { yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.XOR; }
":="		{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.ASSIGN; }
"("			{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.LP; }
")"			{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.RP; }
"["			{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.LAP; }
"]"			{ yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.RAP; }
";"         { yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.COMMA; }
","         { yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.SEMI; }
"\""{STR_S}*"\"" {
	yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol);
    yylval.sVal = yytext.Substring(1, yytext.Length - 2);
    return (int)Tokens.STRING_LITERAL;
}
"$"[A-Z0-9]+ {
	yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol);
    yylval.sVal = CompilerServices.GetCharLiteral(yytext.Substring(1, yytext.Length - 1)).ToString();
    return (int)Tokens.STRING_LITERAL; 
}
"'"{CHAR_S}"'" {
	yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol);
    yylval.iVal = Convert.ToInt32(Convert.ToChar(yytext.Substring(1, 1)));
    return (int)Tokens.CHAR_LITERAL;
}
"#{"[0-9a-fA-F]+"}" {
	yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol);
    yylval.iVal = Convert.ToInt32(CompilerServices.ParseCharFromCode(yytext));
    return (int)Tokens.CHAR_LITERAL;
}
"#"[A-Z0-9]+ {
	yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol);
    yylval.iVal = Convert.ToInt32(CompilerServices.GetCharLiteral(yytext.Substring(1, yytext.Length - 1)));
    return (int)Tokens.CHAR_LITERAL;
}
"''"  {
	yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol);
    yylval.iVal = Convert.ToInt32('\'');
    return (int)Tokens.CHAR_LITERAL;
}
([A-Za-z]([A-Za-z0-9])*)+	{
	yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol);
    yylval.sVal = yytext;
	return (int)Tokens.IDENTIFIER;
}
[0-9]+	{
	yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol);
    yylval.iVal = Convert.ToInt32(yytext);
	return (int)Tokens.INTEGER;
}
"{"[0-9]+"}"[0-9a-z]+	{
	yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol);
    yylval.iVal = CompilerServices.ParseInt(yytext);
	return (int)Tokens.INTEGER;
}
(" "|\t|\n|\r\n)* ;
			
.           { yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol); return (int)Tokens.ILLEGAL; }


%%
