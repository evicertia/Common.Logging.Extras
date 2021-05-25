using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

namespace Common.Logging.Scopes
{
	// XXX: Originally taken from MappedDiagnosticsLogicalContext.cs (NLog).
	internal static class DummyMappedDiagnosticsLogicalContext
	{
		#region Inner Types

		private sealed class ItemRemover : IDisposable
		{
			#region Fields

			private readonly string _item;
			private readonly bool _wasEmpty;

			private int _disposed; //< Bool as int to allow the use of Interlocked.Exchange..

			#endregion

			#region .ctors

			public ItemRemover(string item, bool wasEmpty)
			{
				_item = item;
				_wasEmpty = wasEmpty;
			}

			#endregion

			#region Private methods

			private bool RemoveScopeWillClearContext()
			{
				var immutableDict = GetLogicalThreadDictionary(false);

				if (immutableDict.Count == 1 && immutableDict.ContainsKey(_item))
					return true;

				return false;
			}

			#endregion

			public void Dispose()
			{
				if (Interlocked.Exchange(ref _disposed, 1) == 0)
				{
					if (_wasEmpty && RemoveScopeWillClearContext())
					{
						Clear();
						return;
					}

					var dictionary = GetLogicalThreadDictionary(true);
					dictionary.Remove(_item);
				}
			}

			public override string ToString()
			{
				return _item?.ToString() ?? base.ToString();
			}
		}

		#endregion

		#region Fields

		private static readonly IDictionary<string, object> EmptyDefaultDictionary = new Dictionary<string, object>();

		private const string LogicalThreadDictionaryKey = "Common.Logging.Scopes.DummyLogicalVariablesContext";

		#endregion

		#region Private methods

		private static IDictionary<string, object> GetLogicalThreadDictionary(bool clone = false, int initialCapacity = 0)
		{
			var dictionary = GetThreadLocal();

			if (dictionary == null)
			{
				if (!clone)
					return EmptyDefaultDictionary;

				dictionary = new Dictionary<string, object>(initialCapacity);

				SetThreadLocal(dictionary);
			}
			else if (clone)
			{
				var newDictionary = new Dictionary<string, object>(dictionary.Count + initialCapacity);

				foreach (var keyValue in dictionary)
					newDictionary[keyValue.Key] = keyValue.Value;

				SetThreadLocal(newDictionary);

				return newDictionary;
			}

			return dictionary;
		}

		private static void SetThreadLocal(Dictionary<string, object> newValue)
		{
			if (newValue == null)
				CallContext.FreeNamedDataSlot(LogicalThreadDictionaryKey);
			else
				CallContext.LogicalSetData(LogicalThreadDictionaryKey, newValue);
		}

		private static Dictionary<string, object> GetThreadLocal()
		{
			return CallContext.LogicalGetData(LogicalThreadDictionaryKey) as Dictionary<string, object>;
		}

		#endregion

		#region Public methods

		public static void Set(string item, object value)
		{
			var logicalContext = GetLogicalThreadDictionary(true, 1);
			logicalContext[item] = value;
		}

		public static object GetObject(string item)
		{
			GetLogicalThreadDictionary().TryGetValue(item, out var value);

			return value;
		}

		public static bool Contains(string item)
		{
			return GetLogicalThreadDictionary().ContainsKey(item);
		}

		public static void Remove(string item)
		{
			GetLogicalThreadDictionary(true).Remove(item);
		}

		public static void Clear()
		{
			SetThreadLocal(null);
		}

		#endregion
	}
}