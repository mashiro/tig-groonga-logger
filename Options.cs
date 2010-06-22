using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spica.Applications.TwitterIrcGateway.AddIns.GroongaLogger
{
	public class GroongaLoggerOptions : Dictionary<String, String>
	{
		public GroongaLoggerOptions()
			: base(StringComparer.InvariantCultureIgnoreCase)
		{
		}

		public GroongaLoggerOptions(GroongaLoggerOptions options)
			: base(options, StringComparer.InvariantCultureIgnoreCase)
		{
		}
	}
}
