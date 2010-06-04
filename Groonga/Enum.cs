using System;

namespace Spica.Data.Groonga
{
	public enum GroongaResultCode : int
	{
		Success = 0,
		EndOfData = 1,
		UnknownError = -1,
		OperationNotPermitted = -2,
		NoSuchFileOrDirectory = -3,
		NoSuchProcess = -4,
		InterruptedFunctionCall = -5,
		InputOutputError = -6,
		NoSuchDeviceOrAddress = -7,
		ArgListTooLong = -8,
		ExecFormatError = -9,
		BadFileDescriptor = -10,
		NoChildProcesses = -11,
		ResourceTemporarilyUnavailable = -12,
		NotEnoughSpace = -13,
		PermissionDenied = -14,
		BadAddress = -15,
		ResourceBusy = -16,
		FileExists = -17,
		ImproperLink = -18,
		NoSuchDevice = -19,
		NotADirectory = -20,
		IsADirectory = -21,
		InvalidArgument = -22,
		TooManyOpenFilesInSystem = -23,
		TooManyOpenFiles = -24,
		InappropriateIOControlOperation = -25,
		FileTooLarge = -26,
		NoSpaceLeftOnDevice = -27,
		InvalidSeek = -28,
		ReadOnlyFileSystem = -29,
		TooManyLinks = -30,
		BrokenPipe = -31,
		DomainError = -32,
		ResultTooLarge = -33,
		ResourceDeadlockAvoided = -34,
		NoMemoryAvailable = -35,
		FilenameTooLong = -36,
		NoLocksAvailable = -37,
		FunctionNotImplemented = -38,
		DirectoryNotEmpty = -39,
		IllegalByteSequence = -40,
		SocketNotInitialized = -41,
		OperationWouldBlock = -42,
		AddressIsNotAvailable = -43,
		NetworkIsDown = -44,
		NoBuffer = -45,
		SocketIsAlreadyConnected = -46,
		SocketIsNotConnected = -47,
		SocketIsAlreadyShutdowned = -48,
		OperationTimeout = -49,
		ConnectionRefused = -50,
		RangeError = -51,
		TokenizerError = -52,
		FileCorrupt = -53,
		InvalidFormat = -54,
		ObjectCorrupt = -55,
		TooManySymbolicLinks = -56,
		NotSocket = -57,
		OperationNotSupported = -58,
		AddressIsInUse = -59,
		ZlibError = -60,
		LzoError = -61,
		StackOverFlow = -62,
		SyntaxError = -63,
		RetryMax = -64,
		IncompatibleFileFormat = -65,
		UpdateNotAllowed = -66,
	}

	public enum GroongaEncoding : int
	{
		Default = 0,
		None,
		EUC_JP,
		UTF8,
		SJIS,
		LATIN1,
		KOI8R
	}

	public enum GroongaLogLevel : int
	{
		None = 0,
		Emergency,
		Alert,
		Critical,
		Error,
		Warning,
		Notice,
		Info,
		Debug,
		Dump,
	}

	public enum GroongaContentType : int
	{
		None = 0,
		TSV,
		Json,
		Xml,
	}

	public enum GroongaContextFlags : int
	{
		None = 0x00,
		UseQL = 0x03,
		BatchMode = 0x04,
	}
}

