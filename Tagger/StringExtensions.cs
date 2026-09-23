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
}