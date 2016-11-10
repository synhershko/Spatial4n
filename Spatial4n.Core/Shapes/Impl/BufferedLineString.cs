using Spatial4n.Core.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Spatial4n.Core.Shapes.Impl
{
    /// <summary>
    /// A BufferedLineString is a collection of {@link com.spatial4j.core.shape.impl.BufferedLine} shapes,
    /// resulting in what some call a "Track" or "Polyline" (ESRI terminology).
    /// The buffer can be 0.  Note that BufferedLine isn't yet aware of geodesics (e.g. the dateline).
    /// </summary>
    public class BufferedLineString : Shape
    {
        //TODO add some geospatial awareness like:
        // segment that spans at the dateline (split it at DL?).

        private readonly /*ShapeCollection<BufferedLine>*/ ShapeCollection segments;
        private readonly double buf;

        /**
         * Needs at least 1 point, usually more than that.  If just one then it's
         * internally treated like 2 points.
         */
        public BufferedLineString(IList<Point> points, double buf, SpatialContext ctx)
            : this(points, buf, false, ctx)
        {
        }

        /**
         * @param points ordered control points. If empty then this shape is empty.
         * @param buf Buffer >= 0
         * @param expandBufForLongitudeSkew See {@link BufferedLine
         * #expandBufForLongitudeSkew(com.spatial4j.core.shape.Point,
         * com.spatial4j.core.shape.Point, double)}.
         *                                  If true then the buffer for each segment
         *                                  is computed.
         * @param ctx
         */
        public BufferedLineString(IList<Point> points, double buf, bool expandBufForLongitudeSkew,
                                  SpatialContext ctx)
        {
            this.buf = buf;

            if (!points.Any())
            {
                this.segments = ctx.MakeCollection(/*new List<BufferedLine>()*/ new List<Shape>());
            }
            else
            {
                //List<BufferedLine> segments = new List<BufferedLine>(points.Count - 1);
                List<Shape> segments = new List<Shape>(points.Count - 1);

                Point prevPoint = null;
                foreach (Point point in points)
                {
                    if (prevPoint != null)
                    {
                        double segBuf = buf;
                        if (expandBufForLongitudeSkew)
                        {
                            //TODO this is faulty in that it over-buffers.  See Issue#60.
                            segBuf = BufferedLine.ExpandBufForLongitudeSkew(prevPoint, point, buf);
                        }
                        segments.Add(new BufferedLine(prevPoint, point, segBuf, ctx));
                    }
                    prevPoint = point;
                }
                if (!segments.Any())
                {//TODO throw exception instead?
                    segments.Add(new BufferedLine(prevPoint, prevPoint, buf, ctx));
                }
                this.segments = ctx.MakeCollection(segments);
            }
        }


        public virtual bool IsEmpty
        {
            get { return segments.IsEmpty; }
        }

        public virtual Shape GetBuffered(double distance, SpatialContext ctx)
        {
            return ctx.MakeBufferedLineString(GetPoints(), buf + distance);
        }

        public virtual /*ShapeCollection<BufferedLine>*/ ShapeCollection GetSegments()
        {
            return segments;
        }

        public double GetBuf()
        {
            return buf;
        }

        public virtual double GetArea(SpatialContext ctx)
        {
            return segments.GetArea(ctx);
        }

        public virtual SpatialRelation Relate(Shape other)
        {
            return segments.Relate(other);
        }

        public virtual bool HasArea()
        {
            return segments.HasArea();
        }


        public virtual Point GetCenter()
        {
            return segments.GetCenter();
        }


        public virtual Rectangle GetBoundingBox()
        {
            return segments.GetBoundingBox(); 
        }


        public override string ToString()
        {
            StringBuilder str = new StringBuilder(100);
            str.Append("BufferedLineString(buf=").Append(buf).Append(" pts=");
            bool first = true;
            foreach (Point point in GetPoints())
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    str.Append(", ");
                }
                str.Append(point.GetX()).Append(' ').Append(point.GetY());
            }
            str.Append(')');
            return str.ToString();
        }

        private class PointListAnonymousHelper : IList<Point>
        {
            private readonly IList<Point> pointList = new List<Point>();
            private readonly IList<Shape> lines;

            public PointListAnonymousHelper(IList<Shape> lines)
            {
                this.lines = lines;
            }


            public Point this[int index]
            {
                get
                {
                    if (index == 0)
                        return ((BufferedLine)lines[0]).GetA();
                    return ((BufferedLine)lines[index - 1]).GetB();
                }
                set
                {
                    pointList[index] = value;
                }
            }

            public int Count
            {
                get
                {
                    return pointList.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return pointList.IsReadOnly;
                }
            }

            public void Add(Point item)
            {
                pointList.Add(item);
            }

            public void Clear()
            {
                pointList.Clear();
            }

            public bool Contains(Point item)
            {
                return pointList.Contains(item);
            }

            public void CopyTo(Point[] array, int arrayIndex)
            {
                pointList.CopyTo(array, arrayIndex);
            }

            public IEnumerator<Point> GetEnumerator()
            {
                return pointList.GetEnumerator();
            }

            public int IndexOf(Point item)
            {
                return pointList.IndexOf(item);
            }

            public void Insert(int index, Point item)
            {
                pointList.Insert(index, item);
            }

            public bool Remove(Point item)
            {
                return pointList.Remove(item);
            }

            public void RemoveAt(int index)
            {
                pointList.RemoveAt(index);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return pointList.GetEnumerator();
            }
        }

        public virtual IList<Point> GetPoints()
        {
            if (!segments.Any())
                return new List<Point>();
            //List<BufferedLine> lines = segments.GetShapes();
            IList<Shape> lines = segments.GetShapes();
            return new PointListAnonymousHelper(lines);

            //        return new AbstractList<Point>() {
            //  @Override
            //  public Point get(int index)
            //    {
            //        if (index == 0)
            //            return lines.get(0).getA();
            //        return lines.get(index - 1).getB();
            //    }

            //    @Override
            //  public int size()
            //    {
            //        return lines.size() + 1;
            //    }
            //};
        }


        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;

            BufferedLineString that = (BufferedLineString)o;

            if (that.buf.CompareTo(buf) != 0) return false;
            if (!segments.Equals(that.segments)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            int result;
            long temp;
            result = segments.GetHashCode();
            temp = buf != +0.0d ? BitConverter.DoubleToInt64Bits(buf) : 0L;
            result = 31 * result + (int)(temp ^ (long)((ulong)temp >> 32));
            return result;
        }
    }
}
