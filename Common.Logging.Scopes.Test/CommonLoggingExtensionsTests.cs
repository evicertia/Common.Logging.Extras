using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using NUnit.Framework;

using Common.Logging.NLog45;
using Common.Logging.Factory;

namespace Common.Logging.Scopes
{
	public class CommonLoggingExtensionsTests
	{
		#region Inner Types

		private class DummyLog : AbstractLogger
		{
			#region AbstractLogger members..

			public override bool IsTraceEnabled => false;

			public override bool IsDebugEnabled => false;

			public override bool IsInfoEnabled => false;

			public override bool IsWarnEnabled => false;

			public override bool IsErrorEnabled => false;

			public override bool IsFatalEnabled => false;

			protected override void WriteInternal(LogLevel level, object message, Exception exception)
			{
				// Do nothing..
			}

			#endregion

			#region Properties

			public static bool Async { get; set; }

			public override IVariablesContext GlobalVariablesContext => new NLogGlobalVariablesContext();
			public override IVariablesContext ThreadVariablesContext => Async ? LogicalThreadVariablesContext : PerThreadVariablesContext;
			public override INestedVariablesContext NestedThreadVariablesContext => Async ? NestedLogicalThreadVariablesContext : NestedPerThreadVariablesContext;

			private IVariablesContext PerThreadVariablesContext => new NLogThreadVariablesContext();
			private IVariablesContext LogicalThreadVariablesContext => new NLogLogicalThreadVariablesContext();

			private INestedVariablesContext NestedPerThreadVariablesContext => new NLogNestedThreadVariablesContext();
			private INestedVariablesContext NestedLogicalThreadVariablesContext => new NLogNestedLogicalThreadVariablesContext();

			#endregion
		}

		#endregion

		#region Fields

		private static DummyLog _log;

		#endregion

		#region SetUp & TearDown

		[SetUp]
		public void SetUp()
		{
			_log = new DummyLog();
		}

		[TearDown]
		public void TearDown()
		{
			NLogLogger.FavorLogicalVariableContexts = false; //< XXX: Hack.. see async test(s).
			DummyLog.Async = false;
			_log = null;
		}

		#endregion

		#region Private methods

		private static bool IsValid(INestedVariablesContext context, IEnumerable<string> messages)
		{
			if (!context.HasItems)
				return false;

			// Peek would be the good choice.. but is not available..
			var items = new List<string>();
			while (context.HasItems)
				items.Add(context.Pop());

			items.Reverse();
			items.ForEach(x => context.Push(x));

			if (items.Count != messages.Count())
				return false;

			for (var x = 0; x < items.Count; x++)
			{
				if (!string.Equals(items[x], messages.ElementAt(x)))
					return false;
			}

			return true;
		}

		private static bool IsValid(IVariablesContext context, IDictionary<string, object> variables)
		{
			foreach (var item in variables)
			{
				if (!context.Contains(item.Key) || context.Get(item.Key) != item.Value)
					return false;
			}

			return true;
		}

		#endregion

		[Test]
		public void Begin_Thread_Scope_Is_Properly_Created_And_Contexts_Works_As_Expected()
		{
			// XXX: Basically, this test will see (per-thread contexts)..
			//		0. Nested context is valid if the context only have 'one message inside'.
			//		1. Thread context is valid if the context have the property 'First' with value 'Field'.
			//		2. Nested context should contain 'both messages'..
			//		3. Thread context should contain the first property.. and the last one.
			//		...
			//
			// [!] Indirectly, we are testing CustomStack<T> here too.. (Black box?)

			var messages = new string[] { "First scope", "Inner scope.." };
			var variables = new Dictionary<string, object>() { { "X", "Y" } };

			using (_log.BeginThreadScope(messages[0], variables))
			{
				// Per-thread contexts should have the first message on nested and the first variable..
				Assert.That(IsValid(_log.NestedThreadVariablesContext, messages.Take(1)), "#0");
				Assert.That(IsValid(_log.ThreadVariablesContext, variables), "#1");

				variables.Add("Second", "Field");

				using (_log.BeginThreadScope(messages[1], variables))
				{
					// Both should have all the 'logging fields'..
					Assert.That(IsValid(_log.NestedThreadVariablesContext, messages), "#2");
					Assert.That(IsValid(_log.ThreadVariablesContext, variables), "#3");
				}

				variables.Remove("Second");

				// Per-thread contexts should have the first message and the first variable again due inner scope was closed..
				Assert.That(IsValid(_log.NestedThreadVariablesContext, messages.Take(1)), "#4");
				Assert.That(IsValid(_log.ThreadVariablesContext, variables), "#5");
			}

			// Due dispose.. any logging field must be present!
			Assert.That(!_log.NestedThreadVariablesContext.HasItems, "#6");
			Assert.That(!_log.ThreadVariablesContext.Contains("X"), "#7");
		}

