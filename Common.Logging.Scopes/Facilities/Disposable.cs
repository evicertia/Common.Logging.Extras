using System;

namespace Common.Logging.Scopes
{
	internal class Disposable : IDisposable
	{
		#region Fields

		private readonly Action _disposeAction;

		private bool _disposed;

		#endregion

		#region .ctors

		public Disposable(Action disposeAction)
		{
			_disposeAction = disposeAction;
		}

		public Disposable(Action createAction, Action disposeAction)
		{
			createAction();
			_disposeAction = disposeAction;
		}

		#endregion

		#region Public methods

		public static Disposable<T> Create<T>(Func<T> createAction, Action<T> disposeAction)
		{
			return new Disposable<T>(createAction, disposeAction);
		}

		public static Disposable<T> Using<T>(T instance, Action<T> disposeAction)
		{
			return new Disposable<T>(() => instance, disposeAction);
		}

		public static Disposable For(Action disposeAction)
		{
			return new Disposable(disposeAction);
		}

		public static Disposable For(Action createAction, Action disposeAction)
		{
			return new Disposable(createAction, disposeAction);
		}

		public void Dispose()
		{
			if (!_disposed && _disposeAction != null)
			{
				_disposed = true;
				_disposeAction();
			}
		}

		#endregion
	}

	internal class Disposable<T> : IDisposable
	{
		#region Fields & Properties

		private bool _disposed;
		private readonly T _instance;
		private readonly Action<T> _disposeAction;

		public T Instance => _instance;

		#endregion

		#region .ctors

		public Disposable(Func<T> createAction, Action<T> disposeAction)
		{
			_instance = createAction();
			_disposeAction = disposeAction;
		}

		#endregion

		#region Public methods

		public void Dispose()
		{
			if (!_disposed && _disposeAction != null)
			{
				_disposed = true;
				_disposeAction(_instance);
			}
		}

		#endregion
	}
}