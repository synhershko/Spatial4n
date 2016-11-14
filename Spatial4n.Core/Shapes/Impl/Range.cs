using Spatial4n.Core.Context;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Spatial4n.Core.Shapes.Impl
{
    /// <summary>
    /// INTERNAL: A numeric range between a pair of numbers.
    /// Perhaps this class could become 1st class citizen extending Shape but not now.
    /// Only public so is accessible from tests in another package.
    /// </summary>
    public class Range
    {
        protected readonly double min, max;

        public static Range XRange(IRectangle rect, SpatialContext ctx)
        {
            if (ctx.IsGeo())
                return new LongitudeRange(rect.GetMinX(), rect.GetMaxX());
            else
                return new Range(rect.GetMinX(), rect.GetMaxX());
        }

        public static Range YRange(IRectangle rect, SpatialContext ctx)
        {
            return new Range(rect.GetMinY(), rect.GetMaxY());
        }

        public Range(double min, double max)
        {
            this.min = min;
            this.max = max;
        }

        public virtual double GetMin()
        {
            return min;
        }

        public virtual double GetMax()
        {
            return max;
        }

        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;

            Range range = (Range)o;

            if (range.max.CompareTo(max) != 0) return false;
            if (range.min.CompareTo(min) != 0) return false;

            return true;
        }

        public override int GetHashCode()
        {
            int result;
            long temp;
            temp = min != +0.0d ? BitConverter.DoubleToInt64Bits(min) : 0L;
            result = (int)(temp ^ (long)((ulong)temp >> 32));
            temp = max != +0.0d ? BitConverter.DoubleToInt64Bits(max) : 0L;
            result = 31 * result + (int)(temp ^ (long)((ulong)temp >> 32));
            return result;
        }

        public override string ToString()
        {
            return "Range{" + min + " TO " + max + '}';
        }

        public virtual double GetWidth()
        {
            return max - min;
        }

        public virtual bool Contains(double v)
        {
            return v >= min && v <= max;
        }

        public virtual double GetCenter()
        {
            return min + GetWidth() / 2;
        }

        public virtual Range ExpandTo(Range other)
        {
            Debug.Assert(this.GetType() == other.GetType());
            return new Range(Math.Min(min, other.min), Math.Max(max, other.max));
        }

        public virtual double DeltaLen(Range other)
        {
            double min3 = Math.Max(min, other.min);
            double max3 = Math.Min(max, other.max);
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
                    : base(r.GetMinX(), r.GetMaxX())
            {
            }

            public override double GetWidth()
            {
                double w = base.GetWidth();
                if (w < 0)
                    w += 360;
                return w;
            }


            public override bool Contains(double v)
            {
                if (!CrossesDateline())
                    return base.Contains(v);
                return v >= min || v <= max;// the OR is the distinction from non-dateline cross
            }

            public virtual bool CrossesDateline()
            {
                return min > max;
            }

            public override double GetCenter()
            {
                double ctr = base.GetCenter();
                if (ctr > 180)
                    ctr -= 360;
                return ctr;
            }

            public double CompareTo(LongitudeRange b)
            {
                return Diff(GetCenter(), b.GetCenter());
            }

            /** a - b (compareTo order).  < 0 if a < b */
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
                LongitudeRange newMin = b.Contains(a.min) ? b : a;//usually 'a'
                LongitudeRange newMax = a.Contains(b.max) ? a : b;//usually 'b'
                if (newMin == newMax)
                    return newMin;
                if (newMin == b && newMax == a)
                    return WORLD_180E180W;
                return new LongitudeRange(newMin.min, newMax.max);
            }
        }
    }
}
