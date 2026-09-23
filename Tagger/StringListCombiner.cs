using System.Text;

namespace Tagger;

public class StringListCombiner(string separator, string finalSeparator, string empty)
{
    public StringListCombiner(string separator, string finalSeparator) : this(separator, finalSeparator, "")
        {}
    public StringListCombiner(string separator) : this(separator, separator, "")
        {}

    public string CombineFromEnumerable(IEnumerable<string> input)
    {
        return Combine([..input]);
    }

    public string Combine(List<string> strings)
    {
        if (strings.Count == 0) return empty;
        var builder = new StringBuilder();

        for (var index = 0; index < strings.Count; ++index)
        {
            var value = strings[index];
            builder.Append(value);

            if (strings.Count != index + 1) // if this item isn't the last one in the list
            {
                var s = separator;
                if (strings.Count == index + 2)
                {
                    s = finalSeparator;
                }
                builder.Append(s);
            }
        }
        return builder.ToString();
    }
}