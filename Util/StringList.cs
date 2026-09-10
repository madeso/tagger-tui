using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Util
{
	public class StringList : List<string>
	{
		public StringList()
		{
		}
		public StringList(IEnumerable<string> data)
			: base(data)
		{
		}
		public StringList(int cap)
			: base(cap)
		{
		}
		public StringList AddFormat(string format, params object[] arguments)
		{
			Add(string.Format(format, arguments));
			return this;
		}

		public StringList AddRange<Key, Value>(IEnumerable<KeyValuePair<Key, Value>> e, string format)
		{
			AddRange( CSharp.Convert<string, KeyValuePair<Key, Value>>(e, k => string.Format(format, k.Key, k.Value) ));
			return this;
		}
		public StringList AddRange<Value>(IEnumerable<Value> e, string format)
		{
			AddRange(CSharp.Convert<string, Value>(e, k => string.Format(format, k)));
			return this;
		}
		public StringList AddRange<Value>(IEnumerable<Value> e, Func<Value, string> c)
		{
			AddRange(CSharp.Convert<string, Value>(e, c));
			return this;
		}
		public StringList AddRange<Key, Value>(IEnumerable<KeyValuePair<Key, Value>> e, Func<KeyValuePair<Key, Value>, string> c)
		{
			AddRange(CSharp.Convert<string, KeyValuePair<Key, Value>>(e, c));
			return this;
		}

		public string Combine(StringListCombiner c)
		{
			return c.combine(this);
		}
	}
}
