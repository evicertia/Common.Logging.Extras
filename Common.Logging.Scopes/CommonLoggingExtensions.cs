using System;
using System.Threading;
using System.Collections.Generic;

using Common.Logging.Scopes;
using System.Collections;

namespace Common.Logging
{
	public static class CommonLoggingExtensions
	{
		#region Inner Types

		// INFO: Custom Stack overriding ToString so we can persist it at
		//		 ThreadVariableContext w/o actual values being logged on.
		//
		// Also we allow saving current thread's id, so it can be
		//		 used as by guards later on as additional safe checks.
		//
		// Additionally, we needed to wrap the stack as property instead of
		//		 inherit this class with the data structure stack due to NLog's
		//		 not hitting ToString() on collections of IEnumerable<>..
		private sealed class CustomStack<T>
		{
			#region Properties

			public Stack<T> Collection { get; }
			public int OwnerThreadId { get; }

			#endregion

			#region .ctors

			public CustomStack(int ownerThreadId)
			{
				OwnerThreadId = ownerThreadId;
				Collection = new Stack<T>();
			}

			#endregion

			public override string ToString() => null; //< XXX: We don't want this property to be painted on the log line..
		}

		#endregion

		#region Fields

		public const string STACK_KEY = nameof(ThreadLoggingScope) + "s";

		#endregion

		#region Private methods

		private static ThreadLoggingScope GetCurrentThreadScope(this ILog @this)
		{
			var context = @this.ThreadVariablesContext;
			var customStack = context.Get(STACK_KEY) as CustomStack<ThreadLoggingScope>;

			Guard.IsNotNull(customStack, nameof(CustomStack<ThreadLoggingScope>));

			return customStack.Collection
				.ThrowIfNull(nameof(ThreadLoggingScope), "No logging scopes available for current thread?!")
				.ThrowIfEmpty(nameof(ThreadLoggingScope), "No logging scope available for current thread?!")
				.Peek();
		}

		private static bool IsNoOpVariablesContext(this ILog @this)
		{
			return @this.ThreadVariablesContext is Simple.NoOpVariablesContext || @this.NestedThreadVariablesContext is Simple.NoOpNestedVariablesContext;
		}

		#endregion

		#region Public methods

		public static IDisposable BeginThreadScope(this ILog @this, string description, IDictionary<string, object> variables = null, string prefix = null)
		{
			CustomStack<ThreadLoggingScope> customStack;

			Guard.IsNotNull(@this, nameof(@this));

			if (@this.IsNoOpVariablesContext())
			{
				@this.Warn("BeginThreadScope called using an ILogger not supporting variable contexts, ignoring request.");
				return null;
			}

			var context = @this.ThreadVariablesContext;

			try
			{
				customStack = context.Get(STACK_KEY) as CustomStack<ThreadLoggingScope>;
			}
			catch (Exception ex) when (ex is NotSupportedException || ex is NotImplementedException)
			{
				@this.Warn("BeginThreadScope called using an ILogger not implementing variable contexts, ignoring request.");
				return null;
			}

			if (customStack == null)
			{
				customStack = new CustomStack<ThreadLoggingScope>(Thread.CurrentThread.ManagedThreadId);
				context.Set(STACK_KEY, customStack);
			}

			var scope = new ThreadLoggingScope(context, x =>
			{
				if (customStack.Collection.Count <= 0)
				{
					@this.WarnFormat("Empty stack while disposing current ThreadLoggingScope?!");
					return;
				}
				var scope2 = customStack.Collection.Pop();

				if (x != scope2)
				{
					@this.WarnFormat("Unbalanced stack while disposing current ThreadLoggingScope?!");
					return;
				}

				if (@this.NestedThreadVariablesContext.HasItems)
					@this.NestedThreadVariablesContext.Pop();
			});

			if (variables != null)
			{
				foreach (var keyValue in variables)
					scope.Set((prefix + keyValue.Key), keyValue.Value);
			}

			@this.NestedThreadVariablesContext.Push(description);
			customStack.Collection.Push(scope);

			return scope;
		}

		public static IReadOnlyDictionary<string, object> GetScopeVariables(this ILog @this)
		{
			Guard.IsNotNull(@this, nameof(@this));

			if (@this.IsNoOpVariablesContext())
			{
				@this.Warn("GetScopeVariables called using an ILogger not supporting variable contexts, ignoring request.");
				return null;
			}

			return @this.GetCurrentThreadScope().GetVariables();
		}

		public static void PushThreadScopedVariable(this ILog @this, string name, object value)
		{
			try
			{
				Guard.IsNotNull(@this, nameof(@this));
				Guard.IsNotNullNorEmpty(name, nameof(name));

				if (@this.IsNoOpVariablesContext())
				{
					@this.Warn("PushThreadScopedVariable called using an ILogger not supporting variable contexts, ignoring request.");
					return;
				}

				@this.GetCurrentThreadScope().Set(name, value);
			}
			catch (Exception ex) when (ex is NotSupportedException || ex is NotImplementedException)
			{
				@this.Warn("PushThreadScopedVariable called using an ILogger not implementing variable contexts, ignoring request.");
			}
			catch (Exception ex) when (ThreadLoggingScope.SwallowExceptions)
			{
				@this.Warn("Unexpected exception throw inside PushThreadScopedVariable?!", ex);
			}
		}

		public static void PushThreadScopedVariablesFor(this ILog @this, IEnumerable<KeyValuePair<string, object>> variables, string prefix = null)
		{
			try
			{
				Guard.IsNotNull(@this, nameof(@this));
				Guard.IsNotNull(variables, nameof(variables));

				if (@this.IsNoOpVariablesContext())
				{
					@this.Warn("PushThreadScopedVariablesFor called using an ILogger not supporting variable contexts, ignoring request.");
					return;
				}

				var scope = @this.GetCurrentThreadScope();

				foreach (var item in variables)
					scope.Set((prefix + item.Key), item.Value);

			}
			catch (Exception ex) when (ex is NotSupportedException || ex is NotImplementedException)
			{
				@this.Warn("PushThreadScopedVariablesFor called using an ILogger not implementing variable contexts, ignoring request.");
			}
			catch (Exception ex) when (ThreadLoggingScope.SwallowExceptions)
			{
				@this.Warn("Unexpected exception throw inside PushThreadScopedVariable?!", ex);
			}
		}

		#endregion
	}
}