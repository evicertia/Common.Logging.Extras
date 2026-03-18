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
	public sealed class NLogNestedLogicalThreadVariablesContext : INestedVariablesContext
	{
		private static readonly AsyncLocal<ImmutableStack<NestedScopeEntry>> _stack = new();

		private static ImmutableStack<NestedScopeEntry> State
		{
			get => _stack.Value ?? [];
			set => _stack.Value = value;
		}

		public IDisposable Push(string text)
		{
			var scope = ScopeContext.PushNestedState(text);
			var entry = new NestedScopeEntry(text, scope);
			State = State.Push(entry);

			return new PopHandle(() => Remove(entry.Scope));
		}

		public string Pop()
		{
			var state = State;
			if (state.IsEmpty)
				return null;

			var entry = state.Peek();
			entry.Scope.Dispose();
			State = state.Pop();
			return entry.Value;
		}

		public void Clear()
		{
			foreach (var entry in State)
				entry.Scope.Dispose();

			State = [];
		}

		public bool HasItems => !State.IsEmpty;

		private static void Remove(IDisposable scope)
		{
			var state = State;
			if (state.IsEmpty)
				return;

			NestedScopeEntry? removed = null;
			var remaining = state.Where(x => !ReferenceEquals(x.Scope, scope)).ToArray();
			if (remaining.Length == state.Count())
				return;

			foreach (var current in state)
			{
				if (ReferenceEquals(current.Scope, scope))
				{
					removed = current;
					break;
				}
			}

			if (removed.HasValue)
				removed.Value.Scope.Dispose();

			var rebuilt = ImmutableStack<NestedScopeEntry>.Empty;
			for (var i = remaining.Length - 1; i >= 0; i--)
				rebuilt = rebuilt.Push(remaining[i]);

			State = rebuilt;
		}

		private readonly record struct NestedScopeEntry(string Value, IDisposable Scope);

		private sealed class PopHandle(Action popAction) : IDisposable
		{
			private Action _popAction = popAction;

			public void Dispose()
			{
				Interlocked.Exchange(ref _popAction, null)?.Invoke();
			}
		}
	}
}
