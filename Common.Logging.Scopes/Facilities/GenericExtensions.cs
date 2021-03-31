using System;

namespace Common.Logging.Scopes
{
	internal static class GenericExtensions
	{
		/// <summary>
		/// Throws if null.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source">The source.</param>
		/// <param name="paramName">Name of the param.</param>
		/// <returns></returns>
		public static T ThrowIfNull<T>(this T source, string paramName)
			where T : class
		{
			if (source == null)
				throw new ArgumentNullException(paramName);

			return source;
		}

		/// <summary>
		/// Throws if source is null.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source">The source.</param>
		/// <param name="message">Name of the param.</param>
		/// <returns></returns>
		public static T ThrowIfNull<T>(this T source, string paramName, string message, params object[] args)
			where T : class
		{
			if (source == null)
				throw new ArgumentNullException(paramName, args != null ? string.Format(message, args) : message);

			return source;
		}
	}
}