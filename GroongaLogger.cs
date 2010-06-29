using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
			if (config is GroongaLoggerConfiguration)
			{
				AddIn.Config = config as GroongaLoggerConfiguration;
				AddIn.SaveConfig();

				if (memberInfo.Name == "ChannelName" && !String.IsNullOrEmpty(AddIn.Config.ChannelName))
					AddIn.AttachConsole();
			}
		}

		/// <summary>
		/// コンソールでコマンドを解釈する前に実行する処理
		/// </summary>
		[Browsable(false)]
		public override Boolean OnPreProcessInput(String inputLine)
		{
			if (CurrentSession.Config.EnableTypableMap && AddIn.TypableMapCommands != null)
			{
				// コンテキスト名を求める
				StringBuilder sb = new StringBuilder();
				foreach (Context ctx in Console.ContextStack) sb.Insert(0, ctx.ContextName.Replace("Context", "") + "\\");
				sb.Append(Console.CurrentContext.ContextName.Replace("Context", ""));

				// PRIVを作成
				PrivMsgMessage priv = new PrivMsgMessage(Console.ConsoleChannelName, inputLine);
				priv.SenderNick = sb.ToString();
				priv.SenderHost = "twitter@" + Server.ServerName;

				// TypableMapCommandProcessorで処理
				if (AddIn.TypableMapCommands.Process(priv))
				{
					return true;
				}
			}

			return false;
		}


		[Description("ロギングを有効にします")]
		public void Enable()
		{
			ErrorHandler(() =>
			{
				AddIn.Config.Enabled = true;
				AddIn.SaveConfig();
				AddIn.Setup(AddIn.Config.Enabled);
				Console.NotifyMessage("ロギングを有効にしました。");
			});
		}

		[Description("ロギングを無効にします")]
		public void Disable()
		{
			ErrorHandler(() =>
			{
				AddIn.Config.Enabled = false;
				AddIn.SaveConfig();
				AddIn.Setup(AddIn.Config.Enabled);
				Console.NotifyMessage("ロギングを無効にしました。");
			});
		}

		[Description("クエリで検索を行います")]
		public void Find(String query)
		{
			ErrorHandler(() =>
			{
				var options = CreateOptions(query);
				FindInternal(options, true);
			});
		}

		[Description("ユーザ名で検索を行います")]
		public void FindByScreenName(String screenName)
		{
			ErrorHandler(() =>
			{
				if (!String.IsNullOrEmpty(screenName))
				{
					var options = CreateOptions("user.screen_name:" + screenName);
					FindInternal(options, true);
				}
				else
				{
					throw new ArgumentException("ユーザ名に空の文字列は指定できません。", "screenName");
				}
			});
		}

		[Description("テキストで検索を行います")]
		public void FindByText(String text)
		{
			ErrorHandler(() =>
			{
				if (!String.IsNullOrEmpty(text))
				{
					// 条件式が指定されてなければデフォルトで全文検索
					if (!Regex.IsMatch(text, @"^(!|<=?|>=?|@|\^|\$)"))
						text = "@" + text;

					var options = CreateOptions("text:" + text);
					FindInternal(options, true);
				}
				else
				{
					throw new ArgumentException("テキストに空の文字列は指定できません。", "text");
				}
			});
		}

		[Description("次のページの検索を行ないます")]
		public void Next(String limit)
		{
			ErrorHandler(() =>
			{
				Int32? numLimit = null;
				if (!String.IsNullOrEmpty(limit))
				{
					Int32 tmp;
					if (!Int32.TryParse(limit, out tmp))
						throw new ArgumentException("正しい数値を指定してください。", "limit");
					else
						numLimit = tmp;
				}

				if (AddIn.State.Next(numLimit ?? AddIn.Config.Limit))
					FindInternal(AddIn.State.Options, false);
				else
					throw new InvalidOperationException("次のページは存在しません。");
			});
		}

		[Description("前のページの検索を行ないます")]
		public void Previous(String limit)
		{
			ErrorHandler(() =>
			{
				Int32? numLimit = null;
				if (!String.IsNullOrEmpty(limit))
				{
					Int32 tmp;
					if (!Int32.TryParse(limit, out tmp))
						throw new ArgumentException("正しい数値を指定してください。", "limit");
					else
						numLimit = tmp;
				}

				if (AddIn.State.Previous(numLimit ?? AddIn.Config.Limit))
					FindInternal(AddIn.State.Options, false);
				else
					throw new InvalidOperationException("前のページは存在しません。");
			});
		}

		private void ErrorHandler(Action action)
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				Console.NotifyMessage(ex.Message);
#if DEBUG
				Console.NotifyMessage(ex.StackTrace);
