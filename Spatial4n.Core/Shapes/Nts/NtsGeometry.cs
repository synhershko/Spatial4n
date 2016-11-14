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
using System.Collections.Generic;
using System.Diagnostics;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Union;
using NetTopologySuite.Operation.Valid;
using Spatial4n.Core.Context;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.Shapes.Impl;
using GeoAPI.Geometries.Prepared;
using NetTopologySuite.Geometries.Prepared;

namespace Spatial4n.Core.Shapes.Nts
{
    /// <summary>
    /// Wraps a NTS {@link Geometry} (i.e. may be a polygon or basically anything).
    /// NTS's does a great deal of the hard work, but there is work here in handling
    /// dateline wrap.
    /// </summary>
    public class NtsGeometry : IShape
    {
        /** System property boolean that can disable auto validation in an assert. */
        public static readonly string SYSPROP_ASSERT_VALIDATE = "spatial4n.NtsGeometry.assertValidate";

        private readonly IGeometry geom;//cannot be a direct instance of GeometryCollection as it doesn't support relate()
        private readonly bool _hasArea;
        private readonly IRectangle bbox;
        protected readonly NtsSpatialContext ctx;
        protected IPreparedGeometry preparedGeometry;
        protected bool validated = false;

        public NtsGeometry(IGeometry geom, NtsSpatialContext ctx, bool dateline180Check, bool allowMultiOverlap)
        {
            this.ctx = ctx;

            //GeometryCollection isn't supported in relate()
            if (geom.GetType() == typeof(GeometryCollection))
                throw new ArgumentException("NtsGeometry does not support GeometryCollection but does support its subclasses.");

            //NOTE: All this logic is fairly expensive. There are some short-circuit checks though.
            if (ctx.IsGeo)
            {
                //Unwraps the geometry across the dateline so it exceeds the standard geo bounds (-180 to +180).
                if (dateline180Check)
                    UnwrapDateline(geom); //potentially modifies geom
                //If given multiple overlapping polygons, fix it by union
                if (allowMultiOverlap)
                    geom = UnionGeometryCollection(geom); //returns same or new geom

                //Cuts an unwrapped geometry back into overlaid pages in the standard geo bounds.
                geom = CutUnwrappedGeomInto360(geom); //returns same or new geom
                Debug.Assert(geom.EnvelopeInternal.Width <= 360);
                Debug.Assert(geom.GetType() != typeof(GeometryCollection)); //double check

                //Compute bbox
                bbox = ComputeGeoBBox(geom);
            }
            else
            {//not geo
                if (allowMultiOverlap)
                    geom = UnionGeometryCollection(geom);//returns same or new geom
                Envelope env = geom.EnvelopeInternal;
                bbox = new Rectangle(env.MinX, env.MaxX, env.MinY, env.MaxY, ctx);
            }
            var _ = geom.EnvelopeInternal;//ensure envelope is cached internally, which is lazy evaluated. Keeps this thread-safe.

            this.geom = geom;
            Debug.Assert(AssertValidate());//kinda expensive but caches valid state

            this._hasArea = !((geom is ILineal) || (geom is IPuntal));
        }

        /** called via assertion */
        private bool AssertValidate()
        {
            string assertValidate = Environment.GetEnvironmentVariable(SYSPROP_ASSERT_VALIDATE); //System.getProperty(SYSPROP_ASSERT_VALIDATE);
            if (assertValidate == null || bool.Parse(assertValidate))
                Validate();
            return true;
        }

        /**
        * Validates the shape, throwing a descriptive error if it isn't valid. Note that this
        * is usually called automatically by default, but that can be disabled.
        *
        * @throws InvalidShapeException with descriptive error if the shape isn't valid
        */
        public void Validate()
        {
            if (!validated)
            {
                IsValidOp isValidOp = new IsValidOp(geom);
                if (!isValidOp.IsValid)
                    throw new InvalidShapeException(isValidOp.ValidationError.ToString());
                validated = true;
            }
        }

