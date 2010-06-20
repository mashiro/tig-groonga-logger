using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using System.Runtime.Serialization;
using System.Reflection;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Misuzilla.Net.Irc;
using Misuzilla.Applications.TwitterIrcGateway;
using Misuzilla.Applications.TwitterIrcGateway.AddIns;
using Misuzilla.Applications.TwitterIrcGateway.AddIns.Console;
using Misuzilla.Applications.TwitterIrcGateway.AddIns.TypableMap;
using Spica.Data.Groonga;

namespace Spica.Applications.TwitterIrcGateway.AddIns.GroongaLogger
{
	[Description("ロガーの設定を行うコンテキストに切り替えます")]
	public class GroongaLoggerContext : Context
	{
		private GroongaLoggerAddIn AddIn { get { return CurrentSession.AddInManager.GetAddIn<GroongaLoggerAddIn>(); } }

		public override IConfiguration[] Configurations { get { return new IConfiguration[] { AddIn.Config }; } }
		protected override void OnConfigurationChanged(IConfiguration config, System.Reflection.MemberInfo memberInfo, object value)
		{
			if (config is GroongaLoggerConfigration)
			{
				AddIn.Config = config as GroongaLoggerConfigration;
				AddIn.SaveConfig();
			}
		}

		[Description("ロギングを有効にします")]
		public void Enable()
		{
			AddIn.Config.Enabled = true;
			AddIn.SaveConfig();
			AddIn.Setup(AddIn.Config.Enabled);
			AddIn.NotifyMessage("ロギングを有効にしました。");
		}

		[Description("ロギングを無効にします")]
		public void Disable()
		{
			AddIn.Config.Enabled = false;
			AddIn.SaveConfig();
			AddIn.Setup(AddIn.Config.Enabled);
			AddIn.NotifyMessage("ロギングを無効にしました。");
		}

		[Description("ユーザ名で検索を行います。")]
		public void FindByScreenName(String screenName)
		{
			Catch(() =>
			{
				var options = new Dictionary<String, String>() {
					{ "table", AddIn.StatusTableName },
					{ "output_columns", "created_at,user.screen_name,text" },
					{ "query", "user.screen_name:" + screenName },
					{ "sortby", "-created_at" },
					{ "limit", "20" }
				};
				var query = String.Format("select {0}", AddIn.ParseOptions(options));
				var response = AddIn.Select(query);

				var items = response.Data.Items
					.SelectMany(data => data.Items)
					.Reverse();

				foreach (var item in items)
				{
					var created_at = (DateTime)item["created_at"];
					var screen_name = (String)item["user.screen_name"];
					var text = (String)item["text"];
					AddIn.NotifyMessage(screen_name, String.Format("{0} {1}", created_at.ToString("yyyy/MM/dd HH:mm:ss"), text));
				}
			});
		}

		[Description("テキストで検索を行います。")]
		public void FindByText(String findText)
		{
			Catch(() =>
			{
				var options = new Dictionary<String, String>() {
					{ "table", AddIn.StatusTableName },
					{ "output_columns", "created_at,user.screen_name,text" },
					{ "query", "text:@" + findText },
					{ "sortby", "-created_at" },
					{ "limit", "20" }
				};
				var query = String.Format("select {0}", AddIn.ParseOptions(options));
				var response = AddIn.Select(query);

				var items = response.Data.Items
					.SelectMany(data => data.Items)
					.Reverse();

				foreach (var item in items)
				{
					var created_at = (DateTime)item["created_at"];
					var screen_name = (String)item["user.screen_name"];
					var text = (String)item["text"];
					AddIn.NotifyMessage(screen_name, String.Format("{0} {1}", created_at.ToString("yyyy/MM/dd HH:mm:ss"), text));
				}
			});
		}

