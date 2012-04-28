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
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;

namespace Spatial4n.Core.Context
{
	/// <summary>
	/// This holds things like distance units, distance calculator, and world bounds.
	/// Threadsafe & immutable.
	/// </summary>
	public class SpatialContext
	{
		//These are non-null
		private DistanceUnits units;
		private DistanceCalculator calculator;
		private Rectangle worldBounds;

		public static readonly RectangleImpl GEO_WORLDBOUNDS = new RectangleImpl(-180, 180, -90, 90);
		public static readonly RectangleImpl MAX_WORLDBOUNDS;

		static SpatialContext()
		{
			const double v = Double.MaxValue;
			MAX_WORLDBOUNDS = new RectangleImpl(-v, v, -v, v);
		}

		public static readonly SpatialContext GEO_KM = new SpatialContext(DistanceUnits.KILOMETERS);

		protected double? maxCircleDistance;//only for geo

		/// <summary>
		/// 
		/// </summary>
		/// <param name="units">Required; and establishes geo vs cartesian.</param>
		/// <param name="calculator">Optional; defaults to Haversine or cartesian depending on units.</param>
		/// <param name="worldBounds">Optional; defaults to GEO_WORLDBOUNDS or MAX_WORLDBOUNDS depending on units.</param> 
		public SpatialContext(DistanceUnits units, DistanceCalculator calculator, Rectangle worldBounds)
		{
			Init(units, calculator, worldBounds);
		}

		public SpatialContext(DistanceUnits units)
		{
			Init(units, null, null);
		}

		protected void Init(DistanceUnits units, DistanceCalculator calculator, Rectangle worldBounds)
		{
			if (units == null)
				throw new ArgumentException("units can't be null", "units");

			this.units = units;

			if (calculator == null)
			{
				calculator = IsGeo()
					? (DistanceCalculator)new GeodesicSphereDistCalc.Haversine(units.EarthRadius())
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

			this.maxCircleDistance = IsGeo() ? calculator.DegreesToDistance(180) : (double?)null;
		}

		public DistanceUnits GetUnits()
		{
			return units;
		}

		public DistanceCalculator GetDistCalc()
		{
			return calculator;
		}

		public Rectangle GetWorldBounds()
		{
			return worldBounds;
		}

		public double NormX(double x)
		{
			if (IsGeo())
			{
				return DistanceUtils.NormLonDEG(x);
			}
			return x;
		}

		public double NormY(double y)
		{
			if (IsGeo())
			{
				y = DistanceUtils.NormLatDEG(y);
			}
			return y;
		}

		/**
		 * Is this a geospatial context (true) or simply 2d spatial (false)
		 */
		public bool IsGeo()
		{
			return GetUnits().IsGeo();
		}

		/**
		 * Read a shape from a given string (ie, X Y, XMin XMax... WKT)
		 *
		 * (1) Point: X Y
		 *   1.23 4.56
		 *
		 * (2) BOX: XMin YMin XMax YMax
		 *   1.23 4.56 7.87 4.56
		 *
		 * (3) WKT
		 *   POLYGON( ... )
		 *   http://en.wikipedia.org/wiki/Well-known_text
		 *
		 */
		public Shape ReadShape(String value)
		{
			Shape s = ReadStandardShape(value);
			if (s == null)
			{
				throw new InvalidShapeException("Unable to read: " + value);
			}
			return s;
		}

		public String ToString(Shape shape)
		{
			var point = shape as Point;
			if (point != null)
			{
				return point.GetX().ToString("F6", CultureInfo.CreateSpecificCulture("en-US")) + " " +
					   point.GetY().ToString("F6", CultureInfo.CreateSpecificCulture("en-US"));
			}

			var rect = shape as Rectangle;
			if (rect != null)
			{
				return rect.GetMinX().ToString("F6", CultureInfo.CreateSpecificCulture("en-US")) + " " +
					   rect.GetMinY().ToString("F6", CultureInfo.CreateSpecificCulture("en-US")) + " " +
					   rect.GetMaxX().ToString("F6", CultureInfo.CreateSpecificCulture("en-US")) + " " +
					   rect.GetMaxY().ToString("F6", CultureInfo.CreateSpecificCulture("en-US"));
			}
			return shape.ToString();
		}

		/** Construct a point. The parameters will be normalized. */
		public Point MakePoint(double x, double y)
		{
			return new PointImpl(NormX(x), NormY(y));
		}

		public Point ReadLatCommaLonPoint(String value)
		{
			double[] latLon = ParseUtils.ParseLatitudeLongitude(value);
			return MakePoint(latLon[1], latLon[0]);
		}

		/** Construct a rectangle. The parameters will be normalized. */
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
				}
				if (minY > maxY)
				{
					throw new ArgumentException("maxY must be >= minY");
				}
				if (minY < -90 || minY > 90 || maxY < -90 || maxY > 90)
					throw new ArgumentException("minY or maxY is outside of -90 to 90 bounds. What did you mean?");
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


