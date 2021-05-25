using System;
using System.Threading;
using System.Runtime.Remoting.Messaging;

namespace Common.Logging.Scopes
{
	// XXX: Originally taken from NestedDiagnosticsLogicalContext.cs (NLog).
	internal static class DummyNestedDiagnosticsLogicalContext
	{
		#region Inner Types

		private interface INestedContext : IDisposable
		{
			INestedContext Parent { get; }

			int FrameLevel { get; }
			string Value { get; }
		}

		[Serializable]
		private class NestedContext : INestedContext
		{
			#region Properties & Fields

			public INestedContext Parent { get; }
			public int FrameLevel { get; }
			public string Value { get; }

			private int _disposed;

			#endregion

			public static INestedContext CreateNestedContext(INestedContext parent, string value)
			{
				return new NestedContext(parent, value);
			}

			public NestedContext(INestedContext parent, string value)
			{
				Value = value;
				Parent = parent;
				FrameLevel = parent?.FrameLevel + 1 ?? 1;
			}

			public override string ToString()
			{
				return Value;
			}

			public void Dispose()
			{
				if (Interlocked.Exchange(ref _disposed, 1) != 1)
					PopString();
			}
		}

		#endregion

		#region Fields

		private const string NestedDiagnosticsContextKey = "C";

		#endregion

		#region Private methods

		private static void SetThreadLocal(INestedContext newValue)
		{
			if (newValue == null)
				CallContext.FreeNamedDataSlot(NestedDiagnosticsContextKey);
			else
				CallContext.LogicalSetData(NestedDiagnosticsContextKey, newValue);
		}

		private static INestedContext GetThreadLocal()
		{
			return CallContext.LogicalGetData(NestedDiagnosticsContextKey) as INestedContext;
		}

		#endregion

		#region Public methods

		public static IDisposable Push(string value)
		{
			var parent = GetThreadLocal();
			var current = NestedContext.CreateNestedContext(parent, value);

			SetThreadLocal(current);

			return current;
		}

		public static string[] GetAllMessages()
		{
			var currentContext = GetThreadLocal();

			if (currentContext == null)
				return new string[] { };

			var index = 0;
			var messages = new string[currentContext.FrameLevel];

			while (currentContext != null)
			{
				messages[index++] = currentContext.Value;
				currentContext = currentContext.Parent;
			}

			return messages;
		}

		public static string PopString()
		{
			var current = GetThreadLocal();

			if (current != null)
				SetThreadLocal(current.Parent);

			return current?.Value;
		}

		public static string Peek()
		{
			return GetThreadLocal()?.Value;
		}

		public static string Pop()
		{
			return PopString() ?? string.Empty;
		}

		public static void Clear()
		{
			SetThreadLocal(null);
		}

		#endregion
	}
}