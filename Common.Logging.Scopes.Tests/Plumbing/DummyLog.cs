using System;

using Common.Logging.Factory;

namespace Common.Logging.Scopes
{
	internal class DummyLog : AbstractLogger
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

		private readonly IVariablesContext _threadVariablesContext = new DummyVariablesContext();
		private readonly IVariablesContext _logicalThreadVariablesContext = new DummyLogicalVariablesContext();

		private readonly INestedVariablesContext _nestedVariablesContext = new DummyNestedVariablesContext();
		private readonly INestedVariablesContext _logicalNestedVariablesContext = new DummyLogicalNestedVariablesContext();

		public override IVariablesContext ThreadVariablesContext => _useLogicalContexts ? _logicalThreadVariablesContext : _threadVariablesContext;
		public override INestedVariablesContext NestedThreadVariablesContext => _useLogicalContexts ? _logicalNestedVariablesContext : _nestedVariablesContext;

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
				? (NestedThreadVariablesContext as DummyLogicalNestedVariablesContext).Peek()
				: (NestedThreadVariablesContext as DummyNestedVariablesContext).Peek();
		}
	}
}