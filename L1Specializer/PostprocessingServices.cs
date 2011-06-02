using System;
using System.Text;
using System.Collections.Generic;

namespace L1Specializer
{
	public static class PostprocessingServices
	{
		
		public static StringBuilder RemoveDummyVariables(StringBuilder functionSource) {
			
			string[] lines = functionSource.ToString().Split(new string[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
			List<string> ls = new List<string>(lines);
			
			bool changed = false;		
			do {
				changed = false;
				for (int i = 0; i < ls.Count - 1; ++i) {
					string lineOne = ls[i];
					string lineTwo = ls[i + 1];
					
					string r = m_removeDummyVariables(lineOne, lineTwo);
					if (r != String.Empty) {
						ls[i] = r;
						ls.RemoveAt(i + 1);
						changed = true;
						break;
					}
				}
			} while (changed);
				
			changed = false;		
			do {
				changed = false;
				for (int i = ls.Count - 1; i > 0; --i) {
					string lineOne = ls[i];
					string lineTwo = ls[i - 1];
		
					string r = m_reduceReturn(lineOne, lineTwo);
					if (r != String.Empty) {
						ls[i] = r;
						ls.RemoveAt(i - 1);
						changed = true;
						break;
					}
				}
			} while (changed);
			
			
			return new StringBuilder(String.Join("\n", ls)).AppendLine();
		}
		
		private static string m_removeDummyVariables(string lineOne, string lineTwo) {
			if (!lineOne.StartsWith("\t_V") || !lineTwo.StartsWith("\t")) { return String.Empty; }
			if (!lineOne.Contains(" := ")) { return String.Empty; };
			
			string[] parsOne = lineOne.Substring(1).Split(new string[] { " := " }, StringSplitOptions.RemoveEmptyEntries);
			string varNameOne = parsOne[0];
			string varValOne = parsOne[1].Substring(0, parsOne[1].Length - 1);
			
			if (lineTwo.Contains(" := ")) {
				if (lineTwo.IndexOf(" := ") <  lineTwo.IndexOf(varNameOne)) {
					lineTwo = lineTwo.Replace(varNameOne, "(" + varValOne + ")");
					return lineTwo;
				} else {
					return String.Empty;
				}
			}
			if (lineTwo.Contains("if " + varNameOne + " then ")) {
				lineTwo = lineTwo.Replace(varNameOne, varValOne);
				return lineTwo;
			} 
			                                                
			return String.Empty;
		}
		
		private static string m_reduceReturn(string lineOne, string lineTwo) {
			if (!lineOne.StartsWith("\treturn ") || !lineTwo.StartsWith("\t") || !lineTwo.Contains(" := ")) { return string.Empty; }
			
			string[] pars = lineTwo.Substring(1).Split(new string[] { " := " }, StringSplitOptions.RemoveEmptyEntries);
			string varName = pars[0];
			string varVal = pars[1].Substring(0, pars[1].Length - 1);
		
			if (lineOne.Contains(varName)) {
				lineOne = lineOne.Replace(varName, "(" + varVal + ")");
				return lineOne;
			}
			
			return string.Empty;
		}
		
	}
}

