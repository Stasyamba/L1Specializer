using System;
using System.Collections.Generic;
using System.Text;

using L1Runtime.SyntaxTree;
using L1Specializer.IL;

using System.Linq;

namespace L1Specializer
{
	
	#region Helpers
	
	internal class SpecPoint {
		
		public int P { get; set; }
		public AbstractEnvironment Env { get; set; }
	
		public int L { get; set; }
	}
	
	internal class SpecPointContainer {
		
		private Dictionary<int, List<SpecPoint>> f_container;
		
		public SpecPointContainer() { f_container = new Dictionary<int, List<SpecPoint>>(); }
		
		public void AddSpecPoint(SpecPoint sp) {
			if (f_container.ContainsKey(sp.P) == false) {
				f_container.Add(sp.P, new List<SpecPoint>());
			}
			f_container[sp.P].Add(sp);
		}
		
		///env must be freezed!!!
		public SpecPoint GetSpecPoint(int P, AbstractEnvironment env) {
			if (f_container.ContainsKey(P)) {
				var list = f_container[P];
				var hash = env.GetHashCode();
				foreach (var sp in list) {
					if (sp.Env.GetHashCode() == hash && sp.Env.Equals(env))
						return sp;
				}
			}
			return null;
		}
	}
	
	#region Resolving function calls
	
	internal class FunctionCallContainer {
		
		private static int IdSeed = 1;
		
		#region Constructor
		
		public FunctionCallContainer() {
			Id = IdSeed++;
			Parameters = new List<object>();
		}
		
		#endregion
		
		public int Id {
			get;
			private set;
		}
		
		public ILFunction Function {
			get;
			set;
		}
		
		public ILFunction SourceFunction {
			get;
			set;
		}
		
		public string ResultVariable {
			get;
			set;
		}
		
		public List<object> Parameters {
			get;
			set;
		}
		
		public void AddParameter(object val) {
			Parameters.Add(val);
		}
		
		
		public override string ToString ()
		{
			return String.Format("<<FunctionCall_{0}>>", Id);
		}
		
	}
	
	internal class FunctionSpecializationResult {
	
		public FunctionSpecializationResult() {
			FunctionCallsNeedToResolve = new List<FunctionCallContainer>();
			SpecializationRules = new Dictionary<string, object>();
		}
	
		public StringBuilder Source {
			get;
			set;
		}
		
		public ILFunction Function {
			get;
			set;
		}
		
		public Dictionary<string, object> SpecializationRules {
			get;
			set;
		}
		
		public List<FunctionCallContainer> FunctionCallsNeedToResolve {
			get;
			set;
		}
		
	}
	
	
	#endregion
	
	#region Func spec name resolver
	
	internal class FunctionNameResolveResult {
		
		public string Name {
			get;
			set;
		}
		
		public Dictionary<string, object> SepecializationRules {
			get;
			set;
		}
	
		public bool IsPresented {
			get;
			set;
		}
		
	}
	
	internal class FunctionNameResolver {
		
		public FunctionNameResolver() {
			f_container = new Dictionary<string, List<KeyValuePair<string, Dictionary<string, object>>>>();
			f_stopped = false;
		}
		
		#region Internal structure
		
		private Dictionary<string, List<KeyValuePair<string, Dictionary<string, object>>>> f_container;
		
		private bool f_stopped;
		
		private Dictionary<string, object> m_getDefaultSpecRules(Dictionary<string, object> specRules) {
			var r = new Dictionary<string, object>(specRules.Count);
			foreach (var k in specRules.Keys) {
				r.Add(k, Dynamic.Value);
			}	
			return r;
		}
		
		private bool SpecRulesEquals(Dictionary<string, object> one, Dictionary<string, object> two) {
			if (one.Count != two.Count)
				return false;
			
			foreach (var k in one.Keys) {
				if (two.ContainsKey(k) == false)
					return false;
				bool t = AbstractEnvironment.ValsEquals(one[k], two[k]);
				if (t == false)
					return false;
			}
			return true;
		}
		
