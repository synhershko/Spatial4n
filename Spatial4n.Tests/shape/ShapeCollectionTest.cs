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

using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;
using System.Collections.Generic;
using Xunit;

namespace Spatial4n.Core.Shape
{
    /** @author David Smiley - dsmiley@mitre.org */
    public class ShapeCollectionTest : RandomizedShapeTest
    {
        // TODO: figure out how to implement
        //      @Rule
        //public final TestLog testLog = TestLog.instance;

        [Fact]
        public virtual void TestBbox()
        {
            ValidateWorld(-180, 180, -180, 180);
            ValidateWorld(-180, 0, 0, +180);
            ValidateWorld(-90, +90, +90, -90);
        }

        private void ValidateWorld(double r1MinX, double r1MaxX, double r2MinX, double r2MaxX)
        {
            ctx = SpatialContext.GEO;
            IRectangle r1 = ctx.MakeRectangle(r1MinX, r1MaxX, -10, 10);
            IRectangle r2 = ctx.MakeRectangle(r2MinX, r2MaxX, -10, 10);

            ShapeCollection/*<Rectangle>*/ s = new ShapeCollection/*<Rectangle>*/(new IShape[] { r1, r2 }, ctx);
            Assert.Equal(Range.LongitudeRange.WORLD_180E180W, new Range.LongitudeRange(s.BoundingBox));

            //flip r1, r2 order
            s = new ShapeCollection/*<Rectangle>*/(new IShape[] { r2, r1 }, ctx);
            Assert.Equal(Range.LongitudeRange.WORLD_180E180W, new Range.LongitudeRange(s.BoundingBox));
        }

        [Fact]
        public virtual void TestRectIntersect()
        {
            SpatialContext ctx = new SpatialContextFactory()
            { geo = false, worldBounds = new Rectangle(-100, 100, -50, 50, null) }.CreateSpatialContext();

            new ShapeCollectionRectIntersectionTestHelper(ctx).TestRelateWithRectangle();
        }

        [Fact]
        public virtual void TestGeoRectIntersect()
        {
            ctx = SpatialContext.GEO;
            new ShapeCollectionRectIntersectionTestHelper(ctx).TestRelateWithRectangle();
        }

        private class ShapeCollectionRectIntersectionTestHelper : RectIntersectionTestHelper/*<ShapeCollection>*/
        {

            public ShapeCollectionRectIntersectionTestHelper(SpatialContext ctx)
                        : base(ctx)
            {
            }

            protected override /*ShapeCollection*/ IShape GenerateRandomShape(IPoint nearP)
            {
                //testLog.log("Break on nearP.toString(): {}", nearP);
                IList<IShape> shapes = new List<IShape>();
                int count = random.Next(1, 4 + 1);
                for (int i = 0; i < count; i++)
                {
                    //1st 2 are near nearP, the others are anywhere
                    shapes.Add(RandomRectangle(i < 2 ? nearP : null));
                }
                ShapeCollection shapeCollection = new ShapeCollection/*<Rectangle>*/(shapes, ctx);

                //test shapeCollection.getBoundingBox();
                IRectangle msBbox = shapeCollection.BoundingBox;
                if (shapes.Count == 1)
                {
                    Assert.Equal(shapes[0], msBbox.BoundingBox);
                }
                else
                {
                    foreach (IRectangle shape in shapes)
                    {
                        AssertRelation("bbox contains shape", SpatialRelation.Contains, msBbox, shape);
                    }
                }
                return shapeCollection;
            }

            protected override IPoint RandomPointInEmptyShape(/*ShapeCollection*/ IShape shape)
            {
                IRectangle r = (IRectangle)((ShapeCollection)shape).Shapes[0];
                return RandomPointIn(r);
            }
        }
    }
}
