using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Remoting.Messaging;

namespace Common.Logging.Scopes
{
	public sealed class ThreadLoggingScope : IDisposable
	{
		#region Fields
		private static readonly ILog _log = LogManager.GetLogger(typeof(ThreadLoggingScope));
		private static readonly object _removeMarker = new object();

		private readonly IDictionary<string, object> _variables = new Dictionary<string, object>();
		private readonly IVariablesContext _context;
		private Action<ThreadLoggingScope> _disposer;

		private bool _disposed;

		public static bool SwallowExceptions = false; //< Used to allow configuring swallowing exceptions on unexpected error conditions.

		#endregion

		#region .ctors

		public ThreadLoggingScope(IVariablesContext context, Action<ThreadLoggingScope> disposer)
		{
			_context = context.ThrowIfNull(nameof(context));
			_disposer = disposer.ThrowIfNull(nameof(disposer));
		}

		#endregion

		#region Public methods

		public IReadOnlyDictionary<string, object> GetVariables() => new ReadOnlyDictionary<string, object>(_variables);

		public void Set(string key, object value)
		{
			try
			{
				Guard.IsNotNullNorEmpty(key, nameof(key));
				Guard.Against<InvalidOperationException>(_disposed, "{0} already disposed?!", nameof(ThreadLoggingScope));

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
			catch (Exception ex)
			{
				var message =
					$"{{0}} unexpected exception while setting a thread scoped variable '{key}'. " +
					$"This maybe related to issue https://github.com/evicertia/Common.Logging.Extras/issues/5, " +
					$"which is an issue not easy to reproduce (and hence fix), but you can help us to fix this " +
					$"issue faster by providing additional diagnostics details ;)";

				if (SwallowExceptions) _log.WarnFormat(message, ex, "Ignoring");
				else throw new InvalidOperationException(string.Format(message, "Caught"), ex);
			}
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			try
			{
				_disposer?.Invoke(this);

				foreach (var item in _variables)
				{
					if (item.Value == _removeMarker)
						_context.Remove(item.Key);
					else
						_context.Set(item.Key, item.Value);
				}

				_variables.Clear(); //< Housekeeping..
			}
			catch (Exception ex)
			{
				var message =
					$"{{0}} unexpected exception while disposing scoped. " +
					$"This maybe related to issue https://github.com/evicertia/Common.Logging.Extras/issues/5, " +
					$"which is an issue not easy to reproduce (and hence fix), but you can help us to fix this " +
					$"issue faster by providing additional diagnostics details ;)";

				if (SwallowExceptions) _log.WarnFormat(message, ex, "Ignoring");
				else throw new InvalidOperationException(string.Format(message, "Caught"), ex);
			}

			GC.SuppressFinalize(true);

			_disposer = null;
			_disposed = true;
		}

		#endregion
	}
}