		private bool IsDefaultSpecRule(Dictionary<string, object> specRule) {
			foreach (var v in specRule.Values) {
				if (v != Dynamic.Value)
					return false;
			}
			return true;
		}
		
		private static int counter = 1;
		public string GetSpecName(string originalName, bool IsDefault) {
			if (IsDefault)
				return originalName;
			else
				return originalName + "_" + (counter++) + "_spec";
		}
		
		private string PushSpecialization(string originalName, Dictionary<string, object> specRules) {
			if (f_container.ContainsKey(originalName) == false)
					f_container.Add(originalName, new List<KeyValuePair<string, Dictionary<string, object>>>());
			var specName = GetSpecName(originalName, IsDefaultSpecRule(specRules));
			f_container[originalName].Add(new KeyValuePair<string, Dictionary<string, object>>(specName, specRules));
			return specName;
		}
		
		#endregion
		
		#region Public methods
	
		public FunctionNameResolveResult GetSpecializedFunctionInfo(string originalName, Dictionary<string, object> specRules) {
			if (f_stopped)
				specRules = m_getDefaultSpecRules(specRules);
			
			bool presented = true;
			string specName = null;
			if (f_container.ContainsKey(originalName) == false) {
				specName = PushSpecialization(originalName, specRules);
				presented = false;
			} else {
				var specList = f_container[originalName];
				bool contains = false;
				foreach (var spec in specList) {
					if (SpecRulesEquals(spec.Value, specRules)) {
						specName = spec.Key;
						presented = true;
						contains = true;
						break;
					}
				}
				if (contains == false) {
					specName = PushSpecialization(originalName, specRules);
					presented = false;					
				}
			}
			
			return new FunctionNameResolveResult { IsPresented = presented, Name = specName, SepecializationRules = specRules };
		}
		
		public void Stop() {
			f_stopped = true;
		}
		
		#endregion

	}
	
	
	#endregion
	
	
	#endregion
	
	public static class SpecializerServices
	{
		
		#region Helpers
		
		
		
		public static string RenderConst(object c) {
			if (c == null) {
				return "NULL";
			}
			else if (c is Int32) {
				return Convert.ToInt32(c).ToString();
			}
			else if (c is Boolean) {
				var b = Convert.ToBoolean(c);
				if (b)
					return "T";
				else
					return "F" ;
			}
			else if (c is Array) {
				Array a = c as Array;
				if (a.GetValue(0) is Int32) {
					int l = a.GetLength(0);
					var sb = new StringBuilder();
					sb.Append("\"");
					for (int i = 0; i < l; ++i) {
						sb.Append((Char)((Int32)a.GetValue(i)));
					}
					sb.Append("\"");
					return sb.ToString();
				} else {
					//TODO: Add array literals and render it
					return "<<ARRAY>>";
				}
			}
			else
				return "<<BAD CONST>>";		
		}
		
		private static void m_renderConst(StringBuilder sb, object c) {
			sb.Append(RenderConst(c));
		}
		
		#endregion
		
		#region Methods
		
		
		
