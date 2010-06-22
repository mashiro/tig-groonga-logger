using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spica.Applications.TwitterIrcGateway.AddIns.GroongaLogger
{
	public class GroongaLoggerSelectState
	{
		public GroongaLoggerOptions Options { get; set; }
		public Int32 Offset { get; set; }
		public Int32 Limit { get; set; }
		public Int32 Total { get; set; }

		public void Reset(GroongaLoggerOptions options, Int32 limit, Int32 total)
		{
			Options = new GroongaLoggerOptions(options);
			Offset = 0;
			Limit = limit;
			Total = total;
			Setup();
		}

		public Boolean Next(Int32 limit)
		{
			if ((Offset + Limit) < Total)
			{
				Offset += Limit;
				Limit = limit;
				Setup();
				return true;
			}

			return false;
		}

		public Boolean Prev(Int32 limit)
		{
			if ((Offset > 0))
			{
				Offset = Math.Max(0, Offset - Limit);
				Limit = limit;
				Setup();
				return true;
			}

			return false;
		}

		private void Setup()
		{
			if (Options != null)
			{
				Options["offset"] = Offset.ToString();
				Options["limit"] = Limit.ToString();
			}
		}
	}
}
