using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Spica.Applications.TwitterIrcGateway.AddIns.GroongaLogger
{
	public interface IGroongaLoggerResponse
	{
		void Parse(XElement e);
	}

	public class GroongaLoggerResponseStatus : IGroongaLoggerResponse
	{
		public Int32 StatusCode { get; set; }
		public DateTime ProcessStartTime { get; set; }
		public Double ProcessTime { get; set; }

		public void Parse(XElement e)
		{
			var elements = e.Elements();
			StatusCode = (Int32)elements.ElementAt(0);
			ProcessStartTime = GroongaLoggerUtility.UnixTimeToDateTime((Double)elements.ElementAt(1));
			ProcessTime = (Double)elements.ElementAt(2);
		}
	}

	public class GroongaLoggerResponseData : IGroongaLoggerResponse
	{
		public Int32? SearchCount { get; set; }
		public List<Dictionary<String, Object>> Items { get; set; }

		public void Parse(XElement e)
		{
			var index = 0;
			var countElement = e.Elements().ElementAt(0);
			if (countElement.Elements().Count() == 1 &&
				countElement.Elements().ElementAt(0).Attribute("type").Value == "number")
			{
				SearchCount = (Int32)countElement.Elements().ElementAt(0);
				index += 1;
			}

			var types = e.Elements().ElementAt(index).Elements()
				.Select(_ => _.Elements())
				.Select(_ => new { Name = (String)_.ElementAt(0), Type = (String)_.ElementAt(1) })
				.ToList();

			Items = e.Elements().Skip(index + 1)
				.Select(_1 => _1.Elements()
					.Select((_2, n2) => new { Key = types[n2].Name, Value = Convert(types[n2].Type, _2.Value) })
					.ToDictionary(_2 => _2.Key, _2 => _2.Value))
				.ToList();
		}

		private Object Convert(String type, String value)
		{
			switch (type)
			{
				case "Bool":
					return Boolean.Parse(value);
				case "Int8":
					return SByte.Parse(value);
				case "UInt8":
					return Byte.Parse(value);
				case "Int16":
					return Int16.Parse(value);
				case "UInt16":
					return UInt16.Parse(value);
				case "Int32":
					return Int32.Parse(value);
				case "UInt32":
					return UInt32.Parse(value);
				case "Int64":
					return Int64.Parse(value);
				case "UInt64":
					return UInt64.Parse(value);
				case "Float":
					return Single.Parse(value);
				case "Time":
					return Double.Parse(value);
				case "ShortText":
				case "Text":
				case "LongText":
				case "Object":
				default:
					return value;
			}
		}
	}

	public class GroongaLoggerResponseDataList : IGroongaLoggerResponse
	{
		public List<GroongaLoggerResponseData> Items { get; set; }

		public void Parse(XElement e)
		{
			Items = new List<GroongaLoggerResponseData>();
			foreach (var dataElement in e.Elements())
			{
				var data = new GroongaLoggerResponseData();
				data.Parse(dataElement);
				Items.Add(data);
			}
		}
	}

	public class GroongaLoggerResponse<TData> : IGroongaLoggerResponse
		where TData : IGroongaLoggerResponse, new()
	{
		public GroongaLoggerResponseStatus Status { get; set; }
		public TData Data { get; set; }

		public GroongaLoggerResponse()
		{
			Status = new GroongaLoggerResponseStatus();
			Data = new TData();
		}

		public void Parse(XElement e)
		{
			var elements = e.Elements();
			Status.Parse(elements.ElementAt(0));
			Data.Parse(elements.ElementAt(1));
		}
	}

	public class GroongaLoggerResponseEqualityComparer : IEqualityComparer<KeyValuePair<String, Object>>
	{
		public bool Equals(KeyValuePair<string, object> x, KeyValuePair<string, object> y)
		{
			return x.Key.Equals(y.Key);
		}

		public int GetHashCode(KeyValuePair<string, object> obj)
		{
			return obj.Key.GetHashCode();
		}
	}
}