		/** Construct a circle. The parameters will be normalized. */
		public Circle MakeCircle(double x, double y, double distance)
		{
			return MakeCircle(MakePoint(x, y), distance);
		}

		/**
		 * @param point
		 * @param distance The units of "distance" should be the same as {@link #getUnits()}.
		 */
		public Circle MakeCircle(Point point, double distance)
		{
			if (distance < 0)
				throw new InvalidShapeException("distance must be >= 0; got " + distance);
			if (IsGeo())
				return new GeoCircle(point, Math.Min(distance, maxCircleDistance ?? 0), this);

			return new CircleImpl(point, distance, this);
		}


		protected Shape ReadStandardShape(String str)
		{
			if (str.Length < 1)
			{
				throw new InvalidShapeException(str);
			}

			string[] st;
			var tokenPos = 0;
			if (Char.IsLetter(str[0]))
			{
				if (str.StartsWith("Circle("))
				{
					int idx = str.LastIndexOf(')');
					if (idx > 0)
					{
						var body = str.Substring("Circle(".Length, idx);

						st = body.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
						String token = st[tokenPos++];
						Point pt;
						if (token.IndexOf(',') != -1)
						{
							pt = ReadLatCommaLonPoint(token);
						}
						else
						{
							double x = Double.Parse(token);
							double y = Double.Parse(st[tokenPos++]);
							pt = MakePoint(x, y);
						}

						double d;
						String arg = st[tokenPos++];
						idx = arg.IndexOf('=');
						if (idx > 0)
						{
							String k = arg.Substring(0, idx);
							if (k.Equals("d") || k.Equals("distance"))
							{
								if (!Double.TryParse(arg.Substring(idx + 1), out d)) throw new InvalidShapeException("Missing Distance: " + str);
							}
							else
							{
								throw new InvalidShapeException("unknown arg: " + k + " :: " + str);
							}
						}
						else
						{
							if (!Double.TryParse(arg, out d)) throw new InvalidShapeException("Missing Distance: " + str);
						}
						if (st.Length > tokenPos)
						{
							throw new InvalidShapeException("Extra arguments: " + st[tokenPos] + " :: " + str);
						}
						//NOTE: we are assuming the units of 'd' is the same as that of the spatial context.
						return MakeCircle(pt, d);
					}
				}
				return null;
			}

			if (str.IndexOf(',') != -1)
				return ReadLatCommaLonPoint(str);
			st = str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			double p0 = Double.Parse(st[tokenPos++]);
			double p1 = Double.Parse(st[tokenPos++]);
			if (st.Length > tokenPos)
			{
				double p2 = Double.Parse(st[tokenPos++]);
				double p3 = Double.Parse(st[tokenPos++]);
				if (st.Length > tokenPos)
					throw new InvalidShapeException("Only 4 numbers supported (rect) but found more: " + str);
				return MakeRect(p0, p2, p1, p3);
			}
			return MakePoint(p0, p1);
		}

		public override String ToString()
		{
			return GetType().Name + "{" +
				"units=" + units +
				", calculator=" + calculator +
				", worldBounds=" + worldBounds +
				'}';
		}
	}
}
