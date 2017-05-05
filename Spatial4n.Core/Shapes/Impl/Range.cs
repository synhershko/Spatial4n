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
    /// INTERNAL: A numeric range between a pair of numbers.
    /// Perhaps this class could become 1st class citizen extending Shape but not now.
    /// Only public so is accessible from tests in another package.
    /// </summary>
    public class Range
    {
        protected readonly double m_min, m_max;

        public static Range XRange(IRectangle rect, SpatialContext ctx)
        {
            if (ctx.IsGeo)
                return new LongitudeRange(rect.MinX, rect.MaxX);
            else
                return new Range(rect.MinX, rect.MaxX);
        }

        public static Range YRange(IRectangle rect, SpatialContext ctx)
        {
            return new Range(rect.MinY, rect.MaxY);
        }

        public Range(double min, double max)
        {
            this.m_min = min;
            this.m_max = max;
        }

        public virtual double Min
        {
            get { return m_min; }
        }

        public virtual double Max
        {
            get { return m_max; }
        }

        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;

            Range range = (Range)o;

            if (range.m_max.CompareTo(m_max) != 0) return false;
            if (range.m_min.CompareTo(m_min) != 0) return false;

            return true;
        }

        public override int GetHashCode()
        {
            int result;
            long temp;
            temp = m_min != +0.0d ? BitConverter.DoubleToInt64Bits(m_min) : 0L;
            result = (int)(temp ^ (long)((ulong)temp >> 32));
            temp = m_max != +0.0d ? BitConverter.DoubleToInt64Bits(m_max) : 0L;
            result = 31 * result + (int)(temp ^ (long)((ulong)temp >> 32));
            return result;
        }

        public override string ToString()
        {
            return "Range{" + m_min + " TO " + m_max + '}';
        }

        public virtual double Width
        {
            get { return m_max - m_min; }
        }

        public virtual bool Contains(double v)
        {
            return v >= m_min && v <= m_max;
        }

        public virtual double Center
        {
            get { return m_min + Width / 2; }
        }

        public virtual Range ExpandTo(Range other)
        {
            Debug.Assert(this.GetType() == other.GetType());
            return new Range(Math.Min(m_min, other.m_min), Math.Max(m_max, other.m_max));
        }

        public virtual double DeltaLen(Range other)
        {
            double min3 = Math.Max(m_min, other.m_min);
            double max3 = Math.Min(m_max, other.m_max);
            return max3 - min3;
        }

        public class LongitudeRange : Range
        {

            public static readonly LongitudeRange WORLD_180E180W = new LongitudeRange(-180, 180);

            public LongitudeRange(double min, double max)
                    : base(min, max)
            {
            }

            public LongitudeRange(IRectangle r)
                    : base(r.MinX, r.MaxX)
            {
            }

            public override double Width
            {
                get
                {
                    double w = base.Width;
                    if (w < 0)
                        w += 360;
                    return w;
                }
            }


            public override bool Contains(double v)
            {
                if (!CrossesDateline)
                    return base.Contains(v);
                return v >= m_min || v <= m_max;// the OR is the distinction from non-dateline cross
            }

            public virtual bool CrossesDateline
            {
                get { return m_min > m_max; }
            }

            public override double Center
            {
                get
                {
                    double ctr = base.Center;
                    if (ctr > 180)
                        ctr -= 360;
                    return ctr;
                }
            }

            public double CompareTo(LongitudeRange b)
            {
                return Diff(Center, b.Center);
            }

            /// <summary>
            /// <c>a - b (compareTo order).  &lt; 0 if a &lt; b</c>
            /// </summary>
            private static double Diff(double a, double b)
            {
                double diff = a - b;
                if (diff <= 180)
                {
                    if (diff >= -180)
                        return diff;
                    return diff + 360;
                }
                else
                {
                    return diff - 360;
                }
            }

            public override Range ExpandTo(Range other)
            {
                return ExpandTo((LongitudeRange)other);
            }

            public LongitudeRange ExpandTo(LongitudeRange other)
            {
                LongitudeRange a, b;// a.ctr <= b.ctr
                if (this.CompareTo(other) <= 0)
                {
                    a = this;
                    b = other;
                }
                else
                {
                    a = other;
                    b = this;
                }
                LongitudeRange newMin = b.Contains(a.m_min) ? b : a;//usually 'a'
                LongitudeRange newMax = a.Contains(b.m_max) ? a : b;//usually 'b'
                if (newMin == newMax)
                    return newMin;
                if (newMin == b && newMax == a)
                    return WORLD_180E180W;
                return new LongitudeRange(newMin.m_min, newMax.m_max);
            }
        }
    }
}
