using Common.Logging.Configuration;
using NLog.Config;
using NLog.Targets;

namespace Common.Logging.NLog53.Tests
{
	[TestFixture]
	[NonParallelizable]
	public class NLogLoggerFactoryAdapterTests
	{
		[TearDown]
		public void TearDown()
		{
			NLog.LogManager.Configuration = null;
		}

		[Test]
		public void Factory_Adapter_Default_Creates_Logger_Without_Throwing()
		{
			var config = new LoggingConfiguration();
			config.AddRuleForAllLevels(new MemoryTarget("memory") { Layout = "${message}" });
			NLog.LogManager.Configuration = config;

			var sut = new NLogLoggerFactoryAdapter();

			Assert.That(() => sut.GetLogger("factory-test"), Throws.Nothing);
		}

		[Test]
		public void Factory_Adapter_File_Mode_Without_Path_Throws_Configuration_Exception()
		{
			var properties = new NameValueCollection()
			{
				["configType"] = "FILE"
			};

			Assert.That(() => new NLogLoggerFactoryAdapter(properties), Throws.TypeOf<ConfigurationException>());
		}

		[Test]
		public void Factory_Adapter_File_Mode_With_Missing_File_Throws_Configuration_Exception()
		{
			var properties = new NameValueCollection()
			{
				["configType"] = "FILE",
				["configFile"] = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.config")
			};

			Assert.That(() => new NLogLoggerFactoryAdapter(properties), Throws.TypeOf<ConfigurationException>());
		}

		[Test]
		public void Factory_Adapter_External_Mode_Uses_Existing_NLog_Configuration()
		{
			var target = new MemoryTarget("existing") { Layout = "${message}" };
			var config = new LoggingConfiguration();
			config.AddRuleForAllLevels(target);
			NLog.LogManager.Configuration = config;

			var properties = new NameValueCollection()
			{
				["configType"] = "EXTERNAL"
			};

			var sut = new NLogLoggerFactoryAdapter(properties);
			var logger = sut.GetLogger("external-mode");

			logger.Info("hello");

			Assert.That(target.Logs, Has.Count.EqualTo(1));
			Assert.That(target.Logs[0], Is.EqualTo("hello"));
		}

		[Test]
		public void Factory_Adapter_Inline_Mode_Uses_Existing_NLog_Configuration()
		{
			var target = new MemoryTarget("existing-inline") { Layout = "${message}" };
			var config = new LoggingConfiguration();
			config.AddRuleForAllLevels(target);
			NLog.LogManager.Configuration = config;

			var properties = new NameValueCollection()
			{
				["configType"] = "INLINE"
			};

			var sut = new NLogLoggerFactoryAdapter(properties);
			var logger = sut.GetLogger("inline-mode");

			logger.Info("hello-inline");

			Assert.That(target.Logs, Has.Count.EqualTo(1));
			Assert.That(target.Logs[0], Is.EqualTo("hello-inline"));
		}
	}
}
