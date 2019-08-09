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

using Spatial4n.Core.Context;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.IO.Nts;
using Spatial4n.Core.Shapes;
using Xunit;

namespace Spatial4n.Core.IO
{
#pragma warning disable 612, 618
    public class NtsWKTReaderShapeParserTest
    {
        internal readonly SpatialContext ctx;

        public NtsWKTReaderShapeParserTest()
        {
            NtsSpatialContextFactory factory = new NtsSpatialContextFactory();
            factory.datelineRule = DatelineRule.CcwRect;
            factory.wktShapeParserClass = typeof(NtsWKTReaderShapeParser);
            ctx = factory.NewSpatialContext();
        }

        [Fact]
        public virtual void WktGeoPt()
        {
            IShape s = ctx.ReadShape("Point(-160 30)");
            Assert.Equal(ctx.MakePoint(-160, 30), s);
        }

        [Fact]
        public virtual void WktGeoRect()
        {
            //REMEMBER: Polygon WKT's outer ring is counter-clockwise order. If you accidentally give the other direction,
            // NtsSpatialContext will give the wrong result for a rectangle crossing the dateline.

            // In these two tests, we give the same set of points, one that does not cross the dateline, and the 2nd does. The
            // order is counter-clockwise in both cases as it should be.

            IShape sNoDL = ctx.ReadShape("Polygon((-170 30, -170 15,  160 15,  160 30, -170 30))");
            IRectangle expectedNoDL = ctx.MakeRectangle(-170, 160, 15, 30);
            Assert.True(!expectedNoDL.CrossesDateLine);
            Assert.Equal(expectedNoDL, sNoDL);

            IShape sYesDL = ctx.ReadShape("Polygon(( 160 30,  160 15, -170 15, -170 30,  160 30))");
            IRectangle expectedYesDL = ctx.MakeRectangle(160, -170, 15, 30);
            Assert.True(expectedYesDL.CrossesDateLine);
            Assert.Equal(expectedYesDL, sYesDL);

        }


        [Fact]
        public virtual void TestWrapTopologyException()
        {
            try
            {
                ctx.ReadShape("POLYGON((0 0, 10 0, 10 20))");//doesn't connect around
                Assert.True(false);
            }
#pragma warning disable 168
            catch (InvalidShapeException e)
#pragma warning restore 168
            {
                //expected
            }

            try
            {
                ctx.ReadShape("POLYGON((0 0, 10 0, 10 20, 5 -5, 0 20, 0 0))");//Topology self-intersect
                Assert.True(false);
            }
#pragma warning disable 168
            catch (InvalidShapeException e)
#pragma warning restore 168
            {
                //expected
            }
        }
    }
#pragma warning restore 612, 618
}

#endif