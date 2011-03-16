using System;
using System.Collections.Generic;
using System.Text;

using L1Specializer.IL;

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
					if (sp.Env.GetHashCode() == hash)
						return sp;
				}
			}
			return null;
		}
	}
	
	internal class FunctionCallContainer {
		
		public FunctionCallContainer() {
			Parameters = new List<object>();
		}
		
		public int Id {
			get;
			set;
		}
		
		public ILFunction Function {
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
	
	}
	
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
				//TODO: Add array literals and render it
				return "ARRAY_CONST";
			}
			else
				return "<<BAD CONST>>";		
		}
		
		private static void m_renderConst(StringBuilder sb, object c) {
			sb.Append(RenderConst(c));
		}
		
		#endregion
		
		#region Methods
		
		internal static void Specialize(List<ILFunction> ilProgram) {
		
			foreach (var f in ilProgram) {
				if (f.Name == "Main") {
					var p = new Dictionary<string, object>();
					p.Add("m1", Dynamic.Value);
					p.Add("m2", Dynamic.Value);
					
					foreach (var kvp in f.LocalTypes) {
						Console.WriteLine (kvp.Key + " = " + kvp.Value);
					}
					
					
					SpecializeFunction(f, p);
				}
			}
		
		}
		
		
		
		private static void SpecializeFunction(ILFunction function, Dictionary<string, object> pars) {
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
			
			var initialSp = new SpecPoint { Env = initEnv, P = 1, L = LabelId++ } ;
			q.Enqueue(initialSp);
			initialSp.Env.Freeze();
			visited.AddSpecPoint(initialSp);

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
						
						stopped = SpecializeExpression(function, expr, env, source);
						
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
							}
							if (failSp == null) {
								failSp = new SpecPoint { Env = env, P = br.FailJump, L = LabelId++ };
								q.Enqueue(failSp);
							}
							
							source.Append("if "); source.Append(condVar); source.Append(" then"); source.Append(System.Environment.NewLine);
							source.Append("\tgoto "); source.Append("L_"); source.Append(succSp.L); source.Append(System.Environment.NewLine);
							source.Append("else"); source.Append(System.Environment.NewLine);
							source.Append("\tgoto "); source.Append("L_"); source.Append(failSp.L); source.Append(System.Environment.NewLine);
							source.Append("end; "); source.Append(System.Environment.NewLine);
							
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
							source.Append("return;"); source.Append(System.Environment.NewLine);
						}
						else {
							source.Append("return ");
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
			}
			
			System.Console.WriteLine(source.ToString());
		}
		
		private static bool SpecializeExpression(ILFunction f, ILExpression expr, AbstractEnvironment env, StringBuilder source) {
			
			if (expr.Type == ILExpressionType.Assign) {
				
				var left = expr.LeftNode;
				var right = expr.RightNode;
				if (left.Type == ILExpressionType.VariableAccess) {
					var varName = left.Const.ToString();
					try {
						var rightRed = right.AbstactReduce(varName, env, f.LocalTypes);
						if (env.IsDynamic(varName)) {
							
							source.Append(varName); source.Append(" := ");
							
							if (rightRed is string) {
								source.Append((string)rightRed);
							}
							else if (rightRed is ILExpression) {
								var fcall = rightRed as ILExpression;
								
								SpecializeFunctionCall(f, fcall, source, env);
								//source.Append("<<FUNCTION_CALL>>");
							}
							else {
								source.Append(RenderConst(rightRed));
							}
							source.Append(";"); source.Append(System.Environment.NewLine);
						}
						else {
							object val = rightRed;
							
							System.Diagnostics.Debug.Assert(val is string == false);
							System.Diagnostics.Debug.Assert(val is ILExpression == false);
							
							env.SetValue(varName, val);
						}
					}
					catch (NullReferenceException) {
						source.Append("__spec_raise_null_ref_exception();"); source.Append(System.Environment.NewLine);
					}
					catch (IndexOutOfRangeException) {
						source.Append("__spec_raise_index_out_of_range_exception();"); source.Append(System.Environment.NewLine);
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
							source.Append(arrayName); source.Append("["); source.Append(index); source.Append("] := ");
							source.Append(rightVar); source.Append(";"); source.Append(System.Environment.NewLine);
						}
						else {
							object[] a = (object[])env.GetValue(arrayName);
							a[Convert.ToInt32(index)] = rightVar;
						}
					}
					catch (NullReferenceException) {
						source.Append("__spec_raise_null_ref_exception();"); source.Append(System.Environment.NewLine);
					}
					catch (IndexOutOfRangeException) {
						source.Append("__spec_raise_index_out_of_range_exception();"); source.Append(System.Environment.NewLine);
					}
				}
				
			}
			else if (expr.Type == ILExpressionType.FunctionCall) {
				source.Append(expr.Const.ToString()); source.Append("(");
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
		
		private static void SpecializeFunctionCall(ILFunction context, ILExpression fcall, StringBuilder source, AbstractEnvironment env) {
		
			
			
		}
                                                          
		
		
		#endregion
		
		#region Methods for preprocessing
		
		internal static AbstractEnvironment getInitialAbstractEnvironment(ILFunction func, Dictionary<string, object> staticParameters) {
			
			
			
			return null;
		}
		
		//Simple expr contains dynamic variables?
		private static bool m_simpleExprContainsDynamicVariables(ILExpression expr, AbstractEnvironment env) {
			if (expr.Type == ILExpressionType.FunctionCall) {
				foreach (var arg in expr.VAList) {
					if (env.IsDynamic(arg.LeftNode.Const.ToString()))
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