        /**
        * Adds an index to this class internally to compute spatial relations faster. In NTS this
        * is called a {@link com.vividsolutions.jts.geom.prep.PreparedGeometry}.  This
        * isn't done by default because it takes some time to do the optimization, and it uses more
        * memory.  Calling this method isn't thread-safe so be careful when this is done. If it was
        * already indexed then nothing happens.
        */
        public void Index()
        {
            if (preparedGeometry == null)
                preparedGeometry = PreparedGeometryFactory.Prepare(geom);
        }


        public virtual bool IsEmpty
        {
            get { return geom.IsEmpty; }
        }

        /** Given {@code geoms} which has already been checked for being in world
        * bounds, return the minimal longitude range of the bounding box.
        */
        protected IRectangle ComputeGeoBBox(IGeometry geoms)
        {
            if (geoms.IsEmpty)
                return new Rectangle(double.NaN, double.NaN, double.NaN, double.NaN, ctx);
            Envelope env = geoms.EnvelopeInternal;//for minY & maxY (simple)
            if (env.Width > 180 && geoms.NumGeometries > 1)
            {
                // This is ShapeCollection's bbox algorithm
                Range xRange = null;
                for (int i = 0; i < geoms.NumGeometries; i++)
                {
                    Envelope envI = geoms.GetGeometryN(i).EnvelopeInternal;
                    Range xRange2 = new Range.LongitudeRange(envI.MinX, envI.MaxX);
                    if (xRange == null)
                    {
                        xRange = xRange2;
                    }
                    else
                    {
                        xRange = xRange.ExpandTo(xRange2);
                    }
                    if (xRange == Range.LongitudeRange.WORLD_180E180W)
                        break; // can't grow any bigger
                }
                // TODO: Inconsistent API between this and GeoAPI
                return new Rectangle(xRange.Min, xRange.Max, env.MinY, env.MaxY, ctx);
            }
            else
            {
                return new Rectangle(env.MinX, env.MaxX, env.MinY, env.MaxY, ctx);
            }
        }

        public virtual IShape GetBuffered(double distance, SpatialContext ctx)
        {
            //TODO doesn't work correctly across the dateline. The buffering needs to happen
            // when it's transiently unrolled, prior to being sliced.
            return this.ctx.MakeShape(geom.Buffer(distance), true, true);
        }

        public virtual bool HasArea
        {
            get { return _hasArea; }
        }

        public virtual double GetArea(SpatialContext ctx)
        {
            double geomArea = geom.Area;
            if (ctx == null || geomArea == 0)
                return geomArea;
            //Use the area proportional to how filled the bbox is.
            double bboxArea = BoundingBox.GetArea(null);//plain 2d area
            Debug.Assert(bboxArea >= geomArea);
            double filledRatio = geomArea / bboxArea;
            return BoundingBox.GetArea(ctx) * filledRatio;
            // (Future: if we know we use an equal-area projection then we don't need to
            //  estimate)
        }

        public virtual IRectangle BoundingBox
        {
            get { return bbox; }
        }

        public virtual IPoint Center
        {
            get
            {
                if (IsEmpty) //geom.getCentroid == null
                    return new NtsPoint(ctx.GeometryFactory.CreatePoint((Coordinate)null), ctx);
                return new NtsPoint((NetTopologySuite.Geometries.Point)geom.Centroid, ctx);
            }
        }

        public virtual SpatialRelation Relate(IShape other)
        {
            if (other is IPoint)
                return Relate((IPoint)other);
            else if (other is IRectangle)
                return Relate((IRectangle)other);
            else if (other is ICircle)
                return Relate((ICircle)other);
            else if (other is NtsGeometry)
                return Relate((NtsGeometry)other);
            else if (other is BufferedLineString)
                throw new NotSupportedException("Can't use BufferedLineString with NtsGeometry");
            return other.Relate(this).Transpose();
        }

