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

using Context = Common.Logging.NLog45.MappedDiagnosticsLogicalContext;

namespace Common.Logging.NLog45
{
	/// <summary>
	/// A logical thread context for logger variables
	/// </summary>
	public class NLogLogicalThreadVariablesContext : IVariablesContext
	{
		/// <summary>
		/// Sets the value of a new or existing variable within the logical thread context
		/// </summary>
		/// <param name="key">The key of the variable that is to be added</param>
		/// <param name="value">The value to add</param>
		public void Set(string key, object value)
		{
			Context.Set(key, value);
		}

		/// <summary>
		/// Gets the value of a variable within the logical thread context
		/// </summary>
		/// <param name="key">The key of the variable to get</param>
		/// <returns>The value or null if not found</returns>
		public object Get(string key)
		{
			return Context.GetObject(key);
		}

		/// <summary>
		/// Checks if a variable is set within the logical thread context
		/// </summary>
		/// <param name="key">The key of the variable to check for</param>
		/// <returns>True if the variable is set</returns>
		public bool Contains(string key)
		{
			return Context.Contains(key);
		}

		/// <summary>
		/// Removes a variable from the logical thread context by key
		/// </summary>
		/// <param name="key">The key of the variable to remove</param>
		public void Remove(string key)
		{
			Context.Remove(key);
		}

		/// <summary>
		/// Clears the logical thread context variables
		/// </summary>
		public void Clear()
		{
			Context.Clear();
		}
	}
}