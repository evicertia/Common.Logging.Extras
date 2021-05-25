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

namespace Common.Logging.NLog45
{
	public partial class NLogLogger
	{
		// XXX: Use a setting (NLog setting? AppSetting?) in order to select the actual implementation
		//		of ThreadVariablesContext && NestedThreadVariablesContext, so we can choose
		//		between logical or per-thread-only, as this might have performance implications.
		//
		//		We may get rid of this once NLog v5 is released as they have combined all
		//		variable context implementations (ie. MDC, MDLC, NDC & NDLC) under a single
		//		ScopeLogging class. (pruiz)
		public static bool FavorLogicalVariableContexts { get; set; } = false;

		/// <summary>
		/// Returns the global context for variables
		/// </summary>
		public override IVariablesContext GlobalVariablesContext => new NLogGlobalVariablesContext();

		/// <summary>
		/// Returns the thread-specific context for variables.
		/// </summary>
		public override IVariablesContext ThreadVariablesContext => FavorLogicalVariableContexts ? LogicalThreadVariablesContext : PerThreadVariablesContext;

		/// <summary>
		/// Returns the thread-specific context for nested variables (for NDC, eg.).
		/// </summary>
		public override INestedVariablesContext NestedThreadVariablesContext => FavorLogicalVariableContexts ? NestedLogicalThreadVariablesContext : NestedPerThreadVariablesContext;

		/// <summary>
		/// Returns the per-thread-specific context for variables.
		/// </summary>
		public IVariablesContext PerThreadVariablesContext => new NLogThreadVariablesContext();

		/// <summary>
		/// Returns the per-thread-specific context for nested variables (for NDC, eg.).
		/// </summary>
		public INestedVariablesContext NestedPerThreadVariablesContext => new NLogNestedThreadVariablesContext();

		/// <summary>
		/// Returns async version of ThreadVariablesContext. Allows for maintaining state across
		/// asynchronous tasks and call contexts.
		/// </summary>
		/// <remarks>
		/// This is not actually supported by Common.Logging, but it is exposed here so it can be accessed
		/// by casting and/or reflection.
		/// </remarks>
		public virtual IVariablesContext LogicalThreadVariablesContext => new NLogLogicalThreadVariablesContext();

		/// <summary>
		/// Returns async version of NestedThreadVariablesContext. Allows for maintaining state across
		/// asynchronous tasks and call contexts.
		/// </summary>
		/// <remarks>
		/// This is not actually supported by Common.Logging, but it is exposed here so it can be accessed
		/// by casting and/or reflection.
		/// </remarks>
		public virtual INestedVariablesContext NestedLogicalThreadVariablesContext => new NLogNestedLogicalThreadVariablesContext();
	}
}