using System;

namespace Spatial4n.Tests
{
	public static class RandomSeed
	{
		private static readonly long _seed;

		static RandomSeed()
		{
			//_seed = long.Parse(System.getProperty("tests.seed", "" + System.currentTimeMillis()));
			_seed = DateTime.Now.Ticks;
			//System.out.println("tests.seed="+_seed);
		}
		public static int Seed()
		{
			return (int)_seed;
		}
	}
}
