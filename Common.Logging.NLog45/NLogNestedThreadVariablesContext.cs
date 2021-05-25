#region License

/*
 * Copyright © 2002-2009 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *		http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

// Originally taken from: https://github.com/net-commons/common-logging/pull/176

using System;

using Context = NLog.NestedDiagnosticsContext;

namespace Common.Logging.NLog45
{
	/// <summary>
	/// A nested thread context for logger variables
	/// </summary>
	public class NLogNestedThreadVariablesContext : INestedVariablesContext
	{
		/// <summary>Pushes a new context message into this stack.</summary>
		/// <param name="text">The new context message text.</param>
		/// <returns>
		/// An <see cref="T:System.IDisposable" /> that can be used to clean up the context stack.
		/// </returns>
		public IDisposable Push(string text)
		{
			return Context.Push(text);
		}

		/// <summary>Removes the top context from this stack.</summary>
		/// <returns>The message in the context that was removed from the top of this stack.</returns>
		public string Pop()
		{
			return Context.Pop();
		}

		/// <summary>
		/// Remove all items from nested thread context
		/// </summary>
		public void Clear()
		{
			Context.Clear();
		}

		/// <summary>
		/// Returns true if there is at least one item in the nested thread context; false, if empty
		/// </summary>
		public bool HasItems => Context.GetAllObjects().Length > 0;
	}
}