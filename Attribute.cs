using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spica.Applications.TwitterIrcGateway.AddIns.GroongaLogger
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class GroongaLoggerTableAttribute : Attribute
	{
		public String Name { get; set; }
		public String Flags { get; set; }
		public String KeyType { get; set; }
		public String ValueType { get; set; }
		public String DefaultTokenizer { get; set; }
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class GroongaLoggerColumnAttribute : Attribute
	{
		public String Table { get; set; }
		public String Name { get; set; }
		public String Flags { get; set; }
		public String Type { get; set; }
		public String Source { get; set; }
		public Type ColumnType { get; set; }
	}
}
