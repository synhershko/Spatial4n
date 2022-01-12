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
using Spatial4n.Core.Shapes.Impl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spatial4n.Core.Shapes
{
    /// <summary>
    /// A collection of Shape objects, analogous to an OGC GeometryCollection. The
    /// implementation demands a List (with random access) so that the order can be
    /// retained if an application requires it, although logically it's treated as an
    /// unordered Set, mostly.
    /// <para>
    /// Ideally, <see cref="Relate(IShape)"/> should return the same result no matter what
    /// the shape order is, although the default implementation can be order
    /// dependent when the shapes overlap; see <see cref="RelateContainsShortCircuits()"/>.
    /// To improve performance slightly, the caller could order the shapes by
    /// largest first so that Relate() will have a greater chance of
    /// short-circuit'ing sooner.  As the Shape contract states; it may return
    /// intersects when the best answer is actually contains or within.If any shape
    /// intersects the provided shape then that is the answer.
    /// </para>
    /// <para>
    /// This implementation is not optimized for a large number of shapes; relate is
    /// O(N).  A more sophisticated implementation might do an R-Tree based on
    /// bbox'es, for example.
    /// </para>
    /// </summary>
    public class ShapeCollection : ICollection<IShape>, IShape
    {
        protected readonly IList<IShape> m_shapes;
        protected readonly IRectangle m_bbox;

        /// <summary>
        /// WARNING: <paramref name="shapes"/> is copied by reference.
        /// </summary>
        /// <param name="shapes">Copied by reference! (make a defensive copy if caller modifies)</param>
        /// <param name="ctx"></param>
        public ShapeCollection(IList<IShape> shapes, SpatialContext ctx)
        {
            // TODO: Work out if there is a way to mimic this behavior (create a custom IRandomAccess?)
            //if (!(shapes is RandomAccess))
            //    throw new ArgumentException("Shapes arg must implement RandomAccess: " + shapes.GetType());
            this.m_shapes = shapes;
            this.m_bbox = ComputeBoundingBox(shapes, ctx);
        }

        protected virtual IRectangle ComputeBoundingBox(ICollection<IShape> shapes, SpatialContext ctx)
        {
            if (!shapes.Any())
                return ctx.MakeRectangle(double.NaN, double.NaN, double.NaN, double.NaN);
            Range? xRange = null;
            double minY = double.PositiveInfinity;
            double maxY = double.NegativeInfinity;
            foreach (Shapes.IShape geom in shapes)
            {
                IRectangle r = geom.BoundingBox;

                Range xRange2 = Range.XRange(r, ctx);
                if (xRange is null)
                {
                    xRange = xRange2;
                }
                else
                {
                    xRange = xRange.ExpandTo(xRange2);
                }
                minY = Math.Min(minY, r.MinY);
                maxY = Math.Max(maxY, r.MaxY);
            }
            return ctx.MakeRectangle(xRange!.Min, xRange.Max, minY, maxY);
        }

        public virtual IList<IShape> Shapes => m_shapes;

        public virtual IShape this[int index] => m_shapes[index];

        public virtual int Count => m_shapes.Count;

        public virtual IRectangle BoundingBox => m_bbox;

        public virtual IPoint Center => m_bbox.Center;


        public virtual bool HasArea
        {
            get
            {
                foreach (IShape geom in m_shapes)
                {
                    if (geom.HasArea)
                    {
                        return true;
                    }
                }
                return false;
            }
        }


        public virtual IShape GetBuffered(double distance, SpatialContext ctx)
        {
            IList<IShape> bufColl = new List<IShape>(Count);
            foreach (IShape shape in m_shapes)
            {
                bufColl.Add(shape.GetBuffered(distance, ctx));
            }
            return ctx.MakeCollection(bufColl);
        }


        public virtual SpatialRelation Relate(IShape other)
        {
            SpatialRelation bboxSect = m_bbox.Relate(other);
            if (bboxSect == SpatialRelation.Disjoint || bboxSect == SpatialRelation.Within)
                return bboxSect;

            bool containsWillShortCircuit = (other is IPoint) ||
                RelateContainsShortCircuits();
            SpatialRelation? sect = null;
            foreach (IShape shape in m_shapes)
            {
                SpatialRelation nextSect = shape.Relate(other);

                if (sect == null)
                {//first pass
                    sect = nextSect;
                }
                else
                {
                    // TODO: What is the logic supposed to be if sect is null?
                    sect = sect.Value.Combine(nextSect);
                }

                if (sect == SpatialRelation.Intersects)
                    return SpatialRelation.Intersects;

                if (sect == SpatialRelation.Contains && containsWillShortCircuit)
                    return SpatialRelation.Contains;
            }
            return sect.GetValueOrDefault(); // TODO: What to return if null??
        }

        /// <summary>
        /// Called by <see cref="Relate(IShape)"/> to determine whether to return early if it finds
        /// <see cref="SpatialRelation.Contains"/>, instead of checking the remaining shapes. It will do so without
        /// calling this method if the "other" shape is a Point.  If a remaining shape
        /// finds <see cref="SpatialRelation.Intersects"/>, then <see cref="SpatialRelation.Intersects"/> will be returned.  The only problem with
        /// this returning true is that if some of the shapes overlap, it's possible
        /// that the result of <see cref="Relate(IShape)"/> could be dependent on the order of the shapes,
        /// which could be unexpected / wrong depending on the application. The default
        /// implementation returns true because it probably doesn't matter.  If it
        /// does, a subclass could add a boolean flag that this method could return.
        /// That flag could be initialized to true only if the shapes are mutually
        /// disjoint.
        /// </summary>
        /// <seealso cref="ComputeMutualDisjoint(IList{IShape})"/>
        protected virtual bool RelateContainsShortCircuits()
        {
            return true;
        }

        /// <summary>
        /// Computes whether the shapes are mutually disjoint. This is a utility method
        /// offered for use by a subclass implementing <see cref="RelateContainsShortCircuits()"/>.
        /// <b>Beware: this is an O(N^2) algorithm.</b>.  Consequently, consider safely
        /// assuming non-disjoint if shapes.Count > 10 or something.  And if all shapes
        /// are a <see cref="IPoint"/> then the result of this method doesn't ultimately matter.
        /// </summary>
        protected static bool ComputeMutualDisjoint(IList<IShape> shapes)
        {
            //WARNING: this is an O(n^2) algorithm.
            //loop through each shape and see if it intersects any shape before it
            for (int i = 1; i < shapes.Count; i++)
            {
                IShape shapeI = shapes[i];
                for (int j = 0; j < i; j++)
                {
                    IShape shapeJ = shapes[j];
                    if (shapeJ.Relate(shapeI).Intersects())
                        return false;
                }
            }
            return true;
        }

        public virtual double GetArea(SpatialContext? ctx)
        {
            double MAX_AREA = m_bbox.GetArea(ctx);
            double sum = 0;
            foreach (IShape geom in m_shapes)
            {
                sum += geom.GetArea(ctx);
                if (sum >= MAX_AREA)
                    return MAX_AREA;
            }

            return sum;
        }

        public override string ToString()
        {
            StringBuilder buf = new StringBuilder(100);
            buf.Append("ShapeCollection(");
            int i = 0;
            foreach (IShape shape in m_shapes)
            {
                if (i++ > 0)
                    buf.Append(", ");
                buf.Append(shape);
                if (buf.Length > 150)
                {
                    buf.Append(" ...").Append(m_shapes.Count);
                    break;
                }
            }
            buf.Append(")");
            return buf.ToString();
        }

        public override bool Equals(object o)
        {
            // Spatial4n NOTE: This was modified from the original implementation
            // to act like the collections of Java, which compare values rather than references.
            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;

            ShapeCollection that = (ShapeCollection)o;

            if (!ValueEquals(that)) return false;

            return true;
        }

        private bool ValueEquals(ShapeCollection other)
        {
            using var iter = other.GetEnumerator();
            foreach (IShape value in this)
            {
                if (!iter.MoveNext())
                    return false;
                if (!value.Equals(iter.Current))
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            // Spatial4n NOTE: This was modified from the original implementation
            // to act like the collections of Java, which compare values rather than references.
            int hashCode = 31;
            foreach (IShape value in this)
            {
                if (value != null)
                {
                    hashCode = ((hashCode << 5) + hashCode) ^ value.GetHashCode();
                }
                else
                {
                    hashCode = ((hashCode << 5) + hashCode) ^ 0; /* 0 for null */
                }
            }

            return hashCode;
        }

        #region ICollection<T>

        public void Add(IShape item)
        {
            m_shapes.Add(item);
        }

        public void Clear()
        {
            m_shapes.Clear();
        }

        public bool Contains(IShape item)
        {
            return m_shapes.Contains(item);
        }

        public void CopyTo(IShape[] array, int arrayIndex)
        {
            m_shapes.CopyTo(array, arrayIndex);
        }

        public bool Remove(IShape item)
        {
            return m_shapes.Remove(item);
        }

        public IEnumerator<IShape> GetEnumerator()
        {
            return m_shapes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_shapes.GetEnumerator();
        }

        public bool IsReadOnly => m_shapes.IsReadOnly;

        #endregion

        #region Added for .NET support of the IShape interface

        public virtual bool IsEmpty => m_shapes.Count == 0;

        #endregion
    }
}
