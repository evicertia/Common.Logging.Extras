using System.Threading;
using System.Collections.Generic;

namespace Common.Logging.Scopes
{
	internal class DummyVariablesContext : IVariablesContext
	{
		#region Fields

		private readonly static ThreadLocal<IDictionary<string, object>> _context = new ThreadLocal<IDictionary<string, object>>(() => new Dictionary<string, object>());

		#endregion

		#region IVariablesContext members..

		public object Get(string key) => _context.Value.ContainsKey(key) ? _context.Value[key] : null;

		public bool Contains(string key) => _context.Value.ContainsKey(key);

		public void Remove(string key) => _context.Value.Remove(key);

		public void Clear() => _context.Value.Clear();

		public void Set(string key, object value)
		{
			if (_context.Value.ContainsKey(key))
				_context.Value[key] = value;
			else
				_context.Value.Add(key, value);
		}

		#endregion
	}
}