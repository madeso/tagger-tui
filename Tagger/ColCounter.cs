using Spectre.Console;

namespace Tagger;

public class ColCounter<T>
    where T : notnull
{
    private readonly Dictionary<T, int> _data = new();

    public int UniqueCount => _data.Count;

    public void Add(T key, int count)
    {
        if (_data.TryGetValue(key, out var value) == false)
        {
            _data.Add(key, count);
            return;
        }
        Set(key, value + count);
    }

    private void Set(T key, int value)
    {
        _data[key] = value;
    }

    public void AddOne(T key)
    {
        Add(key, 1);
    }

    public IEnumerable<(T, int)> MostCommon()
    {
        return _data
            .OrderByDescending(x => x.Value)
            .Select(x => (x.Key, x.Value))
            ;
    }

    public int TotalCount()
    {
        return _data.Select(x => x.Value).Sum();
    }

    public void Update(ColCounter<T> rhs)
    {
        foreach (var (key, count) in rhs._data)
        {
            Add(key, count);
        }
    }

    internal void Max(ColCounter<T> rhs)
    {
        foreach (var (key, rhsValue) in rhs._data)
        {
            Set(key,
                _data.TryGetValue(key, out var selfValue)
                    ? Math.Max(selfValue, rhsValue)
                    : rhsValue
                );
        }
    }

    public IEnumerable<T> Keys => _data.Keys;

    public IEnumerable<KeyValuePair<T, int>> Items => _data;
}


public static class ColCounterExtensions
{
    public static void PrintMostCommon(this ColCounter<string> counter, int mostCommonCount)
    {
        foreach (var (file, count) in counter.MostCommon().Take(mostCommonCount))
        {
            AnsiConsole.WriteLine($"{file}: {count}");
        }
    }

    public static ColCounter<T> ToColCounter<T>(this IEnumerable<T> its)
        where T : notnull
    {
        var ret = new ColCounter<T>();
        foreach (var it in its)
        {
            ret.AddOne(it);
        }
        return ret;
    }
}