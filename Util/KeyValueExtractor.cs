using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Util
{
	public class KeyValueExtractor
	{
		private KeyValueExtractor()
		{
		}

		public static KeyValueExtractor Compile(string pattern)
		{
			KeyValueExtractor p = new KeyValueExtractor();

			const char k = '%';
			bool special = false;
			StringBuilder mem = new StringBuilder();

			foreach (char c in pattern)
			{
				if (c == k)
				{
					string t = mem.ToString();
					mem = new StringBuilder();
					if (special)
					{
						if (string.IsNullOrEmpty(t))
						{
							mem.Append(k);
						}
						else
						{
							p.addArgument(t);
						}
					}
					else
					{
						if (string.IsNullOrEmpty(t))
						{
						}
						else
						{
							p.addText(t);
						}
					}
					special = !special;
				}
				else
				{
					mem.Append(c);
				}
			}

			if (special) throw new Exception("Format error");
			string st = mem.ToString();
			if (false == string.IsNullOrEmpty(st))
			{
				p.addText(st);
			}


			return p;
		}

		int numberOfDirectorySeperators = 0;

		private struct Match
		{
			public bool isText;
			public string data;

			public override string ToString()
			{
				if (isText)
				{
					return data.Replace("%", "%%");
				}
				else return "%" + data + "%";
			}
		}

		List<Match> matchers = new List<Match>();

		public KeyValueExtractor addText(string t)
		{
			Match m = new Match();
			m.isText = true;
			m.data = t;
			matchers.Add(m);

			numberOfDirectorySeperators += CountDirectorySeperators(t);

			return this;
		}

		private static int CountDirectorySeperators(string pattern)
		{
			int count = 0;
			foreach (char c in pattern)
			{
				if (c == Path.DirectorySeparatorChar)
					++count;
			}
			return count;
		}

		public KeyValueExtractor addArgument(string t)
		{
			Match m = new Match();
			m.isText = false;
			m.data = t;
			matchers.Add(m);
			return this;
		}

		private string getText(FileInfo fi)
		{
			StringBuilder s = new StringBuilder();
			s.Append(Path.GetFileNameWithoutExtension(fi.Name));

			DirectoryInfo d = fi.Directory;
			for (int i = 0; i < numberOfDirectorySeperators; ++i)
			{
				s.Insert(0, d.Name + Path.DirectorySeparatorChar);
				d = d.Parent;
			}

			return s.ToString();
		}

		public Dictionary<string, string> extract(FileInfo fi, out string message)
		{
			string t = getText(fi);
			return extract(t, out message);
		}

		public override string ToString()
		{
			StringBuilder s = new StringBuilder();
			foreach (Match m in matchers)
			{
				s.Append(m.ToString());
			}
			return s.ToString();
		}

		private Dictionary<string, string> extract(string t, out string message)
		{
			int start = 0;
			int len = t.Length;

			string arg = string.Empty;

			Dictionary<string, string> r = new Dictionary<string, string>();

			int index = 0;
			foreach (Match m in matchers)
			{
				if (m.isText)
				{
					int end = t.IndexOf(m.data, start);
					if (end == -1)
					{

						message = string.Format("Unable to find {0} in {1}", m.data, t.Substring(start));
						return r;
					}
					if (false == string.IsNullOrEmpty(arg))
					{
						string val = t.Substring(start, end - start);
						if (!apply(ref r, arg, val))
						{
							message = string.Format("Unable to apply <{0}> to {1}", val, arg);
							return r;
						}
						arg = string.Empty;
					}
					start = end + m.data.Length;
				}
				else
				{
					if (false == string.IsNullOrEmpty(arg)) throw new Exception("bad format");
					arg = m.data;
				}
				++index;
			}

			if (false == string.IsNullOrEmpty(arg))
			{
				string val = t.Substring(start);
				if (!apply(ref r, arg, val))
				{
					message = string.Format("Unable to apply <{0}> to {1}", val, arg);
					return r;
				}
			}

			message = string.Empty;
			return r;
		}

		private bool apply(ref Dictionary<string, string> r, string arg, string p)
		{
			if (r.ContainsKey(arg))
			{
				string a = r[arg].ToLower().RemoveUnderscores().Trim().RemoveLeadingZeros();
				string b = p.ToLower().RemoveUnderscores().Trim().RemoveLeadingZeros();
				if (a != b) return false;
			}
			r[arg] = p;
			return true;
		}

		public struct Complexity
		{
			public int Arguments;
			public int Verifiers;
		}

		public Complexity CalculateComplexity()
		{
			Dictionary<string, int> counts = new Dictionary<string, int>();
			foreach (var m in matchers)
			{
				if (m.isText == false)
				{
					string arg = m.data.ToLower();
					int c = 0;
					if (counts.ContainsKey(arg)) c = counts[arg];
					++c;
					counts[arg] = c;
				}
			}
			Complexity cx = new Complexity();
			cx.Arguments = counts.Count(x => x.Value > 0);
			cx.Verifiers = counts.Count(x => x.Value > 1);
			return cx;
		}

		public int countInText(Func<string, int> calculator)
		{
			int c = 0;
			foreach (var m in matchers)
			{
				if (m.isText == true)
				{
					c += calculator(m.data);
				}
			}
			return c;
		}
	}
}
