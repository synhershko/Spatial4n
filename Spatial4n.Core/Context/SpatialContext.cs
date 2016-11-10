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
using System.Diagnostics;
using System.Globalization;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.Io;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;
using System.Collections.Generic;

namespace Spatial4n.Core.Context
{
	/// <summary>
	/// This is a facade to most of Spatial4j, holding things like {@link
	/// DistanceUnits}, {@link DistanceCalculator}, and the coordinate world
	/// boundaries, and acting as a factory for the {@link Shape}s.
	/// <p/>
	/// A SpatialContext has public constructors, but note the convenience
	/// instance {@link #GEO}.  Also, if you wish to construct one based on
	/// configuration information then consider using {@link SpatialContextFactory}.
	/// <p/>
	/// Thread-safe & immutable.
	/// </summary>
	public class SpatialContext
	{
		/// <summary>
		/// A popular default SpatialContext implementation for geospatial.
		/// </summary>
		public static readonly SpatialContext GEO = new SpatialContext(new SpatialContextFactory());

		//These are non-null
		private readonly bool geo;
		private readonly DistanceCalculator calculator;
		private readonly Rectangle worldBounds;

        private readonly WktShapeParser wktShapeParser;
        private readonly BinaryCodec binaryCodec;

        private readonly bool normWrapLongitude;




        /// <summary>
        /// Consider using <see cref="SpatialContextFactory"/> instead.
        /// </summary>
        /// <param name="geo">Establishes geo vs cartesian / Euclidean.</param>
        /// <param name="calculator">Optional; defaults to Haversine or cartesian depending on units.</param>
        /// <param name="worldBounds">Optional; defaults to GEO_WORLDBOUNDS or MAX_WORLDBOUNDS depending on units.</param> 
        [Obsolete]
        public SpatialContext(bool geo, DistanceCalculator calculator, Rectangle worldBounds)
            : this(InitFromLegacyConstructor(geo, calculator, worldBounds))
        { }

        private static SpatialContextFactory InitFromLegacyConstructor(bool geo,
                                                                 DistanceCalculator calculator,
                                                                 Rectangle worldBounds)
        {
            SpatialContextFactory factory = new SpatialContextFactory();
            factory.geo = geo;
            factory.distCalc = calculator;
            factory.worldBounds = worldBounds;
            return factory;
        }

        [Obsolete]
        public SpatialContext(bool geo)
            : this(InitFromLegacyConstructor(geo, null, null))
        { }

        /**
   * Called by {@link com.spatial4j.core.context.SpatialContextFactory#newSpatialContext()}.
   */
        public SpatialContext(SpatialContextFactory factory)
        {
            this.geo = factory.geo;

            if (factory.distCalc == null)
            {
                this.calculator = IsGeo()
                        ? (DistanceCalculator)new GeodesicSphereDistCalc.Haversine()
                        : new CartesianDistCalc();
            }
            else
            {
                this.calculator = factory.distCalc;
            }

            //TODO remove worldBounds from Spatial4j: see Issue #55
            Rectangle bounds = factory.worldBounds;
            if (bounds == null)
            {
                this.worldBounds = IsGeo()
                        ? new RectangleImpl(-180, 180, -90, 90, this)
                        : new RectangleImpl(-double.MaxValue, double.MaxValue,
                        -double.MaxValue, double.MaxValue, this);
            }
            else
            {
                if (IsGeo() && !bounds.Equals(new RectangleImpl(-180, 180, -90, 90, this)))
                    throw new ArgumentException("for geo (lat/lon), bounds must be " + GEO.GetWorldBounds());
                if (bounds.GetMinX() > bounds.GetMaxX())
                    throw new ArgumentException("worldBounds minX should be <= maxX: " + bounds);
                if (bounds.GetMinY() > bounds.GetMaxY())
                    throw new ArgumentException("worldBounds minY should be <= maxY: " + bounds);
                //hopefully worldBounds' rect implementation is compatible
                this.worldBounds = new RectangleImpl(bounds, this);
            }

            this.normWrapLongitude = factory.normWrapLongitude && this.IsGeo();
            this.wktShapeParser = factory.MakeWktShapeParser(this);
            this.binaryCodec = factory.MakeBinaryCodec(this);
        }
       
		public virtual DistanceCalculator GetDistCalc()
		{
			return calculator;
		}