		private void Catch(Action action)
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				AddIn.NotifyMessage(ex.Message);
				AddIn.NotifyMessage(ex.StackTrace);
			}
		}

		private String Replace(String format, Dictionary<String, Object> items)
		{
			foreach (var item in items)
			{
				var placeholder = "${" + item.Key + "}";
				format.Replace(placeholder, item.Value.ToString());
			}

			return format;
		}
	}

	public class GroongaLoggerConfigration : IConfiguration
	{
		public Boolean Enabled { get; set; }
		public String Host { get; set; }
		public Int32 Port { get; set; }

		public GroongaLoggerConfigration()
		{
			Enabled = false;
			Host = "localhost";
			Port = 10041;
		}
	}

	public class GroongaLoggerAddIn : AddInBase
	{
		// テーブル名はとりあえずこれ+ユーザIDで固定
		public const String DefaultUserTableName = "TwitterUser";
		public const String DefaultStatusTableName = "TwitterStatus";
		public const String DefaultTermTableName = "TwitterTerm";

		public GroongaLoggerConfigration Config { get; set; }
		public String UserTableName { get; set; }
		public String StatusTableName { get; set; }
		public String TermTableName { get; set; }

		private Thread _thread = null;
		private EventWaitHandle _threadEvent = null;
		private Boolean _isThreadRunning = false;
		private Object _setupSync = new Object();
		private Queue<Status> _threadQueue = new Queue<Status>();
		private TypableMapCommandProcessor _typableMapCommands = null;

		public override void Initialize()
		{
			base.Initialize();

			CurrentSession.PreProcessTimelineStatus += new EventHandler<TimelineStatusEventArgs>(CurrentSession_PreProcessTimelineStatus);
			CurrentSession.AddInsLoadCompleted += (sender, e) =>
			{
				// コンテキストを登録
				CurrentSession.AddInManager.GetAddIn<ConsoleAddIn>().RegisterContext<GroongaLoggerContext>();

				// TypableMap
				var typableMapSupport = CurrentSession.AddInManager.GetAddIn<TypableMapSupport>();
				if (typableMapSupport != null)
					_typableMapCommands = typableMapSupport.TypableMapCommands;

				// ユニークなテーブル名を設定
				UserTableName = ToUniqueTableName(DefaultUserTableName);
				StatusTableName = ToUniqueTableName(DefaultStatusTableName);
				TermTableName = ToUniqueTableName(DefaultTermTableName);

				// 設定を読み込む
				Config = CurrentSession.AddInManager.GetConfig<GroongaLoggerConfigration>();
				Setup(Config.Enabled);				
			};
		}

		public override void Uninitialize()
		{
			Setup(false);
			base.Uninitialize();
		}

		private void CurrentSession_PreProcessTimelineStatus(object sender, TimelineStatusEventArgs e)
		{
			Enqueue(e.Status);
		}

		/// <summary>
		/// 設定を保存します。
		/// </summary>
		public void SaveConfig()
		{
			CurrentSession.AddInManager.SaveConfig(Config);
		}

		/// <summary>
		/// コンソールにメッセージを送信する。
		/// </summary>
		public void NotifyMessage(String message)
		{
			var console = CurrentSession.AddInManager.GetAddIn<ConsoleAddIn>();
			console.NotifyMessage(message);
		}

		/// <summary>
		/// コンソールにメッセージを送信する。
		/// </summary>
		public void NotifyMessage(String senderNick, String message)
		{
			var console = CurrentSession.AddInManager.GetAddIn<ConsoleAddIn>();
			console.NotifyMessage(senderNick, message);
		}

		#region Logging
		/// <summary>
		/// ワーカースレッドのセットアップを行います。
		/// </summary>
		/// <param name="isStart">開始するか否か</param>
		public void Setup(Boolean isStart)
		{
			lock (_setupSync)
			{
				// stop
				if (_isThreadRunning)
				{
					_threadEvent.Set();
					_thread.Join();
				}

				// cleanup
				if (_threadEvent != null)
				{
					_threadEvent.Close();
					_threadEvent = null;
				}
				if (_thread != null)
				{
					_thread = null;
				}

				// setup
				if (isStart)
				{
					_threadEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
					_thread = new Thread(LoggingThread);
					_thread.Start();
					_isThreadRunning = true;
				}
			}
		}

		/// <summary>
		/// キューに追加します。
		/// </summary>
		/// <param name="status">ステータス</param>
		public void Enqueue(Status status)
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
		/// コンテキストを作成し、指定したアクションを実行します。
		/// </summary>
		public void CreateContext(Action<GroongaContext> action)
		{
			CreateContext(context =>
			{
				action(context);
				return 0;
			});
		}

		/// <summary>
		/// コンテキストを作成し、指定したファンクションを実行します。
		/// </summary>
		public TResult CreateContext<TResult>(Func<GroongaContext, TResult> func)
		{
			using (GroongaContext context = new GroongaContext())
			{
				context.Connect(Config.Host, Config.Port);
				return func(context);
			}
		}

		/// <summary>
		/// ログ取りスレッド
		/// </summary>
		private void LoggingThread()
		{
			try
			{
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
			catch (Exception ex)
			{
				NotifyMessage(ex.Message);
				NotifyMessage(ex.StackTrace);
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
			var statusList = statuses.ToList();
			if (statusList.Count > 0)
			{
				var usersJson = String.Join(",", statusList
					.Select(s => s.User)
					.Where(u => u != null)
					.Select(u => JsonUtility.Serialize((GroongaLoggerUser)u))
					.ToArray());

				var statusesJson = String.Join(",", statusList
					.Select(s => JsonUtility.Serialize((GroongaLoggerStatus)s))
					.ToArray());

				CreateContext(context =>
				{
					Execute(context, "load --table {0}", UserTableName);
					Execute(context, "[" + usersJson + "]");
					Execute(context, "load --table {0}", StatusTableName);
					Execute(context, "[" + statusesJson + "]");
				});
			}
		}
		#endregion

		#region Query
		/// <summary>
		/// Groonga のクエリを実行します。
		/// </summary>
		/// <param name="context">コンテキスト</param>
		/// <param name="str">クエリ</param>
		/// <returns>結果</returns>
		public String Execute(GroongaContext context, String query)
		{
			context.Send(query);
			return context.Receive();
		}

		/// <summary>
		/// Groonga のクエリを実行します。
		/// </summary>
		/// <param name="context">コンテキスト</param>
		/// <param name="format">クエリのフォーマット</param>
		/// <param name="args">可変長引数</param>
		/// <returns>結果</returns>
		public String Execute(GroongaContext context, String format, params Object[] args)
		{
			return Execute(context, String.Format(format, args));
		}

		/// <summary>
		/// Select クエリを実行し、結果を返します。
		/// </summary>
		/// <param name="query">クエリ</param>
		/// <returns>レスポンス</returns>
		public GroongaResponse<GroongaResponseDataList> Select(String query)
		{
			return CreateContext(context =>
			{
				var result = Execute(context, query);
				if (!String.IsNullOrEmpty(result))
				{
					var response = new GroongaResponse<GroongaResponseDataList>();
					response.Parse(JsonUtility.Parse(result));
					return response;
				}

				return null;
			});
		}
		#endregion

		#region Table
		/// <summary>
		/// テーブルを初期化します。
		/// </summary>
		private void InitializeTables()
		{
			CreateContext(context =>
			{
				var tables = new Type[] {
					typeof(GroongaLoggerUser),
					typeof(GroongaLoggerStatus),
					typeof(GroongaLoggerTerm)
				};

				CreateTables(context, tables);
			});
		}

		/// <summary>
		/// テーブルを作成します。
		/// </summary>
		/// <param name="tables"></param>
		private void CreateTables(GroongaContext context, IEnumerable<Type> tables)
		{
			var tableNames = GetTableNames(context);

			foreach (var table in tables)
			{
				var attribute = GetCustomAttribute<GroongaLoggerTableAttribute>(table);
				if (attribute != null)
				{
					// ユーザー固有のテーブル名に変換
					attribute.Name = ToUniqueTableName(attribute.Name);

					if (!tableNames.Contains(attribute.Name))
					{
						CreateTable(context, attribute);
						CreateColumns(context, attribute.Name, table
							.GetMembers(BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance)
							.Where(mi => mi.MemberType == MemberTypes.Field || mi.MemberType == MemberTypes.Property));
					}
				}
			}
		}

		/// <summary>
		/// カラムを作成します。
		/// </summary>
		private void CreateColumns(GroongaContext context, String tableName, IEnumerable<MemberInfo> columns)
		{
			var columnNames = GetColumnNames(context, tableName);

			foreach (var column in columns)
			{
				var attribute = GetCustomAttribute<GroongaLoggerColumnAttribute>(column);
				if (attribute != null)
				{
					if (!columnNames.Contains(attribute.Name))
					{
						// ユーザー固有のテーブル名に変換
						attribute.Table = tableName;
						attribute.Type = ToUniqueTableName(attribute.Type);
						CreateColumn(context, attribute);
					}
				}
			}
		}

		/// <summary>
		/// 属性からテーブルを作成します。
		/// </summary>
		/// <param name="attribute">テーブルの属性</param>
		private void CreateTable(GroongaContext context, GroongaLoggerTableAttribute attribute)
		{
			var options = new Dictionary<String, String>() {
				{ "name", attribute.Name },
				{ "flags", attribute.Flags },
				{ "key_type", attribute.KeyType },
				{ "value_type", attribute.ValueType },
				{ "default_tokenizer", attribute.DefaultTokenizer }
			};

			var result = Execute(context, "table_create {0}", ParseOptions(options));
			if (String.IsNullOrEmpty(result))
				throw new Exception(String.Format("テーブル {0} の作成に失敗しました。", attribute.Name));
		}

		/// <summary>
		/// 属性からカラムを作成します。
		/// </summary>
		/// <param name="attribute">カラムの属性</param>
		private void CreateColumn(GroongaContext context, GroongaLoggerColumnAttribute attribute)
		{
			var options = new Dictionary<String, String>() {
				{ "table", attribute.Table },
				{ "name", attribute.Name },
				{ "flags", attribute.Flags },
				{ "type", attribute.Type },
				{ "source", attribute.Source }
			};

			var result = Execute(context, "column_create {0}", ParseOptions(options));
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
		private IEnumerable<String> GetTableNames(GroongaContext context)
		{
			var json = Execute(context, "table_list");
			var response = new GroongaResponse<GroongaResponseData>();
			response.Parse(JsonUtility.Parse(json));
			foreach (var item in response.Data.Items)
				yield return (String)item["name"];
		}

		/// <summary>
		/// Groonga のカラム名の一覧を取得します。
		/// </summary>
		/// <param name="tableName">テーブル名</param>
		/// <returns>カラム名の一覧</returns>
		private IEnumerable<String> GetColumnNames(GroongaContext context, String tableName)
		{
			var json = Execute(context, "table_list --table {0}", tableName);
			var response = new GroongaResponse<GroongaResponseData>();
			response.Parse(JsonUtility.Parse(json));
			foreach (var item in response.Data.Items)
				yield return (String)item["name"];
		}

		/// <summary>
		/// 辞書を Groonga のオプションに変換します。
		/// </summary>
		public String ParseOptions(Dictionary<String, String> options)
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
