using System;

namespace RegionTrigger
{
	internal class CnNameAttribute : Attribute
	{
		public string Name { get; }

		public CnNameAttribute(string cnName)
		{
			Name = cnName;
		}
	}
}
