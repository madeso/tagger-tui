using System.Text;
using Util;

namespace Tagger;

public class KeyValueExtractor
{
    private KeyValueExtractor()
    {
    }

    public static (KeyValueExtractor, string?) Compile(string pattern)
    {
        var p = new KeyValueExtractor();

        const char k = '%';
        var special = false;
        var mem = new StringBuilder();

        foreach (var c in pattern)
        {
            if (c == k)
            {
                var t = mem.ToString();
                mem = new StringBuilder();
                if (special)
                {
                    if (string.IsNullOrEmpty(t))
                    {
                        mem.Append(k);
                    }
                    else
                    {
                        p.AddArgument(t);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(t))
                    {
                    }
                    else
                    {
                        p.AddText(t);
                    }
                }
                special = !special;
            }
            else
            {
                mem.Append(c);
            }
        }

        if (special)
        {
            return (new KeyValueExtractor(), $"Missing {k} at the end");
        }
        var st = mem.ToString();
        if (false == string.IsNullOrEmpty(st))
        {
            p.AddText(st);
        }


        return (p, null);
    }

    private int _numberOfDirectorySeparators = 0;

    private struct Match
    {
        public bool IsText;
        public string Data;

        public override string ToString() => IsText ? Data.Replace("%", "%%") : "%" + Data + "%";
    }

    public IEnumerable<string> Patterns => _matchers.Where(m => m.IsText == false).Select(m => m.Data);

    private readonly List<Match> _matchers = [];

    private void AddText(string t)
    {
        var m = new Match
        {
            IsText = true,
            Data = t
        };

        _matchers.Add(m);
        _numberOfDirectorySeparators += CountDirectorySeparators(t);
    }

    private static int CountDirectorySeparators(string pattern) => pattern.Count(c => c == Path.DirectorySeparatorChar);

    private void AddArgument(string t)
    {
        var m = new Match
        {
            IsText = false,
            Data = t
        };
        _matchers.Add(m);
    }

    private string GetText(FileInfo fi)
    {
        var s = new StringBuilder();
        s.Append(Path.GetFileNameWithoutExtension(fi.Name));

        var d = fi.Directory;
        for (var i = 0; i < _numberOfDirectorySeparators; ++i)
        {
            if (d == null) return s.ToString();
            s.Insert(0, d.Name + Path.DirectorySeparatorChar);
            d = d.Parent;
        }

        return s.ToString();
    }

    public Dictionary<string, string> Extract(FileInfo fi, out string message) => ExtractFromString(GetText(fi), out message);

    public override string ToString()
    {
        var s = new StringBuilder();
        foreach (var m in _matchers)
        {
            s.Append(m.ToString());
        }
        return s.ToString();
    }

    private Dictionary<string, string> ExtractFromString(string t, out string message)
    {
        var start = 0;
        var len = t.Length;

        var arg = string.Empty;

        var ret = new Dictionary<string, string>();

        foreach (var m in _matchers)
        {
            if (m.IsText)
            {
                var end = t.IndexOf(m.Data, start, StringComparison.Ordinal);
                if (end == -1)
                {

                    message = $"Unable to find {m.Data} in {t.Substring(start)}";
                    return ret;
                }
                if (false == string.IsNullOrEmpty(arg))
                {
                    var val = t.Substring(start, end - start);
                    if (!SetValue(ret, arg, val))
                    {
                        message = $"Unable to apply <{val}> to {arg}";
                        return ret;
                    }
                    arg = string.Empty;
                }
                start = end + m.Data.Length;
            }
            else
            {
                if (false == string.IsNullOrEmpty(arg))
                {
                    message = "bad format";
                    return ret;
                }
                arg = m.Data;
            }
        }

        if (false == string.IsNullOrEmpty(arg))
        {
            var val = t.Substring(start);
            if (!SetValue(ret, arg, val))
            {
                message = $"Unable to apply <{val}> to {arg}";
                return ret;
            }
        }

        message = string.Empty;
        return ret;
    }

    private static bool SetValue(Dictionary<string, string> r, string key, string newValue)
    {
        if (r.TryGetValue(key, out var oldValue))
        {
            var oldClean = oldValue.ToLower().RemoveUnderscores().Trim().RemoveLeadingZeros();
            var newClean = newValue.ToLower().RemoveUnderscores().Trim().RemoveLeadingZeros();
            if (oldClean != newClean) return false;
        }
        r[key] = newValue;
        return true;
    }

    public struct Complexity
    {
        public int Arguments;
        public int Verifiers;
    }

    public Complexity CalculateComplexity()
    {
        var counts = new Dictionary<string, int>();

        foreach (var m in _matchers)
        {
            if (m.IsText) continue;
            var arg = m.Data.ToLower();
            var c = 0;
            if (counts.TryGetValue(arg, out var count))
            {
                c = count;
            }
            ++c;
            counts[arg] = c;
        }
        var cx = new Complexity
        {
            Arguments = counts.Count(x => x.Value > 0),
            Verifiers = counts.Count(x => x.Value > 1)
        };
        return cx;
    }

    public int CountInText(Func<string, int> calculator) => _matchers.Where(m => m.IsText).Sum(m => calculator(m.Data));
}