        public virtual SpatialRelation Relate(IPoint pt)
        {
            if (!BoundingBox.Relate(pt).Intersects())
                return SpatialRelation.DISJOINT;
            IGeometry ptGeom;
            if (pt is NtsPoint)
                ptGeom = ((NtsPoint)pt).Geometry;
            else
                ptGeom = ctx.GeometryFactory.CreatePoint(new Coordinate(pt.X, pt.Y));
            return Relate(ptGeom);//is point-optimized
        }

        public virtual SpatialRelation Relate(IRectangle rectangle)
        {
            SpatialRelation bboxR = bbox.Relate(rectangle);
            if (bboxR == SpatialRelation.WITHIN || bboxR == SpatialRelation.DISJOINT)
                return bboxR;
            // FYI, the right answer could still be DISJOINT or WITHIN, but we don't know yet.
            return Relate(ctx.GetGeometryFrom(rectangle));
        }

        public virtual SpatialRelation Relate(ICircle circle)
        {
            SpatialRelation bboxR = bbox.Relate(circle);
            if (bboxR == SpatialRelation.WITHIN || bboxR == SpatialRelation.DISJOINT)
                return bboxR;

            //Test each point to see how many of them are outside of the circle.
            //TODO consider instead using geom.apply(CoordinateSequenceFilter) -- maybe faster since avoids Coordinate[] allocation
            Coordinate[] coords = geom.Coordinates;
            int outside = 0;
            int i = 0;
            foreach (Coordinate coord in coords)
            {
                i++;
                SpatialRelation sect = circle.Relate(new Impl.Point(coord.X, coord.Y, ctx));
                if (sect == SpatialRelation.DISJOINT)
                    outside++;
                if (i != outside && outside != 0)//short circuit: partially outside, partially inside
                    return SpatialRelation.INTERSECTS;
            }
            if (i == outside)
            {
                return (Relate(circle.Center) == SpatialRelation.DISJOINT)
                    ? SpatialRelation.DISJOINT : SpatialRelation.CONTAINS;
            }
            Debug.Assert(outside == 0);
            return SpatialRelation.WITHIN;
        }

        public virtual SpatialRelation Relate(NtsGeometry ntsGeometry)
        {
            //don't bother checking bbox since geom.relate() does this already
            return Relate(ntsGeometry.geom);
        }

        protected virtual SpatialRelation Relate(IGeometry oGeom)
        {
            //see http://docs.geotools.org/latest/userguide/library/jts/dim9.html#preparedgeometry
            if (oGeom is GeoAPI.Geometries.IPoint) // TODO: This may not be the correct data type....
            {
                if (preparedGeometry != null)
                    return preparedGeometry.Disjoint(oGeom) ? SpatialRelation.DISJOINT : SpatialRelation.CONTAINS;
                return geom.Disjoint(oGeom) ? SpatialRelation.DISJOINT : SpatialRelation.CONTAINS;
            }
            if (preparedGeometry == null)
                return IntersectionMatrixToSpatialRelation(geom.Relate(oGeom));
            else if (preparedGeometry.Covers(oGeom))
                return SpatialRelation.CONTAINS;
            else if (preparedGeometry.CoveredBy(oGeom))
                return SpatialRelation.WITHIN;
            else if (preparedGeometry.Intersects(oGeom))
                return SpatialRelation.INTERSECTS;
            return SpatialRelation.DISJOINT;
        }

        public static SpatialRelation IntersectionMatrixToSpatialRelation(IntersectionMatrix matrix)
        {
            //As indicated in SpatialRelation javadocs, Spatial4j CONTAINS & WITHIN are
            // OGC's COVERS & COVEREDBY
            if (matrix.IsCovers())
                return SpatialRelation.CONTAINS;
            else if (matrix.IsCoveredBy())
                return SpatialRelation.WITHIN;
            else if (matrix.IsDisjoint())
                return SpatialRelation.DISJOINT;
            return SpatialRelation.INTERSECTS;
        }

        public override string ToString()
        {
            return geom.ToString();
        }