		internal static StringBuilder Specialize(List<ILFunction> ilProgram) {
		
			var q = new Queue<FunctionSpecializationResult>();
			var nr = new FunctionNameResolver();
			
			foreach (var f in ilProgram) {
				if (f.Name == "Main") {
					var p = new Dictionary<string, object>();
					foreach (var par in f.Parameters) {
						p.Add(par, Dynamic.Value);
					}
					var s = SpecializeFunction(f, p, new List<string>());
					q.Enqueue(s);
					//Dummy get spec info = for mark function as specialized
					nr.GetSpecializedFunctionInfo(f.Name, p);
				}
			}
			
			var resultProgram = new StringBuilder(1024*1024);
			while (q.Count != 0) {
				var spec = q.Dequeue();
				
				var cache = nr.GetSpecializedFunctionInfo(spec.Function.Name, spec.SpecializationRules);
				var generatedName = cache.Name;
				
				if (cache.IsPresented == false)
					throw new InvalidOperationException("Bad spcailziation sequence");
				
				//Generate signature
				
				resultProgram.Append("* Specialization of function ").Append(generatedName).Append(" with parameters:").AppendLine();
				foreach (var kvp in spec.SpecializationRules) {
					resultProgram.Append("*\t").Append(kvp.Key).Append(" = ");
					if (kvp.Value == Dynamic.Value)
						resultProgram.Append("dynamic");
					else
						resultProgram.Append(RenderConst(kvp.Value));
					resultProgram.AppendLine();
				}
				
				resultProgram.Append("define ");
				if (spec.Function.ReturnType != null)
					resultProgram.Append(spec.Function.ReturnType.ToCompileableString()).Append(" ");
				resultProgram.Append(generatedName).Append("(");
				int i = 0;
				foreach (var kvp in spec.SpecializationRules) {
					if (kvp.Value == Dynamic.Value) {
						if (i != 0)
							resultProgram.Append(", ");
						var typeS = spec.Function.LocalTypes.Where(kv => kv.Key == kvp.Key).Select(kv => kv.Value).First().ToCompileableString(); 
						resultProgram.Append(typeS).Append(" ").Append(kvp.Key);
						i++;
					}
				}
				resultProgram.Append(")").AppendLine();
				
				//Resolving function calls and modify function's source
				
				foreach (var fcall in spec.FunctionCallsNeedToResolve) {					
					if (fcall.Parameters.Count != fcall.Function.Parameters.Count)
						throw new InvalidOperationException("Bad spcailziation request");
				
					var specRule = new Dictionary<string, object>();
					for (int j = 0; j < fcall.Parameters.Count; ++j) {
						specRule.Add(fcall.Function.Parameters[j], (fcall.Parameters[j] is string) ? Dynamic.Value : fcall.Parameters[j]);
					}
					var resolved = nr.GetSpecializedFunctionInfo(fcall.Function.Name, specRule);
					
					if (resolved.IsPresented == false) {
						var specialized = SpecializeFunction(fcall.Function, specRule, new List<string>());
					
						q.Enqueue(specialized);
					}
					
					var functionCallString = new StringBuilder(64);
					
					functionCallString.Append(fcall.ResultVariable).Append(" := ").Append(resolved.Name).Append("(");
					i = 0; int k = 0;
					foreach (var kvp in resolved.SepecializationRules) {
						if (kvp.Value == Dynamic.Value) {
							if (i != 0) 
								functionCallString.Append(", ");
							var p = fcall.Parameters[k];
							if (p is string)
								functionCallString.Append(p);
							else
								functionCallString.Append(RenderConst(p));
							i++;
						}
						k++;
					}
					functionCallString.Append(");").AppendLine();

					spec.Source.Replace(fcall.ToString(), functionCallString.ToString());
				}
				
				resultProgram.Append(spec.Source);
				
				//Generate "end" keyword
				
				resultProgram.Append("end").AppendLine();
			}
			
			return resultProgram;
			//Console.WriteLine (resultProgram.ToString());
		}
		
		
		
