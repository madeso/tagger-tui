using System.Text;

namespace Tagger;

//format:
// %arg% replaces arg with its value, error if missing
// [arg] replaces arg with its value, "" if missing
// $func(arg,arg,arg) calls func

public class ErrorList
{
    private readonly List<string> _errors = [];

    private void AddError(string err)
    {
        _errors.Add(err);
    }

    public void AddMissingFunction(string name) => AddError($"Missing function {name}");
    public void AddMissingAttribute(string name) => AddError($"Missing attribute {name}");
    public void AddSyntaxError(string name) => AddError($"Syntax error: {name}");
    public void AddInvalidState() => AddError("invalid state");

    public bool  HasErrors() => _errors.Count > 0;

    public IEnumerable<string> Errors => _errors;
}

public class Pattern
{
    public (string, ErrorList) Eval(Dictionary<string, Func> functions, Dictionary<string, string> data)
    {
        var errors = new ErrorList();
        var ret = this._list.Eval(functions, data, errors);
        return (ret, errors);
    }

    public static (Pattern, ErrorList) Compile(string pattern)
    {
        var errors = new ErrorList();
        var ret = new Pattern(pattern, errors);
        return (ret, errors);
    }

    public static Dictionary<string, Func> DefaultFunctions()
    {
        var t = new Dictionary<string, Func>
        {
            //t.Add("title", args => args[0].title());
            { "capitalize", args => args[0].Capitalize() },
            { "lower", args => args[0].ToLower() },
            { "upper", args => args[0].ToUpper() },
            { "rtrim", args => args[0].TrimEnd(_opt(args, 1).ToCharArray()) },
            { "ltrim", args => args[0].TrimStart(_opt(args, 1).ToCharArray()) },
            { "trim", args => args[0].Trim(_opt(args, 1).ToCharArray()) },
            { "zfill", args => zfill(args[0], _opt(args, 1, "3")) },
            { "replace", args => args[0].Replace(args[1], args[2]) },
            { "substr", args => args[0].Substring(int.Parse(args[1]), int.Parse(_opt(args, 2))) }
        };
        return t;
    }

    public delegate string Func(List<string> args);

    // --------------------------------------------------------------------------------------------

    private abstract class Node
    {
        protected Node()
        {
        }
        public abstract string Eval(Dictionary<string, Func> functions, Dictionary<string, string> data, ErrorList errors);
    }

    private class Text(string text) : Node
    {
        public override string Eval(Dictionary<string, Func> functions, Dictionary<string, string> data, ErrorList errors) => text;
        public override string ToString() => text;
    }

    private class Attribute(string name) : Node
    {
        public override string Eval(Dictionary<string, Func> functions, Dictionary<string, string> data, ErrorList errors) => data.GetValueOrDefault(name, "");
        public override string ToString() => "%" + name + "%";
    }

    private class FunctionCall : Node
    {
        private readonly string _name;
        private readonly List<Node> _args;

        public FunctionCall(string name, string[] args, ErrorList errors)
        {
            this._name = name;
            this._args = [];
            foreach (var a in args)
            {
                this._args.Add(_CompileList(a, errors));
            }
        }

        public override string Eval(Dictionary<string, Func> functions, Dictionary<string, string> data, ErrorList errors)
        {
            if (functions.ContainsKey(this._name))
            {
                var args = this._args.Select(a => a.Eval(functions, data, errors)).ToList();
                return functions[this._name](args);
            }
            errors.AddMissingFunction(this._name);
            return "";
        }

        public override string ToString() => $"${_name}({new StringListCombiner(",").CombineFromEnumerable(_args.Select(x => x.ToString() ?? ""))})";
    }

    private class List(List<Node> nodes) : Node
    {
        public override string Eval(Dictionary<string, Func> functions, Dictionary<string, string> data, ErrorList errors)
            => nodes.Aggregate("", (current, n) => current + n.Eval(functions, data, errors));
    }

