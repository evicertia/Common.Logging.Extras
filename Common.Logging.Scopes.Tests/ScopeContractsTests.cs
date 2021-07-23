using System.Collections.Generic;

using NUnit.Framework;

namespace Common.Logging.Scopes
{
	public class ScopeContractsTests
	{
		#region Fields

		private ILog _log;

		#endregion

		#region SetUp & TearDown

		[SetUp]
		public void SetUp()
		{
			_log = new DummyLog(useLogicalContexts: false);
		}

		[TearDown]
		public void TearDown()
		{
			_log = null;
		}

		#endregion

		[Test]
		public void Push_Thread_Scoped_Variable_Throws_An_Error_If_Scope_Not_Exists()
		{
			// XXX: Due 'dummy log' is cleaned on tear down and initialized again on set up.. CustomStack<> with a logging scope is not present.
			Assert.That(() => _log.PushThreadScopedVariable("X", "Y"), Throws.Exception, "#0");
			Assert.That(() => _log.PushThreadScopedVariablesFor(new Dictionary<string, object>() { { "X", "Y" } }), Throws.Exception, "#1");
		}

		[Test]
		public void Get_Scope_Variables_Returns_Logging_Fields_Without_Modifying_The_Original_Dictionary()
		{
			using (_log.BeginThreadScope("..."))
			{
				_log.PushThreadScopedVariable("A", 1);

				var scopeFields = _log.GetScopeVariables();

				Assert.Multiple(() =>
				{
					Assert.That(scopeFields, Is.Not.Null.Or.Empty, "#1");
					Assert.That(scopeFields.ContainsKey("A"), Is.True, "#2");
				});
			}
		}
	}
}