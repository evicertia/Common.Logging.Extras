using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Common.Logging.Scopes
{
	[TestFixture(true)] //< Execute all tests with logical contexts..
	[TestFixture(false)] //< Execute all the test except async one with non-logical contexts..
	public class AbstractScopesTests
	{
		#region Fields

		private readonly bool _useLogicalContexts;

		private DummyLog _log;

		#endregion

		#region .ctors

		public AbstractScopesTests(bool useLogicalContexts)
		{
			_useLogicalContexts = useLogicalContexts;
		}

		#endregion

		#region SetUp & TearDown

		[SetUp]
		public void SetUp()
		{
			_log = new DummyLog(_useLogicalContexts);
		}

		[TearDown]
		public void TearDown()
		{
			_log = null;
		}

		#endregion

		// INFO: SYNC - Basic.
		//
		//	>> BeginScope()
		//		Set(A, 1);
		//		Set(B, 1);
		//		Context["A"] == 1
		//		Context["B"] == 1
		//		>> BeginScope();
		//			Set(A, 2);
		//			Context["A"] == 2
		//			Context["B"] == 1
		//		<<
		//		Context["A"] == 1
		//		Context["B"] == 1
		//	<<
		[Test]
		[Timeout(1000)]
		public void Sync_Begin_Thread_Scope_Basic()
		{
			using (_log.BeginThreadScope("Z"))
			{
				_log.PushThreadScopedVariable("A", 1);
				_log.PushThreadScopedVariable("B", 1);

				Assert.Multiple(() =>
				{
					Assert.That(_log.ThreadVariablesContext.Get("A"), Is.EqualTo(1), "#0");
					Assert.That(_log.ThreadVariablesContext.Get("B"), Is.EqualTo(1), "#1");
					Assert.That(_log.PeekFromNestedContext(), Is.EqualTo("Z"), "#2");
				});

				using (_log.BeginThreadScope("Y"))
				{
					_log.PushThreadScopedVariable("A", 2);
					_log.PushThreadScopedVariable("B", 2);

					Assert.Multiple(() =>
					{
						Assert.That(_log.ThreadVariablesContext.Get("A"), Is.EqualTo(2), "#3");
						Assert.That(_log.ThreadVariablesContext.Get("B"), Is.EqualTo(2), "#4");
						Assert.That(_log.PeekFromNestedContext(), Is.EqualTo("Y"), "#5");
					});
				}

				Assert.Multiple(() =>
				{
					Assert.That(_log.ThreadVariablesContext.Get("A"), Is.EqualTo(1), "#6");
					Assert.That(_log.ThreadVariablesContext.Get("B"), Is.EqualTo(1), "#7");
					Assert.That(_log.PeekFromNestedContext(), Is.EqualTo("Z"), "#8");
				});
			}

			Assert.Multiple(() =>
			{
				Assert.That(_log.ThreadVariablesContext.Contains("A"), Is.False, "#9");
				Assert.That(_log.ThreadVariablesContext.Contains("B"), Is.False, "#10");
				Assert.That(_log.NestedThreadVariablesContext.HasItems, Is.False, "#11");
			});
		}

		// INFO: SYNC - Two threads.
		//
		//	>> BeginScope()
		//		Set(A, 1);
		//		Set(B, 1)
		//		Context["A"] == 1
		//		Context["B"] == 1
		//		Thread.New.Run(() => {
		//			>> BeginScope()
		//				Context["A"] == null
		//				Context["B"] == null
		//				Set(C, 1);
		//			<<
		//			sem.Set();
		//		});
		//		sem.WaitOne();
		//		Context["A"] == 1
		//		Context["B"] == 1
		//		Context["C"] == null
		//	<<
		[Test]
		[Timeout(2000)]
		public void Sync_Begin_Thread_Scope_Two_Threads()
		{
			using (_log.BeginThreadScope("Z"))
			{
				_log.PushThreadScopedVariable("A", 1);
				_log.PushThreadScopedVariable("B", 1);

				Assert.Multiple(() =>
				{
					Assert.That(_log.ThreadVariablesContext.Get("A"), Is.EqualTo(1), "#0");
					Assert.That(_log.ThreadVariablesContext.Get("B"), Is.EqualTo(1), "#1");
					Assert.That(_log.PeekFromNestedContext(), Is.EqualTo("Z"), "#2");
				});

				var threadId = Thread.CurrentThread.ManagedThreadId;

				using (var mre = new ManualResetEvent(false))
				{
					// XXX: When a thread is created, the execution context is flowed to the new thread.
					//		To our test, we need to disable or suppress this flow.
					//
					// This can be done in the following ways:
					//		0. Using UnsafeQueueUserWorkItem() from ThreadPool..
					//		1. Using the EnsureContext.SuppressFlow() before the thread creation..
					//		2. Creating & executing the thread before add anything on the CallContext.
					ThreadPool.UnsafeQueueUserWorkItem(delegate
					{
						Assert.Multiple(() =>
						{
							Assert.That(threadId, Is.Not.EqualTo(Thread.CurrentThread.ManagedThreadId), "#3");

							Assert.That(_log.ThreadVariablesContext.Contains("A"), Is.False, "#4");
							Assert.That(_log.ThreadVariablesContext.Contains("B"), Is.False, "#5");
							Assert.That(_log.NestedThreadVariablesContext.HasItems, Is.False, "#6");
						});

						using (_log.BeginThreadScope("Y"))
						{
							_log.PushThreadScopedVariable("C", 1);

							Assert.Multiple(() =>
							{
								Assert.That(_log.ThreadVariablesContext.Get("C"), Is.EqualTo(1), "#7");
								Assert.That(_log.PeekFromNestedContext(), Is.EqualTo("Y"), "#8");
							});
						}

						mre.Set();
					}, null);

					mre.WaitOne();
				}

				Assert.Multiple(() =>
				{
					Assert.That(_log.ThreadVariablesContext.Get("A"), Is.EqualTo(1), "#9");
					Assert.That(_log.ThreadVariablesContext.Get("B"), Is.EqualTo(1), "#10");
					Assert.That(_log.ThreadVariablesContext.Contains("C"), Is.False, "#11");
					Assert.That(_log.PeekFromNestedContext(), Is.EqualTo("Z"), "#12");
				});
			}
		}

		// INFO: ASYNC/AWAIT - Complex.
		//
		//	>> BeginScope()
		//		Set(A, 1);
		//		Set(B, 1);
		//		Context["A"] == 1
		//		Context["B"] == 1
		//		sem1 = new WaitHandle();
		//		sem2 = new WaitHandle();
		//		Task.Run(() => {
		//			Context["A"] == 1
		//			Context["B"] == 1
		//			Set(C, 1);
		//			sem1.Set();
		//			sem2.WaitOne();
		//			>> BeginScope()
		//				Set(A, 2);
		//				Set(D, 1);
		//				Context["A"] == 2
		//				Context["B"] == 1
		//				Context["C"] == 1
		//				Context["D"] == 1
		//			<<
		//			Context["A"] == 1
		//			Context["D"] == null
		//		});
		//		sem1.WaitOne();
		//		Context["C"] == null
		//		Set(B, 2);
		//		sem2.Set();
		//		task.Wait();
		//
		//		Context["A"] == 1
		//		Context["B"] == 2
		//		Context["C"] == null
		//		Context["D"] == null
		//		>> BeginScope();
		//			Set(A, 2);
		//			Context["A"] == 2
		//			Context["B"] == 2
		//		<<
		//		Context["A"] == 1
		//		Context["B"] == 2
		//	<<
		[Test]
		[Timeout(3000)]
		public void Async_Begin_Thread_Scope()
		{
			if (!_useLogicalContexts)
				Assert.Ignore("Non-logical contexts does not support async stuff.");

			using (_log.BeginThreadScope("Z"))
			{
				_log.PushThreadScopedVariable("A", 1);
				_log.PushThreadScopedVariable("B", 1);

				Assert.Multiple(() =>
				{
					Assert.That(_log.ThreadVariablesContext.Get("A"), Is.EqualTo(1), "#0");
					Assert.That(_log.ThreadVariablesContext.Get("B"), Is.EqualTo(1), "#1");
					Assert.That(_log.PeekFromNestedContext(), Is.EqualTo("Z"), "#2");
				});

				using (var mre0 = new ManualResetEvent(false))
				using (var mre1 = new ManualResetEvent(false))
				{
					var task = Task.Run(() =>
					{
						Assert.Multiple(() =>
						{
							Assert.That(_log.ThreadVariablesContext.Get("A"), Is.EqualTo(1), "#3");
							Assert.That(_log.ThreadVariablesContext.Get("B"), Is.EqualTo(1), "#4");
							Assert.That(_log.PeekFromNestedContext(), Is.EqualTo("Z"), "#5");
						});

						_log.PushThreadScopedVariable("C", 1);

						mre0.Set();
						mre1.WaitOne();

						using (_log.BeginThreadScope("Y"))
						{
							_log.PushThreadScopedVariable("A", 2);
							_log.PushThreadScopedVariable("D", 1);

							Assert.Multiple(() =>
							{
								Assert.That(_log.ThreadVariablesContext.Get("A"), Is.EqualTo(2), "#7");
								Assert.That(_log.ThreadVariablesContext.Get("B"), Is.EqualTo(1), "#8");
								Assert.That(_log.ThreadVariablesContext.Get("C"), Is.EqualTo(1), "#9");
								Assert.That(_log.ThreadVariablesContext.Get("D"), Is.EqualTo(1), "#10");
								Assert.That(_log.PeekFromNestedContext(), Is.EqualTo("Y"), "#11");
							});
						}

						Assert.Multiple(() =>
						{
							Assert.That(_log.ThreadVariablesContext.Get("A"), Is.EqualTo(1), "#12");
							Assert.That(_log.ThreadVariablesContext.Contains("D"), Is.False, "#13");
							Assert.That(_log.PeekFromNestedContext(), Is.EqualTo("Z"), "#14");
						});
					});

					mre0.WaitOne();

					Assert.That(_log.ThreadVariablesContext.Contains("C"), Is.False, "#6");

					_log.PushThreadScopedVariable("B", 2);

					mre1.Set();

					task.Wait();

					Assert.Multiple(() =>
					{
						Assert.That(_log.ThreadVariablesContext.Get("A"), Is.EqualTo(1), "#15");
						Assert.That(_log.ThreadVariablesContext.Get("B"), Is.EqualTo(2), "#16");
						Assert.That(_log.ThreadVariablesContext.Contains("C"), Is.False, "#17");
						Assert.That(_log.ThreadVariablesContext.Contains("D"), Is.False, "#18");
					});

					using (_log.BeginThreadScope("X"))
					{
						_log.PushThreadScopedVariable("A", 2);


						Assert.Multiple(() =>
						{
							Assert.That(_log.ThreadVariablesContext.Get("A"), Is.EqualTo(2), "#19");
							Assert.That(_log.ThreadVariablesContext.Get("B"), Is.EqualTo(2), "#20");
							Assert.That(_log.PeekFromNestedContext(), Is.EqualTo("X"), "#21");
						});
					}

					Assert.Multiple(() =>
					{
						Assert.That(_log.ThreadVariablesContext.Get("A"), Is.EqualTo(1), "#22");
						Assert.That(_log.ThreadVariablesContext.Get("B"), Is.EqualTo(2), "#23");
						Assert.That(_log.PeekFromNestedContext(), Is.EqualTo("Z"), "#24");
					});
				}
			}
		}
	}
}