using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Util
{
	public class ConsoleArguments
	{
		public ConsoleArguments()
		{
		}

		public ConsoleArguments(string[] args)
		{
			parse(args);
		}
		
		public void parse(string[] args)
		{
			foreach (string s in args)
			{
				string a = s.Trim();
				if (a.Length == 0) continue;
				if (a[0] == '-')
				{
					string[] var = a.Split("=".ToCharArray(), 2);
					string name = var[0].Trim().Substring(1);
					string value = var.Length == 2 ? var[1].Trim() : "";
					Add(name, value);
				}
				else if (a[0] == '/')
				{
					string[] var = a.Split("=".ToCharArray(), 2);
					string name = var[0].Trim().Substring(1);
					string value = var.Length == 2 ? var[1].Trim() : "";
					Handle(name, value);
				}
				else
				{
					Add(a);
				}
			}
		}

		private void Handle(string name, string value)
		{
			if (name == "opt")
			{
				foreach (XmlElement el in Xml.ElementsNamed(Xml.Open( Xml.FromFile(value), "options"), "option"))
				{
					Add(Xml.GetAttributeString(el, "name"), Xml.GetAttributeString(el, "value"));
				}
			}
			else throw new Exception(name + " is not a valid argument action");
		}

		public ConsoleArguments Add(string name, string value)
		{
			if (list.ContainsKey(name))
			{
				list[name].Add(value);
			}
			else
			{
				arguments.Add(name, value);
			}
			return this;
		}
		public ConsoleArguments Add(string name)
		{
			values.Add(name);
			return this;
		}

		public IEnumerable<string> Values
		{
			get
			{
				foreach (string s in values)
				{
					yield return s;
				}
			}
		}

		public IEnumerable<string> valuesFor(string name)
		{
			foreach (string s in list[name])
			{
				yield return s;
			}
		}

		public string this[string name]
		{
			get
			{
				return arguments[name];
			}
		}
		public bool has(string name)
		{
			return arguments.ContainsKey(name);
		}
		public bool hasAny(params string[] names)
		{
			foreach(string name in names)
			{
				if (has(name)) return true;
			}
			return false;
		}

		public string this[string name, string def]
		{
			get
			{
				if (arguments.ContainsKey(name)) return this[name];
				else return def;
			}
		}

		public IEnumerable<KeyValuePair<string, string>> Args
		{
			get
			{
				foreach (KeyValuePair<string, string> kvp in arguments)
				{
					yield return kvp;
				}
			}
		}

		public void setList(string name)
		{
			list.Add(name, new List<string>());
		}

		private Dictionary<string, string> arguments = new Dictionary<string, string>();
		private Dictionary<string, List<string>> list = new Dictionary<string, List<string>>();
		private List<string> values = new List<string>();
	}
}
