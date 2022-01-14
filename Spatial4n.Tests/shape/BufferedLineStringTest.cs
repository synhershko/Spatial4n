﻿/*
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
using System;
using System.Collections.Generic;
using Xunit;

namespace Spatial4n.Core.Shape
{
    public class BufferedLineStringTest
    {
        private readonly SpatialContext ctx = new SpatialContextFactory()
        { geo = false, worldBounds = new Rectangle(-100, 100, -50, 50, null) }.CreateSpatialContext();

        private class RectIntersectionAnonymousHelper : RectIntersectionTestHelper
        {
            public RectIntersectionAnonymousHelper(SpatialContext ctx)
                : base(ctx)
            {
            }

            protected override IShape GenerateRandomShape(Core.Shapes.IPoint nearP)
            {
                IRectangle nearR = RandomRectangle(nearP);
                int numPoints = 2 + random.Next(3 + 1);//2-5 points

                IList<Core.Shapes.IPoint> points = new List<Core.Shapes.IPoint>(numPoints);
                while (points.Count < numPoints)
                {
                    points.Add(RandomPointIn(nearR));
                }
                double maxBuf = Math.Max(nearR.Width, nearR.Height);
                double buf = Math.Abs(RandomGaussian()) * maxBuf / 4;
                buf = random.Next((int)Divisible(buf));
                return new BufferedLineString(points, buf, ctx);
            }

            protected override Core.Shapes.IPoint RandomPointInEmptyShape(IShape shape)
            {
                IList<Core.Shapes.IPoint> points = ((BufferedLineString)shape).Points;
                return points.Count == 0 ? null : points[random.Next(points.Count/* - 1*/)];
            }
        }


        [Fact]
        public virtual void TestRectIntersect()
        {
            new RectIntersectionAnonymousHelper(ctx).TestRelateWithRectangle();
        }
    }
}
