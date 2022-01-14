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
using System.Diagnostics;
using Xunit;

namespace Spatial4n.Core.Shape
{
    public abstract class RectIntersectionTestHelper : RandomizedShapeTest
    {
        public RectIntersectionTestHelper(SpatialContext ctx)
            : base(ctx)
        {
        }

        protected abstract IShape GenerateRandomShape(Core.Shapes.IPoint nearP);

        protected abstract Core.Shapes.IPoint RandomPointInEmptyShape(IShape shape);


        protected override Core.Shapes.IPoint RandomPointIn(IShape shape)
        {
            if (!shape.HasArea)
                return RandomPointInEmptyShape((IShape)shape);
            return base.RandomPointIn(shape);
        }

        public void TestRelateWithRectangle()
        {
            //counters for the different intersection cases
            int i_C = 0, i_I = 0, i_W = 0, i_D = 0, i_bboxD = 0;
            int laps = 0;
            int MINLAPSPERCASE = AtLeast(20);
            while (i_C < MINLAPSPERCASE || i_I < MINLAPSPERCASE || i_W < MINLAPSPERCASE
                || i_D < MINLAPSPERCASE || i_bboxD < MINLAPSPERCASE)
            {
                laps++;

                //TestLog.Clear();

                Core.Shapes.IPoint nearP = RandomPointIn(ctx.WorldBounds);

                IShape s = GenerateRandomShape(nearP);

                IRectangle r = RandomRectangle(s.BoundingBox.Center);

                SpatialRelation ic = s.Relate(r);

                //TestLog.Log("S-R Rel: {}, Shape {}, Rectangle {}", ic, s, r);

                try
                {
                    switch (ic)
                    {
                        case SpatialRelation.Contains:
                            i_C++;
                            for (int j = 0; j < AtLeast(10); j++)
                            {
                                Core.Shapes.IPoint p = RandomPointIn(r);
                                AssertRelation(null, SpatialRelation.Contains, s, p);
                            }
                            break;

                        case SpatialRelation.Within:
                            i_W++;
                            for (int j = 0; j < AtLeast(10); j++)
                            {
                                Core.Shapes.IPoint p = RandomPointIn(s);
                                AssertRelation(null, SpatialRelation.Contains, r, p);
                            }
                            break;

                        case SpatialRelation.Disjoint:
                            if (!s.BoundingBox.Relate(r).Intersects())
                            {//bboxes are disjoint
                                i_bboxD++;
                                if (i_bboxD > MINLAPSPERCASE)
                                    break;
                            }
                            else
                            {
                                i_D++;
                            }
                            for (int j = 0; j < AtLeast(10); j++)
                            {
                                Core.Shapes.IPoint p = RandomPointIn(r);
                                AssertRelation(null, SpatialRelation.Disjoint, s, p);
                            }
                            break;

                        case SpatialRelation.Intersects:
                            i_I++;
                            SpatialRelation? pointR = null;//set once
                            IRectangle randomPointSpace = null;
                            int MAX_TRIES = 1000;
                            for (int j = 0; j < MAX_TRIES; j++)
                            {
                                Core.Shapes.IPoint p;
                                if (j < 4)
                                {
                                    p = new Core.Shapes.Impl.Point(0, 0, ctx);
                                    InfBufLine.CornerByQuadrant(r, j + 1, p);
                                }
                                else
                                {
                                    if (randomPointSpace == null)
                                    {
                                        if (pointR == SpatialRelation.Disjoint)
                                        {
                                            randomPointSpace = IntersectRects(r, s.BoundingBox);
                                        }
                                        else
                                        {//CONTAINS
                                            randomPointSpace = r;
                                        }
                                    }
                                    p = RandomPointIn(randomPointSpace);
                                }
                                SpatialRelation pointRNew = s.Relate(p);
                                if (pointR == null)
                                {
                                    pointR = pointRNew;
                                }
                                else if (pointR != pointRNew)
                                {
                                    break;
                                }
                                else if (j >= MAX_TRIES)
                                {
                                    //TODO consider logging instead of failing
                                    Assert.True(false, "Tried intersection brute-force too many times without success");
                                }
                            }

                            break;

                        default:
                            Assert.True(false, "" + ic);
                            break;
                    }
                }
                catch (/*AssertionError*/ Exception e) // TODO: Is there an exception to catch here??
                {
                    OnAssertFail(e, s, r, ic);
                }
                if (laps > MINLAPSPERCASE * 1000)
                    Assert.True(false, "Did not find enough intersection cases in a reasonable number" +
                        " of random attempts. CWIDbD: " + i_C + "," + i_W + "," + i_I + "," + i_D + "," + i_bboxD
                        + "  Laps exceeded " + MINLAPSPERCASE * 1000);
            }
            Console.WriteLine("Laps: " + laps + " CWIDbD: " + i_C + "," + i_W + "," + i_I + "," + i_D + "," + i_bboxD);
        }

        protected virtual void OnAssertFail(/*AssertionError*/Exception e, IShape s, IRectangle r, SpatialRelation ic)
        {
            throw e;
        }

        private IRectangle IntersectRects(IRectangle r1, IRectangle r2)
        {
            Debug.Assert(r1.Relate(r2).Intersects());
            double minX, maxX;
            if (r1.RelateXRange(r2.MinX, r2.MinX).Intersects())
            {
                minX = r2.MinX;
            }
            else
            {
                minX = r1.MinX;
            }
            if (r1.RelateXRange(r2.MaxX, r2.MaxX).Intersects())
            {
                maxX = r2.MaxX;
            }
            else
            {
                maxX = r1.MaxX;
            }
            double minY, maxY;
            if (r1.RelateYRange(r2.MinY, r2.MinY).Intersects())
            {
                minY = r2.MinY;
            }
            else
            {
                minY = r1.MinY;
            }
            if (r1.RelateYRange(r2.MaxY, r2.MaxY).Intersects())
            {
                maxY = r2.MaxY;
            }
            else
            {
                maxY = r1.MaxY;
            }
            return ctx.MakeRectangle(minX, maxX, minY, maxY);
        }
    }
}
