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

		public static DateTime ToDateTime(Double unixTime)
		{
			return _originDateTime.AddSeconds(unixTime);
		}

		public static String ToString<T>(T value) where T : class
		{
			return value != null ? value.ToString() : String.Empty;
		}

		public static String ToString<T, TResult>(T value, Func<T, TResult> selector) where T : class
		{
			return value != null ? selector(value).ToString() : String.Empty;
		}


		public static T ValueOrDefault<T>(T value)
			where T : class
		{
			return value != null ? value : default(T);
		}

		public static T ValueOrDefault<T>(T value, T defaultValue)
			where T : class
		{
			return value != null ? value : defaultValue;
		}

		public static TResult ValueOrDefault<T, TResult>(T value, Func<T, TResult> selector)
			where T : class
		{
			return value != null ? selector(value) : default(TResult);
		}

		public static TResult ValueOrDefault<T, TResult>(T value, Func<T, TResult> selector, TResult defaultValue)
			where T : class
		{
			return value != null ? selector(value) : defaultValue;
		}
	}
}
