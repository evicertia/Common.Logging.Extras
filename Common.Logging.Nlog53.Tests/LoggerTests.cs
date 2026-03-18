using NLog;
using NLog.Config;
using NLog.Targets;

namespace Common.Logging.NLog53.Tests
{
	[TestFixture]
	[NonParallelizable]
	public class LoggerTests
	{
		private MemoryTarget _target;

		[SetUp]
		public void SetUp()
		{
			_target = new MemoryTarget("memory")
			{
				Layout = "${message}"
			};

			var config = new LoggingConfiguration();
			config.AddRuleForAllLevels(_target);
			NLog.LogManager.Configuration = config;
		}

		[TearDown]
		public void TearDown()
		{
			ScopeContext.Clear();
			NLog.LogManager.Configuration = null;
			_target?.Dispose();
		}

		[Test]
		public void InfoFormat_With_Named_Placeholder_Does_Not_Throw()
		{
			var sut = new NLogLogger(NLog.LogManager.GetLogger("nlog53-test"));

			Assert.That(() => sut.InfoFormat("Unsuccessful authentication by user {user.email}", "user@test"), Throws.Nothing);
		}

		[Test]
		public void InfoFormat_With_Named_Placeholder_Writes_Message_With_Value()
		{
			var sut = new NLogLogger(NLog.LogManager.GetLogger("nlog53-test"));

			sut.InfoFormat("Unsuccessful authentication by user {user.email}", "user@test");

			Assert.That(_target.Logs.Count, Is.EqualTo(1));
			Assert.That(_target.Logs[0], Does.Contain("user@test"));
		}
	}
}
