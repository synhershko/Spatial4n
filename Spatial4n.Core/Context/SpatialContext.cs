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
	/// instance {@link #GEO_KM}.  Also, if you wish to construct one based on
	/// configuration information then consider using {@link SpatialContextFactory}.
	/// <p/>
	/// Thread-safe & immutable.
	/// </summary>
	public class SpatialContext
	{
		public static readonly RectangleImpl GEO_WORLDBOUNDS = new RectangleImpl(-180, 180, -90, 90);
		public static readonly RectangleImpl MAX_WORLDBOUNDS;

		static SpatialContext()
		{
			const double v = Double.MaxValue;
			MAX_WORLDBOUNDS = new RectangleImpl(-v, v, -v, v);
		}

		/// <summary>
		/// A popular default SpatialContext implementation for geospatial.
		/// </summary>
		public static readonly SpatialContext GEO = new SpatialContext(true);
		//note: any static convenience instances must be declared after the world bounds

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
				worldBounds = IsGeo() ? GEO_WORLDBOUNDS : MAX_WORLDBOUNDS;
			}
			else
			{
				if (IsGeo())
					Debug.Assert(new RectangleImpl(worldBounds).Equals(GEO_WORLDBOUNDS));
				if (worldBounds.GetCrossesDateLine())
					throw new ArgumentException("worldBounds shouldn't cross dateline: " + worldBounds, "worldBounds");
			}
			//copy so we can ensure we have the right implementation
			worldBounds = MakeRect(worldBounds.GetMinX(), worldBounds.GetMaxX(), worldBounds.GetMinY(), worldBounds.GetMaxY());
			this.worldBounds = worldBounds;

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

		public Rectangle GetWorldBounds()
		{
			return worldBounds;
		}

		/// <summary>
		/// If {@link #isGeo()} then calls {@link DistanceUtils#normLonDEG(double)}.
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		public double NormX(double x)
		{
			if (IsGeo())
			{
				return DistanceUtils.NormLonDEG(x);
			}
			return x;
		}

		/// <summary>
		/// If {@link #isGeo()} then calls {@link DistanceUtils#normLatDEG(double)}
		/// </summary>
		/// <param name="y"></param>
		/// <returns></returns>
		public double NormY(double y)
		{
			if (IsGeo())
			{
				y = DistanceUtils.NormLatDEG(y);
			}
			return y;
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
		/// Construct a point. The parameters will be normalized.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public virtual Point MakePoint(double x, double y)
		{
			return new PointImpl(NormX(x), NormY(y));
		}

		/// <summary>
		/// Construct a rectangle. The parameters will be normalized.
		/// </summary>
		/// <param name="lowerLeft"></param>
		/// <param name="upperRight"></param>
		/// <returns></returns>
		public Rectangle MakeRect(Point lowerLeft, Point upperRight)
		{
			return MakeRect(lowerLeft.GetX(), upperRight.GetX(),
			                lowerLeft.GetY(), upperRight.GetY());
		}

		/// <summary>
		/// Construct a rectangle. The parameters will be normalized.
		/// </summary>
		/// <param name="minX"></param>
		/// <param name="maxX"></param>
		/// <param name="minY"></param>
		/// <param name="maxY"></param>
		/// <returns></returns>
		public Rectangle MakeRect(double minX, double maxX, double minY, double maxY)
		{
			//--Normalize parameters
			if (IsGeo())
			{
				double delta = CalcWidth(minX, maxX);
				if (delta >= 360)
				{
					//The only way to officially support complete longitude wrap-around is via western longitude = -180. We can't
					// support any point because 0 is undifferentiated in sign.
					minX = -180;
					maxX = 180;
				}
				else
				{
					minX = NormX(minX);
					maxX = NormX(maxX);
					Debug.Assert(Math.Abs(delta - CalcWidth(minX, maxX)) < 0.0001);//recompute delta; should be the same

					//If an edge coincides with the dateline then don't make this rect cross it
					if (delta > 0)
					{
						if (minX == 180)
						{
							minX = -180;
							maxX = -180 + delta;
						}
						else if (maxX == -180)
						{
							maxX = 180;
							minX = 180 - delta;
						}
					}
				}
				if (minY > maxY)
				{
					throw new ArgumentException("maxY must be >= minY: " + minY + " to " + maxY, "maxY");
				}
				if (minY < -90 || minY > 90 || maxY < -90 || maxY > 90)
					throw new ArgumentException("minY or maxY is outside of -90 to 90 bounds. What did you mean?: "+minY+" to "+maxY);
				//        debatable what to do in this situation.
				//        if (minY < -90) {
				//          minX = -180;
				//          maxX = 180;
				//          maxY = Math.min(90,Math.max(maxY,-90 + (-90 - minY)));
				//          minY = -90;
				//        }
				//        if (maxY > 90) {
				//          minX = -180;
				//          maxX = 180;
				//          minY = Math.max(-90,Math.min(minY,90 - (maxY - 90)));
				//          maxY = 90;
				//        }

			}
			else
			{
				//these normalizations probably won't do anything since it's not geo but should probably call them any way.
				minX = NormX(minX);
				maxX = NormX(maxX);
				minY = NormY(minY);
				maxY = NormY(maxY);
			}
			return new RectangleImpl(minX, maxX, minY, maxY);
		}

		private double CalcWidth(double minX, double maxX)
		{
			double w = maxX - minX;
			if (w < 0)
			{//only true when minX > maxX (WGS84 assumed)
				w += 360;
				Debug.Assert(w >= 0);
			}
			return w;
		}

		/// <summary>
		/// Construct a circle. The parameters will be normalized.
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
		/// 
		/// </summary>
		/// <param name="point"></param>
		/// <param name="distance">The units of "distance" should be the same as {@link #GetUnits()}.</param>
		/// <returns></returns>
		public Circle MakeCircle(Point point, double distance)
		{
			if (distance < 0)
				throw new InvalidShapeException("distance must be >= 0; got " + distance);
			if (IsGeo())
				return new GeoCircle(point, Math.Min(distance, 180), this);

			return new CircleImpl(point, distance, this);
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
