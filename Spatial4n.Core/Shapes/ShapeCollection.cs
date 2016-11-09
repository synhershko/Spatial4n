using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes.Impl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Spatial4n.Core.Shapes
{
    /// <summary>
    /// A collection of Shape objects, analogous to an OGC GeometryCollection. The
    /// implementation demands a List(with random access) so that the order can be
    /// retained if an application requires it, although logically it's treated as an
    /// unordered Set, mostly.
    /// <para>
    /// Ideally, <see cref="Relate(Shape)"/> should return the same result no matter what
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
    /// <typeparam name="S"></typeparam>
    public class ShapeCollection<S> : Collection<S>, Shape
        where S : Shape
    {
        protected readonly List<S> shapes;
        protected readonly Rectangle bbox;

        /**
         * WARNING: {@code shapes} is copied by reference.
         * @param shapes Copied by reference! (make a defensive copy if caller modifies)
         * @param ctx
         */
        public ShapeCollection(List<S> shapes, SpatialContext ctx)
        {
            // TODO: Work out if there is a way to mimic this behavior (create a custom IRandomAccess?)
            //if (!(shapes is RandomAccess))
            //    throw new ArgumentException("Shapes arg must implement RandomAccess: " + shapes.GetType());
            this.shapes = shapes;
            this.bbox = ComputeBoundingBox(shapes, ctx);
        }

        protected virtual Rectangle ComputeBoundingBox(ICollection<Shape> shapes, SpatialContext ctx)
        {
            if (!shapes.Any())
                return ctx.MakeRectangle(double.NaN, double.NaN, double.NaN, double.NaN);
            Range xRange = null;
            double minY = double.PositiveInfinity;
            double maxY = double.NegativeInfinity;
            foreach (Shape geom in shapes)
            {
                Rectangle r = geom.GetBoundingBox();

                Range xRange2 = Range.XRange(r, ctx);
                if (xRange == null)
                {
                    xRange = xRange2;
                }
                else
                {
                    xRange = xRange.ExpandTo(xRange2);
                }
                minY = Math.Min(minY, r.GetMinY());
                maxY = Math.Max(maxY, r.GetMaxY());
            }
            return ctx.MakeRectangle(xRange.GetMin(), xRange.GetMax(), minY, maxY);
        }

        public virtual List<S> GetShapes()
        {
            return shapes;
        }

        new public S this[int index]
        {
            get
            {
                return shapes[index];
            }
        }

        //      @Override
        //public S get(int index)
        //      {
        //          return shapes.get(index);
        //      }

        new public int Count
        {
            get { return shapes.Count; }
        }

        public virtual Rectangle GetBoundingBox()
        {
            return bbox;
        }

        public virtual Point GetCenter()
        {
            return bbox.GetCenter();
        }


        public virtual bool HasArea()
        {
            foreach (Shape geom in shapes)
            {
                if (geom.HasArea())
                {
                    return true;
                }
            }
            return false;
        }


        public virtual Shape GetBuffered(double distance, SpatialContext ctx)
        {
            List<Shape> bufColl = new List<Shape>(Count);
            foreach (Shape shape in shapes)
            {
                bufColl.Add(shape.GetBuffered(distance, ctx));
            }
            return ctx.MakeCollection(bufColl);
        }


        public virtual SpatialRelation Relate(Shape other)
        {
            SpatialRelation bboxSect = bbox.Relate(other);
            if (bboxSect == SpatialRelation.DISJOINT || bboxSect == SpatialRelation.WITHIN)
                return bboxSect;

            bool containsWillShortCircuit = (other is Point) ||
                RelateContainsShortCircuits();
            SpatialRelation? sect = null;
            foreach (Shape shape in shapes)
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

                if (sect == SpatialRelation.INTERSECTS)
                    return SpatialRelation.INTERSECTS;

                if (sect == SpatialRelation.CONTAINS && containsWillShortCircuit)
                    return SpatialRelation.CONTAINS;
            }
            return sect.GetValueOrDefault(); // TODO: What to return if null??
        }

        /**
         * Called by relate() to determine whether to return early if it finds
         * CONTAINS, instead of checking the remaining shapes. It will do so without
         * calling this method if the "other" shape is a Point.  If a remaining shape
         * finds INTERSECTS, then INTERSECTS will be returned.  The only problem with
         * this returning true is that if some of the shapes overlap, it's possible
         * that the result of relate() could be dependent on the order of the shapes,
         * which could be unexpected / wrong depending on the application. The default
         * implementation returns true because it probably doesn't matter.  If it
         * does, a subclass could add a boolean flag that this method could return.
         * That flag could be initialized to true only if the shapes are mutually
         * disjoint.
         *
         * @see #computeMutualDisjoint(java.util.List) .
         */
        protected bool RelateContainsShortCircuits()
        {
            return true;
        }

        /**
         * Computes whether the shapes are mutually disjoint. This is a utility method
         * offered for use by a subclass implementing {@link #relateContainsShortCircuits()}.
         * <b>Beware: this is an O(N^2) algorithm.</b>.  Consequently, consider safely
         * assuming non-disjoint if shapes.size() > 10 or something.  And if all shapes
         * are a Point then the result of this method doesn't ultimately matter.
         */
        protected static bool ComputeMutualDisjoint(IList<Shape> shapes)
        {
            //WARNING: this is an O(n^2) algorithm.
            //loop through each shape and see if it intersects any shape before it
            for (int i = 1; i < shapes.Count; i++)
            {
                Shape shapeI = shapes[i];
                for (int j = 0; j < i; j++)
                {
                    Shape shapeJ = shapes[j];
                    if (shapeJ.Relate(shapeI).Intersects())
                        return false;
                }
            }
            return true;
        }


        public virtual double GetArea(SpatialContext ctx)
        {
            double MAX_AREA = bbox.GetArea(ctx);
            double sum = 0;
            foreach (Shape geom in shapes)
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
            foreach (Shape shape in shapes)
            {
                if (i++ > 0)
                    buf.Append(", ");
                buf.Append(shape);
                if (buf.Length > 150)
                {
                    buf.Append(" ...").Append(shapes.Count);
                    break;
                }
            }
            buf.Append(")");
            return buf.ToString();
        }


        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;

            ShapeCollection<S> that = (ShapeCollection<S>)o;

            if (!shapes.Equals(that.shapes)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return shapes.GetHashCode();
        }
    }
}
