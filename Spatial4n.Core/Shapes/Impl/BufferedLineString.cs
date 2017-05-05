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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spatial4n.Core.Shapes.Impl
{
    /// <summary>
    /// A <see cref="BufferedLineString"/> is a collection of <see cref="BufferedLine"/> shapes,
    /// resulting in what some call a "Track" or "Polyline" (ESRI terminology).
    /// The buffer can be 0.  Note that <see cref="BufferedLine"/> isn't yet aware of geodesics (e.g. the dateline).
    /// </summary>
    public class BufferedLineString : IShape
    {
        //TODO add some geospatial awareness like:
        // segment that spans at the dateline (split it at DL?).

        private readonly ShapeCollection segments;
        private readonly double buf;

        /// <summary>
        /// Needs at least 1 point, usually more than that.  If just one then it's
        /// internally treated like 2 points.
        /// </summary>
        public BufferedLineString(IList<IPoint> points, double buf, SpatialContext ctx)
            : this(points, buf, false, ctx)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="points">ordered control points. If empty then this shape is empty.</param>
        /// <param name="buf">Buffer &gt;= 0</param>
        /// <param name="expandBufForLongitudeSkew">
        /// See <see cref="BufferedLine.ExpandBufForLongitudeSkew(IPoint, IPoint, double)"/>
        /// If true then the buffer for each segment is computed.
        /// </param>
        /// <param name="ctx"></param>
        public BufferedLineString(IList<IPoint> points, double buf, bool expandBufForLongitudeSkew,
                                  SpatialContext ctx)
        {
            this.buf = buf;

            if (!points.Any())
            {
                this.segments = ctx.MakeCollection(new List<IShape>());
            }
            else
            {
                List<IShape> segments = new List<IShape>(points.Count - 1);

                IPoint prevPoint = null;
                foreach (IPoint point in points)
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

        public virtual IShape GetBuffered(double distance, SpatialContext ctx)
        {
            return ctx.MakeBufferedLineString(Points, buf + distance);
        }

        public virtual ShapeCollection Segments
        {
            get { return segments; }
        }

        public virtual double Buf
        {
            get { return buf; }
        }

        public virtual double GetArea(SpatialContext ctx)
        {
            return segments.GetArea(ctx);
        }

        public virtual SpatialRelation Relate(IShape other)
        {
            return segments.Relate(other);
        }

        public virtual bool HasArea
        {
            get { return segments.HasArea; }
        }


        public virtual IPoint Center
        {
            get { return segments.Center; }
        }


        public virtual IRectangle BoundingBox
        {
            get { return segments.BoundingBox; }
        }


        public override string ToString()
        {
            StringBuilder str = new StringBuilder(100);
            str.Append("BufferedLineString(buf=").Append(buf).Append(" pts=");
            bool first = true;
            foreach (IPoint point in Points)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    str.Append(", ");
                }
                str.Append(point.X).Append(' ').Append(point.Y);
            }
            str.Append(')');
            return str.ToString();
        }

        public virtual IList<IPoint> Points
        {
            get
            {
                if (!segments.Any())
                    return new List<IPoint>();
                IList<IShape> shapes = segments.Shapes;
                IList<IPoint> points = new List<IPoint>(); ;

                foreach (var shape in shapes)
                {
                    if (!(shape is BufferedLine))
                        continue;

                    BufferedLine line = shape as BufferedLine;

                    points.Add(line.A);
                    points.Add(line.B);
                }

                return points;
            }
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
