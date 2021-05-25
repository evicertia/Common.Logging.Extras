using Context = Common.Logging.Scopes.DummyMappedDiagnosticsLogicalContext;

namespace Common.Logging.Scopes
{
	internal class DummyLogicalVariablesContext : IVariablesContext
	{
		#region IVariablesContext members..

		public void Set(string key, object value) => Context.Set(key, value);

		public bool Contains(string key) => Context.Contains(key);

		public object Get(string key) => Context.GetObject(key);

		public void Remove(string key) => Context.Remove(key);

		public void Clear() => Context.Clear();

		#endregion
	}
}