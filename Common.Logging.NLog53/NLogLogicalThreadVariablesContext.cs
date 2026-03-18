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

using System.Collections.Immutable;

using NLog;

namespace Common.Logging.NLog53
{
	public sealed class NLogLogicalThreadVariablesContext : IVariablesContext
	{
		private static readonly AsyncLocal<ImmutableDictionary<string, ImmutableStack<PropertyScopeEntry>>> _store = new();

		private static ImmutableDictionary<string, ImmutableStack<PropertyScopeEntry>> State
		{
			get => _store.Value ?? ImmutableDictionary<string, ImmutableStack<PropertyScopeEntry>>.Empty;
			set => _store.Value = value;
		}

		public void Set(string key, object value)
		{
			ArgumentNullException.ThrowIfNull(key);

			var state = State;
			state.TryGetValue(key, out var stack);
			stack ??= [];

			var handle = ScopeContext.PushProperty(key, value);
			State = state.SetItem(key, stack.Push(new PropertyScopeEntry(value, handle)));
		}

		public object Get(string key)
		{
			if (key == null)
				return null;

			var state = State;
			if (state.TryGetValue(key, out var stack) && !stack.IsEmpty)
				return stack.Peek().Value;

			return null;
		}

		public bool Contains(string key)
		{
			var state = State;
			return key != null && state.TryGetValue(key, out var stack) && !stack.IsEmpty;
		}

		public void Remove(string key)
		{
			var state = State;
			if (key == null || !state.TryGetValue(key, out var stack) || stack.IsEmpty)
				return;

			var entry = stack.Peek();
			entry.Scope.Dispose();
			stack = stack.Pop();

			State = stack.IsEmpty
				? state.Remove(key)
				: state.SetItem(key, stack);
		}

		public void Clear()
		{
			var state = State;
			foreach (var stack in state.Values)
			{
				foreach (var entry in stack)
					entry.Scope.Dispose();
			}

			State = ImmutableDictionary<string, ImmutableStack<PropertyScopeEntry>>.Empty;
		}

		private readonly record struct PropertyScopeEntry(object Value, IDisposable Scope);
	}
}
