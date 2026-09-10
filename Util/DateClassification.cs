using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Util
{
	public enum DateClassification
	{
		Older,
		LastMonth,
		ThisMonth,
		LastWeek,
		ThisWeek,
		Yesterday,
		Today
	}

	public static class DateClassFunc
	{
		public static DateClassification Classify(DateTime i)
		{
			DateTime n = DateTime.Now;
			if (IsSameDay(n, i)) return DateClassification.Today;
			else if (IsSameDay(n.Subtract(new TimeSpan(1, 0, 0, 0)), i)) return DateClassification.Yesterday;
			else if (IsSameWeek(n, i)) return DateClassification.ThisWeek;
			else if (IsSameWeek(n.Subtract(new TimeSpan(7, 0, 0, 0)), i)) return DateClassification.LastWeek;
			else if (IsSameMonth(n, i)) return DateClassification.ThisMonth;
			else if (IsSameMonth(n, i, -1)) return DateClassification.LastMonth;
			else return DateClassification.Older;
		}

		private static bool IsSameDay(DateTime n, DateTime i)
		{
			return n.DayOfYear == i.DayOfYear && n.Year == i.Year;
		}
		private static bool IsSameWeek(DateTime n, DateTime i)
		{
			return WeekNumber(n) == WeekNumber(i) && n.Year == i.Year;
		}
		private static bool IsSameMonth(DateTime n, DateTime i, int change)
		{
			return n.Month + change == i.Month && n.Year == i.Year;
		}
		private static bool IsSameMonth(DateTime n, DateTime i)
		{
			return IsSameMonth(n, i, 0);
		}

		private static int WeekNumber(DateTime d)
		{
			DateTime n = d;
			int monday = n.DayOfYear - WeekDayNum(n) + WeekDayNum(new DateTime(n.Year, 1, 1)) - 1;
			if (monday % 7 != 0) throw new Exception("bad code");
			int weeknum = monday / 7;
			return weeknum + 1;
		}

		private static int WeekDayNum(DateTime n)
		{
			if (n.DayOfWeek == DayOfWeek.Sunday) return 6;
			return ((int)n.DayOfWeek - 1);
		}
	}
}
