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
using IPoint = Spatial4n.Core.Shapes.IPoint;

namespace Spatial4n.Core.Context.Nts
{
    /// <summary>
    /// Enhances the default <see cref="SpatialContext"/> with support for Polygons (and
    /// other geometry) plus
    /// reading <a href="http://en.wikipedia.org/wiki/Well-known_text">WKT</a>. The
    /// popular <a href="https://sourceforge.net/projects/jts-topo-suite/">JTS</a>
    /// library does the heavy lifting.
    /// </summary>
    public class NtsSpatialContext : SpatialContext
    {
        public new static readonly NtsSpatialContext GEO;

        static NtsSpatialContext()
        {
            NtsSpatialContextFactory factory = new NtsSpatialContextFactory();
            factory.geo = true;
            GEO = new NtsSpatialContext(factory);
        }

        protected readonly GeometryFactory m_geometryFactory;

        protected readonly bool m_allowMultiOverlap;
        protected readonly bool m_useNtsPoint;
        protected readonly bool m_useNtsLineString;

        /// <summary>
        /// Called by <see cref="NtsSpatialContextFactory.NewSpatialContext()"/>.
        /// </summary>
        /// <param name="factory"></param>
        public NtsSpatialContext(NtsSpatialContextFactory factory)
            : base(factory)
        {
            this.m_geometryFactory = factory.GeometryFactory;

            this.m_allowMultiOverlap = factory.allowMultiOverlap;
            this.m_useNtsPoint = factory.useNtsPoint;
            this.m_useNtsLineString = factory.useNtsLineString;
        }

        /// <summary>
        /// If geom might be a multi geometry of some kind, then might multiple
        /// component geometries overlap? Strict OGC says this is invalid but we
        /// can accept it by computing the union. Note: Our ShapeCollection mostly
        /// doesn't care but it has a method related to this
        /// <see cref="Shapes.ShapeCollection.RelateContainsShortCircuits()"/>.
        /// </summary>
        public virtual bool IsAllowMultiOverlap
        {
            get { return m_allowMultiOverlap; }
        }

        ////      protected override ShapeReadWriter MakeShapeReadWriter()
        ////{
        ////	return new NtsShapeReadWriter(this);
        ////}

        public override double NormX(double x)
        {
            x = base.NormX(x);
            return m_geometryFactory.PrecisionModel.MakePrecise(x);
        }

        public override double NormY(double y)
        {
            y = base.NormY(y);
            return m_geometryFactory.PrecisionModel.MakePrecise(y);
        }

        public override string ToString(IShape shape)
        {
            //Note: this logic is from the defunct NtsShapeReadWriter
            if (shape is NtsGeometry)
            {
                NtsGeometry ntsGeom = (NtsGeometry)shape;
                return ntsGeom.Geometry.AsText();
            }
            //Note: doesn't handle ShapeCollection or BufferedLineString
            return base.ToString(shape);
        }

        /// <summary>
        /// Gets a NTS <see cref="Geometry"/> for the given <see cref="IShape"/>. Some shapes hold a
        /// NTS geometry whereas new ones must be created for the rest.
        /// </summary>
        /// <param name="shape">Not null</param>
        /// <returns>Not null</returns>
        public virtual IGeometry GetGeometryFrom(IShape shape)
        {
            if (shape is NtsGeometry)
            {
                return ((NtsGeometry)shape).Geometry;
            }
            if (shape is NtsPoint)
            {
                return ((NtsPoint)shape).Geometry;
            }

            var point = shape as Shapes.IPoint;
            if (point != null)
            {
                return m_geometryFactory.CreatePoint(new Coordinate(point.X, point.Y));
            }

            var r = shape as IRectangle;
            if (r != null)
            {

                if (r.CrossesDateLine)
                {
                    var pair = new List<IGeometry>(2)
                       {
                           m_geometryFactory.ToGeometry(new Envelope(
                                                          r.MinX, WorldBounds.MaxX, r.MinY, r.MaxY)),
                           m_geometryFactory.ToGeometry(new Envelope(
                                                          WorldBounds.MinX, r.MaxX, r.MinY, r.MaxY))
                       };
                    return m_geometryFactory.BuildGeometry(pair);//a MultiPolygon or MultiLineString
                }
                else
                {
                    return m_geometryFactory.ToGeometry(new Envelope(r.MinX, r.MaxX, r.MinY, r.MaxY));
                }
            }

            var circle = shape as ICircle;
            if (circle != null)
            {
                // TODO, this should maybe pick a bunch of points
                // and make a circle like:
                //  http://docs.codehaus.org/display/GEOTDOC/01+How+to+Create+a+Geometry#01HowtoCreateaGeometry-CreatingaCircle
                // If this crosses the dateline, it could make two parts
                // is there an existing utility that does this?

                if (circle.BoundingBox.CrossesDateLine)
                    throw new ArgumentException("Doesn't support dateline cross yet: " + circle);//TODO
                var gsf = new GeometricShapeFactory(m_geometryFactory)
                {
                    Size = circle.BoundingBox.Width / 2.0f,
                    NumPoints = 4 * 25,//multiple of 4 is best
                    Centre = new Coordinate(circle.Center.X, circle.Center.Y)
                };
                return gsf.CreateCircle();
            }
            throw new InvalidShapeException("can't make Geometry from: " + shape);
        }