        /** Convenience that uses {@link #getDistCalc()} */
        public virtual double CalcDistance(Point p, double x2, double y2)
        {
            return GetDistCalc().Distance(p, x2, y2);
        }

        /** Convenience that uses {@link #getDistCalc()} */
        public virtual double CalcDistance(Point p, Point p2)
        {
            return GetDistCalc().Distance(p, p2);
        }

        /// <summary>
        /// The extent of x & y coordinates should fit within the return'ed rectangle.
        /// Do *NOT* invoke reset() on this return type.
        /// </summary>
        /// <returns></returns>
		public virtual Rectangle GetWorldBounds()
		{
			return worldBounds;
		}

        /** If true then {@link #normX(double)} will wrap longitudes outside of the standard
        * geodetic boundary into it. Example: 181 will become -179. */
        public virtual bool IsNormWrapLongitude()
        {
            return normWrapLongitude;
        }

        /// <summary>
        /// Is this a geospatial context (true) or simply 2d spatial (false).
        /// </summary>
        /// <returns></returns>
        public virtual bool IsGeo()
		{
			return geo;
		}

        /** Normalize the 'x' dimension. Might reduce precision or wrap it to be within the bounds. This
   * is called by {@link com.spatial4j.core.io.WktShapeParser} before creating a shape. */
        public virtual double NormX(double x)
        {
            if (normWrapLongitude)
                x = DistanceUtils.NormLonDEG(x);
            return x;
        }

        /** Normalize the 'y' dimension. Might reduce precision or wrap it to be within the bounds. This
         * is called by {@link com.spatial4j.core.io.WktShapeParser} before creating a shape. */
        public virtual double NormY(double y) { return y; }

        /// <summary>
        /// Ensure fits in {@link #getWorldBounds()}
        /// </summary>
        /// <param name="x"></param>
        public virtual void VerifyX(double x)
        {
            Rectangle bounds = GetWorldBounds();
            if (x < bounds.GetMinX() || x > bounds.GetMaxX()) //NaN will fail
                throw new InvalidShapeException("Bad X value " + x + " is not in boundary " + bounds);
        }

        /// <summary>
        /// Ensure fits in {@link #getWorldBounds()}
        /// </summary>
        /// <param name="y"></param>
        public virtual void VerifyY(double y)
        {
            Rectangle bounds = GetWorldBounds();
            if (y < bounds.GetMinY() || y > bounds.GetMaxY()) //NaN will fail
                throw new InvalidShapeException("Bad Y value " + y + " is not in boundary " + bounds);
        }

	    /// <summary>
		/// Construct a point.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public virtual Point MakePoint(double x, double y)
		{
            VerifyX(x);
            VerifyY(y);
			return new PointImpl(x, y, this);
		}

	    /// <summary>
		/// Construct a rectangle. The parameters will be normalized.
		/// </summary>
		/// <param name="lowerLeft"></param>
		/// <param name="upperRight"></param>
		/// <returns></returns>
		public virtual Rectangle MakeRectangle(Point lowerLeft, Point upperRight)
		{
			return MakeRectangle(lowerLeft.GetX(), upperRight.GetX(),
			                lowerLeft.GetY(), upperRight.GetY());
		}

	    /// <summary>
	    /// Construct a rectangle. If just one longitude is on the dateline (+/- 180)
	    /// then potentially adjust its sign to ensure the rectangle does not cross the
	    /// dateline.
	    /// </summary>
	    /// <param name="minX"></param>
	    /// <param name="maxX"></param>
	    /// <param name="minY"></param>
	    /// <param name="maxY"></param>
	    /// <returns></returns>
		public virtual Rectangle MakeRectangle(double minX, double maxX, double minY, double maxY)
	    {
	        Rectangle bounds = GetWorldBounds();
	        // Y
	        if (minY < bounds.GetMinY() || maxY > bounds.GetMaxY()) //NaN will fail
	            throw new InvalidShapeException("Y values [" + minY + " to " + maxY + "] not in boundary " + bounds);
	        if (minY > maxY)
	            throw new InvalidShapeException("maxY must be >= minY: " + minY + " to " + maxY);
	        // X
	        if (IsGeo())
	        {
	            VerifyX(minX);
	            VerifyX(maxX);
	            //TODO consider removing this logic so that there is no normalization here
	            //if (minX != maxX) {   USUALLY TRUE, inline check below
	            //If an edge coincides with the dateline then don't make this rect cross it
	            if (minX == 180 && minX != maxX)
	            {
	                minX = -180;
	            }
	            else if (maxX == -180 && minX != maxX)
	            {
	                maxX = 180;
	            }
	            //}
	        }
	        else
	        {
	            if (minX < bounds.GetMinX() || maxX > bounds.GetMaxX()) //NaN will fail
	                throw new InvalidShapeException("X values [" + minX + " to " + maxX + "] not in boundary " + bounds);
	            if (minX > maxX)
	                throw new InvalidShapeException("maxX must be >= minX: " + minX + " to " + maxX);
	        }
	        return new RectangleImpl(minX, maxX, minY, maxY, this);
	    }

