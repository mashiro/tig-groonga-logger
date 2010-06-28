using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spica.Applications.TwitterIrcGateway.AddIns.GroongaLogger
{
	public class GroongaLoggerSelectState
	{
		public GroongaLoggerCommandOptions Options { get; private set; }
		public Int32 Offset { get; private set; }
		public Int32 Limit { get; private set; }
		public Int32 Total { get; private set; }

		private Int32 LastLimit { get; set; }
		private Int32 Left { get { return Offset; } }
		private Int32 Right { get { return Offset + Limit; } }

		public void Reset(GroongaLoggerCommandOptions options, Int32 limit, Int32 total)
		{
			Options = new GroongaLoggerCommandOptions(options);
			Offset = 0;
			Limit = Math.Min(limit, total);
			Total = total;
			LastLimit = limit;
			Setup();
		}

		public Boolean Next(Int32 limit)
		{
			if (Right >= Total)
				return false;

			Offset += LastLimit;
			LastLimit = limit;

			if (Right > Total)
			{
				Limit = Total - Offset;
			}
			else
			{
				Limit = limit;
			}
		
			Setup();
			return true;
		}

		public Boolean Previous(Int32 limit)
		{
			if (Left <= 0)
				return false;

			Offset -= LastLimit;
			LastLimit = limit;

			if (Left < 0)
			{
				Limit = limit + Offset;
				Offset = 0;
			}
			else
			{
				Limit = limit;
			}

			Setup();
			return true;
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