		[Test]
		public void Async_Begin_Thread_Scope_Is_Properly_Created_And_Contexts_Works_As_Expected()
		{
			// XXX: Currently, we only support NLog.. and we validate that current scope is not accessed by another thread
			//		(if per-thread scopes are enabled.. the same at dispose).
			//
			// Due this tests doesn't need to use NLog to test the common logging extensions..
			//		and we 'ignore' the scopes ownership if async enabled.. we use this 'dummy hack'
			//		to say 'Hey, don't validate my ownership, i'm async' (reason: DummyLog not inherits from NLog, so it's not 'handled' on guard).
			NLogLogger.FavorLogicalVariableContexts = true; //< HACK..

			DummyLog.Async = true; //< Needed to use logical contexts.
			_log = new DummyLog();

			using (_log.BeginThreadScope("...", new Dictionary<string, object>() { { "X", "Y" } }))
			{
				var threadId = Thread.CurrentThread.ManagedThreadId;
				var resetEvent = new ManualResetEvent(false);

				ThreadPool.QueueUserWorkItem(delegate
				{
					Assert.That(threadId, Is.Not.EqualTo(Thread.CurrentThread.ManagedThreadId), "#0");
					Assert.That(_log.NestedThreadVariablesContext.HasItems, "#1");

					using (_log.BeginThreadScope("...", new Dictionary<string, object>() { { "X", "!" }, { "Y", "X" } }))
					{
						Assert.That(_log.ThreadVariablesContext.Get("X"), Is.EqualTo("!"), "#2");
						Assert.That(_log.ThreadVariablesContext.Get("Y"), Is.EqualTo("X"), "#3");

						_log.PushThreadScopedVariable("Z", "?");
					}

					resetEvent.Set();
				}, null);

				Assert.That(resetEvent.WaitOne(), "#4");
				Assert.That(!_log.ThreadVariablesContext.Contains("Z"), "#5");
				Assert.That(_log.ThreadVariablesContext.Get("X"), Is.EqualTo("Y"), "#6");
			}
		}

		[Test]
		public void Inner_Logging_Scope_Sets_A_Distinct_Value_For_Existing_Key_And_Finally_Sets_The_First_One_On_Dispose()
		{
			// XXX: If overrided values are 'not restored' on scope disposing.. it's wrong.

			var variables = new Dictionary<string, object>() { { "Field", "First" } };

			using (_log.BeginThreadScope("...", variables))
			{
				Assert.That(_log.ThreadVariablesContext.Get("Field"), Is.EqualTo("First"), "#0");

				variables["Field"] = "Second";

				using (_log.BeginThreadScope("...", variables))
				{
					Assert.That(_log.ThreadVariablesContext.Get("Field"), Is.EqualTo("Second"), "#1");

					variables["Field"] = "Third";

					using (_log.BeginThreadScope("...", variables))
						Assert.That(_log.ThreadVariablesContext.Get("Field"), Is.EqualTo("Third"), "#3");

					Assert.That(_log.ThreadVariablesContext.Get("Field"), Is.EqualTo("Second"), "#4");
				}

				Assert.That(_log.ThreadVariablesContext.Get("Field"), Is.EqualTo("First"), "#5");
			}

			Assert.That(!_log.ThreadVariablesContext.Contains("Field"), "#6");
		}

		[Test]
		public void Per_Thread_Scopes_Does_Not_Mix_Variables_From_Anoter_Threads()
		{
			// XXX: Basically, we musnt't see variables from another thread..
			//		0. Current thread must be different from the given one from pool.
			//		1. NestedContext from the 'new thread' must be empty due is a new thread (assert not mixing)..
			//		2. ThreadContext from the 'new thread' can't contain the variable from the first thread..
			//		3. Wait for thead execution..
			//		4. 'Main thread' can't contain the thread variable was set in the 'new thread'.


			using (_log.BeginThreadScope("...", new Dictionary<string, object>() { { "X", "Y" } }))
			{
				var threadId = Thread.CurrentThread.ManagedThreadId;
				var resetEvent = new ManualResetEvent(false);

				ThreadPool.QueueUserWorkItem(delegate
				{
					Assert.That(threadId, Is.Not.EqualTo(Thread.CurrentThread.ManagedThreadId), "#0");
					Assert.That(!_log.NestedThreadVariablesContext.HasItems, "#1");

					using (_log.BeginThreadScope("...", new Dictionary<string, object>() { { "Y", "X" } }))
						Assert.That(!_log.ThreadVariablesContext.Contains("X"), "#2");

					resetEvent.Set();
				}, null);

				Assert.That(resetEvent.WaitOne(), "#3");
				Assert.That(!_log.ThreadVariablesContext.Contains("Y"), "#4");
			}
		}

		[Test]
		public void Push_Thread_Scoped_Variable_Throws_An_Error_If_Scope_Not_Exists()
		{
			// XXX: Due 'dummy log' is cleaned on tear down and initialized again on set up.. CustomStack<> with a logging scope is not present.
			Assert.That(() => _log.PushThreadScopedVariable("X", "Y"), Throws.Exception, "#0");
			Assert.That(() => _log.PushThreadScopedVariablesFor(new Dictionary<string, object>() { { "X", "Y" } }), Throws.Exception, "#1");
		}
	}
}