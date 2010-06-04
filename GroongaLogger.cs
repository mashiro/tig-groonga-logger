using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using System.Runtime.Serialization;
using Misuzilla.Net.Irc;
using Misuzilla.Applications.TwitterIrcGateway;
using Misuzilla.Applications.TwitterIrcGateway.AddIns;
using Misuzilla.Applications.TwitterIrcGateway.AddIns.Console;
using Misuzilla.Applications.TwitterIrcGateway.AddIns.TypableMap;
using Spica.Data.Groonga;

namespace Spica.Applications.TwitterIrcGateway.AddIns.GroongaLogger
{
	public class GroongaLoggerException : Exception
	{
		public GroongaLoggerException() { }
		public GroongaLoggerException(string message) : base(message) { }
		public GroongaLoggerException(string message, Exception inner) : base(message, inner) { }
	}

	public class GroongaLoggerContext : Context
	{
	}

	public class GroongaLoggerConfigration : IConfiguration
	{
		public String Host { get; set; }
		public Int32 Port { get; set; }

		public GroongaLoggerConfigration()
		{
			Host = "localhost";
			Port = 10041;
		}
	}

	public class GroongaLoggerAddIn : AddInBase
	{
		public const String DefautlUserTableName = "TwitterUser";
		public const String DefautlStatusTableName = "TwitterStatus";
		public const String DefautlBigramTableName = "TwitterBigram";

		public GroongaLoggerConfigration Config { get; set; }

		private GroongaContext _context = null;
		private Thread _thread = null;
		private EventWaitHandle _threadEvent = null;
		private Boolean _isThreadRunning = false;
		private Queue<Status> _threadQueue = new Queue<Status>();
		private TypableMapCommandProcessor _typableMapCommands = null;

		public GroongaLoggerAddIn()
		{
		}

		public override void Initialize()
		{
			base.Initialize();

			// 設定を読み込む
			Config = CurrentSession.AddInManager.GetConfig<GroongaLoggerConfigration>();

			CurrentSession.PreProcessTimelineStatus += new EventHandler<TimelineStatusEventArgs>(CurrentSession_PreProcessTimelineStatus);
			CurrentSession.AddInsLoadCompleted += (sender, e) =>
			{
				// コンテキストを登録
				CurrentSession.AddInManager.GetAddIn<ConsoleAddIn>().RegisterContext<GroongaLoggerContext>();

				// TypableMap
				var typableMapSupport = CurrentSession.AddInManager.GetAddIn<TypableMapSupport>();
				if (typableMapSupport != null)
					_typableMapCommands = typableMapSupport.TypableMapCommands;
			};
		}

		public override void Uninitialize()
		{
			Stop();
			base.Uninitialize();
		}

		private void CurrentSession_PreProcessTimelineStatus(object sender, TimelineStatusEventArgs e)
		{
			Enqueue(e.Status);
		}

		#region Logging
		/// <summary>
		/// ロギングを開始します。
		/// </summary>
		private void Start()
		{
			if (!_isThreadRunning)
			{
				Cleanup();

				_context = new GroongaContext();
				_context.Connect(Config.Host, Config.Port);
				_threadEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
				_thread = new Thread(LoggingThread);
				_thread.Start();
			}
		}

		/// <summary>
		/// ロギングを停止します。
		/// </summary>
		private void Stop()
		{
			if (_isThreadRunning)
			{
				_threadEvent.Set();
				_thread.Join();
			}

			Cleanup();
		}

		/// <summary>
		/// リソースを破棄します。
		/// </summary>
		private void Cleanup()
		{
			if (_threadEvent != null)
			{
				_threadEvent.Close();
				_threadEvent = null;
			}
			if (_thread != null)
			{
				_thread = null;
			}
			if (_context != null)
			{
				_context.Dispose();
				_context = null;
			}
		}

		/// <summary>
		/// キューに追加します。
		/// </summary>
		/// <param name="status">ステータス</param>
		private void Enqueue(Status status)
		{
			// スレッドが起動してる時のみ
			if (_isThreadRunning)
			{
				lock (_threadQueue)
				{
					_threadQueue.Enqueue(status);
				}
			}
		}		

		/// <summary>
		/// ログ取りスレッド
		/// </summary>
		private void LoggingThread()
		{
			try
			{
				_isThreadRunning = true;

				// テーブルを初期化
				InitializeTables();

				while (true)
				{
					// キューから取れるだけ取ってくる
					var statuses = new List<Status>();
					lock (_threadQueue)
					{
						statuses.AddRange(_threadQueue);
						_threadQueue.Clear();
					}

					// データベースに格納
					StoreStatuses(statuses);

					// 待機
					if (_threadEvent.WaitOne(1000 * 10))
					{
						// シグナルを受け取ったらスレッドを終了
						break;
					}
				}
			}
			catch (Exception)
			{
			}
			finally
			{
				_isThreadRunning = false;
			}
		}

