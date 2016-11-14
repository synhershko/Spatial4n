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

using System;
using Spatial4n.Core.Context;
using System.Diagnostics;

namespace Spatial4n.Core.Shapes.Impl
{
    /// <summary>
    /// A basic 2D implementation of a Point.
    /// </summary>
    public class Point : IPoint
    {
        private readonly SpatialContext ctx;
        private double x;
        private double y;

        /// <summary>
        /// A simple constructor without normalization / validation.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="ctx"></param>
        public Point(double x, double y, SpatialContext ctx)
        {
            this.ctx = ctx;
            Reset(x, y);
        }

        public virtual bool IsEmpty
        {
            get { return double.IsNaN(x); }
        }

        public virtual void Reset(double x, double y)
        {
            Debug.Assert(!IsEmpty);
            this.x = x;
            this.y = y;
        }

        public virtual double X
        {
            get { return x; }
        }

        public virtual double Y
        {
            get { return y; }
        }

        public virtual IRectangle BoundingBox
        {
            get { return ctx.MakeRectangle(this, this); }
        }

        public virtual IPoint Center
        {
            get { return this; }
        }

        public virtual IShape GetBuffered(double distance, SpatialContext ctx)
        {
            return ctx.MakeCircle(this, distance);
        }

        public virtual SpatialRelation Relate(IShape other)
        {
            if (IsEmpty || other.IsEmpty)
                return SpatialRelation.DISJOINT;
            if (other is IPoint)
                return this.Equals(other) ? SpatialRelation.INTERSECTS : SpatialRelation.DISJOINT;
            return other.Relate(this).Transpose();
        }

        public virtual bool HasArea
        {
            get { return false; }
        }

        public virtual double GetArea(SpatialContext ctx)
        {
            return 0;
        }

        public override string ToString()
        {
            return string.Format("Pt(x={0:0.0#############},y={1:0.0#############})", x, y);
        }

        public override bool Equals(object o)
        {
            return Equals(this, o);
        }

        /// <summary>
        /// All {@link Point} implementations should use this definition of {@link Object#equals(Object)}.
        /// </summary>
        /// <param name="thiz"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public static bool Equals(IPoint thiz, Object o)
        {
            if (thiz == null)
                throw new ArgumentNullException("thiz");

            if (thiz == o) return true;

            var point = o as IPoint;
            if (point == null) return false;

            return thiz.X.Equals(point.X) && thiz.Y.Equals(point.Y);
        }

        public override int GetHashCode()
        {
            return GetHashCode(this);
        }

        /// <summary>
        /// All {@link Point} implementations should use this definition of {@link Object#hashCode()}.
        /// </summary>
        /// <param name="thiz"></param>
        /// <returns></returns>
        public static int GetHashCode(IPoint thiz)
        {
            if (thiz == null)
                throw new ArgumentNullException("thiz");

            long temp = thiz.X != +0.0d ? BitConverter.DoubleToInt64Bits(thiz.X) : 0L;
            int result = (int)(temp ^ ((uint)temp >> 32));
            temp = thiz.Y != +0.0d ? BitConverter.DoubleToInt64Bits(thiz.Y) : 0L;
            result = 31 * result + (int)(temp ^ ((uint)temp >> 32));
            return result;
        }
    }
}
