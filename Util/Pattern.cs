using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//format:
// %arg% replaces arg with its value, error if missing
// [arg] replaces arg with its value, "" if missing
// $func(arg,arg,arg) calls func

namespace Util
{
	public class Pattern
	{

		public delegate string Func(List<string> args);

		// ---------------------------------------------------------------------------

		public class MissingFunction : Exception
		{
			public MissingFunction(string name)
				: base(string.Format("Missing function {0}", name))
			{
			}
		}

		public class MissingAttribute : Exception
		{
			public MissingAttribute(string name)
				: base(string.Format("Missing attribute {0}", name))
			{
			}
		}

		public class SyntaxError : Exception
		{
			public SyntaxError(string name)
				: base(string.Format("Syntax error: {0}", name))
			{
			}
		}

		public class InvalidState : Exception
		{
			public InvalidState()
				: base("invalid state")
			{
			}
		}

		// --------------------------------------------------------------------------------------------

		abstract class _Node
		{
			public _Node()
			{
			}
			public abstract string eval(Dictionary<string, Func> funcs, Dictionary<string, string> data);
		}

		class _Text : _Node
		{
			private string text;
			public _Text(string text)
			{
				this.text = text;
			}

			public override string eval(Dictionary<string, Func> funcs, Dictionary<string, string> data)
			{
				return this.text;
			}

			public override string ToString()
			{
				return text;
			}
		}

		class _Attribute : _Node
		{
			private string name = "";
			public _Attribute(string name)
			{
				this.name = name;
			}

			public override string eval(Dictionary<string, Func> funcs, Dictionary<string, string> data)
			{
				if (data.ContainsKey(this.name))
					return data[this.name];
				else
					return "";
			}

			public override string ToString()
			{
				return "%" + name + "%";
			}
		}

		class _FunctionCall : _Node
		{
			private string name;
			List<_Node> args;

			public _FunctionCall(string name, string[] args)
			{
				this.name = name;
				this.args = new List<_Node>();
				foreach (var a in args)
				{
					this.args.Add(_CompileList(a));
				}
			}

			public override string eval(Dictionary<string, Func> funcs, Dictionary<string, string> data)
			{
				if (funcs.ContainsKey(this.name))
				{
					List<string> args = new List<string>();
					foreach (var a in this.args)
					{
						args.Add(a.eval(funcs, data));
					}
					return funcs[this.name](args);
				}
				else throw new MissingFunction(this.name);
			}

			public override string ToString()
			{
				return string.Format("${0}({1})", name, new StringListCombiner(",").combineFromArray(args.ToArray()));
			}
		}

		class _List : _Node
		{
			List<_Node> nodes;
			public _List(List<_Node> nodes)
			{
				this.nodes = nodes;

			}

			public override string eval(Dictionary<string, Func> funcs, Dictionary<string, string> data)
			{
				string r = "";
				foreach (var n in this.nodes)
				{
					r += n.eval(funcs, data);
				}
				return r;
			}
		}

		static List<string> _ParseArguments(ref int start, string pattern)
		{
			// return new index, and a list of string arguments that need to be parsed
			List<string> args = new List<string>();
			int state = 0;
			string mem = "";
			for (int i = start; i < pattern.Length; ++i)
			{
				char c = pattern[i];
				switch (c)
				{
					case _Syntax.BEGINSIGN:
						mem += c;
						state += 1;
						break;
					case _Syntax.ENDSIGN:
						if (state == 0)
						{
							if (mem != "")
							{
								args.Add(mem);
							}
							start = i;
							return args;
						}
						else
						{
							mem += c;
						}
						state -= 1;
						break;
					case _Syntax.SEPSIGN:
						if (state == 0)
						{
							args.Add(mem);
							mem = "";
						}
						else
						{
							mem += c;
						}
						break;
					default:
						mem += c;
						break;
				}
			}
			throw new SyntaxError("should have detected an end before eos");
		}

		enum State
		{
			TEXT, VAR, FUNC
		}

		class _Syntax
		{
			public const char VARSIGN = '%';
			public const char FUNCSIGN = '$';
			public const char BEGINSIGN = '(';
			public const char ENDSIGN = ')';
			public const char SEPSIGN = ',';
		}

		class _Parser
		{
			private State state;
			private string mem = "";
			private List<_Node> nodes = new List<_Node>();
			public _Parser()
			{
			}