#endif
			}
		}

		private GroongaLoggerCommandOptions CreateOptions(String query)
		{
			var columnNames = GroongaLoggerUtility.GetDataMemberNames(typeof(GroongaLoggerStatus));
			var output_columns = String.Join(",", columnNames.ToArray());

			return new GroongaLoggerCommandOptions() {
				{ "table", AddIn.StatusTableName },
				{ "output_columns", output_columns },
				{ "query", query },
				{ "sortby", "-created_at" },
			};
		}

		private void FindInternal(GroongaLoggerCommandOptions options, Boolean reset)
		{
			var response = AddIn.Select(options, reset);
			var items = response.Data.Items
				.SelectMany(data => data.Items)
				.Reverse();

			foreach (var item in items)
			{
				var status = ToStatus(item);
				var text = AddIn.ApplyTypableMap(status.Text, status);

				Console.NotifyMessage(status.User.ScreenName, String.Format("{0} {1}",
					status.CreatedAt.ToString("yyyy/MM/dd HH:mm:ss"),
					text));
			}

			Console.NotifyMessage(String.Format(
				"{0:N0} - {1:N0} 件目 / {2:N0} 件 ({3:f2} 秒)",
				AddIn.State.Offset,
				AddIn.State.Offset + AddIn.State.Limit,
				AddIn.State.Total,
				response.Status.ProcessTime));
		}

		private Status ToStatus(Dictionary<String, Object> item)
		{
			return (GroongaLoggerUtility.Parse(typeof(GroongaLoggerStatus), item) as GroongaLoggerStatus).ToStatus();
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

	public class GroongaLoggerConfiguration : IConfiguration
	{
		public Boolean Enabled { get; set; }
		public String Host { get; set; }
		public Int32 Port { get; set; }
		public String ChannelName { get; set; }
		public Int32 Limit { get; set; }

		public GroongaLoggerConfiguration()
		{
			Enabled = false;
			Host = "localhost";
			Port = 10041;
			ChannelName = String.Empty;
			Limit = 10;
		}
	}

	public class GroongaLoggerAddIn : AddInBase
	{
		// テーブル名はとりあえずこれ+ユーザIDで固定
		public const String DefaultUserTableName = "twitter_users";
		public const String DefaultStatusTableName = "twitter_statuses";
		public const String DefaultTermTableName = "twitter_terms";

		public GroongaLoggerConfiguration Config { get; internal set; }
		public String UserTableName { get; private set; }
		public String StatusTableName { get; private set; }
		public String TermTableName { get; private set; }

		public Misuzilla.Applications.TwitterIrcGateway.AddIns.Console.Console Console { get; private set; }
		public GroongaLoggerSelectState State { get; private set; }
		public TypableMapCommandProcessor TypableMapCommands { get; private set; }

		private Thread _thread = null;
		private EventWaitHandle _threadEvent = null;
		private Boolean _isThreadRunning = false;
		private Object _setupSync = new Object();
		private Queue<Status> _threadQueue = new Queue<Status>();

		public GroongaLoggerAddIn()
		{
			Console = new Misuzilla.Applications.TwitterIrcGateway.AddIns.Console.Console();
			State = new GroongaLoggerSelectState();
		}

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
					TypableMapCommands = typableMapSupport.TypableMapCommands;

				// ユニークなテーブル名を設定
				UserTableName = ToUniqueTableName(DefaultUserTableName);
				StatusTableName = ToUniqueTableName(DefaultStatusTableName);
				TermTableName = ToUniqueTableName(DefaultTermTableName);

				// 設定を読み込む
				Config = CurrentSession.AddInManager.GetConfig<GroongaLoggerConfiguration>();
				Setup(Config.Enabled);

				// 独自コンソールにアタッチ
				if (!String.IsNullOrEmpty(Config.ChannelName))
					AttachConsole();
			};
		}

		public override void Uninitialize()
		{
			Setup(false);

			Console.Detach();
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
		/// 独自コンソールにアタッチします
		/// </summary>
		public void AttachConsole()
		{
			DetachConsole();
			Console.Attach(Config.ChannelName, CurrentServer, CurrentSession, typeof(GroongaLoggerContext), true);
		}

		/// <summary>
		/// 独自コンソールからデタッチします
		/// </summary>
		public void DetachConsole()
		{
			if (Console.IsAttached)
				Console.Detach();
		}

		/// <summary>
		/// サーバにエラーメッセージを送信する。
		/// </summary>
		/// <param name="message">メッセージ</param>
		public void SendServerErrorMessage(String message)
		{
			CurrentSession.SendServerErrorMessage("GroongaLogger: " + message);
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
				Retry(10, (success) =>
				{
					// コンテキストを作成
					CreateContext(context =>
					{
						// テーブルを初期化
						InitializeTables(context);

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
							StoreStatuses(context, statuses);

							// 待機
							if (_threadEvent.WaitOne(10 * 1000))
								break;

							success();
						}
					});
				}, (ex) =>
				{
					SendServerErrorMessage(ex.ToString());
					return _threadEvent.WaitOne(10 * 1000);
				});
			}
			catch (Exception ex)
			{
				SendServerErrorMessage(ex.ToString());
			}
			finally
			{
				_isThreadRunning = false;
			}
		}

		/// <summary>
		/// 指定された回数処理をリトライさせます。
		/// </summary>
		/// <param name="count">リトライさせる回数</param>
		/// <param name="action">リトライさせる処理</param>
		/// <param name="error">エラー時の処理</param>
		private void Retry(Int32 count, Action<Action> action, Func<Exception, Boolean> error)
		{
			Int32 retryCount = count;
			Action success = () => { retryCount = count; };

			while (true)
			{
				try
				{
					action(success);
					break;
				}
				catch (Exception ex)
				{
					if (--retryCount <= 0)
						throw ex;

					if (error(ex))
						break;
				}
			}
		}

		/// <summary>
		/// ステータスをデータストアに格納します。
		/// </summary>
		/// <param name="statuses">ステータス</param>
		private void StoreStatuses(GroongaContext context, IEnumerable<Status> statuses)
		{
			var statusList = statuses.ToList();
			if (statusList.Count > 0)
			{
				var usersJson = String.Join(",", statusList
					.Where(s => s.User != null)
					.Select(s => GroongaLoggerUser.FromUser(s.User))
					.Select(u => JsonUtility.Serialize(u))
					.ToArray());

				var statusesJson = String.Join(",", statusList
					.Select(s => GroongaLoggerStatus.FromStatus(s))
					.Select(s => JsonUtility.Serialize(s))
					.ToArray());

				Execute(context, "load --table {0}", UserTableName);
				Execute(context, "[" + usersJson + "]");
				Execute(context, "load --table {0}", StatusTableName);
				Execute(context, "[" + statusesJson + "]");
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
		/// <param name="options">オプション</param>
		/// <param name="reset">状態をリセットするか</param>
		/// <returns>レスポンス</returns>
		public GroongaLoggerResponse<GroongaLoggerResponseDataList> Select(GroongaLoggerCommandOptions options, Boolean reset)
		{
			if (!options.ContainsKey("limit"))
				options["limit"] = Config.Limit.ToString();

			return CreateContext(context =>
			{
				var result = Execute(context, "select {0}", options);
				if (!String.IsNullOrEmpty(result))
				{
					var response = new GroongaLoggerResponse<GroongaLoggerResponseDataList>();
					response.Parse(JsonUtility.Parse(result));

					Int32 limit = Int32.Parse(options["limit"]);
					Int32 total = response.Data.Items.FirstOrDefault().SearchCount ?? 0;
					if (reset)
						State.Reset(options, limit, total);
					else
						State.Update(total);

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
		private void InitializeTables(GroongaContext context)
		{
			var tables = new Type[] {
				typeof(GroongaLoggerUser),
				typeof(GroongaLoggerStatus),
				typeof(GroongaLoggerTerm)
			};

			CreateTables(context, tables);
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

					// テーブルの作成
					if (!tableNames.Contains(attribute.Name))
						CreateTable(context, attribute);

					// カラムの作成
					CreateColumns(context, attribute.Name, table
						.GetMembers(BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance)
						.Where(mi => mi.MemberType == MemberTypes.Field || mi.MemberType == MemberTypes.Property));
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
			var options = new GroongaLoggerCommandOptions() {
				{ "name", attribute.Name },
				{ "flags", attribute.Flags },
				{ "key_type", attribute.KeyType },
				{ "value_type", attribute.ValueType },
				{ "default_tokenizer", attribute.DefaultTokenizer }
			};

			var result = Execute(context, "table_create {0}", options);
			if (String.IsNullOrEmpty(result))
				throw new Exception(String.Format("テーブル {0} の作成に失敗しました。", attribute.Name));
		}

		/// <summary>
		/// 属性からカラムを作成します。
		/// </summary>
		/// <param name="attribute">カラムの属性</param>
		private void CreateColumn(GroongaContext context, GroongaLoggerColumnAttribute attribute)
		{
			var options = new GroongaLoggerCommandOptions() {
				{ "table", attribute.Table },
				{ "name", attribute.Name },
				{ "flags", attribute.Flags },
				{ "type", attribute.Type },
				{ "source", attribute.Source }
			};

			var result = Execute(context, "column_create {0}", options);
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
			var names = new String[] {
				DefaultUserTableName,
				DefaultStatusTableName,
				DefaultTermTableName
			};

			if (names.Any(name => String.Compare(tableName, name) == 0))
				return String.Format("{0}_{1}", tableName, CurrentSession.TwitterUser.Id);

			return tableName;
		}

		/// <summary>
		/// Groonga のテーブル名の一覧を取得します。
		/// </summary>
		/// <returns>テーブル名の一覧</returns>
		private IEnumerable<String> GetTableNames(GroongaContext context)
		{
			var json = Execute(context, "table_list");
			var response = new GroongaLoggerResponse<GroongaLoggerResponseData>();
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
			var response = new GroongaLoggerResponse<GroongaLoggerResponseData>();
			response.Parse(JsonUtility.Parse(json));
			foreach (var item in response.Data.Items)
				yield return (String)item["name"];
		}
		#endregion

		/// <summary>
		/// Twitter の Status に TypableMap をつける
		/// </summary>
		public String ApplyTypableMap(String str, Status status)
		{
			if (CurrentSession.Config.EnableTypableMap)
			{
				if (TypableMapCommands != null)
				{
					String typableMapId = TypableMapCommands.TypableMap.Add(status);

					// TypableMapKeyColorNumber = -1 の場合には色がつかなくなる
					if (CurrentSession.Config.TypableMapKeyColorNumber < 0)
						return str + String.Format(" ({0})", typableMapId);
					else
						return str + String.Format(" \x03{0}({1})\x03", CurrentSession.Config.TypableMapKeyColorNumber, typableMapId);
				}
			}

			return str;
		}
	}
}
