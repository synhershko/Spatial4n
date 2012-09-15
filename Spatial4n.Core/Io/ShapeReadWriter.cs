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
using System.Globalization;
using Spatial4n.Core.Context;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.Shapes;

namespace Spatial4n.Core.Io
{
	public class ShapeReadWriter
	{
		protected SpatialContext Ctx;

		public ShapeReadWriter(SpatialContext ctx)
		{
			Ctx = ctx;
		}

		/// <summary>
		/// Reads a shape from a given string (ie, X Y, XMin XMax... WKT)
		/// <ul>
		///   <li>Point: X Y
		///   <br /> 1.23 4.56
		///   </li>
		///   <li>BOX: XMin YMin XMax YMax
		///   <br /> 1.23 4.56 7.87 4.56</li>
		///   <li><a href="http://en.wikipedia.org/wiki/Well-known_text">
		///     WKT (Well Known Text)</a>
		///   <br /> POLYGON( ... )
		///   <br /> <b>Note:</b>Polygons and WKT might not be supported by this
		///   spatial context; you'll have to use {@link com.spatial4j.core.context.jts.JtsSpatialContext}.
		///   </li>
		/// </ul>
		/// @param value A string representation of the shape; not null.
		/// @return A Shape; not null.
		///
		/// @see #writeShape
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public virtual Shape ReadShape(string value)
		{
			Shape s = ReadStandardShape(value);
			if (s == null)
			{
				throw new InvalidShapeException("Unable to read: " + value);
			}
			return s;
		}

		/// <summary>
		/// Writes a shape to a String, in a format that can be read by {@link #readShape(String)}.
		/// </summary>
		/// <param name="shape"></param>
		/// <returns></returns>
		public virtual String WriteShape(Shape shape)
		{
			// TODO: Support Java's NumberFormat behavior

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

			var c = shape as Circle;
			if (c != null)
			{
				return "Circle(" +
					   c.GetCenter().GetX().ToString("F6", CultureInfo.CreateSpecificCulture("en-US")) + " " +
					   c.GetCenter().GetY().ToString("F6", CultureInfo.CreateSpecificCulture("en-US")) + " " +
					   "d=" + c.GetRadius().ToString("F6", CultureInfo.CreateSpecificCulture("en-US")) +
					   ")";
			}

			return shape.ToString();
		}

		protected Shape ReadStandardShape(String str)
		{
			if (string.IsNullOrEmpty(str))
				throw new InvalidShapeException(str);

			string[] st;
			var tokenPos = 0;
			if (Char.IsLetter(str[0]))
			{
				if (str.StartsWith("Circle(") || str.StartsWith("CIRCLE("))
				{
					int idx = str.LastIndexOf(')');
					if (idx > 0)
					{
						//Substring in .NET is (startPosn, length), But in Java it's (startPosn, endPosn)
						//see http://docs.oracle.com/javase/1.4.2/docs/api/java/lang/String.html#substring(int, int)
						var body = str.Substring("Circle(".Length,
												 (idx - "Circle(".Length));

						st = body.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
						String token = st[tokenPos++];
						Point pt;
						if (token.IndexOf(',') != -1)
						{
							pt = ReadLatCommaLonPoint(token);
						}
						else
						{
							double x = Double.Parse(token, CultureInfo.InvariantCulture);
							double y = Double.Parse(st[tokenPos++], CultureInfo.InvariantCulture);
							pt = Ctx.MakePoint(x, y);
						}


						double d;
						String arg = st[tokenPos++];
						idx = arg.IndexOf('=');
						if (idx > 0)
						{
							String k = arg.Substring(0, idx);
							if (k.Equals("d") || k.Equals("distance"))
							{
								if (!Double.TryParse(arg.Substring(idx + 1), out d))
									throw new InvalidShapeException("Missing Distance: " + str);
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
						return Ctx.MakeCircle(pt, d);

					}
				}
				return null;
			}

			if (str.IndexOf(',') != -1)
				return ReadLatCommaLonPoint(str);
			st = str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			double p0 = Double.Parse(st[tokenPos++], CultureInfo.InvariantCulture);
			double p1 = Double.Parse(st[tokenPos++], CultureInfo.InvariantCulture);
			if (st.Length > tokenPos)
			{
				double p2 = Double.Parse(st[tokenPos++], CultureInfo.InvariantCulture);
				double p3 = Double.Parse(st[tokenPos++], CultureInfo.InvariantCulture);
				if (st.Length > tokenPos)
					throw new InvalidShapeException("Only 4 numbers supported (rect) but found more: " + str);
				return Ctx.MakeRectangle(p0, p2, p1, p3);
			}
			return Ctx.MakePoint(p0, p1);
		}

		/// <summary>
		/// Reads geospatial latitude then a comma then longitude.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private Point ReadLatCommaLonPoint(String value)
		{
			double[] latLon = ParseUtils.ParseLatitudeLongitude(value);
			return Ctx.MakePoint(latLon[1], latLon[0]);
		}
	}
}