		private static FunctionSpecializationResult SpecializeFunction(
		                                                               ILFunction function, 
		                                                               Dictionary<string, object> pars, 
		                                                               List<string> forceDynamic) {
			var source = new StringBuilder();
			int LabelId = 1;
			
			var visited = new SpecPointContainer();
			var q = new Queue<SpecPoint>();
			var initEnv = new AbstractEnvironment();
			foreach (var kvp in pars) {
				initEnv.SetValue(kvp.Key, kvp.Value);
			}
			foreach (var kvp in function.LocalTypes) {
				if (pars.ContainsKey(kvp.Key))
					continue;
			
				object val = null;
				if (kvp.Value.TypeEnum == L1Runtime.SyntaxTree.VariableTypeEnum.Integer)
					val = 0;
				if (kvp.Value.TypeEnum == L1Runtime.SyntaxTree.VariableTypeEnum.Bool)
					val = false;
				if (kvp.Value.TypeEnum == L1Runtime.SyntaxTree.VariableTypeEnum.Char)
					val = (char)0;
				initEnv.SetValue(kvp.Key, val);
			}
			
			
			initEnv = _bindTimeAnalyze(function, initEnv);
			initEnv.Trace();
			
			Dictionary<VariableType, List<string>> localVarsInfo = new Dictionary<VariableType, List<string>>();
			foreach (var kvp in function.LocalTypes) {
				if (initEnv.IsDynamic(kvp.Key) && !function.Parameters.Contains(kvp.Key)) {
					if (localVarsInfo.ContainsKey(kvp.Value) == false) {
						localVarsInfo.Add(kvp.Value, new List<string>());
					}
					localVarsInfo[kvp.Value].Add(kvp.Key);
				}
			}
			foreach (var kvp in localVarsInfo) {
				source.Append(kvp.Key.ToCompileableString()); source.Append(" ");
				for (int i = 0; i < kvp.Value.Count; ++i) {
					source.Append("\t"); source.Append(kvp.Value[i]);
					if (i != kvp.Value.Count - 1)
						source.Append(", ");
				}
				source.Append(";"); source.Append(System.Environment.NewLine);
			}
			
			foreach (var kvp in pars) {
				if (kvp.Value != Dynamic.Value && initEnv.IsDynamic(kvp.Key)) {
					source.Append("\t"); source.Append(kvp.Key); source.Append(" := "); source.Append(RenderConst(kvp.Value));
				}
			}
			
			
			source.Append("* Begin of function specialized body"); source.Append(System.Environment.NewLine);
			
			var initialSp = new SpecPoint { Env = initEnv, P = 1, L = LabelId++ } ;
			q.Enqueue(initialSp);
			initialSp.Env.Freeze();
			visited.AddSpecPoint(initialSp);
			
			var calls = new List<FunctionCallContainer>();
			while (q.Count != 0) {
				var sp = q.Dequeue();
				var env = sp.Env.Clone();
				
				source.Append("L_"); source.Append(sp.L); source.Append(":"); source.Append(System.Environment.NewLine);
				
				bool stopped = false;
				int p = sp.P;
				
				while (!stopped) {
					var instr = function.Body[p - 1];
					
					if (instr is ILExpression) {
						var expr = instr as ILExpression;
						
						stopped = SpecializeExpression(function, expr, env, source, calls);
						
						p++;
					} 
					else if (instr is ILBranch) {
						var br = (instr as ILBranch);
						
						var condVar = br.Condition.Const.ToString();
						if (env.IsDynamic(condVar)) {
							
							env.Freeze();
							var succSp = visited.GetSpecPoint(br.SuccessJump, env);
							var failSp = visited.GetSpecPoint(br.FailJump, env);
							
							if (succSp == null) {
								succSp = new SpecPoint { Env = env, P = br.SuccessJump, L = LabelId++ };
								q.Enqueue(succSp);
								visited.AddSpecPoint(succSp);
							}
							if (failSp == null) {
								failSp = new SpecPoint { Env = env, P = br.FailJump, L = LabelId++ };
								q.Enqueue(failSp);
								visited.AddSpecPoint(failSp);
							}
							
							source.Append("\tif "); source.Append(condVar); source.Append(" then"); source.Append(System.Environment.NewLine);
							source.Append("\t\tgoto "); source.Append("L_"); source.Append(succSp.L); source.Append(System.Environment.NewLine);
							source.Append("\telse"); source.Append(System.Environment.NewLine);
							source.Append("\t\tgoto "); source.Append("L_"); source.Append(failSp.L); source.Append(System.Environment.NewLine);
							source.Append("\tend; "); source.Append(System.Environment.NewLine);
							
							stopped = true;
						}
						else {
							var cond = (bool)env.GetValue(condVar);
							if (cond)
								p = br.SuccessJump;
							else
								p = br.FailJump;
						}
					}
					else if (instr is ILGoto) {
						p = (instr as ILGoto).GoTo;
					}
					else if (instr is ILReturn) {
						var ret = (instr as ILReturn);
						
						if (ret.Return == null) {
							source.Append("\treturn;"); source.Append(System.Environment.NewLine);
						}
						else {
							source.Append("\treturn ");
							var retVar = ret.Return.Const.ToString();
							if (env.IsDynamic(retVar)) {
								source.Append(retVar);
							}
							else {
								m_renderConst(source, env.GetValue(retVar));
							}
							source.Append(";"); source.Append(System.Environment.NewLine);
						}
						
						stopped = true;
					}
				}
				
				//TODO: Stop if too big query and try with more dynamic variables
				
			}
			source.Append("0").AppendLine();
			
			//System.Console.WriteLine(source.ToString());
			
			var r = new FunctionSpecializationResult { FunctionCallsNeedToResolve = calls, Source = source, Function = function, SpecializationRules = pars };
			return r;
		}
		
