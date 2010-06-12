using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using System.Runtime.Serialization;
using System.Reflection;
using Misuzilla.Net.Irc;
using Misuzilla.Applications.TwitterIrcGateway;
using Misuzilla.Applications.TwitterIrcGateway.AddIns;
using Misuzilla.Applications.TwitterIrcGateway.AddIns.Console;
using Misuzilla.Applications.TwitterIrcGateway.AddIns.TypableMap;
using Spica.Data.Groonga;

namespace Spica.Applications.TwitterIrcGateway.AddIns.GroongaLogger
{
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
		public const String DefaultUserTableName = "TwitterUser";
		public const String DefaultStatusTableName = "TwitterStatus";
		public const String DefaultTermTableName = "TwitterTerm";

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

		/// <summary>
		/// ステータスをデータストアに格納します。
		/// </summary>
		/// <param name="statuses">ステータス</param>
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

			Execute("load --table {0}", ToUniqueTableName(DefaultUserTableName));
			Execute("[" + usersJson + "]");
			Execute("load --table {0}", ToUniqueTableName(DefaultStatusTableName));
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
		/// <summary>
		/// テーブルを初期化します。
		/// </summary>
		private void InitializeTables()
		{
			var tables = new MemberInfo[] {
				typeof(GroongaLoggerUser),
				typeof(GroongaLoggerStatus),
				typeof(GroongaLoggerTerm)
			};

			CreateTables(tables);
		}

		/// <summary>
		/// テーブルを作成します。
		/// </summary>
		/// <param name="tables"></param>
		private void CreateTables(IEnumerable<MemberInfo> tables)
		{
			var tableNames = GetTableNames();

			foreach (var table in tables)
			{
				var attribute = GetCustomAttribute<GroongaLoggerTableAttribute>(table);
				if (attribute != null)
				{
					// ユーザー固有のテーブル名に変換
					attribute.Name = ToUniqueTableName(attribute.Name);

					if (!tableNames.Contains(attribute.Name))
					{
						try
						{
							CreateTable(attribute);
							CreateColumns(attribute.Name, table.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance));
						}
						catch (Exception)
						{
						}
					}
				}
			}
		}

		/// <summary>
		/// カラムを作成します。
		/// </summary>
		private void CreateColumns(String tableName, IEnumerable<MemberInfo> columns)
		{
			var columnNames = GetColumnNames(tableName);

			foreach (var column in columns)
			{
				var attribute = GetCustomAttribute<GroongaLoggerColumnAttribute>(column);
				if (attribute != null)
				{
					if (!columnNames.Contains(attribute.Name))
					{
						try
						{
							// ユーザー固有のテーブル名に変換
							attribute.Table = tableName;
							attribute.Type = ToUniqueTableName(attribute.Type);

							CreateColumn(attribute);
						}
						catch (Exception)
						{
						}
					}
				}
			}
		}

		/// <summary>
		/// 属性からテーブルを作成します。
		/// </summary>
		/// <param name="attribute">テーブルの属性</param>
		private void CreateTable(GroongaLoggerTableAttribute attribute)
		{
			var options = new Dictionary<String, String>() {
				{ "name", attribute.Name },
				{ "flags", attribute.Flags },
				{ "key_type", attribute.KeyType },
				{ "value_type", attribute.ValueType },
				{ "default_tokenizer", attribute.DefaultTokenizer }
			};

			var result = Execute("table_create {0}", ParseOptions(options));
			if (String.IsNullOrEmpty(result))
				throw new Exception(String.Format("テーブル {0} の作成に失敗しました。", attribute.Name));
		}

		/// <summary>
		/// 属性からカラムを作成します。
		/// </summary>
		/// <param name="attribute">カラムの属性</param>
		private void CreateColumn(GroongaLoggerColumnAttribute attribute)
		{
			var options = new Dictionary<String, String>() {
				{ "table", attribute.Table },
				{ "name", attribute.Name },
				{ "flags", attribute.Flags },
				{ "type", attribute.Type },
				{ "source", attribute.Source }
			};

			var result = Execute("column_create {0}", ParseOptions(options));
			if (String.IsNullOrEmpty(result))
				throw new Exception(String.Format("テーブル {0} カラム {1} の作成に失敗しました。", attribute.Table, attribute.Name));
		}

		/// <summary>
		/// カスタム属性を取得します。
		/// </summary>
		/// <typeparam name="TAttribute">属性の型</typeparam>
		/// <param name="memberInfo">属性を取得するメンバ</param>
		/// <returns>属性</returns>
		private TAttribute GetCustomAttribute<TAttribute>(MemberInfo memberInfo) where TAttribute : class
		{
			return Attribute.GetCustomAttribute(memberInfo, typeof(TAttribute)) as TAttribute;
		}

		/// <summary>
		/// ユーザー固有のテーブル名に変換します。
		/// </summary>
		/// <param name="tableName">テーブル名</param>
		/// <returns>ユーザー固有のテーブル名</returns>
		public String ToUniqueTableName(String tableName)
		{
			if (String.Compare(tableName, DefaultUserTableName) == 0 ||
				String.Compare(tableName, DefaultStatusTableName) == 0 ||
				String.Compare(tableName, DefaultTermTableName) == 0)
			{
				return String.Format("{0}_{1}", tableName, CurrentSession.TwitterUser.Id);
			}

			return tableName;
		}

		/// <summary>
		/// Groonga のテーブル名の一覧を取得します。
		/// </summary>
		/// <returns>テーブル名の一覧</returns>
		private IEnumerable<String> GetTableNames()
		{
#if true
			var json = Execute("table_list");
			var response = new GroongaResponse<GroongaResponseData>();
			response.Parse(JsonUtility.Parse(json));
			foreach (var item in response.Data.Items)
				yield return (String)item["name"];
#else
			var json = Execute("table_list");
			return JsonUtility.Parse(json)
				.IndexOf(1).Elements()
				.Where((e, n) => n >= 1)
				.Select(e => e.IndexOf(1).Value);
#endif
		}

		/// <summary>
		/// Groonga のカラム名の一覧を取得します。
		/// </summary>
		/// <param name="tableName">テーブル名</param>
		/// <returns>カラム名の一覧</returns>
		private IEnumerable<String> GetColumnNames(String tableName)
		{
#if true
			var json = Execute("table_list --table {0}", tableName);
			var response = new GroongaResponse<GroongaResponseData>();
			response.Parse(JsonUtility.Parse(json));
			foreach (var item in response.Data.Items)
				yield return (String)item["name"];
#else
			var json = Execute("table_list --table {0}", tableName);
			return JsonUtility.Parse(json)
				.IndexOf(1).Elements()
				.Where((e, n) => n >= 1)
				.Select(e => e.IndexOf(1).Value)
				.ToList();
#endif
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
