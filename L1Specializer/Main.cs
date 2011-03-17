using System;
using System.IO;

using L1Specializer.Metadata;

namespace L1Specializer
{
    class Program
    {
		
		#region Nested types
		
		enum ExecutionMode
		{
			Compile,
			Interpret,
			Specialize
		}
		
		#endregion
		
		#region Debug methods
		
        static void PrintYYLVal(ValueType yylval)
        {
            Console.WriteLine("TOKEN:");
            Console.WriteLine("sVal = {0}", yylval.sVal);
            Console.WriteLine("iVal = {0}", yylval.iVal);
            Console.WriteLine("cVal = {0}", yylval.cVal);
            Console.WriteLine("-----------");
        }
		
		#endregion
		
		#region Service methods and fields
		
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
		
		static string AssemblyName = "program";
	    static string InputFileName = "program.l1";
		static string OutFileName = "program.exe";
		static ExecutionMode Mode = ExecutionMode.Compile;
		
		static void AnalizeCommandLine(string[] args)
		{
			foreach (string arg in args)
			{
				if (arg == "-c")
					Mode = ExecutionMode.Compile;
				else if (arg == "-i")
					Mode = ExecutionMode.Interpret;
				else if (arg == "-s")
					Mode = ExecutionMode.Specialize;
				else if (arg.Length > 5 && arg.Substring(0, 5) == "-out:")
				{
					OutFileName = arg.Substring(5);
					if (OutFileName.Contains("."))
						AssemblyName = OutFileName.Substring(0, OutFileName.LastIndexOf("."));
					else
					    AssemblyName = OutFileName;
				}
				else if (File.Exists(arg))
				{
					InputFileName = arg;
				}
			}
			if (OutFileName == "program.exe" && InputFileName != "program.l1")
			{
				if (InputFileName.Contains("."))
				{
					OutFileName = InputFileName.Substring(0, InputFileName.LastIndexOf(".")) + ((Mode == Program.ExecutionMode.Specialize) ? ".spec.l1" : ".exe");
					AssemblyName = InputFileName.Substring(0, InputFileName.LastIndexOf("."));
				}
				else
				{
					OutFileName = InputFileName + ".exe";
					AssemblyName = InputFileName;
				}
			}
		}
		
		#endregion
		
		#region Mein method

        static void Main(string[] args)
        {
			Tests.SpecializerTests.Run();
			
			AnalizeCommandLine(args);
			Stream newFs = null;
			try
			{
            	newFs = Preprocessor.PreprocessorServices.DeleteComments(InputFileName);
			}
			catch 
			{
				Console.WriteLine("Can't open file with program! File name = " + InputFileName);
				return;
			}
            Scanner scan = new Scanner(newFs);
            Parser p = new Parser(); 
            p.scanner = scan;
            //p.Trace = true;
			
            CompilerServices.InitStdFunctions(CompilerServices.Program);
            bool b = p.Parse();
            if (!b)
            {
                Console.WriteLine("Parse error!");
				Console.WriteLine("Line = " + scan.yylloc.eLin);
                return;
            }

            if (CompilerServices.Errors.Count > 0)
            {
                PrintOutput();
				return;
            }
            else
            {
                L1Program program = CompilerServices.Program;
                CompilerServices.SemanticAnalise(program);
                PrintOutput();

                if (CompilerServices.Errors.Count == 0)
                {
					//Mode = Program.ExecutionMode.Specialize;
					if (Mode == ExecutionMode.Compile)
					{
                    	System.Reflection.Emit.AssemblyBuilder ab = EmitServices.GenerateAssembly(AssemblyName, program);
						try
						{
                    		ab.Save(OutFileName);
						}
						catch
						{
							Console.WriteLine("Can't open file for writing! File name = " + OutFileName);
							return;
						}
					}
					else if (Mode == ExecutionMode.Interpret)
					{
						Console.WriteLine("Inpterpretation =)");
						
					}
					else if (Mode == ExecutionMode.Specialize)
					{
						var ilProgram = ILEmitServices.EmitProgram(program);
						var outProgramSource = SpecializerServices.Specialize(ilProgram);
						
						try
						{
                    		System.IO.File.WriteAllText(OutFileName, outProgramSource.ToString());
						}
						catch
						{
							Console.WriteLine("Can't open file for writing! File name = " + OutFileName);
							return;
						}
						//Console.WriteLine("Specialization ;)");
					}						
				}
            }
			
			Console.WriteLine("In = " + InputFileName);
			Console.WriteLine("Out = " + OutFileName);
			Console.WriteLine("Assembly = " + AssemblyName);
            

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
		
		#endregion
		
    }
}

