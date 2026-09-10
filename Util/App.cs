using System;
using System.Collections.Generic;
using System.Text;

namespace Util
{
	// todo: add parse function from assembly-information
	public static class App
	{
		struct AppData
		{
			public AppData(string company, string readableAppName, string appCode)
			{
				this.company = company;
				this.readableAppName = readableAppName;
				this.appcode = appCode;
			}
			public string company;
			public string readableAppName;
			public string appcode;
		}

		private static AppData? data = null;

		private static AppData Data
		{
			get
			{
				if (data.HasValue == false) throw new Exception("Please make sure that the developer call Setup() the very first thing they do!");
				return data.Value;
			}
		}

		public static void Setup(string company, string readableAppName, string appCode)
		{
			data = new AppData(company, readableAppName, appCode);
		}

		public static string ReadableAppName
		{
			get
			{
				return Data.readableAppName;
			}
		}

		public static string Company
		{
			get
			{
				return Data.company;
			}
		}

		public static string AppCode
		{
			get
			{
				return Data.appcode;
			}
		}
	}
}
