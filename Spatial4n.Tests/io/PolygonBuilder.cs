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

#if FEATURE_NTS

using GeoAPI.Geometries;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Nts;
using System.Collections.Generic;
using System.Linq;

namespace Spatial4n.Core.IO
{
    /// <summary>
    /// Builder for creating a {@link com.spatial4j.core.shape.Shape} instance of a Polygon
    /// </summary>
    public class PolygonBuilder
    {
        private readonly NtsSpatialContext ctx;
        private readonly IList<Coordinate> points = new List<Coordinate>();
        private readonly IList<ILinearRing> holes = new List<ILinearRing>();

        public PolygonBuilder(NtsSpatialContext ctx)
        {
            this.ctx = ctx;
        }

        /**
         * Adds a point to the Polygon
         *
         * @param lon Longitude of the point
         * @param lat Latitude of the point
         * @return this
         */
        public virtual PolygonBuilder Point(double lon, double lat)
        {
            points.Add(new Coordinate(lon, lat));
            return this;
        }

        /**
         * Starts a new hole in the Polygon
         *
         * @return PolygonHoleBuilder to create the new hole
         */
        public virtual PolygonHoleBuilder NewHole()
        {
            return new PolygonHoleBuilder(this);
        }

        /**
         * Registers the LinearRing representing a hole
         *
         * @param linearRing Hole to register
         * @return this
         */
        private PolygonBuilder AddHole(ILinearRing linearRing)
        {
            holes.Add(linearRing);
            return this;
        }

        /**
         * Builds a {@link com.spatial4j.core.shape.Shape} instance representing the polygon
         *
         * @return Built polygon
         */
        public virtual IShape Build()
        {
            return new NtsGeometry(ToPolygon(), ctx, true, true);
        }

        /**
         * Creates the raw {@link com.vividsolutions.jts.geom.Polygon}
         *
         * @return Built polygon
         */
        public virtual IPolygon ToPolygon()
        {
            ILinearRing ring = ctx.GeometryFactory.CreateLinearRing(points.ToArray(/*new Coordinate[points.Count]*/));
            ILinearRing[] holes = !this.holes.Any() ? null : this.holes.ToArray(/*new LinearRing[this.holes.Count]*/);
            return ctx.GeometryFactory.CreatePolygon(ring, holes);
        }

        /**
         * Builder for defining a hole in a {@link com.vividsolutions.jts.geom.Polygon}
         */
        public class PolygonHoleBuilder
        {

            private readonly IList<Coordinate> points = new List<Coordinate>();
            private readonly PolygonBuilder polygonBuilder;

            /**
             * Creates a new PolygonHoleBuilder
             *
             * @param polygonBuilder PolygonBuilder that the hole built by this builder
             *                       will be added to
             */
            internal PolygonHoleBuilder(PolygonBuilder polygonBuilder)
            {
                this.polygonBuilder = polygonBuilder;
            }

            /**
             * Adds a point to the LinearRing
             *
             * @param lon Longitude of the point
             * @param lat Latitude of the point
             * @return this
             */
            public virtual PolygonHoleBuilder Point(double lon, double lat)
            {
                points.Add(new Coordinate(lon, lat));
                return this;
            }

            /**
             * Ends the building of the hole
             *
             * @return PolygonBuilder to use to build the remainder of the Polygon.
             */
            public virtual PolygonBuilder EndHole()
            {
                return polygonBuilder.AddHole(polygonBuilder.ctx.GeometryFactory.CreateLinearRing(points.ToArray(/*new Coordinate[points.size()]*/)));
            }
        }
    }
}

#endif