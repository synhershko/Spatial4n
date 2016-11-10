using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spatial4n.Tests
{
    public static class RandomExtensions
    {
        public static bool nextBoolean(this Random random)
        {
            return random.Next(100) % 2 == 0;
        }
    }
}
