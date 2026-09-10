using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Util
{
	public class IndexCreator
	{
		int index = 0;


		public int generate()
		{
			int r = index;
			++index;
			return r;
		}

		public void clear()
		{
			index = 0;
		}
	}
}
