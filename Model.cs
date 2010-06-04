using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Spica.Applications.TwitterIrcGateway.AddIns.GroongaLogger
{
	[DataContract]
	[GroongaLoggerTable(Name = GroongaLoggerAddIn.DefautlUserTableName, Flags = "TABLE_HASH_KEY", KeyType = "ShortText")]
	public class GroongaLoggerUser
	{
		[DataMember(Name = "_key")]
		public String Id { get; set; }

		[DataMember(Name = "name")]
		[GroongaLoggerColumn(Name = "name", Flags = "COLUMN_SCALAR", Type = "ShortText")]
		public String Name { get; set; }

		[DataMember(Name = "screen_name")]
		[GroongaLoggerColumn(Name = "screen_name", Flags = "COLUMN_SCALAR", Type = "ShortText")]
		public String ScreenName { get; set; }

		[DataMember(Name = "location")]
		[GroongaLoggerColumn(Name = "location", Flags = "COLUMN_SCALAR", Type = "ShortText")]
		public String Location { get; set; }

		[DataMember(Name = "description")]
		[GroongaLoggerColumn(Name = "description", Flags = "COLUMN_SCALAR", Type = "ShortText")]
		public String Description { get; set; }

		[DataMember(Name = "profile_image_url")]
		[GroongaLoggerColumn(Name = "profile_image_url", Flags = "COLUMN_SCALAR", Type = "ShortText")]
		public String ProfileImageUrl { get; set; }

		[DataMember(Name = "url")]
		[GroongaLoggerColumn(Name = "url", Flags = "COLUMN_SCALAR", Type = "ShortText")]
		public String Url { get; set; }

		[DataMember(Name = "protected")]
		[GroongaLoggerColumn(Name = "protected", Flags = "COLUMN_SCALAR", Type = "Bool")]
		public Boolean Protected { get; set; }
	}

	[DataContract]
	[GroongaLoggerTable(Name = GroongaLoggerAddIn.DefautlStatusTableName, Flags = "TABLE_HASH_KEY", KeyType = "ShortText")]
	public class GroongaLoggerStatus
	{
		[DataMember(Name = "created_at")]
		[GroongaLoggerColumn(Name = "created_at", Flags = "COLUMN_SCALAR", Type = "Time")]
		public Double _createdAt;

		[IgnoreDataMember]
		public DateTime CreatedAt
		{
			get { return GroongaLoggerUtility.ToLocalDateTime(_createdAt); }
			set { _createdAt = GroongaLoggerUtility.ToUnixTime(value); }
		}

		[DataMember(Name = "text")]
		[GroongaLoggerColumn(Name = "text", Flags = "COLUMN_SCALAR", Type = "ShortText")]
		public String Text { get; set; }

		[DataMember(Name = "source")]
		[GroongaLoggerColumn(Name = "source", Flags = "COLUMN_SCALAR", Type = "ShortText")]
		public String Source { get; set; }

		[DataMember(Name = "truncated")]
		[GroongaLoggerColumn(Name = "truncated", Flags = "COLUMN_SCALAR", Type = "Bool")]
		public Boolean Truncated { get; set; }

		[DataMember(Name = "favorited")]
		[GroongaLoggerColumn(Name = "favorited", Flags = "COLUMN_SCALAR", Type = "Bool")]
		public Boolean Favorited { get; set; }

		[DataMember(Name = "in_reply_to_status_id")]
		[GroongaLoggerColumn(Name = "in_reply_to_status_id", Flags = "COLUMN_SCALAR", Type = "ShortText")]
		public String InReplyToStatusId { get; set; }

		[DataMember(Name = "in_reply_to_user_id")]
		[GroongaLoggerColumn(Name = "in_reply_to_user_id", Flags = "COLUMN_SCALAR", Type = "ShortText")]
		public String InReplyToUserId { get; set; }

		[DataMember(Name = "retweeted_status")]
		[GroongaLoggerColumn(Name = "retweeted_status", Flags = "COLUMN_SCALAR", Type = GroongaLoggerAddIn.DefautlStatusTableName)]
		public String RetweetedStatus { get; set; }

		[DataMember(Name = "user")]
		[GroongaLoggerColumn(Name = "user", Flags = "COLUMN_SCALAR", Type = GroongaLoggerAddIn.DefautlUserTableName)]
		public String User { get; set; }
	}

	[GroongaLoggerTable(Name = GroongaLoggerAddIn.DefautlBigramTableName, Flags = "TABLE_PAT_KEY|KEY_NORMALIZE", KeyType = "ShortText", DefaultTokenizer = "TokenBigram")]
	public class GroongaLoggerBigram
	{
		[GroongaLoggerColumn(Name = "index_name", Flags = "COLUMN_INDEX|WITH_POSITION", Type = GroongaLoggerAddIn.DefautlUserTableName, Source = "name")]
		public Object IndexName { get; set; }

		[GroongaLoggerColumn(Name = "index_location", Flags = "COLUMN_INDEX|WITH_POSITION", Type = GroongaLoggerAddIn.DefautlUserTableName, Source = "location")]
		public Object IndexLocation { get; set; }

		[GroongaLoggerColumn(Name = "index_description", Flags = "COLUMN_INDEX|WITH_POSITION", Type = GroongaLoggerAddIn.DefautlUserTableName, Source = "description")]
		public Object IndexDescription { get; set; }

		[GroongaLoggerColumn(Name = "index_text", Flags = "COLUMN_INDEX|WITH_POSITION", Type = GroongaLoggerAddIn.DefautlStatusTableName, Source = "text")]
		public Object IndexText { get; set; }
	}
}
