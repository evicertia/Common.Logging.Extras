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

// Originally taken from: https://github.com/net-commons/common-logging/pull/176

using Common.Logging.Configuration;
using Common.Logging.Factory;

namespace Common.Logging.NLog53
{
	public class NLogLoggerFactoryAdapter : AbstractCachingLoggerFactoryAdapter
	{
		public NLogLoggerFactoryAdapter()
			: this(null)
		{ }

		public NLogLoggerFactoryAdapter(NameValueCollection properties)
			: base(true)
		{
			var configType = string.Empty;
			var configFile = string.Empty;

			if (properties != null)
			{
				if (properties["configType"] != null)
					configType = properties["configType"].ToUpperInvariant();

				if (properties["configFile"] != null)
				{
					configFile = properties["configFile"];
					if (configFile.StartsWith("~/") || configFile.StartsWith("~\\"))
						configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('/', '\\') + "/", configFile[2..]);
				}

				if (configType == "FILE")
				{
					if (string.IsNullOrWhiteSpace(configFile))
						throw new ConfigurationException("Configuration property 'configFile' must be set for NLog configuration of type 'FILE'.");

					if (!File.Exists(configFile))
						throw new ConfigurationException($"NLog configuration file '{configFile}' does not exist.");
				}
			}

			switch (configType)
			{
				case "INLINE":
					break;
				case "FILE":
					NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(configFile);
					break;
				default:
					break;
			}
		}

		protected override ILog CreateLogger(string name) =>
			new NLogLogger(NLog.LogManager.GetLogger(name));
	}
}
