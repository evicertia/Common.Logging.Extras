#region License

/*
 * Copyright © 2002-2007 the original author or authors.
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

using LogEventInfo = NLog.LogEventInfo;
using LoggerNLog = NLog.Logger;
using LogLevelNLog = NLog.LogLevel;

namespace Common.Logging.NLog53
{
	public class NLogLogger : ILog
	{
		private readonly LoggerNLog _logger;

		public NLogLogger(LoggerNLog logger)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public static bool FavorLogicalVariableContexts { get; set; }

		public bool IsTraceEnabled => _logger.IsTraceEnabled;
		public bool IsDebugEnabled => _logger.IsDebugEnabled;
		public bool IsInfoEnabled => _logger.IsInfoEnabled;
		public bool IsWarnEnabled => _logger.IsWarnEnabled;
		public bool IsErrorEnabled => _logger.IsErrorEnabled;
		public bool IsFatalEnabled => _logger.IsFatalEnabled;

		public IVariablesContext GlobalVariablesContext => new NLogGlobalVariablesContext();
		public IVariablesContext ThreadVariablesContext => FavorLogicalVariableContexts ? LogicalThreadVariablesContext : PerThreadVariablesContext;
		public INestedVariablesContext NestedThreadVariablesContext => FavorLogicalVariableContexts ? NestedLogicalThreadVariablesContext : NestedPerThreadVariablesContext;

		public IVariablesContext PerThreadVariablesContext => new NLogThreadVariablesContext();
		public INestedVariablesContext NestedPerThreadVariablesContext => new NLogNestedThreadVariablesContext();
		public IVariablesContext LogicalThreadVariablesContext => new NLogLogicalThreadVariablesContext();
		public INestedVariablesContext NestedLogicalThreadVariablesContext => new NLogNestedLogicalThreadVariablesContext();

		public void Trace(object message) => WriteObject(LogLevelNLog.Trace, message, null, IsTraceEnabled);
		public void Trace(object message, Exception exception) => WriteObject(LogLevelNLog.Trace, message, exception, IsTraceEnabled);
		public void Trace(Action<FormatMessageHandler> formatMessageCallback) => WriteCallback(LogLevelNLog.Trace, null, formatMessageCallback, null, IsTraceEnabled);
		public void Trace(Action<FormatMessageHandler> formatMessageCallback, Exception exception) => WriteCallback(LogLevelNLog.Trace, exception, formatMessageCallback, null, IsTraceEnabled);
		public void Trace(IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback) => WriteCallback(LogLevelNLog.Trace, null, formatMessageCallback, formatProvider, IsTraceEnabled);
		public void Trace(IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception) => WriteCallback(LogLevelNLog.Trace, exception, formatMessageCallback, formatProvider, IsTraceEnabled);
		public void TraceFormat(string format, params object[] args) => WriteFormat(LogLevelNLog.Trace, null, null, format, args, IsTraceEnabled);
		public void TraceFormat(string format, Exception exception, params object[] args) => WriteFormat(LogLevelNLog.Trace, null, exception, format, args, IsTraceEnabled);
		public void TraceFormat(IFormatProvider formatProvider, string format, params object[] args) => WriteFormat(LogLevelNLog.Trace, formatProvider, null, format, args, IsTraceEnabled);
		public void TraceFormat(IFormatProvider formatProvider, string format, Exception exception, params object[] args) => WriteFormat(LogLevelNLog.Trace, formatProvider, exception, format, args, IsTraceEnabled);

		public void Debug(object message) => WriteObject(LogLevelNLog.Debug, message, null, IsDebugEnabled);
		public void Debug(object message, Exception exception) => WriteObject(LogLevelNLog.Debug, message, exception, IsDebugEnabled);
		public void Debug(Action<FormatMessageHandler> formatMessageCallback) => WriteCallback(LogLevelNLog.Debug, null, formatMessageCallback, null, IsDebugEnabled);
		public void Debug(Action<FormatMessageHandler> formatMessageCallback, Exception exception) => WriteCallback(LogLevelNLog.Debug, exception, formatMessageCallback, null, IsDebugEnabled);
		public void Debug(IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback) => WriteCallback(LogLevelNLog.Debug, null, formatMessageCallback, formatProvider, IsDebugEnabled);
		public void Debug(IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception) => WriteCallback(LogLevelNLog.Debug, exception, formatMessageCallback, formatProvider, IsDebugEnabled);
		public void DebugFormat(string format, params object[] args) => WriteFormat(LogLevelNLog.Debug, null, null, format, args, IsDebugEnabled);
		public void DebugFormat(string format, Exception exception, params object[] args) => WriteFormat(LogLevelNLog.Debug, null, exception, format, args, IsDebugEnabled);
		public void DebugFormat(IFormatProvider formatProvider, string format, params object[] args) => WriteFormat(LogLevelNLog.Debug, formatProvider, null, format, args, IsDebugEnabled);
		public void DebugFormat(IFormatProvider formatProvider, string format, Exception exception, params object[] args) => WriteFormat(LogLevelNLog.Debug, formatProvider, exception, format, args, IsDebugEnabled);

		public void Info(object message) => WriteObject(LogLevelNLog.Info, message, null, IsInfoEnabled);
		public void Info(object message, Exception exception) => WriteObject(LogLevelNLog.Info, message, exception, IsInfoEnabled);
		public void Info(Action<FormatMessageHandler> formatMessageCallback) => WriteCallback(LogLevelNLog.Info, null, formatMessageCallback, null, IsInfoEnabled);
		public void Info(Action<FormatMessageHandler> formatMessageCallback, Exception exception) => WriteCallback(LogLevelNLog.Info, exception, formatMessageCallback, null, IsInfoEnabled);
		public void Info(IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback) => WriteCallback(LogLevelNLog.Info, null, formatMessageCallback, formatProvider, IsInfoEnabled);
		public void Info(IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception) => WriteCallback(LogLevelNLog.Info, exception, formatMessageCallback, formatProvider, IsInfoEnabled);
		public void InfoFormat(string format, params object[] args) => WriteFormat(LogLevelNLog.Info, null, null, format, args, IsInfoEnabled);
		public void InfoFormat(string format, Exception exception, params object[] args) => WriteFormat(LogLevelNLog.Info, null, exception, format, args, IsInfoEnabled);
		public void InfoFormat(IFormatProvider formatProvider, string format, params object[] args) => WriteFormat(LogLevelNLog.Info, formatProvider, null, format, args, IsInfoEnabled);
		public void InfoFormat(IFormatProvider formatProvider, string format, Exception exception, params object[] args) => WriteFormat(LogLevelNLog.Info, formatProvider, exception, format, args, IsInfoEnabled);

		public void Warn(object message) => WriteObject(LogLevelNLog.Warn, message, null, IsWarnEnabled);
		public void Warn(object message, Exception exception) => WriteObject(LogLevelNLog.Warn, message, exception, IsWarnEnabled);
		public void Warn(Action<FormatMessageHandler> formatMessageCallback) => WriteCallback(LogLevelNLog.Warn, null, formatMessageCallback, null, IsWarnEnabled);
		public void Warn(Action<FormatMessageHandler> formatMessageCallback, Exception exception) => WriteCallback(LogLevelNLog.Warn, exception, formatMessageCallback, null, IsWarnEnabled);
		public void Warn(IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback) => WriteCallback(LogLevelNLog.Warn, null, formatMessageCallback, formatProvider, IsWarnEnabled);
		public void Warn(IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception) => WriteCallback(LogLevelNLog.Warn, exception, formatMessageCallback, formatProvider, IsWarnEnabled);
		public void WarnFormat(string format, params object[] args) => WriteFormat(LogLevelNLog.Warn, null, null, format, args, IsWarnEnabled);
		public void WarnFormat(string format, Exception exception, params object[] args) => WriteFormat(LogLevelNLog.Warn, null, exception, format, args, IsWarnEnabled);
		public void WarnFormat(IFormatProvider formatProvider, string format, params object[] args) => WriteFormat(LogLevelNLog.Warn, formatProvider, null, format, args, IsWarnEnabled);
		public void WarnFormat(IFormatProvider formatProvider, string format, Exception exception, params object[] args) => WriteFormat(LogLevelNLog.Warn, formatProvider, exception, format, args, IsWarnEnabled);

		public void Error(object message) => WriteObject(LogLevelNLog.Error, message, null, IsErrorEnabled);
		public void Error(object message, Exception exception) => WriteObject(LogLevelNLog.Error, message, exception, IsErrorEnabled);
		public void Error(Action<FormatMessageHandler> formatMessageCallback) => WriteCallback(LogLevelNLog.Error, null, formatMessageCallback, null, IsErrorEnabled);
		public void Error(Action<FormatMessageHandler> formatMessageCallback, Exception exception) => WriteCallback(LogLevelNLog.Error, exception, formatMessageCallback, null, IsErrorEnabled);
		public void Error(IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback) => WriteCallback(LogLevelNLog.Error, null, formatMessageCallback, formatProvider, IsErrorEnabled);
		public void Error(IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception) => WriteCallback(LogLevelNLog.Error, exception, formatMessageCallback, formatProvider, IsErrorEnabled);
		public void ErrorFormat(string format, params object[] args) => WriteFormat(LogLevelNLog.Error, null, null, format, args, IsErrorEnabled);
		public void ErrorFormat(string format, Exception exception, params object[] args) => WriteFormat(LogLevelNLog.Error, null, exception, format, args, IsErrorEnabled);
		public void ErrorFormat(IFormatProvider formatProvider, string format, params object[] args) => WriteFormat(LogLevelNLog.Error, formatProvider, null, format, args, IsErrorEnabled);
		public void ErrorFormat(IFormatProvider formatProvider, string format, Exception exception, params object[] args) => WriteFormat(LogLevelNLog.Error, formatProvider, exception, format, args, IsErrorEnabled);

		public void Fatal(object message) => WriteObject(LogLevelNLog.Fatal, message, null, IsFatalEnabled);
		public void Fatal(object message, Exception exception) => WriteObject(LogLevelNLog.Fatal, message, exception, IsFatalEnabled);
		public void Fatal(Action<FormatMessageHandler> formatMessageCallback) => WriteCallback(LogLevelNLog.Fatal, null, formatMessageCallback, null, IsFatalEnabled);
		public void Fatal(Action<FormatMessageHandler> formatMessageCallback, Exception exception) => WriteCallback(LogLevelNLog.Fatal, exception, formatMessageCallback, null, IsFatalEnabled);
		public void Fatal(IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback) => WriteCallback(LogLevelNLog.Fatal, null, formatMessageCallback, formatProvider, IsFatalEnabled);
		public void Fatal(IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception) => WriteCallback(LogLevelNLog.Fatal, exception, formatMessageCallback, formatProvider, IsFatalEnabled);
		public void FatalFormat(string format, params object[] args) => WriteFormat(LogLevelNLog.Fatal, null, null, format, args, IsFatalEnabled);
		public void FatalFormat(string format, Exception exception, params object[] args) => WriteFormat(LogLevelNLog.Fatal, null, exception, format, args, IsFatalEnabled);
		public void FatalFormat(IFormatProvider formatProvider, string format, params object[] args) => WriteFormat(LogLevelNLog.Fatal, formatProvider, null, format, args, IsFatalEnabled);
		public void FatalFormat(IFormatProvider formatProvider, string format, Exception exception, params object[] args) => WriteFormat(LogLevelNLog.Fatal, formatProvider, exception, format, args, IsFatalEnabled);

		private void WriteObject(LogLevelNLog level, object message, Exception exception, bool enabled)
		{
			if (!enabled)
				return;

			if (message is string text)
			{
				Write(level, null, exception, text, null);
				return;
			}

			exception ??= message as Exception;
			Write(level, null, exception, "{0}", new[] { message });
		}

		private void WriteCallback(LogLevelNLog level, Exception exception, Action<FormatMessageHandler> callback, IFormatProvider formatProvider, bool enabled)
		{
			if (!enabled || callback == null)
				return;

			var format = string.Empty;
			object[] args = null;

			callback((msgFormat, msgArgs) =>
			{
				format = msgFormat;
				args = msgArgs;
				return formatProvider != null
					? string.Format(formatProvider, msgFormat, msgArgs)
					: string.Format(msgFormat, msgArgs);
			});

			Write(level, formatProvider, exception, format, args);
		}

		private void WriteFormat(LogLevelNLog level, IFormatProvider formatProvider, Exception exception, string format, object[] args, bool enabled)
		{
			if (!enabled)
				return;

			Write(level, formatProvider, exception, format, args);
		}

		private void Write(LogLevelNLog level, IFormatProvider formatProvider, Exception exception, string format, object[] args)
		{
			var logEvent = new LogEventInfo(level, _logger.Name, formatProvider, format, args, exception);
			_logger.Log(logEvent);
		}
	}
}
