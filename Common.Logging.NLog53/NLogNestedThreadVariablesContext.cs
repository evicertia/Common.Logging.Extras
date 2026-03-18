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
	public sealed class NLogNestedThreadVariablesContext : INestedVariablesContext
	{
		[ThreadStatic]
		private static Stack<NestedScopeEntry> _stack;

		public IDisposable Push(string text)
		{
			var scope = ScopeContext.PushNestedState(text);
			_stack ??= new Stack<NestedScopeEntry>();
			var entry = new NestedScopeEntry(text, scope);
			_stack.Push(entry);

			return new PopHandle(() => Remove(entry));
		}

		public string Pop()
		{
			if (_stack == null || _stack.Count == 0)
				return null;

			var entry = _stack.Pop();
			entry.Scope.Dispose();
			return entry.Value;
		}

		public void Clear()
		{
			if (_stack == null)
				return;

			while (_stack.Count > 0)
				_stack.Pop().Scope.Dispose();
		}

		public bool HasItems => _stack != null && _stack.Count > 0;

		private static void Remove(NestedScopeEntry entry)
		{
			if (_stack == null || _stack.Count == 0)
				return;

			if (ReferenceEquals(_stack.Peek().Scope, entry.Scope))
			{
				_stack.Pop().Scope.Dispose();
				return;
			}

			var temp = new Stack<NestedScopeEntry>();
			var removed = false;

			while (_stack.Count > 0)
			{
				var current = _stack.Pop();
				if (!removed && ReferenceEquals(current.Scope, entry.Scope))
				{
					current.Scope.Dispose();
					removed = true;
					break;
				}
				temp.Push(current);
			}

			while (temp.Count > 0)
				_stack.Push(temp.Pop());
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