        // Should {@link #makePoint(double, double)} return {@link NtsPoint}?
        public virtual bool UseNtsPoint
        {
            get { return m_useNtsPoint; }
        }

        public override IPoint MakePoint(double x, double y)
        {
            if (!UseNtsPoint)
                return base.MakePoint(x, y);
            //A Nts Point is fairly heavyweight!  TODO could/should we optimize this?
            VerifyX(x);
            VerifyY(y);
            Coordinate coord = double.IsNaN(x) ? null : new Coordinate(x, y);
            return new NtsPoint(m_geometryFactory.CreatePoint(coord), this);
        }

        /** Should {@link #makeLineString(java.util.List)} return {@link NtsGeometry}? */
        public virtual bool UseNtsLineString
        {
            get
            {
                //BufferedLineString doesn't yet do dateline cross, and can't yet be relate()'ed with a
                // NTS geometry
                return m_useNtsLineString;
            }
        }

        public override IShape MakeLineString(IList<IPoint> points)
        {
            if (!m_useNtsLineString)
                return base.MakeLineString(points);
            //convert List<Point> to Coordinate[]
            Coordinate[] coords = new Coordinate[points.Count];
            for (int i = 0; i < coords.Length; i++)
            {
                Shapes.IPoint p = points[i];
                if (p is NtsPoint)
                {
                    NtsPoint ntsPoint = (NtsPoint)p;
                    coords[i] = ntsPoint.Geometry.Coordinate;
                }
                else
                {
                    coords[i] = new Coordinate(p.X, p.Y);
                }
            }
            ILineString lineString = m_geometryFactory.CreateLineString(coords);
            return MakeShape(lineString);
        }

        /**
         * INTERNAL
         * @see #makeShape(com.vividsolutions.jts.geom.Geometry)
         *
         * @param geom Non-null
         * @param dateline180Check if both this is true and {@link #isGeo()}, then NtsGeometry will check
         *                         for adjacent coordinates greater than 180 degrees longitude apart, and
         *                         it will do tricks to make that line segment (and the shape as a whole)
         *                         cross the dateline even though NTS doesn't have geodetic support.
         * @param allowMultiOverlap See {@link #isAllowMultiOverlap()}.
         */
        public virtual NtsGeometry MakeShape(IGeometry geom, bool dateline180Check, bool allowMultiOverlap)
        {
            return new NtsGeometry(geom, this, dateline180Check, allowMultiOverlap);
        }

        /**
         * INTERNAL: Creates a {@link Shape} from a NTS {@link Geometry}. Generally, this shouldn't be
         * called when one of the other factory methods are available, such as for points. The caller
         * needs to have done some verification/normalization of the coordinates by now, if any.
         */
        public virtual NtsGeometry MakeShape(IGeometry geom)
        {
            return MakeShape(geom, true/*dateline180Check*/, m_allowMultiOverlap);
        }

        public virtual GeometryFactory GeometryFactory
        {
            get { return m_geometryFactory; }
        }

        public override string ToString()
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
