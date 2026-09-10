using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Util
{
	public static class Reflect
	{
		// resulting type...? typeof 
		public static IEnumerable<KeyValuePair<string, Member>> PublicStaticValuesOf<Container, Member>()
		{
			Type c = typeof(Container);
			Type m = typeof(Member);
			System.Reflection.FieldInfo[] infos = c.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			foreach (System.Reflection.FieldInfo mi in infos)
			{
				if (mi.FieldType == m)
				{
					yield return new KeyValuePair<string, Member>(mi.Name, (Member)mi.GetValue(null));
				}
			}
			System.Reflection.PropertyInfo[] pinfos = c.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			foreach (System.Reflection.PropertyInfo pi in pinfos)
			{
				if (pi.DeclaringType == m)
				{
					yield return new KeyValuePair<string, Member>(pi.Name, (Member)pi.GetValue(null, null));
				}
			}
		}

		public static IEnumerable<KeyValuePair<string, Container>> PublicStaticValuesOf<Container>()
		{
			return PublicStaticValuesOf<Container, Container>();
		}

		public static IEnumerable<Member> MembersOf<Member, Container>(Container c)
			where Member : class
			where Container : class
		{
			Type tc = typeof(Container);
			Type tm = typeof(Member);
			foreach (FieldInfo fi in tc.GetFields())
			{
				object m = fi.GetValue(c);
				Member mc = m as Member;
				if (m != null)
				{
					yield return mc;
				}
			}
		}

		public static T GetEnumValue<T>(string p)
		{
			foreach (KeyValuePair<string, T> mi in PublicStaticValuesOf<T>())
			{
				if (p.ToLower().Trim().CompareTo(mi.Key.ToLower().Trim()) == 0)
				{
					return mi.Value;
				}
			}
			string data = new Util.StringListCombiner(", ", " and ").combineFromEnumerable(CSharp.Keys<string, T>(PublicStaticValuesOf<T>()));
			throw new Exception(p + " is not a valid " + typeof(T).Name + ", valid values are " + data);
		}
	}
}
