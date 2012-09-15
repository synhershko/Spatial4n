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
		public static readonly SpatialContext GEO = new SpatialContext(true);

		//These are non-null
		private readonly bool geo;
		private readonly DistanceCalculator calculator;
		private readonly Rectangle worldBounds;

		private readonly ShapeReadWriter shapeReadWriter;

		protected virtual ShapeReadWriter MakeShapeReadWriter()
		{
			return new ShapeReadWriter(this);
		}

		[Obsolete]
		public Shape ReadShape(String value)
		{
			return shapeReadWriter.ReadShape(value);
		}

		[Obsolete]
		public String ToString(Shape shape)
		{
			return shapeReadWriter.WriteShape(shape);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="geo">Establishes geo vs cartesian / Euclidean.</param>
		/// <param name="calculator">Optional; defaults to Haversine or cartesian depending on units.</param>
		/// <param name="worldBounds">Optional; defaults to GEO_WORLDBOUNDS or MAX_WORLDBOUNDS depending on units.</param> 
		public SpatialContext(bool geo, DistanceCalculator calculator, Rectangle worldBounds)
		{
			this.geo = geo;

			if (calculator == null)
			{
				calculator = IsGeo()
					? (DistanceCalculator)new GeodesicSphereDistCalc.Haversine()
					: new CartesianDistCalc();
			}
			this.calculator = calculator;

			if (worldBounds == null)
			{
				worldBounds = IsGeo() ?
                    new RectangleImpl(-180, 180, -90, 90, this)
                    : new RectangleImpl(-Double.MaxValue, Double.MaxValue,-Double.MaxValue, Double.MaxValue, this);
			}
			else
			{
				if (IsGeo())
					Debug.Assert(worldBounds.Equals(new RectangleImpl(-180, 180, -90, 90, this)));
				if (worldBounds.GetCrossesDateLine())
					throw new ArgumentException("worldBounds shouldn't cross dateline: " + worldBounds, "worldBounds");
			}
			//hopefully worldBounds' rect implementation is compatible
			this.worldBounds = new RectangleImpl(worldBounds, this);

			shapeReadWriter = MakeShapeReadWriter();
		}

		public SpatialContext(bool geo)
			: this(geo, null, null)
		{
		}

		public DistanceCalculator GetDistCalc()
		{
			return calculator;
		}

        /// <summary>
        /// The extent of x & y coordinates should fit within the return'ed rectangle.
        /// Do *NOT* invoke reset() on this return type.
        /// </summary>
        /// <returns></returns>
		public Rectangle GetWorldBounds()
		{
			return worldBounds;
		}

		/// <summary>
		/// Is this a geospatial context (true) or simply 2d spatial (false).
		/// </summary>
		/// <returns></returns>
		public bool IsGeo()
		{
			return geo;
		}

        /// <summary>
        /// Ensure fits in {@link #getWorldBounds()}
        /// </summary>
        /// <param name="x"></param>
        public void VerifyX(double x)
        {
            Rectangle bounds = GetWorldBounds();
            if (!(x >= bounds.GetMinX() && x <= bounds.GetMaxX())) //NaN will fail
                throw new InvalidShapeException("Bad X value " + x + " is not in boundary " + bounds);
        }

        /// <summary>
        /// Ensure fits in {@link #getWorldBounds()}
        /// </summary>
        /// <param name="y"></param>
        public void VerifyY(double y)
        {
            Rectangle bounds = GetWorldBounds();
            if (!(y >= bounds.GetMinY() && y <= bounds.GetMaxY())) //NaN will fail
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
		public Rectangle MakeRectangle(Point lowerLeft, Point upperRight)
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
		public Rectangle MakeRectangle(double minX, double maxX, double minY, double maxY)
	    {
	        Rectangle bounds = GetWorldBounds();
	        // Y
	        if (!(minY >= bounds.GetMinY() && maxY <= bounds.GetMaxY())) //NaN will fail
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
	            if (!(minX >= bounds.GetMinX() && maxX <= bounds.GetMaxX())) //NaN will fail
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
		public Circle MakeCircle(double x, double y, double distance)
		{
			return MakeCircle(MakePoint(x, y), distance);
		}

		/// <summary>
        /// Construct a circle. The units of "distance" should be the same as x & y.
		/// </summary>
		/// <param name="point"></param>
		/// <param name="distance"></param>
		/// <returns></returns>
        public Circle MakeCircle(Point point, double distance)
		{
		    if (distance < 0)
		        throw new InvalidShapeException("distance must be >= 0; got " + distance);
		    if (IsGeo())
		    {
		        if (distance > 180)
		            throw new InvalidShapeException("distance must be <= 180; got " + distance);
		        return new GeoCircle(point, distance, this);
		    }
		    else
		    {
		        return new CircleImpl(point, distance, this);
		    }
		}

	    public override String ToString()
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