        public override bool Equals(Object o)
        {
            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;

            var that = (NtsGeometry)o;
            return geom.EqualsExact(that.geom);//fast equality for normalized geometries
        }

        public override int GetHashCode()
        {
            //FYI if geometry.equalsExact(that.geometry), then their envelopes are the same.
            return geom.EnvelopeInternal.GetHashCode();
        }

        public virtual IGeometry Geometry
        {
            get { return geom; }
        }

        private class S4nGeometryFilter : IGeometryFilter
        {
            private readonly int[] crossings;

            public S4nGeometryFilter(int[] crossings)
            {
                this.crossings = crossings;
            }

            public void Filter(IGeometry geom)
            {
                int cross = 0;
                if (geom is LineString)
                {
                    //note: LinearRing extends LineString
                    if (geom.EnvelopeInternal.Width < 180)
                        return; //can't possibly cross the dateline
                    cross = UnwrapDateline((LineString)geom);
                }
                else
                    if (geom is Polygon)
                {
                    if (geom.EnvelopeInternal.Width < 180)
                        return; //can't possibly cross the dateline
                    cross = UnwrapDateline((Polygon)geom);
                }
                else
                    return;
                crossings[0] = Math.Max(crossings[0], cross);
            }
        }

        /**
		 * If <code>geom</code> spans the dateline, then this modifies it to be a
		 * valid NTS geometry that extends to the right of the standard -180 to +180
		 * width such that some points are greater than +180 but some remain less.
		 * Takes care to invoke {@link com.vividsolutions.jts.geom.Geometry#geometryChanged()}
		 * if needed.
		 *
		 * @return The number of times the geometry spans the dateline.  >= 0
		 */
        private static int UnwrapDateline(IGeometry geom)
        {
            if (geom.EnvelopeInternal.Width < 180)
                return 0;//can't possibly cross the dateline
            int[] crossings = { 0 };//an array so that an inner class can modify it.
            geom.Apply(new S4nGeometryFilter(crossings));

            return crossings[0];
        }

        /** See {@link #unwrapDateline(Geometry)}. */
        private static int UnwrapDateline(Polygon poly)
        {
            var exteriorRing = poly.ExteriorRing;
            int cross = UnwrapDateline(exteriorRing);
            if (cross > 0)
            {
                for (int i = 0; i < poly.NumInteriorRings; i++)
                {
                    var innerLineString = poly.GetInteriorRingN(i);
                    UnwrapDateline(innerLineString);
                    for (int shiftCount = 0; !exteriorRing.Contains(innerLineString); shiftCount++)
                    {
                        if (shiftCount > cross)
                            throw new ArgumentException("The inner ring doesn't appear to be within the exterior: "
                                + exteriorRing + " inner: " + innerLineString);
                        ShiftGeomByX(innerLineString, 360);
                    }
                }
                poly.GeometryChanged();
            }
            return cross;
        }

        /** See {@link #unwrapDateline(Geometry)}. */
        private static int UnwrapDateline(LineString lineString)
        {
            var cseq = lineString.CoordinateSequence;
            int size = cseq.Count;
            if (size <= 1)
                return 0;

            int shiftX = 0;//invariant: == shiftXPage*360
            int shiftXPage = 0;
            int shiftXPageMin = 0/* <= 0 */, shiftXPageMax = 0; /* >= 0 */
            double prevX = cseq.GetX(0);
            for (int i = 1; i < size; i++)
            {
                double thisX_orig = cseq.GetX(i);
                Debug.Assert(thisX_orig >= -180 && thisX_orig <= 180);// : "X not in geo bounds";
                double thisX = thisX_orig + shiftX;
                if (prevX - thisX > 180)
                {//cross dateline from left to right
                    thisX += 360;
                    shiftX += 360;
                    shiftXPage += 1;
                    shiftXPageMax = Math.Max(shiftXPageMax, shiftXPage);
                }
                else if (thisX - prevX > 180)
                {//cross dateline from right to left
                    thisX -= 360;
                    shiftX -= 360;
                    shiftXPage -= 1;
                    shiftXPageMin = Math.Min(shiftXPageMin, shiftXPage);
                }
                if (shiftXPage != 0)
                    cseq.SetOrdinate(i, Ordinate.X, thisX);
                prevX = thisX;
            }
            if (lineString is LinearRing)
            {
                Debug.Assert(cseq.GetCoordinate(0).Equals(cseq.GetCoordinate(size - 1)));
                Debug.Assert(shiftXPage == 0);//starts and ends at 0
            }
            Debug.Assert(shiftXPageMax >= 0 && shiftXPageMin <= 0);
            //Unfortunately we are shifting again; it'd be nice to be smarter and shift once
            ShiftGeomByX(lineString, shiftXPageMin * -360);
            int crossings = shiftXPageMax - shiftXPageMin;
            if (crossings > 0)
                lineString.GeometryChanged();
            return crossings;
        }

