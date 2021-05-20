using System;
using System.Threading;
using System.Collections.Generic;

using Common.Logging.Scopes;

namespace Common.Logging
{
	public static class CommonLoggingExtensions
	{
		#region Inner Types

		// INFO: Custom Stack overriding ToString so we can persist it at
		//		ThreadVariableContext w/o actual values being logged on.
		//
		// Also we allow saving current thread's id, so it can be
		//		used as by guards later on as additional safe checks.
		//
		// Additionally, we needed to wrap the stack as property instead of inherit this class with the data structure stack
		//		due NLog, actually, not hits ToString() with collections of IEnumerable<>..
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

		#endregion

		#region Public methods

		public static IDisposable BeginThreadScope(this ILog @this, string description, IDictionary<string, object> variables = null, string prefix = null)
		{
			Guard.IsNotNull(@this, nameof(@this));

			var context = @this.ThreadVariablesContext;

			if (!(context.Get(STACK_KEY) is CustomStack<ThreadLoggingScope> customStack))
			{
				customStack = new CustomStack<ThreadLoggingScope>(Thread.CurrentThread.ManagedThreadId);
				context.Set(STACK_KEY, customStack);
			}

			@this.NestedThreadVariablesContext.Push(description);

			var scope = new ThreadLoggingScope(context);

			if (variables != null)
			{
				foreach (var keyValue in variables)
					scope.Set((prefix + keyValue.Key), keyValue.Value);
			}

			customStack.Collection.Push(scope);

			return Disposable.Using(@this, @this2 =>
			{
				// XXX: Disposable class ensures this code is only called once.
				var stack2 = @this2.ThreadVariablesContext.Get(STACK_KEY) as CustomStack<ThreadLoggingScope>;

				Guard.IsNotNull(stack2, nameof(CustomStack<ThreadLoggingScope>));

				stack2.Collection.Pop().Dispose();
				@this2.NestedThreadVariablesContext.Pop();
			});
		}

		public static void PushThreadScopedVariable(this ILog @this, string name, object value)
		{
			Guard.IsNotNull(@this, nameof(@this));
			@this.GetCurrentThreadScope().Set(name, value);
		}

		public static void PushThreadScopedVariablesFor(this ILog @this, IEnumerable<KeyValuePair<string, object>> variables, string prefix = null)
		{
			Guard.IsNotNull(@this, nameof(@this));
			Guard.IsNotNull(variables, nameof(variables));

			var scope = @this.GetCurrentThreadScope();

			foreach (var item in variables)
				scope.Set((prefix + item.Key), item.Value);
		}

		#endregion
	}
}