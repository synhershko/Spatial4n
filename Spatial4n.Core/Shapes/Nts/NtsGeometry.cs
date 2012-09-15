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

namespace Spatial4n.Core.Shapes.Nts
{
	/// <summary>
	/// Wraps a JTS {@link Geometry} (i.e. may be a polygon or basically anything).
	/// JTS's does a great deal of the hard work, but there is work here in handling
	/// dateline wrap.
	/// </summary>
	public class NtsGeometry : Shape
	{
		private readonly IGeometry geom;//cannot be a direct instance of GeometryCollection as it doesn't support relate()
		private readonly bool _hasArea;
		private readonly Rectangle bbox;
        private readonly NtsSpatialContext ctx;

		public NtsGeometry(IGeometry geom, NtsSpatialContext ctx, bool dateline180Check)
		{
            this.ctx = ctx;

			//GeometryCollection isn't supported in relate()
			if (geom.GetType() == typeof(GeometryCollection))
				throw new ArgumentException("NtsGeometry does not support GeometryCollection but does support its subclasses.");

			//NOTE: All this logic is fairly expensive. There are some short-circuit checks though.
            if (ctx.IsGeo())
            {
                //Unwraps the geometry across the dateline so it exceeds the standard geo bounds (-180 to +180).
                if (dateline180Check)
                    UnwrapDateline(geom); //potentially modifies geom
                //If given multiple overlapping polygons, fix it by union
                geom = UnionGeometryCollection(geom); //returns same or new geom
                Envelope unwrappedEnv = geom.EnvelopeInternal;

                //Cuts an unwrapped geometry back into overlaid pages in the standard geo bounds.
                geom = CutUnwrappedGeomInto360(geom); //returns same or new geom
                Debug.Assert(geom.EnvelopeInternal.Width <= 360);
                Debug.Assert(geom.GetType() != typeof (GeometryCollection)); //double check

                //note: this bbox may be sub-optimal. If geom is a collection of things near the dateline on both sides then
                // the bbox will needlessly span most or all of the globe longitudinally.
                // TODO so consider using MultiShape's planned minimal geo bounding box algorithm once implemented.
                double envWidth = unwrappedEnv.Width;

                //adjust minX and maxX considering the dateline and world wrap
                double minX, maxX;
                if (envWidth >= 360)
                {
                    minX = -180;
                    maxX = 180;
                }
                else
                {
                    minX = unwrappedEnv.MinX;
                    maxX = DistanceUtils.NormLonDEG(unwrappedEnv.MinX + envWidth);
                }
                bbox = new RectangleImpl(minX, maxX, unwrappedEnv.MinY, unwrappedEnv.MaxY, ctx);
            }
            else
            {//not geo
                Envelope env = geom.EnvelopeInternal;
                bbox = new RectangleImpl(env.MinX, env.MaxX, env.MinY, env.MaxY, ctx);
            }
			var _ = geom.EnvelopeInternal;//ensure envelope is cached internally, which is lazy evaluated. Keeps this thread-safe.

			//Check geom validity; use helpful error
			// TODO add way to conditionally skip at your peril later
			var isValidOp = new IsValidOp(geom);
			if (!isValidOp.IsValid)
				throw new InvalidShapeException(isValidOp.ValidationError.ToString());
			this.geom = geom;

			this._hasArea = !((geom is ILineal) || (geom is IPuntal));
		}

		public static SpatialRelation IntersectionMatrixToSpatialRelation(IntersectionMatrix matrix)
		{
			if (matrix.IsContains())
				return SpatialRelation.CONTAINS;
			else if (matrix.IsWithin() /* TODO needs to be matrix.IsCoveredBy()*/)
				return SpatialRelation.WITHIN;
			else if (matrix.IsDisjoint())
				return SpatialRelation.DISJOINT;
			return SpatialRelation.INTERSECTS;
		}

		//----------------------------------------
		//----------------------------------------

		public bool HasArea()
		{
			return _hasArea;
		}

