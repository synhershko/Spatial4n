using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Extensions;
using Xunit.Sdk;

namespace Spatial4n.Tests
{
	public class RepeatTestAttribute : FactAttribute
	{
		readonly int _count;

		public RepeatTestAttribute(int count)
		{
			_count = count;
		}

		protected override IEnumerable<ITestCommand> EnumerateTestCommands(
			IMethodInfo method)
		{
			return base.EnumerateTestCommands(method)
				.SelectMany(tc => Enumerable.Repeat(tc, _count));
		}
	}

	public class RepeatTheoryAttribute : TheoryAttribute
	{
		readonly int _count;

		public RepeatTheoryAttribute(int count)
		{
			_count = count;
		}

		protected override IEnumerable<ITestCommand> EnumerateTestCommands(
			IMethodInfo method)
		{
			return base.EnumerateTestCommands(method)
				.SelectMany(tc => Enumerable.Repeat(tc, _count));
		}
	}
}
