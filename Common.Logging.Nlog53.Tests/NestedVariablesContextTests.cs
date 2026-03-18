namespace Common.Logging.NLog53.Tests
{
	[TestFixture]
	[NonParallelizable]
	public class NestedVariablesContextTests
	{
		[TearDown]
		public void TearDown()
		{
			new NLogNestedThreadVariablesContext().Clear();
			new NLogNestedLogicalThreadVariablesContext().Clear();
		}

		[Test]
		public void Nested_Thread_Context_Push_Then_Pop_Returns_Last_Value()
		{
			var sut = new NLogNestedThreadVariablesContext();

			sut.Push("a");
			sut.Push("b");

			Assert.That(sut.HasItems, Is.True);
			Assert.That(sut.Pop(), Is.EqualTo("b"));
			Assert.That(sut.Pop(), Is.EqualTo("a"));
			Assert.That(sut.HasItems, Is.False);
		}

		[Test]
		public void Nested_Thread_Context_Dispose_Outer_Before_Inner_Keeps_Stack_Consistent()
		{
			var sut = new NLogNestedThreadVariablesContext();

			using var outer = sut.Push("outer");
			using var inner = sut.Push("inner");

			outer.Dispose();

			Assert.That(sut.HasItems, Is.True);
			Assert.That(sut.Pop(), Is.EqualTo("inner"));
			Assert.That(sut.HasItems, Is.False);
		}

		[Test]
		public void Nested_Logical_Context_Push_Then_Pop_Returns_Last_Value()
		{
			var sut = new NLogNestedLogicalThreadVariablesContext();

			sut.Push("a");
			sut.Push("b");

			Assert.That(sut.HasItems, Is.True);
			Assert.That(sut.Pop(), Is.EqualTo("b"));
			Assert.That(sut.Pop(), Is.EqualTo("a"));
			Assert.That(sut.HasItems, Is.False);
		}

		[Test]
		public void Nested_Logical_Context_Dispose_Outer_Before_Inner_Keeps_Stack_Consistent()
		{
			var sut = new NLogNestedLogicalThreadVariablesContext();

			using var outer = sut.Push("outer");
			using var inner = sut.Push("inner");

			outer.Dispose();

			Assert.That(sut.HasItems, Is.True);
			Assert.That(sut.Pop(), Is.EqualTo("inner"));
			Assert.That(sut.HasItems, Is.False);
		}

		[Test]
		public async Task Nested_Logical_Context_Push_Across_Await_Then_Pop_Preserves_Order()
		{
			var sut = new NLogNestedLogicalThreadVariablesContext();

			sut.Push("a");
			await Task.Yield();
			sut.Push("b");
			await Task.Yield();

			Assert.That(sut.HasItems, Is.True);
			Assert.That(sut.Pop(), Is.EqualTo("b"));
			Assert.That(sut.Pop(), Is.EqualTo("a"));
			Assert.That(sut.HasItems, Is.False);
		}

		[Test]
		public async Task Nested_Logical_Context_Dispose_Pushed_Scope_Across_Await_Removes_Item()
		{
			var sut = new NLogNestedLogicalThreadVariablesContext();

			using var scope = sut.Push("a");
			await Task.Yield();

			Assert.That(sut.HasItems, Is.True);

			scope.Dispose();
			await Task.Yield();

			Assert.That(sut.HasItems, Is.False);
		}

		[Test]
		public async Task Nested_Logical_Context_Task_Run_Child_Push_Does_Not_Modify_Parent_Stack()
		{
			var sut = new NLogNestedLogicalThreadVariablesContext();
			using var parent = sut.Push("parent");

			var childTop = await Task.Run(() =>
			{
				Assert.That(sut.HasItems, Is.True);
				using var child = sut.Push("child");
				return sut.Pop();
			});

			Assert.That(childTop, Is.EqualTo("child"));
			Assert.That(sut.HasItems, Is.True);
			Assert.That(sut.Pop(), Is.EqualTo("parent"));
		}

		[Test]
		public async Task Nested_Logical_Context_Parallel_For_Does_Not_Leak_Child_Stack_To_Parent()
		{
			var sut = new NLogNestedLogicalThreadVariablesContext();
			using var parent = sut.Push("parent");

			var errors = new ConcurrentQueue<Exception>();

			Parallel.For(0, 24, i =>
			{
				try
				{
					using var child = sut.Push($"child-{i}");
					Assert.That(sut.Pop(), Is.EqualTo($"child-{i}"));
					Assert.That(sut.HasItems, Is.True);
				}
				catch (Exception ex)
				{
					errors.Enqueue(ex);
				}
			});

			await Task.Yield();

			Assert.That(errors, Is.Empty);
			Assert.That(sut.HasItems, Is.True);
			Assert.That(sut.Pop(), Is.EqualTo("parent"));
		}
	}
}
