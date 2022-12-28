using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Common.Logging.Scopes
{
	public sealed class ThreadLoggingScope : IDisposable
	{
		#region Fields
		private static readonly ILog _log = LogManager.GetLogger(typeof(ThreadLoggingScope));
		private static readonly object _removeMarker = new object();

		private readonly IDictionary<string, object> _variables = new Dictionary<string, object>();
		private readonly IVariablesContext _context;

		private bool _disposed;

		#endregion

		#region .ctors

		public ThreadLoggingScope(IVariablesContext context)
		{
			_context = context.ThrowIfNull(nameof(context));
		}

		#endregion

		#region Public methods

		public IReadOnlyDictionary<string, object> GetVariables() => new ReadOnlyDictionary<string, object>(_variables);

		public void Set(string key, object value)
		{
			Guard.IsNotNullNorEmpty(key, nameof(key));
			Guard.Against<InvalidOperationException>(_disposed, "{0} already disposed?!", nameof(ThreadLoggingScope));

			try
			{
				if (_variables.ContainsKey(key))
				{
					// Already saved a value for this variable..
				}
				else if (_context.Contains(key))
				{
					// Save value to restore on dispose..
					_variables.Add(key, _context.Get(key));
				}
				else
				{
					// Value not present, so ensure removal on dispose..
					_variables.Add(key, _removeMarker);
				}

				_context.Set(key, value);
			}
			catch (ArgumentException aex)
			{
				_log.Warn("ArgumentException when pushing variables.", aex);
			}
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			foreach (var item in _variables)
			{
				if (item.Value == _removeMarker)
					_context.Remove(item.Key);
				else
					_context.Set(item.Key, item.Value);
			}

			_variables.Clear(); //< Housekeeping..

			GC.SuppressFinalize(true);

			_disposed = true;
		}

		#endregion
	}
}