			public List<_Node> doit(string pattern)
			{
				this.state = State.TEXT;
				int i = 0;
				while (i < pattern.Length)
				{
					char c = pattern[i];
					i += 1;
					switch (this.state)
					{
						case State.TEXT:
							if (c == _Syntax.VARSIGN)
							{
								this.add();
								this.state = State.VAR;
							}
							else if (c == _Syntax.FUNCSIGN)
							{
								this.add();
								this.state = State.FUNC;
							}
							else
							{
								this.mem += c;
							}
							break;
						case State.VAR:
							if (c == _Syntax.VARSIGN)
							{
								this.add();
								this.state = State.TEXT;
							}
							else
							{
								this.mem += c;
							}
							break;
						case State.FUNC:
							if (this.mem == "")
							{
								if (char.IsLetter(c))
									this.mem += c;
								else
									throw new SyntaxError("function name is empty");
							}
							else
							{
								if (char.IsLetterOrDigit(c))
								{
									this.mem += c;
								}
								else if (c == _Syntax.BEGINSIGN)
								{
									var args = _ParseArguments(ref i, pattern);
									i += 1;
									this.add(args);
									this.state = State.TEXT;
								}
								else
									throw new SyntaxError("function calls must end with () and, mus begin with a letter and can only continue with alphanumerics");
							}
							break;
						default:
							throw new InvalidState();
							break;
					}
				}
				if (this.mem != "") this.add();
				return this.nodes;
			}

			void add()
			{
				add(null);
			}

			void add(List<string> args)
			{
				if (this.state == State.TEXT)
				{
					if (this.mem != "")
						this.nodes.Add(new _Text(this.mem));
				}
				else if (this.state == State.VAR)
				{
					if (this.mem != "")
						this.nodes.Add(new _Attribute(this.mem));
					else
						this.nodes.Add(new _Text(_Syntax.VARSIGN.ToString()));
				}
				else if (this.state == State.FUNC)
				{
					if (args == null)
						throw new SyntaxError("weird func call");
					this.nodes.Add(new _FunctionCall(this.mem, args.ToArray()));
				}
				else
					throw new InvalidState();
				this.mem = "";
			}
		}

		static _Node _CompileList(string patt)
		{
			return new _List(new _Parser().doit(patt));
		}

		_Node list;
		private Pattern(string patt)
		{
			this.list = _CompileList(patt);
		}
		public string eval(Dictionary<string, Func> funcs, Dictionary<string, string> data)
		{
			return this.list.eval(funcs, data);
		}

		public static Pattern Compile(string pattern)
		{
			return new Pattern(pattern);
		}

		private static string _opt(List<string> args, int i)
		{
			return _opt(args, i, "");
		}
		private static string _opt(List<string> args, int i, string d)
		{
			if (args.Count > i)
			{
				return args[i];
			}
			else
			{
				return d;
			}
		}
		
		public static Dictionary<string, Func> DefaultFunctions()
		{
			var t = new Dictionary<string, Func>();
			//t.Add("title", args => args[0].title());
			t.Add("capitalize", args => args[0].Capitalize());
			t.Add("lower", args => args[0].ToLower());
			t.Add("upper", args => args[0].ToUpper());
			t.Add("rtrim", args => args[0].TrimEnd(_opt(args,1).ToCharArray()));
			t.Add("ltrim", args => args[0].TrimStart(_opt(args,1).ToCharArray()));
			t.Add("trim", args => args[0].Trim(_opt(args,1).ToCharArray()));
			t.Add("zfill", args => zfill(args[0], _opt(args,1, "3")));
			t.Add("replace", args => args[0].Replace(args[1], args[2]));
			t.Add("substr", args => args[0].Substring(int.Parse(args[1]), int.Parse(_opt(args, 2))));
			return t;
		}

		private static string zfill(string str, string scount)
		{
			int i = int.Parse(scount);
			return str.PadLeft(i, '0');
		}
		/*
if __name__ == "__main__":
		data = {"artist":"Zynic", "title":"dreams in black and white", "album":"Dreams In Black And White", "track":"1"}
		print Compile("%artist% - %title% (%album%)").eval(DefaultFunctions(), data)
		print Compile("%artist% - $title(%title%) (%album%)").eval(DefaultFunctions(), data)
		print Compile("$zfill(%track%,3). $title(%title%)").eval(DefaultFunctions(), data)
		#print _ParseArguments(0, "a,b(1, 3),c)")*/
	}
}