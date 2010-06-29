using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Reflection;
using System.Runtime.Serialization;

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

		#region ValueOrDefault
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
		#endregion



		#region Parse
		public static Object Parse(Type type, Dictionary<String, Object> data)
		{
			return Parse(type, data, null);
		}

		public static Object Parse(Type type, Dictionary<String, Object> data, String prefix)
		{
			return Parse(type, data, prefix, 0);
		}

		public static Object Parse(Type type, Dictionary<String, Object> data, String prefix, Int32 depth)
		{
			var obj = Activator.CreateInstance(type);
			foreach (var mi in GetDataMembers(type))
			{
				var attr = GetCustomAttribute<GroongaLoggerColumnAttribute>(mi);
				if (attr != null && attr.ColumnType != null && depth < 1)
				{
					SetMemberValue(mi, obj, Parse(attr.ColumnType, data, attr.Name, depth + 1));
				}
				else
				{
					String name = GetDataMemberName(mi, prefix);
					Object value;
					if (data.TryGetValue(name, out value))
						SetMemberValue(mi, obj, value);
				}
			}

			return obj;
		}

		public static IEnumerable<MemberInfo> GetDataMembers(Type type)
		{
			var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public)
				.Where(mi => mi.MemberType == MemberTypes.Field || mi.MemberType == MemberTypes.Property)
				.Where(mi => GetCustomAttribute<DataMemberAttribute>(mi) != null);

			return members;
		}

		public static String GetDataMemberName(MemberInfo mi)
		{
			return GetDataMemberName(mi, null);
		}

		public static String GetDataMemberName(MemberInfo mi, String prefix)
		{
			var attr = GetCustomAttribute<DataMemberAttribute>(mi);
			var name = attr != null ? attr.Name : mi.Name;
			return String.IsNullOrEmpty(prefix) ? name : prefix + "." + name;
		}

		public static IEnumerable<String> GetDataMemberNames(Type type)
		{
			return GetDataMemberNames(type, null);
		}

		public static IEnumerable<String> GetDataMemberNames(Type type, String prefix)
		{
			return GetDataMemberNames(type, prefix, 0);
		}

		private static IEnumerable<String> GetDataMemberNames(Type type, String prefix, Int32 depth)
		{
			return GetDataMembers(type)
				.SelectMany(mi =>
				{
					var tmp = new List<String>();
					var attr = GetCustomAttribute<GroongaLoggerColumnAttribute>(mi);
					if (attr != null && attr.ColumnType != null && depth < 1)
						tmp.AddRange(GetDataMemberNames(attr.ColumnType, attr.Name, depth + 1));
					else
						tmp.Add(GetDataMemberName(mi, prefix));
					return tmp;
				});
		}

		private static TAttribute GetCustomAttribute<TAttribute>(MemberInfo mi)
			where TAttribute : Attribute
		{
			var attrs = mi.GetCustomAttributes(typeof(TAttribute), false) as TAttribute[];
			return (attrs != null && attrs.Length > 0) ? attrs[0] : null;
		}

		private static void SetMemberValue(MemberInfo mi, Object obj, Object value)
		{
			switch (mi.MemberType)
			{
				case MemberTypes.Field:
					(mi as FieldInfo).SetValue(obj, value);
					break;

				case MemberTypes.Property:
					(mi as PropertyInfo).SetValue(obj, value, null);
					break;

				default:
					throw new NotSupportedException();
			}
		}
		#endregion
	}
}
