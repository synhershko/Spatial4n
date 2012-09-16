using Xunit;

namespace Spatial4n.Tests
{
	public static class CustomAssert
	{
		/// <summary>
		/// This is purely here so that we have an easy was to make Xunit compatible with some of the JUnit tests methods
		/// </summary>
		/// <param name="expected"></param>
		/// <param name="actual"></param>
		/// <param name="delta"></param>
		public static void EqualWithDelta(double expected, double actual, double delta)
		{
			Assert.InRange(actual, expected - delta, expected + delta);
		}

        public static void EqualWithDelta(string msg, double expected, double actual, double delta)
        {
            Assert.InRange(actual, expected - delta, expected + delta);
        }
	}
}