using System;
using System.Collections.Generic;
using System.Text;

namespace Util
{
	public class StringListCombiner
	{
		public StringListCombiner(string seperator, string finalSeperator, string empty)
		{
			this.seperator = seperator;
			this.finalSeperator = finalSeperator;
			this.empty = empty;
		}
		public StringListCombiner(string seperator, string finalSeperator)
		{
			this.seperator = seperator;
			this.finalSeperator = finalSeperator;
			this.empty = "";
		}
		public StringListCombiner(string seperator)
		{
			this.seperator = seperator;
			this.finalSeperator = seperator;
			this.empty = "";
		}

		public string combineFromEnumerable(IEnumerable<string> input)
		{
			return combine(new List<string>(input));
		}

		public string combine(List<string> strings)
		{
			if (strings.Count == 0) return empty;
			StringBuilder builder = new StringBuilder();
			for (int index = 0; index < strings.Count; ++index)
			{
				string value = strings[index];
				builder.Append(value);

				if (strings.Count != index + 1) // if this item isnt the last one in the list
				{
					string s = seperator;
					if (strings.Count == index + 2)
					{
						s = finalSeperator;
					}
					builder.Append(s);
				}
			}
			return builder.ToString();
		}

		readonly string seperator;
		readonly string finalSeperator;
		readonly string empty;
	}
}
