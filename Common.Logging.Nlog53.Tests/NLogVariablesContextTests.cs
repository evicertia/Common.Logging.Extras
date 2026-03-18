namespace Common.Logging.NLog53.Tests
{
	[TestFixture]
	[NonParallelizable]
	public class NLogVariablesContextTests
	{
		[TearDown]
		public void TearDown()
		{
			new NLogThreadVariablesContext().Clear();
			new NLogLogicalThreadVariablesContext().Clear();
			new NLogGlobalVariablesContext().Clear();
		}

		[Test]
		public void Thread_Context_Set_Then_Get_Returns_Value()
		{
			var sut = new NLogThreadVariablesContext();

			sut.Set("k", "v1");

			Assert.That(sut.Contains("k"), Is.True);
			Assert.That(sut.Get("k"), Is.EqualTo("v1"));
		}

		[Test]
		public void Thread_Context_Set_Twice_Then_Remove_Restores_Previous_Value()
		{
			var sut = new NLogThreadVariablesContext();

			sut.Set("k", "v1");
			sut.Set("k", "v2");
			sut.Remove("k");

			Assert.That(sut.Contains("k"), Is.True);
			Assert.That(sut.Get("k"), Is.EqualTo("v1"));

			sut.Remove("k");

			Assert.That(sut.Contains("k"), Is.False);
			Assert.That(sut.Get("k"), Is.Null);
		}

		[Test]
		public async Task Logical_Context_Set_Then_Await_Preserves_Value()
		{
			var sut = new NLogLogicalThreadVariablesContext();

			sut.Set("k", "v1");
			await Task.Yield();

			Assert.That(sut.Contains("k"), Is.True);
			Assert.That(sut.Get("k"), Is.EqualTo("v1"));
		}

		[Test]
		public async Task Logical_Context_Set_Twice_Across_Await_Then_Remove_Restores_Previous_Value()
		{
			var sut = new NLogLogicalThreadVariablesContext();

			sut.Set("k", "v1");
			await Task.Yield();
			sut.Set("k", "v2");
			await Task.Yield();

			Assert.That(sut.Get("k"), Is.EqualTo("v2"));

			sut.Remove("k");
			await Task.Yield();

			Assert.That(sut.Get("k"), Is.EqualTo("v1"));
			Assert.That(sut.Contains("k"), Is.True);

			sut.Remove("k");
			Assert.That(sut.Contains("k"), Is.False);
		}

		[Test]
		public async Task Logical_Context_Clear_Across_Await_Removes_All_Values()
		{
			var sut = new NLogLogicalThreadVariablesContext();

			sut.Set("k1", "v1");
			sut.Set("k2", "v2");
			await Task.Yield();

			sut.Clear();
			await Task.Yield();

			Assert.That(sut.Contains("k1"), Is.False);
			Assert.That(sut.Contains("k2"), Is.False);
			Assert.That(sut.Get("k1"), Is.Null);
			Assert.That(sut.Get("k2"), Is.Null);
		}

		[Test]
		public async Task Logical_Context_Task_Run_Child_Override_Does_Not_Modify_Parent_State()
		{
			var sut = new NLogLogicalThreadVariablesContext();
			sut.Set("k", "parent");

			var childValue = await Task.Run(() => {
				Assert.That(sut.Get("k"), Is.EqualTo("parent"));
				sut.Set("k", "child");
				return sut.Get("k");
			});

			Assert.That(childValue, Is.EqualTo("child"));
			Assert.That(sut.Get("k"), Is.EqualTo("parent"));
		}

		[Test]
		public async Task Logical_Context_Parallel_For_Does_Not_Leak_Child_Value_To_Parent()
		{
			var sut = new NLogLogicalThreadVariablesContext();
			sut.Set("k", "parent");

			var errors = new ConcurrentQueue<Exception>();

			Parallel.For(0, 32, i => {
				try
				{
					Assert.That(sut.Get("k"), Is.EqualTo("parent"));
					sut.Set("k", $"child-{i}");
					Assert.That(sut.Get("k"), Is.EqualTo($"child-{i}"));
					sut.Remove("k");
					Assert.That(sut.Get("k"), Is.EqualTo("parent"));
				}
				catch (Exception ex)
				{
					errors.Enqueue(ex);
				}
			});

			await Task.Yield();

			Assert.That(errors, Is.Empty);
			Assert.That(sut.Get("k"), Is.EqualTo("parent"));
		}

		[Test]
		public void Global_Context_Set_Then_Clear_Removes_Value()
		{
			var sut = new NLogGlobalVariablesContext();

			sut.Set("k", "v1");
			Assert.That(sut.Contains("k"), Is.True);

			sut.Clear();

			Assert.That(sut.Contains("k"), Is.False);
			Assert.That(sut.Get("k"), Is.Null);
		}
	}
}
