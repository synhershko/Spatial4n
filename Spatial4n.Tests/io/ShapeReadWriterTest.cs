/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#if FEATURE_NTS
using Spatial4n.Core.Context.Nts;
#endif

using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;
using System.Collections.Generic;
using System.Globalization;
using Xunit;
using Xunit.Extensions;

namespace Spatial4n.Core.IO
{
    public class ShapeReadWriterTest
	{
		public static IEnumerable<object[]> Contexts
		{
			get
			{
				yield return new object[] { SpatialContext.GEO };
#if FEATURE_NTS
                yield return new object[] { NtsSpatialContext.GEO };
#endif
			}
		}

		private T WriteThenRead<T>(T s, SpatialContext ctx) where T : IShape
		{
			string buff = ctx.ToString(s);
			return (T)ctx.ReadShape(buff);
		}

		[Theory]
		[PropertyData("Contexts")]
		public virtual void TestPoint(SpatialContext ctx)
		{
			IShape s = ctx.ReadShape("10 20");
			Assert.Equal(ctx.MakePoint(10, 20), s);
			Assert.Equal(s, WriteThenRead(s, ctx));
			Assert.Equal(s, ctx.ReadShape("20,10"));//check comma for y,x format
			Assert.Equal(s, ctx.ReadShape("20, 10"));//test space
			Assert.False(s.HasArea);
		}

		[Theory]
		[PropertyData("Contexts")]
		public virtual void TestRectangle(SpatialContext ctx)
		{
			IShape s = ctx.ReadShape("-10 -20 10 20");
			Assert.Equal(ctx.MakeRectangle(-10, 10, -20, 20), s);
			Assert.Equal(s, WriteThenRead(s, ctx));
			Assert.True(s.HasArea);
		}

		[Theory]
		[PropertyData("Contexts")]
		public virtual void TestCircle(SpatialContext ctx)
		{
			IShape s = ctx.ReadShape("Circle(1.23 4.56 distance=7.89)");
			Assert.Equal(ctx.MakeCircle(1.23, 4.56, 7.89), s);
			Assert.Equal(s, WriteThenRead(s, ctx));
			Assert.Equal(s, ctx.ReadShape("CIRCLE( 4.56,1.23 d=7.89 )")); // use lat,lon and use 'd' abbreviation
			Assert.True(s.HasArea);
		}

        [Theory]
        [PropertyData("Contexts")]
        public virtual void TestCircleWithCriticalCulture(SpatialContext ctx)
        {
            using (new TemporaryCulture(new CultureInfo("de-DE")))
            {
                IShape s = ctx.ReadShape("Circle(1.23 4.56 distance=7.89)");
                Assert.Equal(ctx.MakeCircle(1.23, 4.56, 7.89), s);
                Assert.Equal(s, WriteThenRead(s, ctx));
                Assert.Equal(s, ctx.ReadShape("CIRCLE( 4.56,1.23 d=7.89 )")); // use lat,lon and use 'd' abbreviation
                Assert.True(s.HasArea);
            }
        }


		//  Looking for more tests?  Shapes are tested in TestShapes2D.

	}
}