		/// <summary>
        /// Construct a circle. The units of "distance" should be the same as x & y.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="distance"></param>
		/// <returns></returns>
		public virtual Circle MakeCircle(double x, double y, double distance)
		{
			return MakeCircle(MakePoint(x, y), distance);
		}

		/// <summary>
        /// Construct a circle. The units of "distance" should be the same as x & y.
		/// </summary>
		/// <param name="point"></param>
		/// <param name="distance"></param>
		/// <returns></returns>
        public virtual Circle MakeCircle(Point point, double distance)
		{
		    if (distance < 0)
		        throw new InvalidShapeException("distance must be >= 0; got " + distance);
		    if (IsGeo())
		    {
                if (distance > 180)
                {
                    // (it's debatable whether to error or not)
                    //throw new InvalidShapeException("distance must be <= 180; got " + distance);
                    distance = 180;
                }
                return new GeoCircle(point, distance, this);
		    }
		    else
		    {
		        return new CircleImpl(point, distance, this);
		    }
		}

        /** Constructs a line string. It's an ordered sequence of connected vertexes. There
   * is no official shape/interface for it yet so we just return Shape. */
        public virtual Shape MakeLineString(IList<Point> points)
        {
            return new BufferedLineString(points, 0, false, this);
        }

        /** Constructs a buffered line string. It's an ordered sequence of connected vertexes,
         * with a buffer distance along the line in all directions. There
         * is no official shape/interface for it so we just return Shape. */
        public virtual Shape MakeBufferedLineString(IList<Point> points, double buf)
        {
            return new BufferedLineString(points, buf, IsGeo(), this);
        }

        /** Construct a ShapeCollection, analogous to an OGC GeometryCollection. */
        public virtual ShapeCollection MakeCollection(IList<Shape> coll) //where S : Shape
        {
            return new ShapeCollection(coll, this);
        }

        /** The {@link com.spatial4j.core.io.WktShapeParser} used by {@link #readShapeFromWkt(String)}. */
        public virtual WktShapeParser GetWktShapeParser()
        {
            return wktShapeParser;
        }

        /** Reads a shape from the string formatted in WKT.
   * @see com.spatial4j.core.io.WktShapeParser
   * @param wkt non-null WKT.
   * @return non-null
   * @throws ParseException if it failed to parse.
   */
        public virtual Shape ReadShapeFromWkt(string wkt) 
        {
            return wktShapeParser.Parse(wkt);
        }

        public virtual BinaryCodec BinaryCodec
        { 
            get { return binaryCodec; }
        }

        [Obsolete]
        public virtual Shape ReadShape(string value)
        {
            Shape s = LegacyShapeReadWriterFormat.ReadShapeOrNull(value, this);
            if (s == null)
            {
                try
                {
                    s = ReadShapeFromWkt(value);
                }
                catch (ParseException e)
                {
                    if (e.InnerException is InvalidShapeException)
                        throw (InvalidShapeException)e.InnerException;
                    throw new InvalidShapeException(e.ToString(), e);
                }
            }
            return s;
        }

        [Obsolete]
        public virtual string ToString(Shape shape)
        {
            return LegacyShapeReadWriterFormat.WriteShape(shape);
        }

        public override string ToString()
		{
			if (this.Equals(GEO))
				return GEO.GetType().Name + ".GEO";

			return GetType().Name + "{" +
			       "geo=" + geo +
			       ", calculator=" + calculator +
			       ", worldBounds=" + worldBounds +
			       '}';
		}
	}
}
