using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spica.Applications.TwitterIrcGateway.AddIns.GroongaLogger
{
	public class GroongaLoggerCommandOptions : Dictionary<String, String>
	{
		public GroongaLoggerCommandOptions()
			: base(StringComparer.InvariantCultureIgnoreCase)
		{
		}

		public GroongaLoggerCommandOptions(GroongaLoggerCommandOptions options)
			: base(options, StringComparer.InvariantCultureIgnoreCase)
		{
		}

		public override string ToString()
		{
			return String.Join(" ", this
				.Where(d => !String.IsNullOrEmpty(d.Key) && !String.IsNullOrEmpty(d.Value))
				.Select(d => String.Format("--{0} {1}", d.Key.ToLower(), d.Value))
				.ToArray());
		}
	}
}