        private class S4nCoordinateSequenceFilter : ICoordinateSequenceFilter
        {
            private readonly int _xShift;

            public S4nCoordinateSequenceFilter(int xShift)
            {
                _xShift = xShift;
            }

            public void Filter(ICoordinateSequence seq, int i)
            {
                seq.SetOrdinate(i, Ordinate.X, seq.GetX(i) + _xShift);
            }

            public bool Done
            {
                get { return false; }
            }

            public bool GeometryChanged
            {
                get { return true; }
            }
        };

        private static void ShiftGeomByX(IGeometry geom, int xShift)
        {
            if (xShift == 0)
                return;
            geom.Apply(new S4nCoordinateSequenceFilter(xShift));
        }

        private static IGeometry UnionGeometryCollection(IGeometry geom)
        {
            if (geom is GeometryCollection)
            {
                return geom.Union();
            }
            return geom;
        }

        /**
		 * This "pages" through standard geo boundaries offset by multiples of 360
		 * longitudinally that intersect geom, and the intersecting results of a page
		 * and the geom are shifted into the standard -180 to +180 and added to a new
		 * geometry that is returned.
		 */
        private static IGeometry CutUnwrappedGeomInto360(IGeometry geom)
        {
            Envelope geomEnv = geom.EnvelopeInternal;
            if (geomEnv.MinX >= -180 && geomEnv.MaxX <= 180)
                return geom;
            Debug.Assert(geom.IsValid);

            //TODO support geom's that start at negative pages; will avoid need to previously shift in unwrapDateline(geom).
            var geomList = new List<IGeometry>();
            //page 0 is the standard -180 to 180 range
            for (int page = 0; true; page++)
            {
                double minX = -180 + page * 360;
                if (geomEnv.MaxX <= minX)
                    break;
                var rect = (Geometry)geom.Factory.ToGeometry(new Envelope(minX, minX + 360, -90, 90));
                Debug.Assert(rect.IsValid);
                var pageGeom = (Geometry)rect.Intersection(geom);//NTS is doing some hard work
                Debug.Assert(pageGeom.IsValid);

                ShiftGeomByX(pageGeom, page * -360);
                geomList.Add(pageGeom);
            }
            return UnaryUnionOp.Union(geomList);
        }

        //  private static Geometry removePolyHoles(Geometry geom) {
        //    //TODO this does a deep copy of geom even if no changes needed; be smarter
        //    GeometryTransformer gTrans = new GeometryTransformer() {
        //      @Override
        //      protected Geometry transformPolygon(Polygon geom, Geometry parent) {
        //        if (geom.getNumInteriorRing() == 0)
        //          return geom;
        //        return factory.createPolygon((LinearRing) geom.getExteriorRing(),null);
        //      }
        //    };
        //    return gTrans.transform(geom);
        //  }
        //
        //  private static Geometry snapAndClean(Geometry geom) {
        //    return new GeometrySnapper(geom).snapToSelf(GeometrySnapper.computeOverlaySnapTolerance(geom), true);
        //  }
    }
}
