using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Util
{
	public static class FileUtil
	{
		private static PathBuilder OsFilePath(Environment.SpecialFolder folder)
		{
			return new PathBuilder(folder);
		}
		private static PathBuilder RelativeApplicationPath(PathBuilder c)
		{
			return c.dir(App.Company).dir(App.AppCode);
		}
		private static PathBuilder PathFor(Environment.SpecialFolder folder)
		{
			return RelativeApplicationPath(OsFilePath(folder));
		}
		public static PathBuilder GetCommonPathFor()
		{
			return PathFor(Environment.SpecialFolder.CommonApplicationData);
		}
		public static PathBuilder GetUserPathFor()
		{
			return PathFor(Environment.SpecialFolder.ApplicationData);
		}
		public static PathBuilder GetUserDocument()
		{
			return OsFilePath(Environment.SpecialFolder.MyDocuments);
		}

		public static void MakeSurePathExist(string path)
		{
			System.IO.FileInfo fi = new System.IO.FileInfo(path);
			System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(fi.DirectoryName);
			if (dir.Exists == false)
			{
				dir.Create();
			}
		}

		public static string GetTempFilePath(string extension)
		{
			List<string> used = new List<string>();
			string result;

			while (true)
			{
				string path = System.IO.Path.GetTempFileName();
				used.Add(path);
				path = System.IO.Path.ChangeExtension(path, extension);
				if (System.IO.File.Exists(path))
				{
				}
				else
				{
					result = path;
					break;
				}
			}

			foreach (string path in used)
			{
				System.IO.File.Delete(path);
			}

			return result;
		}

		public static void OpenFolder(string p)
		{
			string windir = Environment.GetEnvironmentVariable("WINDIR");
			Process prc = new Process();
			prc.StartInfo.FileName = windir + @"\explorer.exe";
			prc.StartInfo.Arguments = p;
			prc.Start();
		}

		public static PathBuilder GetFolder(string folder)
		{
			return new PathBuilder(folder);
		}

		public static bool Has(FileAttributes at, FileAttributes val)
		{
			return (at & val) == val;
		}
	}
}
