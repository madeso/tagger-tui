using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Util
{
	public static class Parse
	{
		// todo: do more teststing
		public static DateTime DateTime(string dateString, params string[] formats)
		{
			CultureInfo us = new CultureInfo("en-US");


			dateString = Strings.RemoveFromEndIfFound(dateString.Trim(), "PDT"); // Pacific Day Time

			try
			{
				return System.DateTime.Parse(dateString);
			}
			catch (FormatException) { }

			foreach (string f in formats)
			{
				try
				{
					return System.DateTime.ParseExact(dateString, f, us);
				}
				catch (FormatException)
				{
				}
			}

			throw new FormatException("Unable to parse " + dateString);
		}
	}
}
