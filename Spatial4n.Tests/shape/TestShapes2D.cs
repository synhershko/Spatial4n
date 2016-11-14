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
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Xunit.Extensions;

namespace Spatial4n.Tests.shape
{
    public class TestShapes2D : AbstractTestShapes
	{
		public static IEnumerable<object[]> Contexts
		{
			get
			{
                IRectangle WB = new Rectangle(-2000, 2000, -300, 300, null);//whatever
				yield return new object[] { new SpatialContextFactory() { geo = false, worldBounds = WB }.NewSpatialContext() };
				yield return new object[] { new NtsSpatialContextFactory() { geo = false, worldBounds = WB }.NewSpatialContext() };
			}
		}

        //public TestShapes2D(SpatialContext ctx)
        //    : base(ctx)
        //{
        //}

        [Theory]
		[PropertyData("Contexts")]
		public virtual void TestSimplePoint(SpatialContext ctx)
		{
			base.ctx = ctx;

		    Assert.Throws<InvalidShapeException>(() => ctx.MakePoint(2001, 0));
		    Assert.Throws<InvalidShapeException>(() => ctx.MakePoint(0, -301));

			IPoint pt = ctx.MakePoint(0, 0);
			string msg = pt.ToString();

			//test equals & hashcode
			IPoint pt2 = ctx.MakePoint(0, 0);
			Assert.Equal(/*msg,*/ pt, pt2);
			Assert.Equal(/*msg,*/ pt.GetHashCode(), pt2.GetHashCode());

			Assert.False(pt.HasArea, msg);
			Assert.Equal(/*msg,*/ pt.Center, pt);
			IRectangle bbox = pt.BoundingBox;
			Assert.False(bbox.HasArea, msg);
			
			var center = bbox.Center;
			Assert.True(pt.Equals(center));
			//Assert.Equal(/*msg,*/ pt, center);

			AssertRelation(msg, SpatialRelation.CONTAINS, pt, pt2);
			AssertRelation(msg, SpatialRelation.DISJOINT, pt, ctx.MakePoint(0, 1));
			AssertRelation(msg, SpatialRelation.DISJOINT, pt, ctx.MakePoint(1, 0));
			AssertRelation(msg, SpatialRelation.DISJOINT, pt, ctx.MakePoint(1, 1));

            pt.Reset(1, 2);
            Assert.Equal(ctx.MakePoint(1, 2), pt);

            Assert.Equal(ctx.MakeCircle(pt, 3), pt.GetBuffered(3, ctx));

            TestEmptiness(ctx.MakePoint(double.NaN, double.NaN));
        }

		[Theory]
		[PropertyData("Contexts")]
		public virtual void TestSimpleRectangle(SpatialContext ctx)
		{
			base.ctx = ctx;

            double v = 2001 * (random.NextDouble() > 0.5 ? -1 : 1);
		    Assert.Throws<InvalidShapeException>(() => ctx.MakeRectangle(v,0,0,0));
            Assert.Throws<InvalidShapeException>(() => ctx.MakeRectangle(0,v,0,0));
            Assert.Throws<InvalidShapeException>(() => ctx.MakeRectangle(0,0,v,0));
            Assert.Throws<InvalidShapeException>(() => ctx.MakeRectangle(0,0,0,v));
            Assert.Throws<InvalidShapeException>(() => ctx.MakeRectangle(0,0,10,-10));
            Assert.Throws<InvalidShapeException>(() => ctx.MakeRectangle(10, -10, 0, 0));

			double[] minXs = new double[] { -1000, -360, -180, -20, 0, 20, 180, 1000 };
			foreach (double minX in minXs)
			{
				double[] widths = new double[] { 0, 10, 180, 360, 400 };
				foreach (double width in widths)
				{
					TestRectangle(minX, width, 0, 0);
					TestRectangle(minX, width, -10, 10);
					TestRectangle(minX, width, 5, 10);
				}
			}

            IRectangle r = ctx.MakeRectangle(0, 0, 0, 0);
            r.Reset(1, 2, 3, 4);
            Assert.Equal(ctx.MakeRectangle(1, 2, 3, 4), r);

			TestRectIntersect();

            if (!ctx.IsGeo)
                AssertEquals(ctx.MakeRectangle(0.9, 2.1, 2.9, 4.1), ctx.MakeRectangle(1, 2, 3, 4).GetBuffered(0.1, ctx));

            TestEmptiness(ctx.MakeRectangle(double.NaN, double.NaN, double.NaN, double.NaN));
        }

		[Theory]
		[PropertyData("Contexts")]
		public virtual void TestSimpleCircle(SpatialContext ctx)
		{
			base.ctx = ctx;

			double[] theXs = new double[] { -10, 0, 10 };
			foreach (double x in theXs)
			{
				double[] theYs = new double[] { -20, 0, 20 };
				foreach (double y in theYs)
				{
					TestCircle(x, y, 0);
					TestCircle(x, y, 5);
				}
			}

            TestCircleReset(ctx);

			//INTERSECTION:
			//Start with some static tests that have shown to cause failures at some point:
			Assert.Equal( /*"getX not getY",*/
				SpatialRelation.INTERSECTS,
				ctx.MakeCircle(107, -81, 147).Relate(ctx.MakeRectangle(92, 121, -89, 74)));

			TestCircleIntersect();

            Assert.Equal(ctx.MakeCircle(1, 2, 10), ctx.MakeCircle(1, 2, 6).GetBuffered(4, ctx));

            TestEmptiness(ctx.MakeCircle(double.NaN, double.NaN, random.nextBoolean() ? 0 : double.NaN));
        }

        public static void TestCircleReset(SpatialContext ctx)
        {
            ICircle c = ctx.MakeCircle(3, 4, 5);
            ICircle c2 = ctx.MakeCircle(5, 6, 7);
            c2.Reset(3, 4, 5); // to c1
            Assert.Equal(c, c2);
            Assert.Equal(c.BoundingBox, c2.BoundingBox);
        }

        [Theory]
        [PropertyData("Contexts")]
        public virtual void TestBufferedLineString(SpatialContext ctx)
        {
            base.ctx = ctx;

            //see BufferedLineStringTest & BufferedLineTest for more

            TestEmptiness(ctx.MakeBufferedLineString(new List<IPoint>(), random.Next(3+1)));
        }

        [Fact]
        public virtual void TestImplementsEqualsAndHash()
        {
            CheckShapesImplementEquals(new[]
                                    {
                                        typeof(Point),
                                        typeof(Circle),
										//GeoCircle.class  no: its fields are caches, not part of its identity
                                        typeof(Rectangle),
                                        typeof(ShapeCollection),
                                        typeof(BufferedLineString),
                                        typeof(BufferedLine)
                                    });
        }

        public static void CheckShapesImplementEquals(Type[] classes)
		{
			foreach (var clazz in classes)
			{
				try
				{
					//getDeclaredMethod( "equals", Object.class );
					var method = clazz.GetMethod("Equals", new[] { typeof(Object) });
				}
				catch (Exception)
				{
					//We want the equivalent of Assert.Fail(msg)
					Assert.True(false, "Shape needs to define 'equals' : " + clazz.Name);
				}
				try
				{
					//clazz.getDeclaredMethod( "hashCode" );
					var method = clazz.GetMethod("GetHashCode", BindingFlags.Public | BindingFlags.Instance);
				}
				catch (Exception)
				{
					//We want the equivalent of Assert.Fail(msg)
					Assert.True(false, "Shape needs to define 'GetHashCode' : " + clazz.Name);
				}
			}
		}
	}
}