    private static List<string> _ParseArguments(ref int start, string pattern, ErrorList errors)
    {
        // return new index, and a list of string arguments that need to be parsed
        List<string> args = [];
        var state = 0;
        var mem = "";
        for (var i = start; i < pattern.Length; ++i)
        {
            var c = pattern[i];
            switch (c)
            {
                case Syntax.BeginSign:
                    mem += c;
                    state += 1;
                    break;
                case Syntax.EndSign:
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
                case Syntax.SepSign:
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
        
        errors.AddSyntaxError("should have detected an end before eos");
        return args;
    }

    private enum State
    {
        Text, Var, Func
    }

    private static class Syntax
    {
        public const char VarSign = '%';
        public const char FuncSign = '$';
        public const char BeginSign = '(';
        public const char EndSign = ')';
        public const char SepSign = ',';
    }

    private class Parser
    {
        private State _state;
        private string _mem = "";
        private readonly List<Node> _nodes = [];

        public List<Node> ParseToNodes(string pattern, ErrorList errors)
        {
            this._state = State.Text;
            int i = 0;
            while (i < pattern.Length)
            {
                char c = pattern[i];
                i += 1;
                switch (this._state)
                {
                    case State.Text:
                        if (c == Syntax.VarSign)
                        {
                            this.Add(errors);
                            this._state = State.Var;
                        }
                        else if (c == Syntax.FuncSign)
                        {
                            this.Add(errors);
                            this._state = State.Func;
                        }
                        else
                        {
                            this._mem += c;
                        }
                        break;
                    case State.Var:
                        if (c == Syntax.VarSign)
                        {
                            this.Add(errors);
                            this._state = State.Text;
                        }
                        else
                        {
                            this._mem += c;
                        }
                        break;
                    case State.Func:
                        if (this._mem == "")
                        {
                            if (char.IsLetter(c))
                                this._mem += c;
                            else
                            {
                                errors.AddSyntaxError("function name is empty");
                            }
                        }
                        else
                        {
                            if (char.IsLetterOrDigit(c))
                            {
                                this._mem += c;
                            }
                            else if (c == Syntax.BeginSign)
                            {
                                var args = _ParseArguments(ref i, pattern, errors);
                                i += 1;
                                this.AddList(args, errors);
                                this._state = State.Text;
                            }
                            else
                            {
                                errors.AddSyntaxError(
                                    "function calls must end with () and, must begin with a letter and can only continue with alphanumerics");
                                return this._nodes;
                            }
                        }
                        break;
                    default:
                        errors.AddInvalidState();
                        return this._nodes;
                }
            }
            if (this._mem != "") this.Add(errors);
            return this._nodes;
        }

        private void Add(ErrorList errors)
        {
            AddList(null, errors);
        }

        private void AddList(List<string>? args, ErrorList errors)
        {
            if (this._state == State.Text)
            {
                if (this._mem != "")
                    this._nodes.Add(new Text(this._mem));
            }
            else if (this._state == State.Var)
            {
                if (this._mem != "")
                    this._nodes.Add(new Attribute(this._mem));
                else
                    this._nodes.Add(new Text(Syntax.VarSign.ToString()));
            }
            else if (this._state == State.Func)
            {
                if (args == null)
                {
                    errors.AddSyntaxError("weird func call");
                    return;
                }

                this._nodes.Add(new FunctionCall(this._mem, args.ToArray(), errors));
            }
            else
            {
                errors.AddInvalidState();
            }

            this._mem = "";
        }
    }

    private static Node _CompileList(string pattern, ErrorList errors)
    {
        return new List(new Parser().ParseToNodes(pattern, errors));
    }

    private readonly Node _list;
    private Pattern(string pattern, ErrorList errors)
    {
        this._list = _CompileList(pattern, errors);
    }
    
    private static string _opt(List<string> args, int i, string d = "")
    {
        return args.Count > i ? args[i] : d;
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