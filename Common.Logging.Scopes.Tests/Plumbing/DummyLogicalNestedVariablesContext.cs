using System;

using Context = Common.Logging.Scopes.DummyNestedDiagnosticsLogicalContext;

namespace Common.Logging.Scopes
{
	internal class DummyLogicalNestedVariablesContext : INestedVariablesContext
	{
		#region INestedVariablesContext members..

		public bool HasItems => Context.GetAllMessages().Length > 0;

		public IDisposable Push(string text) => Context.Push(text);

		public void Clear() => Context.Clear();

		public string Peek() => Context.Peek();

		public string Pop() => Context.Pop();

		#endregion
	}
}