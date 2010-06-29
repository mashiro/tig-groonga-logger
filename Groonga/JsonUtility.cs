using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Spica.Data.Groonga
{
	public static class JsonUtility
	{
		public static XElement Parse(String json)
		{
			return Parse(json, Encoding.UTF8);
		}

		public static XElement Parse(String json, Encoding encoding)
		{
			using (var jsonReader = JsonReaderWriterFactory.CreateJsonReader(encoding.GetBytes(json), XmlDictionaryReaderQuotas.Max))
				return XElement.Load(jsonReader);
		}

		public static XElement Parse(Stream stream)
		{
			return Parse(stream, null);
		}

		public static XElement Parse(Stream stream, Encoding encoding)
		{
			using (var jsonReader = JsonReaderWriterFactory.CreateJsonReader(stream, encoding, XmlDictionaryReaderQuotas.Max, null))
				return XElement.Load(jsonReader);
		}

		public static String Serialize(Object obj)
		{
			return CreateJsonValue(obj);
		}

		private static String CreateJsonValue(Object obj)
		{
			var typeCode = obj != null ? Type.GetTypeCode(obj.GetType()) : TypeCode.Empty;
			switch (typeCode)
			{
				case TypeCode.Boolean:
					return obj.ToString().ToLower();

				case TypeCode.String:
				case TypeCode.Char:
				case TypeCode.DateTime:
					return CreateJsonString(obj);

				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
				case TypeCode.SByte:
				case TypeCode.Byte:
					return obj.ToString();

				case TypeCode.Object:
					if (obj is IEnumerable)
						return CreateJsonArray(obj as IEnumerable);
					else
						return CreateJsonObject(obj);

				case TypeCode.DBNull:
				case TypeCode.Empty:
				default:
					return "null";
			}
		}

		private static String CreateJsonString(Object obj)
		{
			var escaped = Regex.Replace(obj.ToString(), @"\\|""|/", m => @"\" + m.Value);
			return "\"" + escaped + "\"";
		}

		private static String CreateJsonObject(Object obj)
		{
			var items = obj.GetType()
				.GetMembers(BindingFlags.Public | BindingFlags.Instance)
				.Where(mi => IsDataMember(mi))
				.Select(mi => new { Name = GetDataMemberName(mi), Value = GetDataMemberValue(mi, obj) })
				.Select(arg => String.Format("\"{0}\":{1}", arg.Name, CreateJsonValue(arg.Value)));
			return "{" + String.Join(",", items.ToArray()) + "}";
		}

		private static String CreateJsonArray(IEnumerable source)
		{
			var items = source.Cast<Object>()
				.Select(item => CreateJsonValue(item));
			return "[" + String.Join(",", items.ToArray()) + "]";
		}

		private static String GetDataMemberName(MemberInfo mi)
		{
			var attr = Attribute.GetCustomAttribute(mi, typeof(DataMemberAttribute)) as DataMemberAttribute;
			return attr != null ? attr.Name : mi.Name;
		}

		private static Object GetDataMemberValue(MemberInfo mi, Object obj)
		{
			switch (mi.MemberType)
			{
				case MemberTypes.Field:
					return (mi as FieldInfo).GetValue(obj);

				case MemberTypes.Property:
					return (mi as PropertyInfo).GetValue(obj, null);

				default:
					throw new NotSupportedException();
			}
		}

		private static Boolean IsDataMember(MemberInfo mi)
		{
			switch (mi.MemberType)
			{
				case MemberTypes.Field:
				case MemberTypes.Property:
					// 匿名型なんかも考慮してIgnoreDataMemberAttributeを持っていないメンバが対象とする
					var attr = Attribute.GetCustomAttribute(mi, typeof(IgnoreDataMemberAttribute)) as IgnoreDataMemberAttribute;
					return attr == null;

				default:
					return false;
			}
		}
	}
}