		private void StoreStatuses(IEnumerable<Status> statuses)
		{
			var usersJson = String.Join(",", statuses
				.Select(s => s.User)
				.Where(u => u != null)
				.Select(u => JsonUtility.Serialize(ToGroongaUser(u)))
				.ToArray());

			var statusesJson = String.Join(",", statuses
				.Select(s => JsonUtility.Serialize(ToGroongaStatus(s)))
				.ToArray());

			Execute("load --table {0}", UserTableName);
			Execute("[" + usersJson + "]");
			Execute("load --table {0}", StatusTableName);
			Execute("[" + statusesJson + "]");
		}
		#endregion

		#region Conversion
		private Object ToGroongaStatus(Status status)
		{
			return new
			{
				_key = status.Id.ToString(),
				created_at = GroongaLoggerUtility.ToUnixTime(status.CreatedAt),
				source = status.Source,
				truncated = status.Truncated,
				favorited = status.Favorited,
				in_reply_to_status_id = status.InReplyToStatusId,
				in_reply_to_user_id = status.InReplyToUserId,
				retweeted_status = status.RetweetedStatus != null ? status.RetweetedStatus.Id.ToString() : null,
				user = status.User != null ? status.User.Id.ToString() : null,
			};
		}

		private Object ToGroongaUser(User user)
		{
			return new
			{
				_key = user.Id.ToString(),
				name = user.Name,
				screen_name = user.ScreenName,
				location = user.Location,
				description = user.Description,
				profile_image_url = user.ProfileImageUrl,
				url = user.Url,
				@protected = user.Protected,
			};
		}
		#endregion

		#region Query
		/// <summary>
		/// Groonga のクエリを実行します。
		/// </summary>
		/// <param name="str">クエリ</param>
		/// <returns>結果</returns>
		private String Execute(String query)
		{
			_context.Send(query);
			return _context.Recv();
		}

		/// <summary>
		/// Groonga のクエリを実行します。
		/// </summary>
		/// <param name="format">クエリのフォーマット</param>
		/// <param name="args">可変長引数</param>
		/// <returns>結果</returns>
		private String Execute(String format, params Object[] args)
		{
			return Execute(String.Format(format, args));
		}
		#endregion

		#region Table
		public String StatusTableName { get { return "TwitterStatus_" + CurrentSession.TwitterUser.Id; } }
		public String UserTableName { get { return "TwitterUser_" + CurrentSession.TwitterUser.Id; } }
		public String BigramTableName { get { return "TwitterBigram_" + CurrentSession.TwitterUser.Id; } }

		/// <summary>
		/// Groonga のテーブル名の一覧を取得します。
		/// </summary>
		/// <returns>テーブル名の一覧</returns>
		private List<String> GetTableNames()
		{
			var json = Execute("table_list");
			return JsonUtility.Parse(json)
				.IndexOf(1).Elements()
				.Where((e, n) => n >= 1)
				.Select(e => e.IndexOf(1).Value)
				.ToList();
		}

		/// <summary>
		/// Groonga のカラム名の一覧を取得します。
		/// </summary>
		/// <param name="tableName">テーブル名</param>
		/// <returns>カラム名の一覧</returns>
		private List<String> GetColumnNames(String tableName)
		{
			var json = Execute("table_list --table {0}", tableName);
			return JsonUtility.Parse(json)
				.IndexOf(1).Elements()
				.Where((e, n) => n >= 1)
				.Select(e => e.IndexOf(1).Value)
				.ToList();
		}

		/// <summary>
		/// テーブルを初期化します。
		/// </summary>
		private void InitializeTables()
		{
			var tableNames = GetTableNames();
			InitializeUserTable(tableNames);
			InitializeStatusTable(tableNames);
			InitializeBigramTable(tableNames);
		}

		/// <summary>
		/// User テーブルの初期化を行います。
		/// </summary>
		private void InitializeUserTable(List<String> tableNames)
		{
			var columnNames = GetColumnNames(UserTableName);
			CreateTable(tableNames, UserTableName, "TABLE_HASH_KEY", "ShortText", null);
			CreateColumn(columnNames, UserTableName, "name", "COLUMN_SCALAR", "ShortText", null);
			CreateColumn(columnNames, UserTableName, "screen_name", "COLUMN_SCALAR", "ShortText", null);
			CreateColumn(columnNames, UserTableName, "location", "COLUMN_SCALAR", "ShortText", null);
			CreateColumn(columnNames, UserTableName, "description", "COLUMN_SCALAR", "ShortText", null);
			CreateColumn(columnNames, UserTableName, "profile_image_url", "COLUMN_SCALAR", "ShortText", null);
			CreateColumn(columnNames, UserTableName, "url", "COLUMN_SCALAR", "ShortText", null);
			CreateColumn(columnNames, UserTableName, "protected", "COLUMN_SCALAR", "Bool", null);
		}

