using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Spica.Data.Groonga
{
	public class GroongaException : Exception
	{
		public GroongaResultCode Result { get; private set; }

		public GroongaException(GroongaResultCode result)
		{
			Result = result;
		}

		public GroongaException(GroongaResultCode result, String message)
			: base(message)
		{
			Result = result;
		}

		public GroongaException(GroongaResultCode result, String message, Exception inner)
			: base(message, inner)
		{
			Result = result;
		}
	}

	public class GroongaContext : IDisposable
	{
		private static readonly Library _library = new Library();
		private class Library
		{
			private Boolean _initialized = false;

			~Library()
			{
				Uninitialize();
			}

			public void Initialize()
			{
				if (!_initialized)
				{
					GroongaApi.grn_init();
					_initialized = true;
				}
			}

			public void Uninitialize()
			{
				if (_initialized)
				{
					GroongaApi.grn_fin();
					_initialized = false;
				}
			}
		}

		private GroongaApi.grn_ctx _context;
		private Boolean _disposed = false;

		public GroongaContext()
			: this(GroongaContextFlags.None)
		{
		}

		public GroongaContext(GroongaContextFlags flags)
		{
			// ライブラリの初期化(初回のみ)
			_library.Initialize();

			// コンテキストの初期化
			GroongaResultCode result = GroongaApi.grn_ctx_init(out _context, flags);
			if (result != GroongaResultCode.Success)
				throw new GroongaException(result, "grn_ctx_init() faield");
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				GroongaApi.grn_ctx_fin(ref _context);
				_disposed = true;
			}
		}

		public void Connect(String host, Int32 port)
		{
			GroongaResultCode result = GroongaApi.grn_ctx_connect(ref _context, host, port, 0);
			if (result != GroongaResultCode.Success)
				throw new GroongaException(result, "grn_ctx_connect() faield");
		}

		public void Send(String str)
		{
			Send(str, 0);
		}

		public void Send(String str, Int32 flags)
		{
			UInt32 length = (UInt32)Encoding.UTF8.GetByteCount(str);
			GroongaApi.grn_ctx_send(ref _context, str, length, flags);
			if (_context.rc != GroongaResultCode.Success)
				throw new GroongaException(_context.rc, "grn_ctx_send() faield");
		}

		public String Receive()
		{
			StringBuilder sb = new StringBuilder();
			IntPtr str;
			UInt32 str_len;
			Int32 flags;

			do
			{
				GroongaApi.grn_ctx_recv(ref _context, out str, out str_len, out flags);
				if (_context.rc != GroongaResultCode.Success)
					throw new GroongaException(_context.rc, "grn_ctx_recv() faield");
				sb.Append(Marshal.PtrToStringAnsi(str, (Int32)str_len));
			} while ((flags & GroongaApi.GRN_CTX_MORE) != 0);

			return sb.ToString();
		}
	}
}
