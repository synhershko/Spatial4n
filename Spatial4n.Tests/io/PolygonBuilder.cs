using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Nts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spatial4n.Tests.io
{
    /// <summary>
    /// Builder for creating a {@link com.spatial4j.core.shape.Shape} instance of a Polygon
    /// </summary>
    public class PolygonBuilder
    {
        private readonly NtsSpatialContext ctx;
        private readonly List<Coordinate> points = new List<Coordinate>();
        private readonly List<ILinearRing> holes = new List<ILinearRing>();

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
        public virtual Shape Build()
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
            ILinearRing ring = ctx.GetGeometryFactory().CreateLinearRing(points.ToArray(/*new Coordinate[points.Count]*/));
            ILinearRing[] holes = !this.holes.Any() ? null : this.holes.ToArray(/*new LinearRing[this.holes.Count]*/);
            return ctx.GetGeometryFactory().CreatePolygon(ring, holes);
        }

        /**
         * Builder for defining a hole in a {@link com.vividsolutions.jts.geom.Polygon}
         */
        public class PolygonHoleBuilder
        {

            private readonly List<Coordinate> points = new List<Coordinate>();
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
                return polygonBuilder.AddHole(polygonBuilder.ctx.GetGeometryFactory().CreateLinearRing(points.ToArray(/*new Coordinate[points.size()]*/)));
            }
        }
    }
}
