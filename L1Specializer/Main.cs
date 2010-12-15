using System;
using System.IO;

using L1Specializer.Metadata;

namespace L1Specializer
{
    class Program
    {
        static void PrintYYLVal(ValueType yylval)
        {
            Console.WriteLine("TOKEN:");
            Console.WriteLine("sVal = {0}", yylval.sVal);
            Console.WriteLine("iVal = {0}", yylval.iVal);
            Console.WriteLine("cVal = {0}", yylval.cVal);
            Console.WriteLine("-----------");
        }

        static void PrintOutput()
        {
            foreach (CompilerMessage msg in CompilerServices.Errors)
            {
                Console.WriteLine(msg);
            }
            foreach (CompilerMessage msg in CompilerServices.Warnings)
            {
                Console.WriteLine(msg);
            }
        }


        static void Main(string[] args)
        {
            try
            {
                //ileStream fs = File.OpenRead("program.txt");

                Stream newFs = Preprocessor.PreprocessorServices.DeleteComments("program.txt");

                Scanner scan = new Scanner(newFs);

                Parser p = new Parser();
                p.scanner = scan;
                //p.Trace = true;

                CompilerServices.InitStdFunctions(CompilerServices.Program);

                bool b = p.Parse();

                if (!b)
                {
                    Console.WriteLine("Parse error");
                    return;
                }

                if (CompilerServices.Errors.Count > 0)
                {
                    PrintOutput();
                }
                else
                {
                    L1Program program = CompilerServices.Program;
                    

                    CompilerServices.SemanticAnalise(program);
                   
                    PrintOutput();

                    if (CompilerServices.Errors.Count == 0)
                    {
                        System.Reflection.Emit.AssemblyBuilder ab = EmitServices.GenerateAssembly("out", program);
                        ab.Save("out.exe");
                    }

                }

                

                //int lex = scan.yylex();
                //while (lex != 2)
                //{
                //    Console.Write(lex);
                //    Console.Write(" - ");
                //    Console.WriteLine(scan.yytext);
                //    lex = scan.yylex();
                //    ;
                //}
                //;

                ;
            }
            catch
            {
                ;
            }
        }
    }
}