		private static bool SpecializeExpression(ILFunction f, ILExpression expr, AbstractEnvironment env, StringBuilder source, List<FunctionCallContainer> calls) {
			
			if (expr.Type == ILExpressionType.Assign) {
				
				var left = expr.LeftNode;
				var right = expr.RightNode;
				if (left.Type == ILExpressionType.VariableAccess) {
					var varName = left.Const.ToString();
					try {
						var rightRed = right.AbstactReduce(varName, env, f.LocalTypes);
						if (rightRed is ILExpression) {
								var fcall = rightRed as ILExpression;
								
								SpecializeFunctionCall(f, fcall, varName, source, env, calls);
								//source.Append("<<FUNCTION_CALL>>");
						}
						else if (env.IsDynamic(varName)) {
							
							//source.Append(varName); source.Append(" := ");
							
							if (rightRed is string) {
								source.Append("\t"); source.Append(varName); source.Append(" := ");
								source.Append((string)rightRed);
								source.Append(";"); source.Append(System.Environment.NewLine);
							}
							else {
								source.Append("\t"); source.Append(varName); source.Append(" := ");
								source.Append(RenderConst(rightRed));
								source.Append(";"); source.Append(System.Environment.NewLine);
							}
						}
						else {
							object val = rightRed;
							
							System.Diagnostics.Debug.Assert(val is string == false);
							System.Diagnostics.Debug.Assert(val is ILExpression == false);
							
							env.SetValue(varName, val);
						}
					}
					catch (NullReferenceException) {
						source.Append("\t__spec_raise_null_ref_exception();"); source.Append(System.Environment.NewLine);
					}
					catch (IndexOutOfRangeException) {
						source.Append("\t__spec_raise_index_out_of_range_exception();"); source.Append(System.Environment.NewLine);
					}
				}
				else {
					System.Diagnostics.Debug.Assert(left.Type == ILExpressionType.ArrayAccess);
					
					try {
						var arrayName = left.LeftNode.Const.ToString();
						object index = null;
						if (env.IsDynamic(left.RightNode.Const.ToString())) {
							index = left.RightNode.Const.ToString();
						} else {
							index = env.GetValue(left.RightNode.Const.ToString());
						}
						
						object rightVar = null;
						if (env.IsDynamic(right.Const.ToString())) {
							rightVar = right.Const.ToString();
						} else {
							rightVar = env.GetValue(right.Const.ToString());
						}
						
						if (env.IsDynamic(arrayName)) {
							source.Append("\t"); source.Append(arrayName); source.Append("["); source.Append(index); source.Append("] := ");
							source.Append(rightVar); source.Append(";"); source.Append(System.Environment.NewLine);
						}
						else {
							object[] a = (object[])env.GetValue(arrayName);
							a[Convert.ToInt32(index)] = rightVar;
						}
					}
					catch (NullReferenceException) {
						source.Append("\t__spec_raise_null_ref_exception();"); source.Append(System.Environment.NewLine);
					}
					catch (IndexOutOfRangeException) {
						source.Append("\t__spec_raise_index_out_of_range_exception();"); source.Append(System.Environment.NewLine);
					}
				}
				
			}
			else if (expr.Type == ILExpressionType.FunctionCall) {
				source.Append("\t"); source.Append(expr.Const.ToString()); source.Append("(");
				for (int i = 0; i < expr.VAList.Count; ++i) {
					string p = null;
					if (env.IsDynamic(expr.VAList[i].Const.ToString())) {
						p = expr.VAList[i].Const.ToString();
					} else {
						p = RenderConst(env.GetValue(expr.VAList[i].Const.ToString()));
					}
					source.Append(p);
					if (i != expr.VAList.Count - 1)
						source.Append(", ");
				}
				source.Append(");"); source.Append(System.Environment.NewLine);
			}
			
			return false;
		}
		
