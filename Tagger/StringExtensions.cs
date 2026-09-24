using System.Text;

namespace Tagger;

public static class StringExtensions
{
    public static string RemoveUnderscores(this string str)
    {
        return str.Replace('_', ' ');
    }

    public static string RemoveLeadingZeros(this string s)
    {
        return s.Trim().TrimStart('0');
    }

    public static string Capitalize(this string p, bool alsoFirstChar = true)
    {
        var cap = alsoFirstChar;
        var sb = new StringBuilder();
        foreach (var h in p.ToLower())
        {
            var c = h;
            if (char.IsLetter(c) && cap)
            {
                c = char.ToUpper(c);
                cap = false;
            }
            if (char.IsWhiteSpace(c)) cap = true;
            sb.Append(c);
        }
        return sb.ToString();
    }
}