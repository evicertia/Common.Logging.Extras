using System;
using System.Threading;
using System.Collections.Generic;

namespace Common.Logging.Scopes
{
	internal class DummyNestedVariablesContext : INestedVariablesContext
	{
		#region Fields

		private readonly static ThreadLocal<Stack<string>> _context = new ThreadLocal<Stack<string>>(() => new Stack<string>());

		#endregion

		#region INestedVariablesContext members..

		public bool HasItems => _context.Value.Count > 0;

		public string Peek() => _context.Value.Peek();

		public void Clear() => _context.Value.Clear();

		public string Pop() => _context.Value.Pop();

		public IDisposable Push(string text)
		{
			_context.Value.Push(text);

			return null; //< XXX: We don't need? To do here 'something' disposable to our tests btw..
		}

		#endregion
	}
}