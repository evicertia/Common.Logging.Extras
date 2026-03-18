#region License

/*
 * Copyright © 2002-2009 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

// Originally taken from: https://github.com/net-commons/common-logging/pull/176

using NLog;

namespace Common.Logging.NLog53
{
	public sealed class NLogThreadVariablesContext : IVariablesContext
	{
		[ThreadStatic]
		private static Dictionary<string, Stack<PropertyScopeEntry>> _store;

		public void Set(string key, object value)
		{
			ArgumentNullException.ThrowIfNull(key);

			var stack = GetOrCreate(key);
			var handle = ScopeContext.PushProperty(key, value);
			stack.Push(new PropertyScopeEntry(value, handle));
		}

		public object Get(string key)
		{
			if (key == null)
				return null;

			if (_store != null && _store.TryGetValue(key, out var stack) && stack.Count > 0)
				return stack.Peek().Value;

			return null;
		}

		public bool Contains(string key) =>
			key != null
			&& _store != null
			&& _store.TryGetValue(key, out var stack)
			&& stack.Count > 0;

		public void Remove(string key)
		{
			if (!Contains(key))
				return;

			var stack = _store[key];
			var entry = stack.Pop();
			entry.Scope.Dispose();

			if (stack.Count == 0)
				_store.Remove(key);
		}

		public void Clear()
		{
			if (_store == null)
				return;

			foreach (var stack in _store.Values)
			{
				while (stack.Count > 0)
					stack.Pop().Scope.Dispose();
			}

			_store.Clear();
		}

		private static Stack<PropertyScopeEntry> GetOrCreate(string key)
		{
			_store ??= new Dictionary<string, Stack<PropertyScopeEntry>>(StringComparer.Ordinal);

			if (!_store.TryGetValue(key, out var stack))
			{
				stack = new Stack<PropertyScopeEntry>();
				_store[key] = stack;
			}

			return stack;
		}

		private readonly record struct PropertyScopeEntry(object Value, IDisposable Scope);
	}
}