		public double GetArea(SpatialContext ctx)
		{
			double geomArea = geom.Area;
			if (ctx == null || geomArea == 0)
				return geomArea;
			//Use the area proportional to how filled the bbox is.
			double bboxArea = GetBoundingBox().GetArea(null);//plain 2d area
			Debug.Assert(bboxArea >= geomArea);
			double filledRatio = geomArea / bboxArea;
			return GetBoundingBox().GetArea(ctx) * filledRatio;
			// (Future: if we know we use an equal-area projection then we don't need to
			//  estimate)
		}

		public Rectangle GetBoundingBox()
		{
			return bbox;
		}

		public Point GetCenter()
		{
			return new NtsPoint((NetTopologySuite.Geometries.Point)geom.Centroid, ctx);
		}

		public SpatialRelation Relate(Shape other)
		{
			if (other is Point)
				return Relate((Point)other);
			else if (other is Rectangle)
				return Relate((Rectangle)other);
			else if (other is Circle)
				return Relate((Circle)other, ctx);
			else if (other is NtsGeometry)
				return Relate((NtsGeometry)other);
			return other.Relate(this).Transpose();
		}

		public SpatialRelation Relate(Point pt)
		{
			//TODO if not jtsPoint, test against bbox to avoid JTS if disjoint
			var jtsPoint = (NtsPoint)(pt is NtsPoint ? pt : ctx.MakePoint(pt.GetX(), pt.GetY()));
			return geom.Disjoint(jtsPoint.GetGeom()) ? SpatialRelation.DISJOINT : SpatialRelation.CONTAINS;
		}

		public SpatialRelation Relate(Rectangle rectangle)
		{
			SpatialRelation bboxR = bbox.Relate(rectangle);
			if (bboxR == SpatialRelation.WITHIN || bboxR == SpatialRelation.DISJOINT)
				return bboxR;
			IGeometry oGeom = ctx.GetGeometryFrom(rectangle);
			return IntersectionMatrixToSpatialRelation(geom.Relate(oGeom));
		}

		public SpatialRelation Relate(Circle circle, SpatialContext ctx)
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
				SpatialRelation sect = circle.Relate(new PointImpl(coord.X, coord.Y, ctx));
				if (sect == SpatialRelation.DISJOINT)
					outside++;
				if (i != outside && outside != 0)//short circuit: partially outside, partially inside
					return SpatialRelation.INTERSECTS;
			}
			if (i == outside)
			{
				return (Relate(circle.GetCenter()) == SpatialRelation.DISJOINT)
					? SpatialRelation.DISJOINT : SpatialRelation.CONTAINS;
			}
			Debug.Assert(outside == 0);
			return SpatialRelation.WITHIN;
		}

		public SpatialRelation Relate(NtsGeometry jtsGeometry)
		{
			IGeometry oGeom = jtsGeometry.geom;
			//don't bother checking bbox since geom.relate() does this already
			return IntersectionMatrixToSpatialRelation(geom.Relate(oGeom));
		}

		public override String ToString()
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

		public IGeometry GetGeom()
		{
			return geom;
		}

		private class S4nGeometryFilter : IGeometryFilter
		{
			private readonly int[] _result;

			public S4nGeometryFilter(int[] result)
			{
				_result = result;
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
				_result[0] = Math.Max(_result[0], cross);
			}
		}

		/**
		 * If <code>geom</code> spans the dateline, then this modifies it to be a
		 * valid JTS geometry that extends to the right of the standard -180 to +180
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
			int[] result = { 0 };//an array so that an inner class can modify it.
			geom.Apply(new S4nGeometryFilter(result));

			int crossings = result[0];
			return crossings;
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
				var pageGeom = (Geometry)rect.Intersection(geom);//JTS is doing some hard work
				Debug.Assert(pageGeom.IsValid);

				ShiftGeomByX(pageGeom, page * -360);
				geomList.Add(pageGeom);
			}
			return UnaryUnionOp.Union(geomList);
		}
	}
}
