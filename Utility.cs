using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Spica.Applications.TwitterIrcGateway.AddIns.GroongaLogger
{
	public static class GroongaLoggerUtility
	{
		internal static XElement IndexOf(this XElement e, Int32 index)
		{
			return e.Elements().ElementAt(index);
		}

		private static readonly DateTime _originDateTime = new DateTime(1970, 1, 1, 0, 0, 0);
		public static Double ToUnixTime(DateTime dateTime)
		{
			TimeSpan timeSpan = dateTime - _originDateTime;
			return timeSpan.TotalSeconds;
		}

		public static DateTime ToLocalDateTime(Double unixTime)
		{
			DateTime dateTime = _originDateTime.AddSeconds(unixTime);
			return dateTime.ToLocalTime();
		}
	}
}