		private static void SpecializeFunctionCall(ILFunction context, 
		                                           ILExpression fcall, 
		                                           string resultVar, 
		                                           StringBuilder source, 
		                                           AbstractEnvironment env, 
		                                           List<FunctionCallContainer> calls) {
		
			var f = fcall.Function;
			//Stdlib function
			if (f.EmbeddedBody != null) {
				if (f.CanBeCalculatedWithoutRun && !fcall.VAList.Any(pexpr => env.IsDynamic(pexpr.Const.ToString()))) {
					
					var args = fcall.VAList.Select(ilexpr => ilexpr.Eval(env))
						.Select(o => (o is Int32) ? o : ( (o is Boolean) ? o : L1Runtime.L1Runtime.GetArrayFromObjectArray((object[])o))).ToArray(); 
					var result = f.EmbeddedBody.Invoke(null, args);                                                                                                                  
	
					if (env.IsDynamic(resultVar)) {
						source.Append("\t"); source.Append(resultVar); source.Append(" := "); RenderConst(result); source.Append(");"); source.Append(System.Environment.NewLine);
					} else {
						if (result is L1Runtime.L1Array<int>) {
							var l1arr = result as L1Runtime.L1Array<int>;
							var a = new object[l1arr.GetLength()];
							for (int i = 0; i < a.Length; ++i) {
								a[i] = l1arr.GetValue(i);
							}
							result = a;
						}
						env.SetValue(resultVar, result);
					}
				} else {
					source.Append("\t"); source.Append(resultVar); source.Append(" := "); source.Append(f.Name); source.Append("(");
					for (int i = 0; i < fcall.VAList.Count; ++i) {
						var p = fcall.VAList[i].Const.ToString();
						if (env.IsDynamic(p)) {
							source.Append(p);
						} else {
							source.Append(RenderConst(env.GetValue(p)));
						}
						if (i != fcall.VAList.Count - 1)
							source.Append(", ");
					}
					source.Append(");"); source.Append(System.Environment.NewLine);
				}
			} else {
				if (f.CanBeCalculatedWithoutRun && !fcall.VAList.Any(pexpr => env.IsDynamic(pexpr.Const.ToString()))) {
					var args = fcall.VAList.Select(ilexpr => ilexpr.Eval(env)).ToArray();
					var res = f.Call(args);
					
					if (env.IsDynamic(resultVar)) {
						source.Append("\t").Append(resultVar).Append(" := ").Append(RenderConst(res)).Append(";").AppendLine();
					} else {
						env.SetValue(resultVar, res);
					}
				}
				else {
					var functionCallRef = new FunctionCallContainer { 
						Function = fcall.Function, 
						SourceFunction = context, 
						ResultVariable = resultVar, 
						Parameters = fcall.VAList.Select(ilexpr => env.IsDynamic(ilexpr.Const.ToString()) ? ilexpr.Const.ToString() : env.GetValue(ilexpr.Const.ToString())).ToList() 
					};
					calls.Add(functionCallRef);
					source.Append("\t").Append(functionCallRef.ToString()).AppendLine();
				}
			}
		}
                                                          
		
		
