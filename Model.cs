using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Misuzilla.Applications.TwitterIrcGateway;

namespace Spica.Applications.TwitterIrcGateway.AddIns.GroongaLogger
{
	[DataContract]
	[GroongaLoggerTable(Name = GroongaLoggerAddIn.DefaultUserTableName, Flags = "TABLE_HASH_KEY", KeyType = "ShortText")]
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
		public Int32 _protected { get; set; }
		[IgnoreDataMember]
		public Boolean Protected { get { return _protected != 0; } set { _protected = value ? 1 : 0; } }

		public static explicit operator GroongaLoggerUser(User user)
		{
			return new GroongaLoggerUser()
			{
				Id = user.Id.ToString(),
				Name = user.Name,
				ScreenName = user.ScreenName,
				Location = user.Location,
				Description = user.Description,
				ProfileImageUrl = user.ProfileImageUrl,
				Url = user.Url,
				Protected = user.Protected,
			};
		}
	}

	[DataContract]
	[GroongaLoggerTable(Name = GroongaLoggerAddIn.DefaultStatusTableName, Flags = "TABLE_HASH_KEY", KeyType = "ShortText")]
	public class GroongaLoggerStatus
	{
		[DataMember(Name = "_key")]
		public String Id { get; set; }

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
		public Int32 _truncated { get; set; }
		[IgnoreDataMember]
		public Boolean Truncated { get { return _truncated != 0; } set { _truncated = value ? 1 : 0; } }

		[DataMember(Name = "favorited")]
		[GroongaLoggerColumn(Name = "favorited", Flags = "COLUMN_SCALAR", Type = "Bool")]
		public Int32 _favorited { get; set; }
		[IgnoreDataMember]
		public Boolean Favorited { get { return _favorited != 0; } set { _favorited = value ? 1 : 0; } }

		[DataMember(Name = "in_reply_to_status_id")]
		[GroongaLoggerColumn(Name = "in_reply_to_status_id", Flags = "COLUMN_SCALAR", Type = "ShortText")]
		public String InReplyToStatusId { get; set; }

		[DataMember(Name = "in_reply_to_user_id")]
		[GroongaLoggerColumn(Name = "in_reply_to_user_id", Flags = "COLUMN_SCALAR", Type = "ShortText")]
		public String InReplyToUserId { get; set; }

		[DataMember(Name = "retweeted_status")]
		[GroongaLoggerColumn(Name = "retweeted_status", Flags = "COLUMN_SCALAR", Type = GroongaLoggerAddIn.DefaultStatusTableName)]
		public String RetweetedStatus { get; set; }

		[DataMember(Name = "user")]
		[GroongaLoggerColumn(Name = "user", Flags = "COLUMN_SCALAR", Type = GroongaLoggerAddIn.DefaultUserTableName)]
		public String User { get; set; }

		public static explicit operator GroongaLoggerStatus(Status status)
		{
			return new GroongaLoggerStatus()
			{
				Id = status.Id.ToString(),
				CreatedAt = status.CreatedAt,
				Text = status.Text,
				Source = status.Source,
				Truncated = status.Truncated,
				Favorited = String.IsNullOrEmpty(status.Favorited) ? false : Boolean.Parse(status.Favorited),
				InReplyToStatusId = status.InReplyToStatusId,
				InReplyToUserId = status.InReplyToUserId,
				RetweetedStatus = status.RetweetedStatus != null ? status.RetweetedStatus.Id.ToString() : null,
				User = status.User != null ? status.User.Id.ToString() : null,
			};
		}
	}

	[GroongaLoggerTable(Name = GroongaLoggerAddIn.DefaultTermTableName, Flags = "TABLE_PAT_KEY|KEY_NORMALIZE", KeyType = "ShortText", DefaultTokenizer = "TokenBigram")]
	public class GroongaLoggerTerm
	{
		[GroongaLoggerColumn(Name = "index_name", Flags = "COLUMN_INDEX|WITH_POSITION", Type = GroongaLoggerAddIn.DefaultUserTableName, Source = "name")]
		public Object IndexName { get; set; }

		[GroongaLoggerColumn(Name = "index_location", Flags = "COLUMN_INDEX|WITH_POSITION", Type = GroongaLoggerAddIn.DefaultUserTableName, Source = "location")]
		public Object IndexLocation { get; set; }

		[GroongaLoggerColumn(Name = "index_description", Flags = "COLUMN_INDEX|WITH_POSITION", Type = GroongaLoggerAddIn.DefaultUserTableName, Source = "description")]
		public Object IndexDescription { get; set; }

		[GroongaLoggerColumn(Name = "index_text", Flags = "COLUMN_INDEX|WITH_POSITION", Type = GroongaLoggerAddIn.DefaultStatusTableName, Source = "text")]
		public Object IndexText { get; set; }
	}
}
