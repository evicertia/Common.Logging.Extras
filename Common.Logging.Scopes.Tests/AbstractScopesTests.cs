using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.Remoting.Messaging;

using NUnit.Framework;

using Common.Logging.Factory;
using System.Collections.ObjectModel;

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

		#region Inner Classes

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

			#region Fields & Properties

			private readonly bool _useLogicalContexts;

			private readonly IVariablesContext _threadVariablesContext = new VariablesContext();
			private readonly IVariablesContext _logicalThreadVariablesContext = new LogicalVariablesContext();

			private readonly INestedVariablesContext _nestedVariablesContext = new NestedVariablesContext();
			private readonly INestedVariablesContext _logicalNestedVariablesContext = new LogicalNestedVariablesContext();

			public override IVariablesContext ThreadVariablesContext => _useLogicalContexts ? _logicalThreadVariablesContext : _threadVariablesContext;
			public override INestedVariablesContext NestedThreadVariablesContext => _useLogicalContexts ? _logicalNestedVariablesContext : _nestedVariablesContext;

			#endregion

			#region Inner Classes

			private class VariablesContext : IVariablesContext
			{
				#region Fields

				private readonly static ThreadLocal<IDictionary<string, object>> _context = new ThreadLocal<IDictionary<string, object>>(() => new Dictionary<string, object>());

				#endregion

				#region IVariablesContext members..

				public object Get(string key) => _context.Value.ContainsKey(key) ? _context.Value[key] : null;

				public bool Contains(string key) => _context.Value.ContainsKey(key);

				public void Remove(string key) => _context.Value.Remove(key);

				public void Clear() => _context.Value.Clear();

				public void Set(string key, object value)
				{
					if (_context.Value.ContainsKey(key))
						_context.Value[key] = value;
					else
						_context.Value.Add(key, value);
				}

				#endregion
			}

			private class NestedVariablesContext : INestedVariablesContext
			{
				#region Fields

				private readonly static ThreadLocal<Stack<string>> _context = new ThreadLocal<Stack<string>>(() => new Stack<string>());


				#endregion

				#region INestedVariablesContext members..

				public bool HasItems => _context.Value.Count > 0;

				public string Peek() => _context.Value.Peek();

				public void Clear() => _context.Value.Clear();

				public string Pop() => _context.Value.Pop();

				public IDisposable Push(string text)
				{
					_context.Value.Push(text);

					return null; //< XXX: We don't need? To do here 'something' disposable to our tests btw..
				}

				#endregion
			}

			private class LogicalVariablesContext : IVariablesContext
			{
				#region Inner Classes

				// XXX: Originally taken from MappedDiagnosticsLogicalContext.cs (NLog).
				private static class Context
				{
					#region Inner Types

					private sealed class ItemRemover : IDisposable
					{
						#region Fields

						private readonly string _item;
						private readonly bool _wasEmpty;

						private int _disposed; //< Bool as int to allow the use of Interlocked.Exchange..

						#endregion

						#region .ctors

						public ItemRemover(string item, bool wasEmpty)
						{
							_item = item;
							_wasEmpty = wasEmpty;
						}

						#endregion

						private bool RemoveScopeWillClearContext()
						{
							var immutableDict = GetLogicalThreadDictionary(false);

							if (immutableDict.Count == 1 && immutableDict.ContainsKey(_item))
								return true;

							return false;
						}

						public void Dispose()
						{
							if (Interlocked.Exchange(ref _disposed, 1) == 0)
							{
								if (_wasEmpty && RemoveScopeWillClearContext())
								{
									Clear();
									return;
								}

								var dictionary = GetLogicalThreadDictionary(true);
								dictionary.Remove(_item);
							}
						}

						public override string ToString()
						{
							return _item?.ToString() ?? base.ToString();
						}
					}

					#endregion

					#region Fields

					private static readonly IDictionary<string, object> EmptyDefaultDictionary = new Dictionary<string, object>();

					private const string LogicalThreadDictionaryKey = nameof(LogicalThreadDictionaryKey);

					#endregion

					#region Private methods

					private static IDictionary<string, object> GetLogicalThreadDictionary(bool clone = false, int initialCapacity = 0)
					{
						var dictionary = GetThreadLocal();

						if (dictionary == null)
						{
							if (!clone)
								return EmptyDefaultDictionary;

							dictionary = new Dictionary<string, object>(initialCapacity);

							SetThreadLocal(dictionary);
						}
						else if (clone)
						{
							var newDictionary = new Dictionary<string, object>(dictionary.Count + initialCapacity);

							foreach (var keyValue in dictionary)
								newDictionary[keyValue.Key] = keyValue.Value;

							SetThreadLocal(newDictionary);

							return newDictionary;
						}

						return dictionary;
					}

					private static void SetThreadLocal(Dictionary<string, object> newValue)
					{
						if (newValue == null)
							CallContext.FreeNamedDataSlot(LogicalThreadDictionaryKey);
						else
							CallContext.LogicalSetData(LogicalThreadDictionaryKey, newValue);
					}

					private static Dictionary<string, object> GetThreadLocal()
					{
						return CallContext.LogicalGetData(LogicalThreadDictionaryKey) as Dictionary<string, object>;
					}

					#endregion

					public static void Set(string item, object value)
					{
						var logicalContext = GetLogicalThreadDictionary(true, 1);
						logicalContext[item] = value;
					}

					public static object GetObject(string item)
					{
						GetLogicalThreadDictionary().TryGetValue(item, out var value);

						return value;
					}

					public static bool Contains(string item)
					{
						return GetLogicalThreadDictionary().ContainsKey(item);
					}

					public static void Remove(string item)
					{
						GetLogicalThreadDictionary(true).Remove(item);
					}

					public static void Clear()
					{
						SetThreadLocal(null);
					}
				}

				#endregion

				#region IVariablesContext members..

				public void Set(string key, object value) => Context.Set(key, value);

				public bool Contains(string key) => Context.Contains(key);

				public object Get(string key) => Context.GetObject(key);

				public void Remove(string key) => Context.Remove(key);

				public void Clear() => Context.Clear();

				#endregion
			}

			private class LogicalNestedVariablesContext : INestedVariablesContext
			{
				#region Inner Classes

				// XXX: Originally taken from NestedDiagnosticsLogicalContext.cs (NLog).
				private static class Context
				{
					#region Inner Types

					private interface INestedContext : IDisposable
					{
						INestedContext Parent { get; }

						int FrameLevel { get; }
						string Value { get; }
					}

					[Serializable]
					private class NestedContext : INestedContext
					{
						#region Properties & Fields

						public INestedContext Parent { get; }
						public int FrameLevel { get; }
						public string Value { get; }

						private int _disposed;

						#endregion

						public static INestedContext CreateNestedContext(INestedContext parent, string value)
						{
							return new NestedContext(parent, value);
						}

						public NestedContext(INestedContext parent, string value)
						{
							Value = value;
							Parent = parent;
							FrameLevel = parent?.FrameLevel + 1 ?? 1;
						}

						public override string ToString()
						{
							return Value;
						}

						public void Dispose()
						{
							if (Interlocked.Exchange(ref _disposed, 1) != 1)
								PopString();
						}
					}

					#endregion

					#region Fields

					private const string NestedDiagnosticsContextKey = nameof(NestedDiagnosticsContextKey);

					#endregion

					#region Private methods

					private static void SetThreadLocal(INestedContext newValue)
					{
						if (newValue == null)
							CallContext.FreeNamedDataSlot(NestedDiagnosticsContextKey);
						else
							CallContext.LogicalSetData(NestedDiagnosticsContextKey, newValue);
					}

					private static INestedContext GetThreadLocal()
					{
						return CallContext.LogicalGetData(NestedDiagnosticsContextKey) as INestedContext;
					}

					#endregion

					public static IDisposable Push(string value)
					{
						var parent = GetThreadLocal();
						var current = NestedContext.CreateNestedContext(parent, value);

						SetThreadLocal(current);

						return current;
					}

					public static string[] GetAllMessages()
					{
						var currentContext = GetThreadLocal();

						if (currentContext == null)
							return new string[] { };

						var index = 0;
						var messages = new string[currentContext.FrameLevel];

						while (currentContext != null)
						{
							messages[index++] = currentContext.Value;
							currentContext = currentContext.Parent;
						}

						return messages;
					}

					public static string PopString()
					{
						var current = GetThreadLocal();

						if (current != null)
							SetThreadLocal(current.Parent);

						return current?.Value;
					}

					public static string Peek()
					{
						return GetThreadLocal()?.Value;
					}

					public static string Pop()
					{
						return PopString() ?? string.Empty;
					}

					public static void Clear()
					{
						SetThreadLocal(null);
					}
				}

				#endregion

				#region INestedVariablesContext members..

				public bool HasItems => Context.GetAllMessages().Length > 0;

				public IDisposable Push(string text) => Context.Push(text);

				public void Clear() => Context.Clear();

				public string Peek() => Context.Peek();

				public string Pop() => Context.Pop();

				#endregion
			}

			#endregion

			#region .ctors

			public DummyLog(bool useLogicalContexts)
			{
				_useLogicalContexts = useLogicalContexts;
			}

			#endregion

			// XXX: Expose our custom peek method for NestedVariablesContext.. that will facilitate test asserts.
			protected internal string PeekFromNestedContext()
			{
				return _useLogicalContexts
					? (NestedThreadVariablesContext as LogicalNestedVariablesContext).Peek()
					: (NestedThreadVariablesContext as NestedVariablesContext).Peek();
			}
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