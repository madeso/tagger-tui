using System;
using System.Collections.Generic;
using System.Text;

namespace Util
{
	public static class Strings
	{
		public static IEnumerable<string> RemoveEmpty(IEnumerable<string> p)
		{
			foreach (string s in p)
			{
				if (String.IsNullOrEmpty(s) == false) yield return s;
			}
		}

		public static string ExpandEnviroment(string var, Func<string, string> s)
		{
			if (var == null) throw new NullReferenceException("unable to expand enviroment variable when var is null");
			string temp = var;
			char sep = '%';
			int start = temp.IndexOf(sep);

			while(start != -1 && start < temp.Length)
			{
				int end = temp.IndexOf(sep, start+1);
				if (start == end - 1) start = end + 1;
				else
				{
					string send = temp.Substring(end+1);
					string sstart = temp.Substring(0, start);
					string name = temp.Substring(start+1, end - start-1).ToUpper().Trim();
					string val = s(name);
					temp = sstart + val + send;
					start = temp.IndexOf(sep, start);
				}
			}

			return temp;
		}

		public static IEnumerable<string> Sort(IEnumerable<string> strings)
		{
			List<string> str = new List<string>(strings);
			str.Sort();
			return str;
		}

		public static IEnumerable<string> Unique(IEnumerable<string> strings)
		{
			SortedList<string, string> list = new SortedList<string, string>();
			foreach(string s in strings)
			{
				if (list.ContainsKey(s) == false)
				{
					list.Add(s, s);
				}
			}
			foreach (KeyValuePair<string, string> s in list)
			{
				yield return s.Key;
			}
		}

		public static string FirstChars(string s, int count, string extra)
		{
			int l = s.Length;
			if (s.Length + extra.Length > count) return s.Substring(0, count - extra.Length) + extra;
			else return s;
		}

		public static string FirstChars(string s, int count)
		{
			return FirstChars(s, count, "...");
		}

		public static IEnumerable<string> Enumerate(params string[] args)
		{
			return CSharp.Enumerate<string>(args);
		}

		public static string RemoveFromEndIfFound(this string str, string extra)
		{
			if (str.EndsWith(extra)) return str.Substring(0, str.Length - extra.Length);
			else return str;
		}

		public static string RemoveFromStartIfFound(this string str, string extra)
		{
			if (str.StartsWith(extra)) return str.Substring(extra.Length);
			else return str;
		}

		public static string RemoveAll(this string str, params string[] torem)
		{
			string t = str;
			foreach (var s in torem)
			{
				t = t.Replace(s, "");
			}
			return t;
		}

		public static string RemoveUnderscores(this string str)
		{
			return str.Replace('_', ' ');
		}

		public static string Capitalize(this string p)
		{
			return Capitalize(p, true);
		}

		public static string Capitalize(this string p, bool alsoFirstChar)
		{
			bool cap = alsoFirstChar;
			StringBuilder sb = new StringBuilder();
			foreach (char h in p.ToLower())
			{
				char c = h;
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

		public static string RemoveLeadingZeros(this string s)
		{
			return s.Trim().TrimStart('0');
		}

		public static int CountCharacters(this string s, Func<char, bool> count)
		{
			int n = 0;
			foreach (char c in s)
			{
				if (count(c)) ++n;
			}
			return n;
		}

		// http://www.stereopsis.com/strcmp4humans.html
		public static int StringCompare(string sa, string sb)
		{
			if (sa == sb) return 0;

			CharPointer a = new CharPointer{ String=sa};
			CharPointer b = new CharPointer{String=sb};

			while (a.hasMore() && b.hasMore()) {

				int a0, b0;	// will contain either a number or a letter

				if (isnum(a.Char)) {
					a0 = parsenum(ref a) + 256;
				} else {
					a0 = char.ToLower(a.Char);
				}
				if (isnum(b.Char)) {
					b0 = parsenum(ref b) + 256;
				} else {
					b0 = char.ToLower(b.Char);
				}

				if (a0 < b0) return -1;
				if (a0 > b0) return 1;

				a.next();
				b.next();
			}

			if (a.hasMore()) return 1;
			if (b.hasMore()) return -1;

			return 0;
		}

		#region Implementation deatils
		private class CharPointer
		{
			public string String = string.Empty;
			public int Index = 0;

			public char Char
			{
				get
				{
					return String[Index];
				}
			}

			public void next()
			{
				++Index;
			}

			internal void prev()
			{
				--Index;
			}

			internal bool hasMore()
			{
				return Index < String.Length;
			}
		}

		private static bool isnum(char b)
		{
			if (b >= '0' && b <= '9') return true;
			return false;
		}

		private static int parsenum(ref CharPointer a)
		{
			int result = a.Char - '0';
			a.next();

			while (a.hasMore() && isnum(a.Char))
			{
				result *= 10;
				result += a.Char - '0';
				a.next();
			}

			a.prev();
			return result;
		}
		#endregion
	}
}