		#endregion
		
		#region Methods for preprocessing
		
		//Simple expr contains dynamic variables?
		private static bool m_simpleExprContainsDynamicVariables(ILExpression expr, AbstractEnvironment env) {
			if (expr.Type == ILExpressionType.FunctionCall) {
				if (expr.Function.EmbeddedBody != null && !expr.Function.CanBeCalculatedWithoutRun)
					return true;
				foreach (var arg in expr.VAList) {
					if (env.IsDynamic(arg.Const.ToString()))
						return true;					
				}
				if (expr.Function.CanBeCalculatedWithoutRun == false) {
					return true;
				}
			} else if (expr.Type == ILExpressionType.VariableAccess) {
				if (env.IsDynamic(expr.Const.ToString()))
					return true;				
			} else {
				if (expr.LeftNode != null) {
					if (env.IsDynamic(expr.LeftNode.Const.ToString()))
						return true;
				}
				if (expr.RightNode != null) {
					if (env.IsDynamic(expr.RightNode.Const.ToString()))
						return true;
				}
			}
			return false;
		}
		
	
		
		internal static AbstractEnvironment _bindTimeAnalyze(ILFunction func, AbstractEnvironment initialEnvironment) {
			var env = initialEnvironment;
			int dynamicAdded = 0;
			
			do {
				dynamicAdded = 0;
				foreach (var instr in func.Body) {
					if (instr is ILExpression) {
						var expr = instr as ILExpression;
						if (expr.Type == ILExpressionType.Assign) {
							
							
							//Case 1 - assign to local variable
							if (expr.LeftNode.Type == ILExpressionType.VariableAccess) {
								if (m_simpleExprContainsDynamicVariables(expr.RightNode, env)) {
									//If reading from static array by dynamic index
									if (expr.RightNode.Type == ILExpressionType.ArrayAccess) {
										if (env.IsDynamic(expr.RightNode.LeftNode.Const.ToString()) == false) {
											env.SetValue(expr.RightNode.LeftNode.Const.ToString(), Dynamic.Value);
											dynamicAdded++;
										}
									}
									if (env.IsDynamic(expr.LeftNode.Const.ToString()) == false) {
										env.SetValue(expr.LeftNode.Const.ToString(), Dynamic.Value);
										dynamicAdded++;
									}
								}
							}
							//Case 2 - assign to array
							else if (expr.LeftNode.Type == ILExpressionType.ArrayAccess) {
								
								// A(s,d)[D] = S, D -> A:=D
								if (m_simpleExprContainsDynamicVariables(expr.LeftNode, env)) {
									if (env.IsDynamic(expr.LeftNode.LeftNode.Const.ToString()) == false) {
										env.SetValue(expr.LeftNode.LeftNode.Const.ToString(), Dynamic.Value);
										dynamicAdded++;
									}									
								}
								// A(s)[S] = D -> A:=D
								else if (m_simpleExprContainsDynamicVariables(expr.RightNode, env)) {
									if (env.IsDynamic(expr.LeftNode.LeftNode.Const.ToString()) == false) {
										env.SetValue(expr.LeftNode.LeftNode.Const.ToString(), Dynamic.Value);
										dynamicAdded++;
									}									
								}
								// A(s)[S] = S - do nothing, array is static (in this scope
								
							}
						
						}
						
					}
				}
			} while (dynamicAdded != 0);
			                                  
			
			
			return env;
		}
			
		
		#endregion
		
		
	}
}

