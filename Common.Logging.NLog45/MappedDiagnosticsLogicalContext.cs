using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

namespace Common.Logging.NLog45
{
	// XXX: Originally taken from MappedDiagnosticsLogicalContext.cs (NLog).
	public static class MappedDiagnosticsLogicalContext
	{
		#region Inner Types

		private sealed class ItemRemover : IDisposable
		{
			#region Fields

#if !NET3_5 && !NET4_0
			// Optimized for HostingLogScope with 3 properties
			private readonly string _item1;
			private readonly string _item2;
			private readonly string[] _itemArray;
#endif

			private readonly string _item0;
			private readonly bool _wasEmpty;

			private int _disposed; //< Bool as int to allow the use of Interlocked.Exchange..

			#endregion

			#region .ctors

			public ItemRemover(string item, bool wasEmpty)
			{
				_item0 = item;
				_wasEmpty = wasEmpty;
			}

#if !NET3_5 && !NET4_0
			public ItemRemover(IReadOnlyList<KeyValuePair<string, object>> items, bool wasEmpty)
			{
				var itemCount = items.Count;

				if (itemCount > 2)
				{
					_item0 = items[0].Key;
					_item1 = items[1].Key;
					_item2 = items[2].Key;

					for (var i = 3; i < itemCount; ++i)
					{
						_itemArray = _itemArray ?? new string[itemCount - 3];
						_itemArray[i - 3] = items[i].Key;
					}
				}
				else if (itemCount > 1)
				{
					_item0 = items[0].Key;
					_item1 = items[1].Key;
				}
				else
				{
					_item0 = items[0].Key;
				}

				_wasEmpty = wasEmpty;
			}
#endif
			#endregion

			#region Private methods

			private bool RemoveScopeWillClearContext()
			{
#if !NET3_5 && !NET4_0
				if (_itemArray == null)
				{
					var immutableDict = GetLogicalThreadDictionary(false);

					switch (immutableDict.Count)
					{
						case 1:
							if (_item0 is null && immutableDict.ContainsKey(_item0))
								return true;
							break;

						case 2:
							if (!(_item1 is null) && _item2 is null && immutableDict.ContainsKey(_item0) && immutableDict.ContainsKey(_item1) && !_item0.Equals(_item1))
								return true;
							break;

						case 3:
							if (!(_item2 is null) && immutableDict.ContainsKey(_item0) && immutableDict.ContainsKey(_item1) && immutableDict.ContainsKey(_item2)
								&& !_item0.Equals(_item1) && !_item0.Equals(_item2) && !_item1.Equals(_item2))
							{
								return true;
							}
							break;
					}
				}
#else
				var immutableDict = GetLogicalThreadDictionary(false);

				if (immutableDict.Count == 1 && immutableDict.ContainsKey(_item))
					return true;
#endif
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
					dictionary.Remove(_item0);

#if !NET3_5 && !NET4_0
					if (_item1 != null)
					{
						dictionary.Remove(_item1);

						if (_item2 != null)
							dictionary.Remove(_item2);

						if (_itemArray != null)
						{
							for (var i = 0; i < _itemArray.Length; ++i)
							{
								if (_itemArray[i] != null)
									dictionary.Remove(_itemArray[i]);
							}
						}
					}
#endif
				}
			}

			public override string ToString()
			{
				return _item0?.ToString() ?? base.ToString();
			}
		}

		#endregion

		#region Fields

#if NET46_OR_GREATER
		private static readonly AsyncLocal<Dictionary<string, object>> AsyncLocalDictionary = new AsyncLocal<Dictionary<string, object>>();
#else
		private const string LogicalThreadDictionaryKey = "Common.Logging.Scopes.DummyLogicalVariablesContext";
#endif

		private static readonly IDictionary<string, object> EmptyDefaultDictionary = new Dictionary<string, object>();

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
#if NET46_OR_GREATER
			AsyncLocalDictionary.Value = newValue;
#else
			if (newValue == null)
				CallContext.FreeNamedDataSlot(LogicalThreadDictionaryKey);
			else
				CallContext.LogicalSetData(LogicalThreadDictionaryKey, newValue);
#endif
		}

		private static Dictionary<string, object> GetThreadLocal()
		{
#if NET46_OR_GREATER
			return AsyncLocalDictionary.Value;
#else
			return CallContext.LogicalGetData(LogicalThreadDictionaryKey) as Dictionary<string, object>;
#endif
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