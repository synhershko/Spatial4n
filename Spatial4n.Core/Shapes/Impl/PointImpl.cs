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
using System;
using System.Diagnostics;

namespace Spatial4n.Core.Shapes.Impl
{
    /// <summary>
    /// A basic 2D implementation of a Point.
    /// </summary>
    public class Point : IPoint
    {
        private readonly SpatialContext? ctx;
        private double x;
        private double y;

        /// <summary>
        /// A simple constructor without normalization / validation.
        /// </summary>
        public Point(double x, double y, SpatialContext? ctx)
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
            get 
            {
                if (ctx is null)
                    throw new InvalidOperationException("Must provide a SpatialContext in the constructor."); // spatial4n specific - use InvalidOperationException instead of NullReferenceException
                return ctx.MakeRectangle(this, this); 
            }
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

        public virtual double GetArea(SpatialContext? ctx)
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
        /// All <see cref="IPoint"/> implementations should use this definition of <see cref="object.Equals(object)"/>.
        /// </summary>
        public static bool Equals(IPoint thiz, object o)
        {
            if (thiz == null)
                throw new ArgumentNullException(nameof(thiz));

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
        /// All <see cref="IPoint"/> implementations should use this definition of <see cref="object.GetHashCode()"/>.
        /// </summary>
        public static int GetHashCode(IPoint thiz)
        {
            if (thiz == null)
                throw new ArgumentNullException(nameof(thiz));

            long temp = thiz.X != +0.0d ? BitConverter.DoubleToInt64Bits(thiz.X) : 0L;
            int result = (int)(temp ^ ((uint)temp >> 32));
            temp = thiz.Y != +0.0d ? BitConverter.DoubleToInt64Bits(thiz.Y) : 0L;
            result = 31 * result + (int)(temp ^ ((uint)temp >> 32));
            return result;
        }
    }
}
