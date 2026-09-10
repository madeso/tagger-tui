using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Util
{
	public static class Disk
	{
		private const string kEntries = "entries";
		private const string kSingleEntry = "entry";
		private const string kKeyName = "key";

		public static Dictionary<string, string> LoadStringStringDictionary(string file)
		{
			Dictionary<string, string> r = new Dictionary<string,string>();
			var root = Xml.Open(Xml.FromFile(file), kEntries);
			foreach (var var in root.ElementsNamed(kSingleEntry))
			{
				r.Add(var.GetAttributeString(kKeyName), var.GetFirstText());
			}
			return r;
		}

		public static Dictionary<string, string> LoadStringStringDictionaryOrNull(string file)
		{
			if (File.Exists(file) == false) return new Dictionary<string, string>();
			return LoadStringStringDictionary(file);
		}

		public static void WriteStringStringDictionary(Dictionary<string, string> data, string file)
		{
			ElementBuilder builder = new ElementBuilder().child(kEntries);
			foreach (var k in data)
			{
				builder.child(kSingleEntry).attribute(kKeyName, k.Key).text(k.Value);
			}
			builder.Document.Save(file);
		}
	}
}
