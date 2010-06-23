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
		[GroongaLoggerColumn(Name = "protected", Flags = "COLUMN_SCALAR", Type = "Int32")]
		public Int32 _protected { get; set; }
		[IgnoreDataMember]
		public Boolean Protected { get { return _protected != 0; } set { _protected = value ? 1 : 0; } }

		#region Index
		[IgnoreDataMember]
		[GroongaLoggerColumn(Name = "index_screen_name", Flags = "COLUMN_INDEX", Type = GroongaLoggerAddIn.DefaultUserTableName, Source = "screen_name")]
		public Object IndexScreenName { get; set; }
		#endregion

		public static GroongaLoggerUser FromUser(User user)
		{
			return new GroongaLoggerUser()
			{
				Id = user.Id.ToString(),
				Name = GroongaLoggerUtility.ValueOrDefault(user.Name, String.Empty),
				ScreenName = GroongaLoggerUtility.ValueOrDefault(user.ScreenName, String.Empty),
				Location = GroongaLoggerUtility.ValueOrDefault(user.Location, String.Empty),
				Description = GroongaLoggerUtility.ValueOrDefault(user.Description, String.Empty),
				ProfileImageUrl = GroongaLoggerUtility.ValueOrDefault(user.ProfileImageUrl, String.Empty),
				Url = GroongaLoggerUtility.ValueOrDefault(user.Url, String.Empty),
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
			get { return GroongaLoggerUtility.ToDateTime(_createdAt); }
			set { _createdAt = GroongaLoggerUtility.ToUnixTime(value); }
		}

		[DataMember(Name = "text")]
		[GroongaLoggerColumn(Name = "text", Flags = "COLUMN_SCALAR", Type = "ShortText")]
		public String Text { get; set; }

		[DataMember(Name = "source")]
		[GroongaLoggerColumn(Name = "source", Flags = "COLUMN_SCALAR", Type = "ShortText")]
		public String Source { get; set; }

		[DataMember(Name = "truncated")]
		[GroongaLoggerColumn(Name = "truncated", Flags = "COLUMN_SCALAR", Type = "Int32")]
		public Int32 _truncated { get; set; }
		[IgnoreDataMember]
		public Boolean Truncated { get { return _truncated != 0; } set { _truncated = value ? 1 : 0; } }

		[DataMember(Name = "favorited")]
		[GroongaLoggerColumn(Name = "favorited", Flags = "COLUMN_SCALAR", Type = "Int32")]
		public Int32 _favorited { get; set; }
		[IgnoreDataMember]
		public Boolean Favorited { get { return _favorited != 0; } set { _favorited = value ? 1 : 0; } }

		[DataMember(Name = "in_reply_to_status_id")]
		[GroongaLoggerColumn(Name = "in_reply_to_status_id", Flags = "COLUMN_SCALAR", Type = GroongaLoggerAddIn.DefaultStatusTableName)]
		public String InReplyToStatusId { get; set; }

		[DataMember(Name = "in_reply_to_user_id")]
		[GroongaLoggerColumn(Name = "in_reply_to_user_id", Flags = "COLUMN_SCALAR", Type = GroongaLoggerAddIn.DefaultUserTableName)]
		public String InReplyToUserId { get; set; }

		[DataMember(Name = "retweeted_status")]
		[GroongaLoggerColumn(Name = "retweeted_status", Flags = "COLUMN_SCALAR", Type = GroongaLoggerAddIn.DefaultStatusTableName)]
		public String RetweetedStatus { get; set; }

		[DataMember(Name = "user")]
		[GroongaLoggerColumn(Name = "user", Flags = "COLUMN_SCALAR", Type = GroongaLoggerAddIn.DefaultUserTableName)]
		public String User { get; set; }

		#region Index
		[IgnoreDataMember]
		[GroongaLoggerColumn(Name = "index_in_reply_to_status_id", Flags = "COLUMN_INDEX", Type = GroongaLoggerAddIn.DefaultStatusTableName, Source = "in_reply_to_status_id")]
		public Object IndexInReplyToStatusId { get; set; }

		[IgnoreDataMember]
		[GroongaLoggerColumn(Name = "index_in_reply_to_user_id", Flags = "COLUMN_INDEX", Type = GroongaLoggerAddIn.DefaultUserTableName, Source = "in_reply_to_user_id")]
		public Object IndexInReplyToUserId { get; set; }

		[IgnoreDataMember]
		[GroongaLoggerColumn(Name = "index_retweeted_status", Flags = "COLUMN_INDEX", Type = GroongaLoggerAddIn.DefaultStatusTableName, Source = "retweeted_status")]
		public Object IndexRetweetedStatus { get; set; }
		#endregion

		public static GroongaLoggerStatus FromStatus(Status status)
		{
			return new GroongaLoggerStatus()
			{
				Id = status.Id.ToString(),
				CreatedAt = status.CreatedAt,
				Text = GroongaLoggerUtility.ValueOrDefault(status.Text, String.Empty),
				Source = GroongaLoggerUtility.ValueOrDefault(status.Source, String.Empty),
				Truncated = status.Truncated,
				Favorited = GroongaLoggerUtility.ValueOrDefault(status.Favorited, fav => Boolean.Parse(fav), false),
				InReplyToStatusId = GroongaLoggerUtility.ValueOrDefault(status.InReplyToStatusId, String.Empty),
				InReplyToUserId = GroongaLoggerUtility.ValueOrDefault(status.InReplyToUserId, String.Empty),
				RetweetedStatus = GroongaLoggerUtility.ValueOrDefault(status.RetweetedStatus, s => s.Id.ToString(), String.Empty),
				User = GroongaLoggerUtility.ValueOrDefault(status.User, u => u.Id.ToString(), String.Empty),
			};
		}
	}

	[GroongaLoggerTable(Name = GroongaLoggerAddIn.DefaultTermTableName, Flags = "TABLE_PAT_KEY|KEY_NORMALIZE", KeyType = "ShortText", DefaultTokenizer = "TokenMecab")]
	public class GroongaLoggerTerm
	{
		#region Index
		[IgnoreDataMember]
		[GroongaLoggerColumn(Name = "index_name", Flags = "COLUMN_INDEX|WITH_POSITION|WITH_SECTION", Type = GroongaLoggerAddIn.DefaultUserTableName, Source = "name")]
		public Object IndexName { get; set; }

		[IgnoreDataMember]
		[GroongaLoggerColumn(Name = "index_location", Flags = "COLUMN_INDEX|WITH_POSITION|WITH_SECTION", Type = GroongaLoggerAddIn.DefaultUserTableName, Source = "location")]
		public Object IndexLocation { get; set; }

		[IgnoreDataMember]
		[GroongaLoggerColumn(Name = "index_description", Flags = "COLUMN_INDEX|WITH_POSITION|WITH_SECTION", Type = GroongaLoggerAddIn.DefaultUserTableName, Source = "description")]
		public Object IndexDescription { get; set; }

		[IgnoreDataMember]
		[GroongaLoggerColumn(Name = "index_text", Flags = "COLUMN_INDEX|WITH_POSITION|WITH_SECTION", Type = GroongaLoggerAddIn.DefaultStatusTableName, Source = "text")]
		public Object IndexText { get; set; }
		#endregion
	}
}
