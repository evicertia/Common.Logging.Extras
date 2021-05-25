using System;

namespace Common.Logging.Scopes
{
	internal static class Guard
	{
		/// <summary>
		/// Throws an exception of type <typeparamref name="TException"/> with the specified message
		/// when the assertion statement is true.
		/// </summary>
		/// <typeparam name="TException">The type of exception to throw.</typeparam>
		/// <param name="assertion">The assertion to evaluate. If true then the <typeparamref name="TException"/> exception is thrown.</param>
		/// <param name="message">string. The exception message to throw.</param>
		public static void Against<TException>(bool assertion, string message, params object[] args)
			where TException : Exception
		{
			if (assertion)
				throw (TException)Activator.CreateInstance(typeof(TException), args != null ? string.Format(message, args) : message);
		}

		/// <summary>
		/// Throws an exception if instance is null.
		/// </summary>
		/// <param name="instance">The instance.</param>
		/// <param name="message">The message.</param>
		public static void IsNotNull(object instance, string message, params object[] args)
		{
			if (instance == null)
				throw new ArgumentNullException(args != null ? string.Format(message, args) : message);
		}

		/// <summary>
		/// Throws an exception if instance is null or empty.
		/// </summary>
		/// <param name="instance">The instance.</param>
		/// <param name="message">The message.</param>
		public static void IsNotNullNorEmpty(string instance, string message, params object[] args)
		{
			if (string.IsNullOrEmpty(instance))
				throw new ArgumentNullException(args != null ? string.Format(message, args) : message);
		}
	}
}