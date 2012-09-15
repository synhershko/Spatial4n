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
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Utilities;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.Io;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Nts;

namespace Spatial4n.Core.Context.Nts
{
	/// <summary>
	/// Enhances the default {@link SpatialContext} with support for Polygons (and
	/// other geometry) plus
	/// reading <a href="http://en.wikipedia.org/wiki/Well-known_text">WKT</a>. The
	/// popular <a href="https://sourceforge.net/projects/jts-topo-suite/">JTS</a>
	/// library does the heavy lifting.
	/// </summary>
	public class NtsSpatialContext : SpatialContext
	{
		public new static readonly NtsSpatialContext GEO = new NtsSpatialContext(true);

		private readonly GeometryFactory geometryFactory;

		public NtsSpatialContext(bool geo)
			: this(null, geo, null, null)
		{
		}

		/**
		 * See {@link SpatialContext#SpatialContext(com.spatial4j.core.distance.DistanceUnits, com.spatial4j.core.distance.DistanceCalculator, com.spatial4j.core.shape.Rectangle)}.
		 *
		 * @param geometryFactory optional
		 */
		public NtsSpatialContext(GeometryFactory geometryFactory, bool geo, DistanceCalculator calculator, Rectangle worldBounds)
			: base(geo, calculator, worldBounds)
		{
			this.geometryFactory = geometryFactory ?? new GeometryFactory();
		}

		protected override ShapeReadWriter MakeShapeReadWriter()
		{
			return new NtsShapeReadWriter(this);
		}

		/**
		 * Gets a JTS {@link Geometry} for the given {@link Shape}. Some shapes hold a
		 * JTS geometry whereas new ones must be created for the rest.
		 * @param shape Not null
		 * @return Not null
		 */
		public IGeometry GetGeometryFrom(Shape shape)
		{
			if (shape is NtsGeometry)
			{
				return ((NtsGeometry)shape).GetGeom();
			}
			if (shape is NtsPoint)
			{
				return ((NtsPoint)shape).GetGeom();
			}

			var point = shape as Shapes.Point;
			if (point != null)
			{
				return geometryFactory.CreatePoint(new Coordinate(point.GetX(), point.GetY()));
			}

			var r = shape as Rectangle;
			if (r != null)
			{

				if (r.GetCrossesDateLine())
				{
					var pair = new List<IGeometry>(2)
                   	{
                   		geometryFactory.ToGeometry(new Envelope(
                   		                           	r.GetMinX(), GetWorldBounds().GetMaxX(), r.GetMinY(), r.GetMaxY())),
                   		geometryFactory.ToGeometry(new Envelope(
                   		                           	GetWorldBounds().GetMinX(), r.GetMaxX(), r.GetMinY(), r.GetMaxY()))
                   	};
					return geometryFactory.BuildGeometry(pair);//a MultiPolygon or MultiLineString
				}
				else
				{
					return geometryFactory.ToGeometry(new Envelope(r.GetMinX(), r.GetMaxX(), r.GetMinY(), r.GetMaxY()));
				}
			}

			var circle = shape as Circle;
			if (circle != null)
			{
				// TODO, this should maybe pick a bunch of points
				// and make a circle like:
				//  http://docs.codehaus.org/display/GEOTDOC/01+How+to+Create+a+Geometry#01HowtoCreateaGeometry-CreatingaCircle
				// If this crosses the dateline, it could make two parts
				// is there an existing utility that does this?

				if (circle.GetBoundingBox().GetCrossesDateLine())
					throw new ArgumentException("Doesn't support dateline cross yet: " + circle);//TODO
				var gsf = new GeometricShapeFactory(geometryFactory)
							{
								Size = circle.GetBoundingBox().GetWidth() / 2.0f,
								NumPoints = 4 * 25,//multiple of 4 is best
								Base = new Coordinate(circle.GetCenter().GetX(), circle.GetCenter().GetY())
							};
				return gsf.CreateCircle();
			}
			throw new InvalidShapeException("can't make Geometry from: " + shape);
		}

		public override Shapes.Point MakePoint(double x, double y)
		{
			//A Nts Point is fairly heavyweight!  TODO could/should we optimize this?
			VerifyX(x);
            VerifyY(y);
            return new NtsPoint(geometryFactory.CreatePoint(new Coordinate(x, y)), this);
		}

		public GeometryFactory GetGeometryFactory()
		{
			return geometryFactory;
		}

		public override String ToString()
		{
			if (this.Equals(GEO))
			{
				return GEO.GetType().Name + ".GEO";
			}
			else
			{
				return base.ToString();
			}
		}
	}
}