		/// <summary>
		/// Status テーブルの初期化を行います。
		/// </summary>
		private void InitializeStatusTable(List<String> tableNames)
		{
			var columnNames = GetColumnNames(StatusTableName);
			CreateTable(tableNames, StatusTableName, "TABLE_HASH_KEY", "ShortText", null);
			CreateColumn(columnNames, StatusTableName, "created_at", "COLUMN_SCALAR", "Time", null);
			CreateColumn(columnNames, StatusTableName, "text", "COLUMN_SCALAR", "ShortText", null);
			CreateColumn(columnNames, StatusTableName, "source", "COLUMN_SCALAR", "ShortText", null);
			CreateColumn(columnNames, StatusTableName, "truncated", "COLUMN_SCALAR", "Bool", null);
			CreateColumn(columnNames, StatusTableName, "favorited", "COLUMN_SCALAR", "Bool", null);
			CreateColumn(columnNames, StatusTableName, "in_reply_to_status_id", "COLUMN_SCALAR", "ShortText", null);
			CreateColumn(columnNames, StatusTableName, "in_reply_to_user_id", "COLUMN_SCALAR", "ShortText", null);
			CreateColumn(columnNames, StatusTableName, "retweeted_status", "COLUMN_SCALAR", StatusTableName, null);
			CreateColumn(columnNames, StatusTableName, "user", "COLUMN_SCALAR", UserTableName, null);
		}

		/// <summary>
		/// Bigram テーブルの初期化を行います。
		/// </summary>
		private void InitializeBigramTable(List<String> tableNames)
		{
			var columnNames = GetColumnNames(BigramTableName);
			CreateTable(tableNames, BigramTableName, "TABLE_PAT_KEY|KEY_NORMALIZE", "ShortText", "TokenBigram");
			CreateColumn(columnNames, BigramTableName, "index_name", "COLUMN_INDEX|WITH_POSITION", UserTableName, "name");
			CreateColumn(columnNames, BigramTableName, "index_location", "COLUMN_INDEX|WITH_POSITION", UserTableName, "location");
			CreateColumn(columnNames, BigramTableName, "index_description", "COLUMN_INDEX|WITH_POSITION", UserTableName, "description");
			CreateColumn(columnNames, BigramTableName, "index_text", "COLUMN_INDEX|WITH_POSITION", StatusTableName, "text");
		}

		/// <summary>
		/// テーブルを作成します。
		/// </summary>
		private void CreateTable(List<String> tableNames, String name, String flags, String key_type, String default_tokenizer)
		{
			if (!tableNames.Contains(name))
			{
				var options = new Dictionary<String, String>() {
					{ "name", name },
					{ "flags", flags },
					{ "key_type", key_type },
					{ "default_tokenizer", default_tokenizer }
				};

				var result = Execute("table_create {0}", ParseOptions(options));
				if (String.IsNullOrEmpty(result))
					throw new Exception(String.Format("テーブル {0} の作成に失敗しました。", name));
			}
		}

		/// <summary>
		/// カラムを作成します。
		/// </summary>
		private void CreateColumn(List<String> columnNames, String table, String name, String flags, String type, String source)
		{
			if (!columnNames.Contains(name))
			{
				var options = new Dictionary<String, String>() {
					{ "table", table },
					{ "name", name },
					{ "flags", flags },
					{ "type", type },
					{ "source", source }
				};

				var result = Execute("column_create {0}", ParseOptions(options));
				if (String.IsNullOrEmpty(result))
					throw new Exception(String.Format("テーブル {0} カラム {1} の作成に失敗しました。", table, name));
			}
		}

		/// <summary>
		/// 辞書を Groonga のオプションに変換します。
		/// </summary>
		private String ParseOptions(Dictionary<String, String> options)
		{
			if (options == null)
				return String.Empty;

			return String.Join(" ", options
				.Where(d => !String.IsNullOrEmpty(d.Key) && !String.IsNullOrEmpty(d.Value))
				.Select(d => String.Format("--{0} {1}", d.Key, d.Value))
				.ToArray());
		}
		#endregion
	}
}
