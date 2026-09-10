using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Util
{
	public class PathBuilder
	{
		private string path;

		public PathBuilder(Environment.SpecialFolder folder)
		{
			path = System.Environment.GetFolderPath(folder);
		}

		public PathBuilder(string path)
		{
			this.path = path;
		}

		public PathBuilder dir(string name)
		{
			string safe = name;
			List<char> cs = new List<char>(InvalidPathCharacters());
			foreach (char c in cs)
			{
				safe = safe.Replace(c, '_');
			}
			return new PathBuilder(Path.Combine(path, safe));
		}
		const string kExtraInvalidCharacters = ".:\' !?()#,&";

		private IEnumerable<char> InvalidPathCharacters()
		{
			foreach (char c in System.IO.Path.GetInvalidPathChars())
				yield return c;
			foreach (char c in kExtraInvalidCharacters)
				yield return c;
		}

		public string Directory
		{
			get
			{
				return path;
			}
		}

		public string file(string name, string extention)
		{
			string safe = name;
			foreach (char c in InvalidFileCharacters())
			{
				safe = safe.Replace(c, '_');
			}
			return Path.ChangeExtension(Path.Combine(path, safe), extention);
		}

		private IEnumerable<char> InvalidFileCharacters()
		{
			foreach (char c in System.IO.Path.GetInvalidFileNameChars())
				yield return c;
			foreach (char c in kExtraInvalidCharacters)
				yield return c;
		}
	}
}
