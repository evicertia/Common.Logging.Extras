#region License

/*
 * Copyright © 2002-2009 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using Context = NLog.GlobalDiagnosticsContext;

namespace Common.Logging.NLog53
{
	public sealed class NLogGlobalVariablesContext : IVariablesContext
	{
		public void Set(string key, object value) => Context.Set(key, value);
		public object Get(string key) => Context.GetObject(key);
		public bool Contains(string key) => Context.Contains(key);
		public void Remove(string key) => Context.Remove(key);
		public void Clear() => Context.Clear();
	}
}
