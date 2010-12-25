using System;
using L1Specializer.Environment;

using SDDebug = System.Diagnostics.Debug;

namespace L1Specializer.Tests
{
	public static class SpecializerTests
	{
		
		public static void Run()
		{
			AbstractEnvironment env1 = new AbstractEnvironment();
			env1.SetValue("A", new int[] {0,0,0});
			env1.SetValue("B", false);
			env1.SetValue("I", 32);
			AbstractEnvironment env2 = new AbstractEnvironment();
			env2.SetValue("A", new int[] {0,0,0});
			env2.SetValue("B", false);
			env2.SetValue("I", 32);
			
			SDDebug.Assert(env1.Equals(env2));
			
			env2.SetValue("A", new int[] {0,0,1});
			
			SDDebug.Assert(env1.Equals(env2) == false);
			
		}
	
	}
}

